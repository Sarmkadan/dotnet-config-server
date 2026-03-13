#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Caching;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;

namespace DotnetConfigServer.Events;

/// <summary>
/// Event handlers that respond to domain events.
/// These handlers perform side effects like cache invalidation and notifications.
/// </summary>
sealed public class ConfigurationEventHandlers
{
    private readonly ICacheService _cache;
    private readonly IWebhookService _webhookService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConfigurationEventHandlers> _logger;

    public ConfigurationEventHandlers(
        ICacheService cache,
        IWebhookService webhookService,
        INotificationService notificationService,
        ILogger<ConfigurationEventHandlers> logger)
    {
        _cache = cache;
        _webhookService = webhookService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles configuration created event.
    /// </summary>
    public async Task HandleConfigurationCreatedAsync(ConfigurationCreatedEvent @event)
    {
        _logger.LogInformation("Handling ConfigurationCreated event for {ConfigId}", @event.ConfigurationId);

        // Invalidate cache
        var cacheKeys = new[]
        {
            Caching.CacheKeyGenerator.GetApplicationConfigurationsKey(@event.ApplicationId),
            Caching.CacheKeyGenerator.GetAllApplicationsKey()
        };

        foreach (var key in cacheKeys)
        {
            await _cache.RemoveAsync(key);
        }

        // Send webhook notifications
        await _webhookService.NotifyAsync("configuration.created", @event);

        // Send internal notifications
        await _notificationService.NotifyAsync(new Notification
        {
            Type = "configuration.created",
            Message = $"Configuration '{@event.ConfigurationName}' created in environment '{@event.Environment}'",
            Severity = "info",
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles configuration updated event.
    /// </summary>
    public async Task HandleConfigurationUpdatedAsync(ConfigurationUpdatedEvent @event)
    {
        _logger.LogInformation("Handling ConfigurationUpdated event for {ConfigId}", @event.ConfigurationId);

        // Invalidate relevant caches
        var cacheKeys = Caching.CacheKeyGenerator.GetInvalidationPatternsForConfiguration(
            @event.ConfigurationId,
            @event.ApplicationId
        ).ToList();

        foreach (var key in cacheKeys)
        {
            await _cache.RemoveAsync(key);
        }

        // Send webhook notifications
        await _webhookService.NotifyAsync("configuration.updated", @event);

        // Send notification
        await _notificationService.NotifyAsync(new Notification
        {
            Type = "configuration.updated",
            Message = $"Configuration '{@event.ConfigurationName}' was updated",
            Severity = "info",
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles configuration key changed event.
    /// </summary>
    public async Task HandleConfigurationKeyChangedAsync(ConfigurationKeyChangedEvent @event)
    {
        _logger.LogInformation("Handling ConfigurationKeyChanged event for key {Key} in config {ConfigId}", @event.Key, @event.ConfigurationId);

        // Invalidate cache for this configuration's keys
        await _cache.RemoveAsync(Caching.CacheKeyGenerator.GetConfigurationKeysKey(@event.ConfigurationId));
        await _cache.RemoveAsync(Caching.CacheKeyGenerator.GetConfigurationKeyKey(@event.KeyId));

        // Send webhook notification
        await _webhookService.NotifyAsync("configuration.key.changed", @event);

        // Log as high severity if sensitive key changed
        var severity = @event.IsEncrypted ? "warning" : "info";

        await _notificationService.NotifyAsync(new Notification
        {
            Type = "configuration.key.changed",
            Message = $"Configuration key '{@event.Key}' was changed",
            Severity = severity,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Handles configuration deleted event.
    /// </summary>
    public async Task HandleConfigurationDeletedAsync(ConfigurationDeletedEvent @event)
    {
        _logger.LogInformation("Handling ConfigurationDeleted event for {ConfigId}", @event.ConfigurationId);

        // Invalidate all related caches
        var cacheKeys = Caching.CacheKeyGenerator.GetInvalidationPatternsForConfiguration(
            @event.ConfigurationId,
            @event.ApplicationId
        ).ToList();

        foreach (var key in cacheKeys)
        {
            await _cache.RemoveAsync(key);
        }

        // Send webhook notification
        await _webhookService.NotifyAsync("configuration.deleted", @event);

        // Send notification
        await _notificationService.NotifyAsync(new Notification
        {
            Type = "configuration.deleted",
            Message = $"Configuration '{@event.ConfigurationName}' was deleted",
            Severity = "warning",
            CreatedAt = DateTime.UtcNow
        });
    }
}
