// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotnetConfigServer.Caching;

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

public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ICacheService? _cacheService;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(ILogger<HealthCheckService> logger, ICacheService? cacheService = null)
    {
        _logger = logger;
        _cacheService = cacheService;
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
        if (_cacheService != null)
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

public class HealthReport
{
    public string Status { get; set; } = "healthy"; // healthy, degraded, unhealthy
    public DateTime Timestamp { get; set; }
    public double Uptime { get; set; }
    public int ProcessId { get; set; }
    public long MemoryMb { get; set; }
    public int ThreadCount { get; set; }
    public Dictionary<string, HealthCheck> Checks { get; set; } = new();
}

public class HealthCheck
{
    public string Status { get; set; } = string.Empty; // healthy, warning, unhealthy
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
}
