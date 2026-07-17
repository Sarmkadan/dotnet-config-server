#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;
using DotnetConfigServer.Caching;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;

namespace DotnetConfigServer.Events;

/// <summary>
/// Extension methods for <see cref="ConfigurationEventHandlers"/> that provide convenience methods
/// for common event handling scenarios.
/// </summary>
public static class ConfigurationEventHandlersExtensions
{
    /// <summary>
    /// Handles configuration created event with additional metadata.
    /// </summary>
    /// <param name="handlers">The event handlers instance.</param>
    /// <param name="@event">The configuration created event.</param>
    /// <param name="metadata">Optional metadata to include in notifications.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers or event is null.</exception>
    public static async Task HandleConfigurationCreatedAsync(
        this ConfigurationEventHandlers handlers,
        ConfigurationCreatedEvent @event,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(@event);

        // Invalidate cache
        var cacheKeys = new[]
        {
            CacheKeyGenerator.GetApplicationConfigurationsKey(@event.ApplicationId),
            CacheKeyGenerator.GetAllApplicationsKey()
        };

        foreach (var key in cacheKeys)
        {
            await handlers.GetCacheService().RemoveAsync(key);
        }

        // Send webhook notifications
        await handlers.GetWebhookService().NotifyAsync("configuration.created", @event);

        // Send internal notifications with metadata
        await handlers.GetNotificationService().NotifyAsync(new Notification
        {
            Type = "configuration.created",
            Message = $"Configuration '{@event.ConfigurationName}' created in environment '{@event.Environment}'",
            Severity = "info",
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata is null
                ? new Dictionary<string, object>()
                : metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        });
    }

    /// <summary>
    /// Handles configuration updated event with change tracking.
    /// </summary>
    /// <param name="handlers">The event handlers instance.</param>
    /// <param name="@event">The configuration updated event.</param>
    /// <param name="changedKeys">List of keys that were changed.</param>
    /// <param name="metadata">Optional metadata to include in notifications.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers or event is null.</exception>
    public static async Task HandleConfigurationUpdatedAsync(
        this ConfigurationEventHandlers handlers,
        ConfigurationUpdatedEvent @event,
        IEnumerable<string>? changedKeys = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(@event);

        // Invalidate relevant caches
        var cacheKeys = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(
            @event.ConfigurationId,
            @event.ApplicationId
        ).ToList();

        foreach (var key in cacheKeys)
        {
            await handlers.GetCacheService().RemoveAsync(key);
        }

        // Send webhook notifications
        await handlers.GetWebhookService().NotifyAsync("configuration.updated", @event);

        // Send notification with change details
        var message = changedKeys?.Any() == true
            ? $"Configuration '{@event.ConfigurationName}' was updated ({changedKeys.Count()} keys changed)"
            : $"Configuration '{@event.ConfigurationName}' was updated";

        await handlers.GetNotificationService().NotifyAsync(new Notification
        {
            Type = "configuration.updated",
            Message = message,
            Severity = "info",
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata is null
                ? new Dictionary<string, object>()
                : metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        });
    }

    /// <summary>
    /// Handles configuration key changed event with value comparison.
    /// </summary>
    /// <param name="handlers">The event handlers instance.</param>
    /// <param name="@event">The configuration key changed event.</param>
    /// <param name="oldValue">The previous value of the key, if known.</param>
    /// <param name="metadata">Optional metadata to include in notifications.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers or event is null.</exception>
    public static async Task HandleConfigurationKeyChangedAsync(
        this ConfigurationEventHandlers handlers,
        ConfigurationKeyChangedEvent @event,
        string? oldValue = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(@event);

        // Invalidate cache for this configuration's keys
        await handlers.GetCacheService().RemoveAsync(CacheKeyGenerator.GetConfigurationKeysKey(@event.ConfigurationId));
        await handlers.GetCacheService().RemoveAsync(CacheKeyGenerator.GetConfigurationKeyKey(@event.KeyId));

        // Send webhook notification
        await handlers.GetWebhookService().NotifyAsync("configuration.key.changed", @event);

        // Log as high severity if sensitive key changed
        var severity = @event.IsEncrypted ? "warning" : "info";

        // Include value change details in notification if old value is provided
        var message = oldValue is not null && @event.NewValue is not null
            ? $"Configuration key '{@event.Key}' changed from '{oldValue}' to '{@event.NewValue}'"
            : $"Configuration key '{@event.Key}' was changed";

        await handlers.GetNotificationService().NotifyAsync(new Notification
        {
            Type = "configuration.key.changed",
            Message = message,
            Severity = severity,
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata is null
                ? new Dictionary<string, object>()
                : metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        });
    }

    /// <summary>
    /// Handles configuration deleted event with cleanup tracking.
    /// </summary>
    /// <param name="handlers">The event handlers instance.</param>
    /// <param name="@event">The configuration deleted event.</param>
    /// <param name="deletedKeys">List of keys that were deleted.</param>
    /// <param name="metadata">Optional metadata to include in notifications.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers or event is null.</exception>
    public static async Task HandleConfigurationDeletedAsync(
        this ConfigurationEventHandlers handlers,
        ConfigurationDeletedEvent @event,
        IEnumerable<string>? deletedKeys = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        ArgumentNullException.ThrowIfNull(@event);

        // Invalidate all related caches
        var cacheKeys = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(
            @event.ConfigurationId,
            @event.ApplicationId
        ).ToList();

        foreach (var key in cacheKeys)
        {
            await handlers.GetCacheService().RemoveAsync(key);
        }

        // Send webhook notification
        await handlers.GetWebhookService().NotifyAsync("configuration.deleted", @event);

        // Send notification with details
        var message = deletedKeys?.Any() == true
            ? $"Configuration '{@event.ConfigurationName}' was deleted ({deletedKeys.Count()} keys removed)"
            : $"Configuration '{@event.ConfigurationName}' was deleted";

        await handlers.GetNotificationService().NotifyAsync(new Notification
        {
            Type = "configuration.deleted",
            Message = message,
            Severity = "warning",
            CreatedAt = DateTime.UtcNow,
            Metadata = metadata is null
                ? new Dictionary<string, object>()
                : metadata.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)
        });
    }
}