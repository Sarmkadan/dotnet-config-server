#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Represents errors that occur during configuration operations
/// </summary>
public class ConfigurationException : Exception
{
    public string? ErrorCode { get; set; }
    public object? Details { get; set; }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConfigurationException(string message, string errorCode, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

/// <summary>
/// Thrown when a requested configuration is not found
/// </summary>
sealed public class ConfigurationNotFoundException : ConfigurationException
{
    public ConfigurationNotFoundException(string configId)
        : base($"Configuration '{configId}' not found", "CONFIG_NOT_FOUND", new { ConfigurationId = configId })
    {
    }
}

/// <summary>
/// Thrown when a configuration key is not found
/// </summary>
sealed public class ConfigurationKeyNotFoundException : ConfigurationException
{
    public ConfigurationKeyNotFoundException(string key)
        : base($"Configuration key '{key}' not found", "KEY_NOT_FOUND", new { Key = key })
    {
    }
}

/// <summary>
/// Thrown when encryption or decryption fails
/// </summary>
sealed public class EncryptionException : ConfigurationException
{
    public EncryptionException(string message)
        : base(message, "ENCRYPTION_FAILED")
    {
    }

    public EncryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ENCRYPTION_FAILED";
    }
}

/// <summary>
/// Thrown when validation fails
/// </summary>
sealed public class ValidationException : ConfigurationException
{
    public Dictionary<string, List<string>> Errors { get; set; }

    public ValidationException(string message, Dictionary<string, List<string>> errors)
        : base(message, "VALIDATION_FAILED", errors)
    {
        Errors = errors;
    }

    public ValidationException(string fieldName, string message)
        : base($"Validation failed: {fieldName} - {message}", "VALIDATION_FAILED")
    {
        Errors = new() { { fieldName, new() { message } } };
    }
}

/// <summary>
/// Thrown when database operation fails
/// </summary>
sealed public class DatabaseException : ConfigurationException
{
    public DatabaseException(string message)
        : base(message, "DATABASE_ERROR")
    {
    }

    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "DATABASE_ERROR";
    }
}

/// <summary>
/// Thrown when webhook operation fails
/// </summary>
sealed public class WebhookException : ConfigurationException
{
    public WebhookException(string message)
        : base(message, "WEBHOOK_ERROR")
    {
    }

    public WebhookException(string webhookId, string message)
        : base(message, "WEBHOOK_ERROR", new { WebhookId = webhookId })
    {
    }
}

/// <summary>
/// Thrown when a requested configuration snapshot is not found
/// </summary>
sealed public class ConfigurationSnapshotNotFoundException : ConfigurationException
{
    public ConfigurationSnapshotNotFoundException(string snapshotId)
        : base($"Configuration snapshot '{snapshotId}' not found", "SNAPSHOT_NOT_FOUND", new { SnapshotId = snapshotId })
    {
    }
}
