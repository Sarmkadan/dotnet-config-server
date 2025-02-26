// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

namespace DotnetConfigServer.BackgroundWorkers;

/// <summary>
/// Background worker that periodically syncs configuration state.
/// Cleans up stale entries, archives old versions, and optimizes cache.
/// </summary>
public class ConfigurationSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationSyncWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public ConfigurationSyncWorker(IServiceProvider serviceProvider, ILogger<ConfigurationSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Configuration sync worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncConfigurationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during configuration sync");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Configuration sync worker stopped");
    }

    private async Task SyncConfigurationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var configRepository = scope.ServiceProvider.GetRequiredService<IConfigurationRepository>();
        var versionRepository = scope.ServiceProvider.GetRequiredService<IConfigurationVersionRepository>();

        try
        {
            // Archive old versions (older than 30 days)
            var archiveDate = DateTime.UtcNow.AddDays(-30);
            var oldVersions = await versionRepository.GetOlderThanAsync(archiveDate);

            var archivedCount = 0;
            foreach (var version in oldVersions)
            {
                version.IsArchived = true;
                await versionRepository.UpdateAsync(version);
                archivedCount++;
            }

            if (archivedCount > 0)
            {
                await versionRepository.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} old configuration versions", archivedCount);
            }

            // Clean up soft-deleted configurations older than 90 days
            var deleteDate = DateTime.UtcNow.AddDays(-90);
            var deletedConfigs = await configRepository.GetDeletedBeforeAsync(deleteDate);

            var permanentDeleteCount = 0;
            foreach (var config in deletedConfigs)
            {
                await configRepository.DeleteAsync(config);
                permanentDeleteCount++;
            }

            if (permanentDeleteCount > 0)
            {
                await configRepository.SaveChangesAsync();
                _logger.LogInformation("Permanently deleted {Count} old configurations", permanentDeleteCount);
            }

            _logger.LogInformation("Configuration sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing configurations");
            throw;
        }
    }
}

/// <summary>
/// Extension interface for repository to support sync operations.
/// </summary>
public interface IConfigurationVersionRepository : IRepository<ConfigurationVersion>
{
    Task<List<ConfigurationVersion>> GetOlderThanAsync(DateTime date);
}

public interface IConfigurationRepository : IRepository<Configuration>
{
    Task<List<Configuration>> GetDeletedBeforeAsync(DateTime date);
}
