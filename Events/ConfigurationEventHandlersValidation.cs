#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;

namespace DotnetConfigServer.Events;

/// <summary>
/// Provides validation helpers for <see cref="ConfigurationEventHandlers"/> instances.
/// </summary>
public static class ConfigurationEventHandlersValidation
{
    /// <summary>
    /// Validates the specified <see cref="ConfigurationEventHandlers"/> instance.
    /// </summary>
    /// <param name="value">The configuration event handlers instance to validate.</param>
    /// <returns>An enumerable of validation error messages; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConfigurationEventHandlers value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate injected services
        if (value.GetCacheService() is null)
        {
            errors.Add("Cache service cannot be null.");
        }

        if (value.GetWebhookService() is null)
        {
            errors.Add("Webhook service cannot be null.");
        }

        if (value.GetNotificationService() is null)
        {
            errors.Add("Notification service cannot be null.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ConfigurationEventHandlers"/> instance is valid.
    /// </summary>
    /// <param name="value">The configuration event handlers instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this ConfigurationEventHandlers value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ConfigurationEventHandlers"/> instance is valid.
    /// </summary>
    /// <param name="value">The configuration event handlers instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid, containing a list of validation errors.</exception>
    public static void EnsureValid(this ConfigurationEventHandlers value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                "ConfigurationEventHandlers instance is not valid. " +
                string.Join(" ", errors),
                nameof(value));
        }
    }
}