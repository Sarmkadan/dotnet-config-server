#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Liveness health check confirming the process is running and able to
/// respond to requests. Performs no external dependency calls, so it can
/// never be affected by storage or network outages.
/// </summary>
public sealed class ProcessLivenessHealthCheck : IHealthCheck
{
    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(HealthCheckResult.Healthy("Process is alive."));
    }
}

/// <summary>
/// Readiness health check confirming the configuration store is reachable.
/// </summary>
public sealed class ConfigStoreReadinessHealthCheck : IHealthCheck
{
    private readonly IConfigStore _configStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigStoreReadinessHealthCheck"/> class.
    /// </summary>
    /// <param name="configStore">The configuration store to probe.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configStore"/> is <see langword="null"/>.</exception>
    public ConfigStoreReadinessHealthCheck(IConfigStore configStore)
    {
        ArgumentNullException.ThrowIfNull(configStore);

        _configStore = configStore;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var reachable = await _configStore.IsReachableAsync(cancellationToken);
            return reachable
                ? HealthCheckResult.Healthy("Configuration store is reachable.")
                : HealthCheckResult.Unhealthy("Configuration store did not respond.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Configuration store health probe failed.", ex);
        }
    }
}

/// <summary>
/// Readiness health check confirming that at least one encryption key can be
/// loaded from the key store, which is a prerequisite for serving encrypted
/// configuration values.
/// </summary>
public sealed class EncryptionKeyReadinessHealthCheck : IHealthCheck
{
    private readonly IEncryptionKeyRepository _encryptionKeyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionKeyReadinessHealthCheck"/> class.
    /// </summary>
    /// <param name="encryptionKeyRepository">The encryption key repository to probe.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionKeyRepository"/> is <see langword="null"/>.</exception>
    public EncryptionKeyReadinessHealthCheck(IEncryptionKeyRepository encryptionKeyRepository)
    {
        ArgumentNullException.ThrowIfNull(encryptionKeyRepository);

        _encryptionKeyRepository = encryptionKeyRepository;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var keys = await _encryptionKeyRepository.GetAllAsync();
            return keys.Count > 0
                ? HealthCheckResult.Healthy($"{keys.Count} encryption key(s) loadable from store.")
                : HealthCheckResult.Degraded("No encryption keys are currently available in the store.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Encryption key store probe failed.", ex);
        }
    }
}
