#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.BackgroundWorkers;

/// <summary>
/// Background worker that periodically migrates stored configuration secrets from
/// older encryption key versions onto the current active key of each configuration,
/// completing the rotation flow started by <see cref="IEncryptionService.RotateKeyAsync"/>.
/// </summary>
public sealed class EncryptionKeyRotationWorker : BackgroundService
{
    private readonly ILogger<EncryptionKeyRotationWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EncryptionKeyRotationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionKeyRotationWorker"/> class.
    /// </summary>
    /// <param name="logger">Logger used to report sweep progress and failures.</param>
    /// <param name="scopeFactory">Factory used to create a scoped service provider per sweep.</param>
    /// <param name="options">Options controlling whether and how often the sweep runs.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/>, <paramref name="scopeFactory"/>, or <paramref name="options"/> is null.</exception>
    public EncryptionKeyRotationWorker(
        ILogger<EncryptionKeyRotationWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<EncryptionKeyRotationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    /// <summary>
    /// Runs the periodic re-encryption sweep for as long as the host is running.
    /// </summary>
    /// <param name="stoppingToken">Signals that the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Encryption Key Rotation Worker is disabled");
            return;
        }

        _logger.LogInformation("Encryption Key Rotation Worker started with interval: {IntervalMinutes} minutes", _options.IntervalMinutes);

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSweepAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Encryption Key Rotation Worker is stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Encryption Key Rotation Worker");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Encryption Key Rotation Worker stopped");
    }

    /// <summary>
    /// Scans every configuration for encrypted values that are still tagged with an
    /// older ciphertext version than <see cref="AppConstants.Encryption.CurrentCiphertextVersion"/>
    /// and re-encrypts them under the configuration's current primary key.
    /// </summary>
    /// <param name="cancellationToken">Signals that the sweep should stop early.</param>
    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configurationRepository = scope.ServiceProvider.GetRequiredService<IConfigurationRepository>();
        var keyRepository = scope.ServiceProvider.GetRequiredService<IConfigurationKeyRepository>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var configurations = await configurationRepository.GetAllAsync();
        var migratedCount = 0;
        var failedCount = 0;

        foreach (var configuration in configurations)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var keys = await keyRepository.GetByConfigurationAsync(configuration.Id);
            var staleKeys = keys
                .Where(k => k.IsEncrypted && encryptionService.GetCiphertextVersion(k.Value) != AppConstants.Encryption.CurrentCiphertextVersion)
                .ToList();

            if (staleKeys.Count == 0)
                continue;

            try
            {
                await encryptionService.ReEncryptConfigurationAsync(configuration.Id, staleKeys, _options.DefaultUserId);

                foreach (var key in staleKeys)
                    await keyRepository.UpdateAsync(key);

                migratedCount += staleKeys.Count;
                _logger.LogInformation(
                    "Migrated {Count} configuration key(s) to the current encryption key version for configuration {ConfigId}",
                    staleKeys.Count, configuration.Id);
            }
            catch (Exception ex)
            {
                failedCount += staleKeys.Count;
                _logger.LogError(ex, "Failed to migrate configuration keys to the current encryption key version for configuration {ConfigId}", configuration.Id);
            }
        }

        _logger.LogInformation("Encryption key rotation sweep completed. Migrated: {MigratedCount}, Failed: {FailedCount}", migratedCount, failedCount);
    }
}
