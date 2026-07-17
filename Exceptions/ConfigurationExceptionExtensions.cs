#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ConfigurationException"/> and its derived exception types.
/// </summary>
public static class ConfigurationExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="ConfigurationNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="ConfigurationNotFoundException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsConfigurationNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is ConfigurationNotFoundException;
    }

    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="ConfigurationKeyNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="ConfigurationKeyNotFoundException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsConfigurationKeyNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is ConfigurationKeyNotFoundException;
    }

    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="EncryptionException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="EncryptionException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsEncryptionException(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is EncryptionException;
    }

    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="ConfigurationSnapshotNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="ConfigurationSnapshotNotFoundException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsConfigurationSnapshotNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is ConfigurationSnapshotNotFoundException;
    }

    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="ConfigurationVersionNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="ConfigurationVersionNotFoundException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsConfigurationVersionNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is ConfigurationVersionNotFoundException;
    }

    /// <summary>
    /// Determines whether the exception is (or inherits from) <see cref="WebhookException"/>.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is (or inherits from) <see cref="WebhookException"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsWebhookException(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is WebhookException;
    }

    /// <summary>
    /// Safely extracts the configuration ID from a <see cref="ConfigurationNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to extract from.</param>
    /// <param name="configId">When this method returns, contains the configuration ID if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the configuration ID was successfully extracted; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool TryGetConfigurationId(this ConfigurationException exception, [NotNullWhen(true)] out string? configId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        configId = null;

        if (exception is ConfigurationNotFoundException configNotFound)
        {
            configId = configNotFound.Message
                .Replace("Configuration '", string.Empty, StringComparison.Ordinal)
                .Replace("' not found", string.Empty, StringComparison.Ordinal)
                .Trim();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Safely extracts the configuration ID from a <see cref="ConfigurationNotFoundException"/> as a GUID.
    /// </summary>
    /// <param name="exception">The exception to extract from.</param>
    /// <param name="configId">When this method returns, contains the configuration ID as GUID if found and valid; otherwise, <see cref="Guid.Empty"/>.</param>
    /// <returns><see langword="true"/> if the configuration ID was successfully extracted and parsed as GUID; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool TryGetConfigurationId(this ConfigurationException exception, out Guid configId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        configId = Guid.Empty;

        if (exception is ConfigurationNotFoundException configNotFound && configNotFound.Details is not null)
        {
            var detailsType = configNotFound.Details.GetType();
            var configIdProperty = detailsType.GetProperty("ConfigurationId");

            if (configIdProperty?.GetValue(configNotFound.Details) is Guid guidValue)
            {
                configId = guidValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Safely extracts the key from a <see cref="ConfigurationKeyNotFoundException"/>.
    /// </summary>
    /// <param name="exception">The exception to extract from.</param>
    /// <param name="key">When this method returns, contains the key if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the key was successfully extracted; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool TryGetKey(this ConfigurationException exception, [NotNullWhen(true)] out string? key)
    {
        ArgumentNullException.ThrowIfNull(exception);
        key = null;

        if (exception is ConfigurationKeyNotFoundException keyNotFound)
        {
            key = keyNotFound.Message
                .Replace("Configuration key '", string.Empty, StringComparison.Ordinal)
                .Replace("' not found", string.Empty, StringComparison.Ordinal)
                .Trim();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Safely extracts the key ID from a <see cref="ConfigurationKeyNotFoundException"/> as a GUID.
    /// </summary>
    /// <param name="exception">The exception to extract from.</param>
    /// <param name="keyId">When this method returns, contains the key ID as GUID if found and valid; otherwise, <see cref="Guid.Empty"/>.</param>
    /// <returns><see langword="true"/> if the key ID was successfully extracted and parsed as GUID; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool TryGetKeyId(this ConfigurationException exception, out Guid keyId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        keyId = Guid.Empty;

        if (exception is ConfigurationKeyNotFoundException keyNotFound && keyNotFound.Details is not null)
        {
            var detailsType = keyNotFound.Details.GetType();
            var keyIdProperty = detailsType.GetProperty("KeyId");

            if (keyIdProperty?.GetValue(keyNotFound.Details) is Guid guidValue)
            {
                keyId = guidValue;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a new <see cref="ConfigurationNotFoundException"/> from an existing <see cref="ConfigurationException"/>.
    /// Useful for rethrowing with more specific exception type.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <returns>A new <see cref="ConfigurationNotFoundException"/> with the same message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static ConfigurationNotFoundException ToConfigurationNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ConfigurationNotFoundException(exception.Message);
    }

    /// <summary>
    /// Creates a new <see cref="ConfigurationKeyNotFoundException"/> from an existing <see cref="ConfigurationException"/>.
    /// Useful for rethrowing with more specific exception type.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <returns>A new <see cref="ConfigurationKeyNotFoundException"/> with the same message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static ConfigurationKeyNotFoundException ToConfigurationKeyNotFound(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ConfigurationKeyNotFoundException(exception.Message);
    }

    /// <summary>
    /// Creates a new <see cref="EncryptionException"/> from an existing <see cref="ConfigurationException"/>.
    /// Useful for rethrowing with more specific exception type.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <returns>A new <see cref="EncryptionException"/> with the same message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static EncryptionException ToEncryptionException(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new EncryptionException(exception.Message, exception);
    }

    /// <summary>
    /// Gets the error details object from the exception if available.
    /// </summary>
    /// <param name="exception">The exception to get details from.</param>
    /// <returns>The details object if available; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static object? GetDetails(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Details;
    }

    /// <summary>
    /// Gets all error details as a dictionary of key-value pairs.
    /// </summary>
    /// <param name="exception">The exception to get details from.</param>
    /// <returns>A read-only dictionary containing the error details; empty if no details available.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static IReadOnlyDictionary<string, object?> GetDetailsDictionary(this ConfigurationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception.Details is null)
        {
            return System.Collections.Immutable.ImmutableDictionary<string, object?>.Empty;
        }

        var dictionaryType = exception.Details.GetType();
        var properties = dictionaryType.GetProperties();
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in properties)
        {
            result[property.Name] = property.GetValue(exception.Details);
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the exception has a specific error code.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <param name="errorCode">The error code to match.</param>
    /// <returns><see langword="true"/> if the exception has the specified error code; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool HasErrorCode(this ConfigurationException exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        return string.Equals(exception.ErrorCode, errorCode, StringComparison.Ordinal);
    }
}