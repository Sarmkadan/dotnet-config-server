#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Common;

/// <summary>
/// Represents the type of a configuration value
/// </summary>
public enum ConfigurationValueType
{
    String = 0,
    Integer = 1,
    Boolean = 2,
    Json = 3,
    Decimal = 4,
    DateTime = 5
}

/// <summary>
/// Represents the environment type
/// </summary>
public enum Environment
{
    Development = 0,
    Staging = 1,
    Production = 2
}

/// <summary>
/// Represents the encryption algorithm
/// </summary>
public enum EncryptionAlgorithm
{
    None = 0,
    AES256 = 1,
    RSA = 2
}

/// <summary>
/// Represents the status of a configuration version
/// </summary>
public enum ConfigurationVersionStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2,
    Deprecated = 3
}

/// <summary>
/// Represents the status of a webhook delivery
/// </summary>
public enum WebhookDeliveryStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Retry = 3
}

/// <summary>
/// Represents the type of change in a configuration
/// </summary>
public enum ChangeType
{
    Added = 0,
    Modified = 1,
    Deleted = 2
}

/// <summary>
/// Represents the status of a webhook subscription
/// </summary>
public enum WebhookStatus
{
    Active = 0,
    Inactive = 1,
    Failed = 2
}

/// <summary>
/// Represents audit log action types
/// </summary>
public enum AuditActionType
{
    ConfigurationCreated = 0,
    ConfigurationUpdated = 1,
    ConfigurationDeleted = 2,
    VersionPublished = 3,
    VersionArchived = 4,
    EncryptionKeyRotated = 5,
    WebhookDelivered = 6,
    UserLogin = 7,
    PermissionChanged = 8
}
