#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotnetConfigServer.Caching;
using DotnetConfigServer.Data;
using DotnetConfigServer.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for checking application health and system status.
/// Provides detailed diagnostics for monitoring and debugging.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Gets a comprehensive health report.
    /// </summary>
    Task<HealthReport> GetHealthReportAsync();

    /// <summary>
    /// Checks if the application is ready to serve requests.
    /// </summary>
    Task<bool> IsReadyAsync();

    /// <summary>
    /// Checks if the application is alive.
    /// </summary>
    Task<bool> IsAliveAsync();
}

public sealed class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ICacheService? _cacheService;
    private readonly ApplicationDbContext? _dbContext;
    private readonly IWebhookDeliveryRepository? _webhookDeliveryRepository;
    private readonly IConfigurationSnapshotRepository? _snapshotRepository;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        ICacheService? cacheService = null,
        ApplicationDbContext? dbContext = null,
        IWebhookDeliveryRepository? webhookDeliveryRepository = null,
        IConfigurationSnapshotRepository? snapshotRepository = null)
    {
        _logger = logger;
        _cacheService = cacheService;
        _dbContext = dbContext;
        _webhookDeliveryRepository = webhookDeliveryRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<HealthReport> GetHealthReportAsync()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - _startTime;

        var report = new HealthReport
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Uptime = uptime.TotalSeconds,
            ProcessId = process.Id,
            MemoryMb = process.WorkingSet64 / (1024 * 1024),
            ThreadCount = process.Threads.Count,
            Checks = new()
        };

        // Cache health
        if (_cacheService is not null)
        {
            try
            {
                var stats = await _cacheService.GetStatsAsync();
                report.Checks.Add("cache", new HealthCheck
                {
                    Status = "healthy",
                    Message = $"Cache operational. Hit rate: {stats.HitRate:P2}",
                    Metrics = new Dictionary<string, object>
                    {
                        { "hitRate", stats.HitRate },
                        { "hits", stats.Hits },
                        { "misses", stats.Misses },
                        { "size", stats.Size }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache health check failed");
                report.Checks.Add("cache", new HealthCheck
                {
                    Status = "unhealthy",
                    Message = ex.Message
                });
                report.Status = "degraded";
            }
        }

        // Database health
        if (_dbContext is not null)
        {
            try
            {
                // Test database connectivity by executing a simple query
                var canConnect = await _dbContext.Database.CanConnectAsync();

                if (canConnect)
                {
                    // Get database size and performance metrics
                    var databaseSize = await _dbContext.Database.SqlQueryRaw<long>($"SELECT SUM(size) FROM sys.master_files WHERE database_id = DB_ID() AND type_desc = 'ROWS'").ToListAsync();
                    var dbSizeMb = databaseSize.FirstOrDefault() / 1024 / 1024;

                    report.Checks.Add("database", new HealthCheck
                    {
                        Status = "healthy",
                        Message = "Database is reachable",
                        Metrics = new Dictionary<string, object>
                        {
                            { "connected", true },
                            { "sizeMb", dbSizeMb }
                        }
                    });
                }
                else
                {
                    report.Checks.Add("database", new HealthCheck
                    {
                        Status = "unhealthy",
                        Message = "Database connection failed"
                    });
                    report.Status = "degraded";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                report.Checks.Add("database", new HealthCheck
                {
                    Status = "unhealthy",
                    Message = ex.Message
                });
                report.Status = "degraded";
            }
        }
        else
        {
            report.Checks.Add("database", new HealthCheck
            {
                Status = "unhealthy",
                Message = "Database context not available"
            });
            report.Status = "degraded";
        }

        // Webhook delivery health
        if (_webhookDeliveryRepository is not null)
        {
            try
            {
                var pendingCount = await _webhookDeliveryRepository.GetPendingDeliveriesAsync();
                var failedCount = await _webhookDeliveryRepository.GetFailedDeliveriesAsync();

                report.Checks.Add("webhooks", new HealthCheck
                {
                    Status = pendingCount.Count > 100 ? "warning" : "healthy",
                    Message = $"Webhook delivery system operational. Pending: {pendingCount.Count}, Failed: {failedCount.Count}",
                    Metrics = new Dictionary<string, object>
                    {
                        { "pendingCount", pendingCount.Count },
                        { "failedCount", failedCount.Count },
                        { "totalDeliveries", pendingCount.Count + failedCount.Count }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook delivery health check failed");
                report.Checks.Add("webhooks", new HealthCheck
                {
                    Status = "unhealthy",
                    Message = ex.Message
                });
                report.Status = "degraded";
            }
        }

        // Configuration snapshot health
        if (_snapshotRepository is not null)
        {
            try
            {
                var latestSnapshot = await _snapshotRepository.GetLatestSnapshotAsync(Guid.Empty);
                var snapshotAgeHours = latestSnapshot?.CreatedAt != null
                    ? (DateTime.UtcNow - latestSnapshot.CreatedAt).TotalHours
                    : (double?)null;

                if (latestSnapshot is not null && snapshotAgeHours.HasValue)
                {
                    var status = snapshotAgeHours > 24 ? "warning" : "healthy";
                    var message = snapshotAgeHours > 24
                        ? $"Latest snapshot is {snapshotAgeHours:F1} hours old"
                        : "Configuration snapshots are current";

                    report.Checks.Add("snapshots", new HealthCheck
                    {
                        Status = status,
                        Message = message,
                        Metrics = new Dictionary<string, object>
                        {
                            { "lastSnapshotAgeHours", snapshotAgeHours },
                            { "lastSnapshotId", latestSnapshot.Id },
                            { "lastSnapshotCreatedAt", latestSnapshot.CreatedAt }
                        }
                    });
                }
                else
                {
                    report.Checks.Add("snapshots", new HealthCheck
                    {
                        Status = "warning",
                        Message = "No snapshots found or repository not available"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration snapshot health check failed");
                report.Checks.Add("snapshots", new HealthCheck
                {
                    Status = "unhealthy",
                    Message = ex.Message
                });
                report.Status = "degraded";
            }
        }

        // Memory check
        var totalMemory = GC.GetTotalMemory(false);
        var availableMemory = GC.GetGCMemoryInfo().HeapSizeBytes;

        if (totalMemory > availableMemory * 0.9)
        {
            report.Checks.Add("memory", new HealthCheck
            {
                Status = "warning",
                Message = "Memory usage is high"
            });
            report.Status = "degraded";
        }
        else
        {
            report.Checks.Add("memory", new HealthCheck
            {
                Status = "healthy",
                Message = "Memory usage is normal"
            });
        }

        return report;
    }

    public async Task<bool> IsReadyAsync()
    {
        try
        {
            var report = await GetHealthReportAsync();
            return report.Status != "unhealthy";
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsAliveAsync()
    {
        return await Task.FromResult(true);
    }
}

public sealed class HealthReport
{
    public string Status { get; set; } = "healthy"; // healthy, degraded, unhealthy
    public DateTime Timestamp { get; set; }
    public double Uptime { get; set; }
    public int ProcessId { get; set; }
    public long MemoryMb { get; set; }
    public int ThreadCount { get; set; }
    public Dictionary<string, HealthCheck> Checks { get; set; } = new();
}

public sealed class HealthCheck
{
    public string Status { get; set; } = string.Empty; // healthy, warning, unhealthy
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
}
