#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Events;

/// <summary>
/// Base class for domain events.
/// Events are used to notify different parts of the system about important changes.
/// </summary>
public abstract class DomainEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Event raised when a configuration is created.
/// </summary>
sealed public class ConfigurationCreatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when a configuration is updated.
/// </summary>
sealed public class ConfigurationUpdatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    public Dictionary<string, object?> Changes { get; set; } = new();
}

/// <summary>
/// Event raised when a configuration key value is changed.
/// </summary>
sealed public class ConfigurationKeyChangedEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid KeyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool IsEncrypted { get; set; }
}

/// <summary>
/// Event raised when a configuration is deleted.
/// </summary>
sealed public class ConfigurationDeletedEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when a new configuration version is created.
/// </summary>
sealed public class ConfigurationVersionCreatedEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid VersionId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
}

/// <summary>
/// Event raised when configuration is rolled back to a previous version.
/// </summary>
sealed public class ConfigurationRolledBackEvent : DomainEvent
{
    public Guid ConfigurationId { get; set; }
    public Guid FromVersionId { get; set; }
    public Guid ToVersionId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when a webhook subscription is created or updated.
/// </summary>
sealed public class WebhookSubscriptionChangedEvent : DomainEvent
{
    public Guid SubscriptionId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string[] Events { get; set; } = [];
}
