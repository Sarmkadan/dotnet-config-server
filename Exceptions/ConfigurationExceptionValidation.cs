#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="ConfigurationException"/> and its derived exception types
/// </summary>
public static class ConfigurationExceptionValidation
{
    /// <summary>
    /// Validates a <see cref="ConfigurationException"/> instance and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <returns>An empty list if valid, otherwise a list of validation error messages</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ConfigurationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate base Exception properties
        if (string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("Exception message cannot be null, empty, or whitespace.");
        }

        // Validate ConfigurationException-specific properties
        if (value is ConfigurationNotFoundException configNotFound)
        {
            ValidateConfigurationNotFoundException(configNotFound, errors);
        }
        else if (value is ConfigurationKeyNotFoundException keyNotFound)
        {
            ValidateConfigurationKeyNotFoundException(keyNotFound, errors);
        }
        else if (value is EncryptionException encryption)
        {
            ValidateEncryptionException(encryption, errors);
        }
        else if (value is ConfigurationSnapshotNotFoundException snapshotNotFound)
        {
            ValidateConfigurationSnapshotNotFoundException(snapshotNotFound, errors);
        }
        else if (value is ConfigurationVersionNotFoundException versionNotFound)
        {
            ValidateConfigurationVersionNotFoundException(versionNotFound, errors);
        }
        else if (value is WebhookException webhook)
        {
            ValidateWebhookException(webhook, errors);
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ConfigurationException"/> instance is valid.
    /// </summary>
    /// <param name="value">The exception to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static bool IsValid(this ConfigurationException? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ConfigurationException"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// if it is not.
    /// </summary>
    /// <param name="value">The exception to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if the exception is invalid, containing validation errors</exception>
    public static void EnsureValid(this ConfigurationException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Configuration exception is invalid. Validation errors:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", errors)
                }",
                nameof(value)
            );
        }
    }

    private static void ValidateConfigurationNotFoundException(
        ConfigurationNotFoundException exception,
        List<string> errors)
    {
        if (exception.Details is not null)
        {
            if (exception.Details is not object)
            {
                errors.Add("ConfigurationNotFoundException.Details must be a valid object.");
            }
            else if (exception.Details is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue("ConfigurationId", out var configId) || configId is not string)
                {
                    errors.Add("ConfigurationNotFoundException.Details must contain a valid 'ConfigurationId' string property.");
                }
            }
        }
    }

    private static void ValidateConfigurationKeyNotFoundException(
        ConfigurationKeyNotFoundException exception,
        List<string> errors)
    {
        if (exception.Details is not null)
        {
            if (exception.Details is not object)
            {
                errors.Add("ConfigurationKeyNotFoundException.Details must be a valid object.");
            }
            else if (exception.Details is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue("Key", out var key) || key is not string)
                {
                    if (!dict.TryGetValue("KeyId", out var keyId) || keyId is not Guid)
                    {
                        errors.Add("ConfigurationKeyNotFoundException.Details must contain either a valid 'Key' string or 'KeyId' Guid property.");
                    }
                }
            }
        }
    }

    private static void ValidateEncryptionException(
        EncryptionException exception,
        List<string> errors)
    {
        // EncryptionException has no additional properties beyond base Exception
        // Only validate base Exception properties which are already checked in Validate()
    }

    private static void ValidateConfigurationSnapshotNotFoundException(
        ConfigurationSnapshotNotFoundException exception,
        List<string> errors)
    {
        if (exception.Details is not null)
        {
            if (exception.Details is not object)
            {
                errors.Add("ConfigurationSnapshotNotFoundException.Details must be a valid object.");
            }
            else if (exception.Details is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue("SnapshotId", out var snapshotId) || snapshotId is not string)
                {
                    errors.Add("ConfigurationSnapshotNotFoundException.Details must contain a valid 'SnapshotId' string property.");
                }
            }
        }
    }

    private static void ValidateConfigurationVersionNotFoundException(
        ConfigurationVersionNotFoundException exception,
        List<string> errors)
    {
        if (exception.Details is not null)
        {
            if (exception.Details is not object)
            {
                errors.Add("ConfigurationVersionNotFoundException.Details must be a valid object.");
            }
            else if (exception.Details is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue("VersionId", out var versionId) || versionId is not string)
                {
                    errors.Add("ConfigurationVersionNotFoundException.Details must contain a valid 'VersionId' string property.");
                }
            }
        }
    }

    private static void ValidateWebhookException(
        WebhookException exception,
        List<string> errors)
    {
        if (exception.Details is not null)
        {
            if (exception.Details is not object)
            {
                errors.Add("WebhookException.Details must be a valid object.");
            }
            else if (exception.Details is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue("WebhookId", out var webhookId) || webhookId is not string)
                {
                    errors.Add("WebhookException.Details must contain a valid 'WebhookId' string property when provided.");
                }
            }
        }
    }
}
