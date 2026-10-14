#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
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
/// Diagnostic health check covering the configuration root, the change watcher and
/// webhook delivery. The config root being unreachable is <see cref="HealthStatus.Unhealthy"/>;
/// a stopped watcher or a failed last webhook delivery is <see cref="HealthStatus.Degraded"/>.
/// Results include the watcher state, the last reload time and the last delivery status as data.
/// </summary>
public sealed class ConfigServerHealthCheck : IHealthCheck
{
    private readonly IConfigStore _configStore;
    private readonly ConfigWatcherStatus _watcherStatus;
    private readonly IWebhookDeliveryRepository _deliveryRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerHealthCheck"/> class.
    /// </summary>
    /// <param name="configStore">The configuration store that acts as the config root.</param>
    /// <param name="watcherStatus">The state of the in-process change watcher.</param>
    /// <param name="deliveryRepository">The webhook delivery repository used to read the last delivery.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is <see langword="null"/>.</exception>
    public ConfigServerHealthCheck(
        IConfigStore configStore,
        ConfigWatcherStatus watcherStatus,
        IWebhookDeliveryRepository deliveryRepository)
    {
        ArgumentNullException.ThrowIfNull(configStore);
        ArgumentNullException.ThrowIfNull(watcherStatus);
        ArgumentNullException.ThrowIfNull(deliveryRepository);

        _configStore = configStore;
        _watcherStatus = watcherStatus;
        _deliveryRepository = deliveryRepository;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = new Dictionary<string, object>();

        bool reachable;
        try
        {
            reachable = await _configStore.IsReachableAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Configuration root probe failed.", ex, data);
        }

        data["configRootReachable"] = reachable;
        if (!reachable)
        {
            return HealthCheckResult.Unhealthy("Configuration root is not reachable.", data: data);
        }

        var issues = new List<string>();

        data["watcherRunning"] = _watcherStatus.IsRunning;
        data["lastReloadUtc"] = _watcherStatus.LastReloadUtc?.ToString("O") ?? "never";
        if (!_watcherStatus.IsRunning)
        {
            issues.Add("Configuration watcher is not running.");
        }

        try
        {
            var lastDelivery = await _deliveryRepository.GetMostRecentAsync();
            if (lastDelivery is null)
            {
                data["lastWebhookDeliveryStatus"] = "none";
            }
            else
            {
                data["lastWebhookDeliveryStatus"] = lastDelivery.Status.ToString();
                data["lastWebhookDeliveryStatusCode"] = lastDelivery.StatusCode;
                data["lastWebhookDeliveryUtc"] = (lastDelivery.SentAt ?? lastDelivery.CreatedAt).ToString("O");

                if (lastDelivery.Status == WebhookDeliveryStatus.Failed)
                {
                    issues.Add("Last webhook delivery failed.");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Webhook delivery probe failed.", ex, data);
        }

        return issues.Count == 0
            ? HealthCheckResult.Healthy("Configuration root is reachable, watcher is running and the last webhook delivery succeeded.", data)
            : HealthCheckResult.Degraded(string.Join(" ", issues), data: data);
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
