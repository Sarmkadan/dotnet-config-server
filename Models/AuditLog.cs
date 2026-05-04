// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents an audit log entry tracking changes and actions
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public AuditActionType ActionType { get; set; }

    [Required]
    [StringLength(256)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string EntityId { get; set; } = string.Empty;

    public string? EntityName { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    [Required]
    public string Status { get; set; } = "Success";

    public string? Details { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [StringLength(256)]
    public string? IpAddress { get; set; }

    [StringLength(256)]
    public string? UserAgent { get; set; }

    [Required]
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Creates an audit log entry for a create action
    /// </summary>
    public static AuditLog CreateEntry(
        Guid configurationId,
        string entityType,
        string entityId,
        string? entityName,
        string userId,
        string? userEmail,
        string? details = null,
        string? newValues = null)
    {
        return new AuditLog
        {
            ActionType = AuditActionType.ConfigurationCreated,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            UserId = userId,
            UserEmail = userEmail,
            Status = "Success",
            Details = details,
            NewValues = newValues,
            ConfigurationId = configurationId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an audit log entry for an update action
    /// </summary>
    public static AuditLog UpdateEntry(
        Guid configurationId,
        string entityType,
        string entityId,
        string? entityName,
        string userId,
        string? userEmail,
        string? oldValues = null,
        string? newValues = null,
        string? details = null)
    {
        return new AuditLog
        {
            ActionType = AuditActionType.ConfigurationUpdated,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            UserId = userId,
            UserEmail = userEmail,
            Status = "Success",
            Details = details,
            OldValues = oldValues,
            NewValues = newValues,
            ConfigurationId = configurationId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an audit log entry for a delete action
    /// </summary>
    public static AuditLog DeleteEntry(
        Guid configurationId,
        string entityType,
        string entityId,
        string? entityName,
        string userId,
        string? userEmail,
        string? oldValues = null)
    {
        return new AuditLog
        {
            ActionType = AuditActionType.ConfigurationDeleted,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            UserId = userId,
            UserEmail = userEmail,
            Status = "Success",
            OldValues = oldValues,
            ConfigurationId = configurationId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sets request context information for audit trail
    /// </summary>
    public void SetRequestContext(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Marks the audit entry as failed
    /// </summary>
    public void MarkAsFailed(string failureReason)
    {
        Status = "Failed";
        Details = failureReason;
    }

    /// <summary>
    /// Validates the audit log entry
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(EntityType))
            errors.AddError("EntityType", "Entity type is required");

        if (string.IsNullOrWhiteSpace(EntityId))
            errors.AddError("EntityId", "Entity ID is required");

        if (string.IsNullOrWhiteSpace(UserId))
            errors.AddError("UserId", "User ID is required");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Audit log validation failed", errors);
    }

    /// <summary>
    /// Gets a summary of the audit entry
    /// </summary>
    public AuditLogSummary GetSummary()
    {
        return new AuditLogSummary
        {
            Id = Id,
            ActionType = ActionType,
            EntityType = EntityType,
            EntityName = EntityName,
            Timestamp = Timestamp,
            UserId = UserId,
            Status = Status
        };
    }
}

/// <summary>
/// Summary view of an audit log entry
/// </summary>
public class AuditLogSummary
{
    public Guid Id { get; set; }
    public AuditActionType ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
