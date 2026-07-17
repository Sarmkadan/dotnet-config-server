#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides validation extension methods for <see cref="ConfigurationException"/> instances
/// that validate using the extension methods from <see cref="ConfigurationExceptionExtensions"/>.
/// </summary>
public static class ConfigurationExceptionExtensionsValidation
{
    /// <summary>
    /// Validates a <see cref="ConfigurationException"/> instance using extension methods validation.
    /// </summary>
    /// <param name="exception">The configuration exception to validate.</param>
    /// <param name="_extensions">Reserved parameter to distinguish from other Validate methods.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this ConfigurationException exception, bool _extensions = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problems = new List<string>();

        // Validate base exception properties
        if (string.IsNullOrEmpty(exception.Message))
        {
            problems.Add("Message cannot be null or empty");
        }

        if (string.IsNullOrEmpty(exception.ErrorCode))
        {
            problems.Add("ErrorCode cannot be null or empty");
        }

        // Validate exception type-specific properties
        switch (exception)
        {
            case ConfigurationNotFoundException configNotFound:
                if (configNotFound.Details is null)
                {
                    problems.Add("ConfigurationNotFoundException.Details cannot be null");
                }
                break;

            case ConfigurationKeyNotFoundException keyNotFound:
                if (keyNotFound.Details is null)
                {
                    problems.Add("ConfigurationKeyNotFoundException.Details cannot be null");
                }
                break;

            case EncryptionException:
                // EncryptionException has no additional required properties
                break;

            case ConfigurationSnapshotNotFoundException snapshotNotFound:
                if (snapshotNotFound.Details is null)
                {
                    problems.Add("ConfigurationSnapshotNotFoundException.Details cannot be null");
                }
                break;

            case ConfigurationVersionNotFoundException versionNotFound:
                if (versionNotFound.Details is null)
                {
                    problems.Add("ConfigurationVersionNotFoundException.Details cannot be null");
                }
                break;

            case WebhookException webhook:
                if (webhook.Details is null)
                {
                    problems.Add("WebhookException.Details cannot be null");
                }
                break;
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ConfigurationException"/> instance is valid using extension methods validation.
    /// </summary>
    /// <param name="exception">The configuration exception to check.</param>
    /// <returns><see langword="true"/> if the exception is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this ConfigurationException exception)
    {
        return exception.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="ConfigurationException"/> instance is valid using extension methods validation.
    /// </summary>
    /// <param name="exception">The configuration exception to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the exception is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problems = exception.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ConfigurationException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
