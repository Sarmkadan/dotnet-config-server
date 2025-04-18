#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Represents errors that occur during configuration operations
/// </summary>
public class ConfigurationException : DotnetConfigServerException
{
    public ConfigurationException(string message) : base(message, "CONFIGURATION_ERROR")
    {
    }

    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "CONFIGURATION_ERROR";
    }

    public ConfigurationException(string message, string errorCode, object? details = null) : base(message, errorCode, details)
    {
    }
}

/// <summary>
/// Thrown when a requested configuration is not found
/// </summary>
public sealed class ConfigurationNotFoundException : ConfigurationException
{
    public ConfigurationNotFoundException(string configId) : base($"Configuration '{configId}' not found", "CONFIG_NOT_FOUND", new { ConfigurationId = configId })
    {
    }

    public ConfigurationNotFoundException(Guid configId) : base($"Configuration '{configId}' not found", "CONFIG_NOT_FOUND", new { ConfigurationId = configId })
    {
    }
}

/// <summary>
/// Thrown when a configuration key is not found
/// </summary>
public sealed class ConfigurationKeyNotFoundException : ConfigurationException
{
    public ConfigurationKeyNotFoundException(string key) : base($"Configuration key '{key}' not found", "KEY_NOT_FOUND", new { Key = key })
    {
    }

    public ConfigurationKeyNotFoundException(Guid keyId) : base($"Configuration key '{keyId}' not found", "KEY_NOT_FOUND", new { KeyId = keyId })
    {
    }
}

/// <summary>
/// Thrown when encryption or decryption fails
/// </summary>
public sealed class EncryptionException : ConfigurationException
{
    public EncryptionException(string message) : base(message, "ENCRYPTION_FAILED")
    {
    }

    public EncryptionException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "ENCRYPTION_FAILED";
    }
}

/// <summary>
/// Thrown when a requested configuration snapshot is not found
/// </summary>
public sealed class ConfigurationSnapshotNotFoundException : ConfigurationException
{
    public ConfigurationSnapshotNotFoundException(string snapshotId) : base($"Configuration snapshot '{snapshotId}' not found", "SNAPSHOT_NOT_FOUND", new { SnapshotId = snapshotId })
    {
    }

    public ConfigurationSnapshotNotFoundException(Guid snapshotId) : base($"Configuration snapshot '{snapshotId}' not found", "SNAPSHOT_NOT_FOUND", new { SnapshotId = snapshotId })
    {
    }
}

/// <summary>
/// Thrown when a requested configuration version is not found
/// </summary>
public sealed class ConfigurationVersionNotFoundException : ConfigurationException
{
    public ConfigurationVersionNotFoundException(string versionId) : base($"Configuration version '{versionId}' not found", "VERSION_NOT_FOUND", new { VersionId = versionId })
    {
    }

    public ConfigurationVersionNotFoundException(Guid versionId) : base($"Configuration version '{versionId}' not found", "VERSION_NOT_FOUND", new { VersionId = versionId })
    {
    }
}

/// <summary>
/// Thrown when a webhook operation fails
/// </summary>
public sealed class WebhookException : ConfigurationException
{
    public WebhookException(string message) : base(message, "WEBHOOK_ERROR")
    {
    }

    public WebhookException(string webhookId, string message) : base(message, "WEBHOOK_ERROR", new { WebhookId = webhookId })
    {
    }
}