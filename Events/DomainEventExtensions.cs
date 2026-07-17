#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>The configuration identifier, or Guid.Empty if not available.</returns>
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
    /// <param name="domainEvent">The domain event.</param>
    /// <returns>The application identifier, or Guid.Empty if not available.</returns>
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
    /// <param name="domainEvent">The domain event.</param>
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
    /// <param name="domainEvent">The domain event.</param>
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
}