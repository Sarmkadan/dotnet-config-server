#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.BackgroundWorkers;

/// <summary>
/// Background worker that periodically creates snapshots of all configurations
/// </summary>
public sealed class ConfigurationSnapshotWorker : BackgroundService
{
  private readonly ILogger<ConfigurationSnapshotWorker> _logger;
  private readonly IConfigurationSnapshotService _snapshotService;
  private readonly IConfigurationRepository _configurationRepository;
  private readonly IConfigurationSnapshotRepository _snapshotRepository;
  private readonly ConfigurationSnapshotOptions _options;

  public ConfigurationSnapshotWorker(
    ILogger<ConfigurationSnapshotWorker> logger,
    IConfigurationSnapshotService snapshotService,
    IConfigurationRepository configurationRepository,
    IConfigurationSnapshotRepository snapshotRepository,
    IOptions<ConfigurationSnapshotOptions> options)
  {
    _logger = logger;
    _snapshotService = snapshotService;
    _configurationRepository = configurationRepository;
    _snapshotRepository = snapshotRepository;
    _options = options.Value;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("Configuration Snapshot Worker started with interval: {IntervalMinutes} minutes", _options.IntervalMinutes);

    // Initial delay to allow application startup
    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var nextRun = DateTime.UtcNow.AddMinutes(_options.IntervalMinutes);
        _logger.LogInformation("Next snapshot run scheduled for {NextRun}", nextRun);

        await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);

        if (stoppingToken.IsCancellationRequested)
          break;

        await CreateScheduledSnapshotsAsync(stoppingToken);
      }
      catch (OperationCanceledException)
      {
        _logger.LogInformation("Configuration Snapshot Worker is stopping...");
        break;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in Configuration Snapshot Worker");
        // Continue running after error
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
      }
    }

    _logger.LogInformation("Configuration Snapshot Worker stopped");
  }

  private async Task CreateScheduledSnapshotsAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Starting scheduled snapshot creation...");

    var configurations = await _configurationRepository.GetAllAsync();
    var snapshotCount = 0;
    var skippedCount = 0;

    foreach (var config in configurations)
    {
      if (cancellationToken.IsCancellationRequested)
        break;

      try
      {
        if (_options.SkipIfUnchanged)
        {
          var latestSnapshot = await _snapshotRepository.GetLatestSnapshotAsync(config.Id);
          if (latestSnapshot != null)
          {
            // Check if configuration has changed since last snapshot
            var hasChanges = await HasConfigurationChangedSinceAsync(config.Id, latestSnapshot.CreatedAt);
            if (!hasChanges)
            {
              _logger.LogDebug("Skipping snapshot for configuration {ConfigId} - no changes detected", config.Id);
              skippedCount++;
              continue;
            }
          }
        }

        var snapshot = await _snapshotService.CreateSnapshotAsync(
          config.Id,
          _options.DefaultUserId,
          _options.DefaultReason
        );

        snapshotCount++;
        _logger.LogInformation("Created scheduled snapshot {SnapshotId} for configuration {ConfigId}",
          snapshot.Id, config.Id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to create snapshot for configuration {ConfigId}", config.Id);
      }
    }

    _logger.LogInformation("Scheduled snapshot creation completed. Created: {SnapshotCount}, Skipped: {SkippedCount}",
      snapshotCount, skippedCount);
  }

  private async Task<bool> HasConfigurationChangedSinceAsync(Guid configurationId, DateTime cutoffDate)
  {
    // Check if configuration itself has been modified
    var config = await _configurationRepository.GetByIdAsync(configurationId);
    if (config == null)
      return false;

    // Check if config was modified after cutoff
    if (config.UpdatedAt > cutoffDate)
      return true;

    // Check if any configuration keys have been modified since the cutoff date
    var dbContext = _configurationRepository.GetType().GetProperty("Context")?.GetValue(_configurationRepository) as Microsoft.EntityFrameworkCore.DbContext;
    if (dbContext != null)
    {
      var modifiedKeys = await dbContext.Set<ConfigurationKey>()
        .Where(k => k.ConfigurationId == configurationId && k.UpdatedAt > cutoffDate)
        .AnyAsync();

      if (modifiedKeys)
        return true;
    }

    return false;
  }
}
