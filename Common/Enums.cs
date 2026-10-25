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
    /// <summary>Free-form text value</summary>
    String = 0,

    /// <summary>Whole number value</summary>
    Integer = 1,

    /// <summary>True/false value</summary>
    Boolean = 2,

    /// <summary>JSON document value</summary>
    Json = 3,

    /// <summary>Fixed-precision decimal value</summary>
    Decimal = 4,

    /// <summary>Date and time value</summary>
    DateTime = 5
}

/// <summary>
/// Represents the environment type
/// </summary>
public enum Environment
{
    /// <summary>Local development environment</summary>
    Development = 0,

    /// <summary>Pre-production testing environment</summary>
    Staging = 1,

    /// <summary>Live production environment</summary>
    Production = 2
}

/// <summary>
/// Represents the encryption algorithm
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>Value is stored without encryption</summary>
    None = 0,

    /// <summary>AES with a 256-bit key</summary>
    AES256 = 1
}

/// <summary>
/// Represents the status of a configuration version
/// </summary>
public enum ConfigurationVersionStatus
{
    /// <summary>Version is being prepared and has not been published</summary>
    Draft = 0,

    /// <summary>Version is the currently published one</summary>
    Active = 1,

    /// <summary>Version was replaced by a newer one and is kept for history</summary>
    Archived = 2,

    /// <summary>Version is still stored but should no longer be used</summary>
    Deprecated = 3
}

/// <summary>
/// Represents the status of a webhook delivery
/// </summary>
public enum WebhookDeliveryStatus
{
    /// <summary>Delivery is queued and has not been attempted yet</summary>
    Pending = 0,

    /// <summary>Delivery was accepted by the receiver</summary>
    Success = 1,

    /// <summary>Delivery failed and will not be retried</summary>
    Failed = 2,

    /// <summary>Delivery failed and is scheduled for another attempt</summary>
    Retry = 3
}

/// <summary>
/// Represents the type of change in a configuration
/// </summary>
public enum ChangeType
{
    /// <summary>A key or value was added</summary>
    Added = 0,

    /// <summary>A key or value was modified</summary>
    Modified = 1,

    /// <summary>A key or value was deleted</summary>
    Deleted = 2
}

/// <summary>
/// Represents the status of a webhook subscription
/// </summary>
public enum WebhookStatus
{
    /// <summary>Subscription receives events</summary>
    Active = 0,

    /// <summary>Subscription deliveries are failing</summary>
    Failed = 2
}

/// <summary>
/// Represents audit log action types
/// </summary>
public enum AuditActionType
{
    /// <summary>A configuration was created</summary>
    ConfigurationCreated = 0,

    /// <summary>A configuration was updated</summary>
    ConfigurationUpdated = 1,

    /// <summary>A configuration was deleted</summary>
    ConfigurationDeleted = 2,

    /// <summary>A configuration version was published</summary>
    VersionPublished = 3,

    /// <summary>An encryption key was rotated</summary>
    EncryptionKeyRotated = 5,

    /// <summary>A webhook notification was delivered</summary>
    WebhookDelivered = 6
}

/// <summary>
/// Represents the status of a configuration change request in the approval workflow
/// </summary>
public enum ChangeRequestStatus
{
    /// <summary>Request is waiting for a decision</summary>
    Pending = 0,

    /// <summary>Request was approved</summary>
    Approved = 1,

    /// <summary>Request was rejected</summary>
    Rejected = 2,

    /// <summary>Approved request has been applied to the configuration</summary>
    Applied = 3,

    /// <summary>Request was cancelled by its author</summary>
    Cancelled = 4
}

/// <summary>
/// Represents the type of operation in a change request
/// </summary>
public enum ChangeRequestOperation
{
    /// <summary>Create a new configuration key</summary>
    CreateKey = 0,

    /// <summary>Update an existing configuration key</summary>
    UpdateKey = 1,

    /// <summary>Delete a configuration key</summary>
    DeleteKey = 2,

    /// <summary>Update the configuration as a whole</summary>
    UpdateConfiguration = 3
}
