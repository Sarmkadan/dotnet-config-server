#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetConfigServer.Events;

/// <summary>
/// Provides validation helpers for domain events.
/// </summary>
public static class DomainEventValidation
{
    /// <summary>
    /// Validates a domain event and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The domain event to validate.</param>
    /// <returns>A read-only list of human-readable validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this DomainEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate base DomainEvent properties
        if (value.Id == Guid.Empty)
        {
            problems.Add("DomainEvent.Id must not be empty");
        }

        if (value.OccurredAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("DomainEvent.OccurredAt must not be in the future");
        }

        if (value.OccurredAt < DateTime.UtcNow.AddYears(-1))
        {
            problems.Add("DomainEvent.OccurredAt must not be older than 1 year");
        }

        if (string.IsNullOrWhiteSpace(value.Source))
        {
            problems.Add("DomainEvent.Source must not be null or whitespace");
        }

        // Validate derived event-specific properties
        switch (value)
        {
            case ConfigurationCreatedEvent createdEvent:
                ValidateConfigurationCreatedEvent(createdEvent, problems);
                break;

            case ConfigurationUpdatedEvent updatedEvent:
                ValidateConfigurationUpdatedEvent(updatedEvent, problems);
                break;

            case ConfigurationKeyChangedEvent keyChangedEvent:
                ValidateConfigurationKeyChangedEvent(keyChangedEvent, problems);
                break;

            case ConfigurationDeletedEvent deletedEvent:
                ValidateConfigurationDeletedEvent(deletedEvent, problems);
                break;

            case ConfigurationVersionCreatedEvent versionCreatedEvent:
                ValidateConfigurationVersionCreatedEvent(versionCreatedEvent, problems);
                break;

            case ConfigurationRolledBackEvent rolledBackEvent:
                ValidateConfigurationRolledBackEvent(rolledBackEvent, problems);
                break;

            case WebhookSubscriptionChangedEvent webhookEvent:
                ValidateWebhookSubscriptionChangedEvent(webhookEvent, problems);
                break;
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a domain event is valid.
    /// </summary>
    /// <param name="value">The domain event to check.</param>
    /// <returns>True if the event is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static bool IsValid(this DomainEvent? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a domain event is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The domain event to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the event is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this DomainEvent? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Domain event validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }

    private static void ValidateConfigurationCreatedEvent(
        ConfigurationCreatedEvent createdEvent,
        List<string> problems)
    {
        if (createdEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationCreatedEvent.ConfigurationId must not be empty");
        }

        if (createdEvent.ApplicationId == Guid.Empty)
        {
            problems.Add("ConfigurationCreatedEvent.ApplicationId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(createdEvent.ConfigurationName))
        {
            problems.Add("ConfigurationCreatedEvent.ConfigurationName must not be null or whitespace");
        }

        if (string.IsNullOrWhiteSpace(createdEvent.Environment))
        {
            problems.Add("ConfigurationCreatedEvent.Environment must not be null or whitespace");
        }
    }

    private static void ValidateConfigurationUpdatedEvent(
        ConfigurationUpdatedEvent updatedEvent,
        List<string> problems)
    {
        if (updatedEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationUpdatedEvent.ConfigurationId must not be empty");
        }

        if (updatedEvent.ApplicationId == Guid.Empty)
        {
            problems.Add("ConfigurationUpdatedEvent.ApplicationId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(updatedEvent.ConfigurationName))
        {
            problems.Add("ConfigurationUpdatedEvent.ConfigurationName must not be null or whitespace");
        }

        if (updatedEvent.Changes == null)
        {
            problems.Add("ConfigurationUpdatedEvent.Changes must not be null");
        }
        else if (updatedEvent.Changes.Count == 0)
        {
            problems.Add("ConfigurationUpdatedEvent.Changes should not be empty");
        }
    }

    private static void ValidateConfigurationKeyChangedEvent(
        ConfigurationKeyChangedEvent keyChangedEvent,
        List<string> problems)
    {
        if (keyChangedEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationKeyChangedEvent.ConfigurationId must not be empty");
        }

        if (keyChangedEvent.KeyId == Guid.Empty)
        {
            problems.Add("ConfigurationKeyChangedEvent.KeyId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(keyChangedEvent.Key))
        {
            problems.Add("ConfigurationKeyChangedEvent.Key must not be null or whitespace");
        }

        if (keyChangedEvent.IsEncrypted && string.IsNullOrWhiteSpace(keyChangedEvent.NewValue))
        {
            problems.Add("ConfigurationKeyChangedEvent.NewValue must not be null or whitespace when IsEncrypted is true");
        }
    }

    private static void ValidateConfigurationDeletedEvent(
        ConfigurationDeletedEvent deletedEvent,
        List<string> problems)
    {
        if (deletedEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationDeletedEvent.ConfigurationId must not be empty");
        }

        if (deletedEvent.ApplicationId == Guid.Empty)
        {
            problems.Add("ConfigurationDeletedEvent.ApplicationId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(deletedEvent.ConfigurationName))
        {
            problems.Add("ConfigurationDeletedEvent.ConfigurationName must not be null or whitespace");
        }
    }

    private static void ValidateConfigurationVersionCreatedEvent(
        ConfigurationVersionCreatedEvent versionCreatedEvent,
        List<string> problems)
    {
        if (versionCreatedEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationVersionCreatedEvent.ConfigurationId must not be empty");
        }

        if (versionCreatedEvent.VersionId == Guid.Empty)
        {
            problems.Add("ConfigurationVersionCreatedEvent.VersionId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(versionCreatedEvent.VersionNumber))
        {
            problems.Add("ConfigurationVersionCreatedEvent.VersionNumber must not be null or whitespace");
        }
    }

    private static void ValidateConfigurationRolledBackEvent(
        ConfigurationRolledBackEvent rolledBackEvent,
        List<string> problems)
    {
        if (rolledBackEvent.ConfigurationId == Guid.Empty)
        {
            problems.Add("ConfigurationRolledBackEvent.ConfigurationId must not be empty");
        }

        if (rolledBackEvent.FromVersionId == Guid.Empty)
        {
            problems.Add("ConfigurationRolledBackEvent.FromVersionId must not be empty");
        }

        if (rolledBackEvent.ToVersionId == Guid.Empty)
        {
            problems.Add("ConfigurationRolledBackEvent.ToVersionId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(rolledBackEvent.Reason))
        {
            problems.Add("ConfigurationRolledBackEvent.Reason must not be null or whitespace");
        }
    }

    private static void ValidateWebhookSubscriptionChangedEvent(
        WebhookSubscriptionChangedEvent webhookEvent,
        List<string> problems)
    {
        if (webhookEvent.SubscriptionId == Guid.Empty)
        {
            problems.Add("WebhookSubscriptionChangedEvent.SubscriptionId must not be empty");
        }

        if (webhookEvent.ApplicationId == Guid.Empty)
        {
            problems.Add("WebhookSubscriptionChangedEvent.ApplicationId must not be empty");
        }

        if (string.IsNullOrWhiteSpace(webhookEvent.Url))
        {
            problems.Add("WebhookSubscriptionChangedEvent.Url must not be null or whitespace");
        }
        else if (!Uri.IsWellFormedUriString(webhookEvent.Url, UriKind.Absolute))
        {
            problems.Add("WebhookSubscriptionChangedEvent.Url must be a valid absolute URI");
        }

        if (webhookEvent.Events == null)
        {
            problems.Add("WebhookSubscriptionChangedEvent.Events must not be null");
        }
        else if (webhookEvent.Events.Length == 0)
        {
            problems.Add("WebhookSubscriptionChangedEvent.Events should not be empty");
        }
        else
        {
            for (int i = 0; i < webhookEvent.Events.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(webhookEvent.Events[i]))
                {
                    problems.Add($"WebhookSubscriptionChangedEvent.Events[{i}] must not be null or whitespace");
                }
            }
        }
    }
}