#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Globalization;

namespace DotnetConfigServer.Events;

/// <summary>
/// Extension methods for domain events providing common operations and utilities.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Gets the configuration identifier from the domain event.
    /// Returns Guid.Empty if the event does not contain a ConfigurationId property.
    /// </summary>
    /// <param name="domainEvent">The domain event to check.</param>
    /// <returns>The configuration identifier, or <see cref="Guid.Empty"/> if not available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static Guid GetConfigurationId(this DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            ConfigurationCreatedEvent created => created.ConfigurationId,
            ConfigurationUpdatedEvent updated => updated.ConfigurationId,
            ConfigurationKeyChangedEvent changed => changed.ConfigurationId,
            ConfigurationDeletedEvent deleted => deleted.ConfigurationId,
            ConfigurationVersionCreatedEvent version => version.ConfigurationId,
            ConfigurationRolledBackEvent rollback => rollback.ConfigurationId,
            _ => Guid.Empty
        };
    }

    /// <summary>
    /// Gets the application identifier from the domain event.
    /// Returns Guid.Empty if the event does not contain an ApplicationId property.
    /// </summary>
    /// <param name="domainEvent">The domain event to check.</param>
    /// <returns>The application identifier, or <see cref="Guid.Empty"/> if not available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static Guid GetApplicationId(this DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            ConfigurationCreatedEvent created => created.ApplicationId,
            ConfigurationUpdatedEvent updated => updated.ApplicationId,
            ConfigurationDeletedEvent deleted => deleted.ApplicationId,
            WebhookSubscriptionChangedEvent subscription => subscription.ApplicationId,
            _ => Guid.Empty
        };
    }

    /// <summary>
    /// Gets the configuration name from the domain event.
    /// Returns null if the event does not contain a ConfigurationName property.
    /// </summary>
    /// <param name="domainEvent">The domain event to check.</param>
    /// <returns>The configuration name, or null if not available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static string? GetConfigurationName(this DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            ConfigurationCreatedEvent created => created.ConfigurationName,
            ConfigurationUpdatedEvent updated => updated.ConfigurationName,
            ConfigurationDeletedEvent deleted => deleted.ConfigurationName,
            _ => null
        };
    }

    /// <summary>
    /// Determines whether the event represents a configuration change that affects the running application.
    /// </summary>
    /// <param name="domainEvent">The domain event to check.</param>
    /// <returns>True if the event affects configuration; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static bool IsConfigurationChange(this DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent is ConfigurationCreatedEvent
            or ConfigurationUpdatedEvent
            or ConfigurationKeyChangedEvent
            or ConfigurationDeletedEvent
            or ConfigurationVersionCreatedEvent
            or ConfigurationRolledBackEvent;
    }

    /// <summary>
    /// Calculates the age of the event at the specified point in time.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="now">The point in time to calculate the age against.</param>
    /// <returns>The <see cref="TimeSpan"/> representing how long ago the event occurred.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static TimeSpan AgeAt(this DomainEvent domainEvent, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return now - domainEvent.OccurredAt;
    }

    /// <summary>
    /// Returns a concise, one‑line string suitable for logging.
    /// Includes the event type name, Id, OccurredAt and the most relevant key fields for the concrete event type.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>A one‑line summary string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static string ToLogString(this DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var baseInfo = $"{domainEvent.GetType().Name} Id={domainEvent.Id} OccurredAt={domainEvent.OccurredAt:o}";

        var extra = domainEvent switch
        {
            ConfigurationCreatedEvent created => $" ConfigId={created.ConfigurationId} AppId={created.ApplicationId} Name={created.ConfigurationName}",
            ConfigurationUpdatedEvent updated => $" ConfigId={updated.ConfigurationId} AppId={updated.ApplicationId} Name={updated.ConfigurationName}",
            ConfigurationKeyChangedEvent keyChanged => $" ConfigId={keyChanged.ConfigurationId} KeyId={keyChanged.KeyId} Key={keyChanged.Key}",
            ConfigurationDeletedEvent deleted => $" ConfigId={deleted.ConfigurationId} AppId={deleted.ApplicationId} Name={deleted.ConfigurationName}",
            ConfigurationVersionCreatedEvent version => $" ConfigId={version.ConfigurationId} VersionId={version.VersionId} Version={version.VersionNumber}",
            ConfigurationRolledBackEvent rollback => $" ConfigId={rollback.ConfigurationId} From={rollback.FromVersionId} To={rollback.ToVersionId}",
            WebhookSubscriptionChangedEvent sub => $" SubId={sub.SubscriptionId} AppId={sub.ApplicationId} Url={sub.Url}",
            _ => string.Empty
        };

        return baseInfo + extra;
    }

    /// <summary>
    /// Determines whether the event is older than the specified <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <param name="timeSpan">The time span to compare against.</param>
    /// <returns>True if the event's age is greater than <paramref name="timeSpan"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    public static bool IsOlderThan(this DomainEvent domainEvent, TimeSpan timeSpan)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return domainEvent.AgeAt(DateTimeOffset.UtcNow) > timeSpan;
    }
}
