#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a pending configuration change that requires approval before being applied.
/// </summary>
public sealed class ChangeRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// The key being modified. Null for configuration-level operations.
    /// </summary>
    public Guid? ConfigurationKeyId { get; set; }

    [Required]
    public ChangeRequestOperation Operation { get; set; }

    [Required]
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    /// <summary>
    /// Serialised representation of the proposed change payload (JSON).
    /// </summary>
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable summary of what this change does.
    /// </summary>
    [StringLength(1024)]
    public string? Summary { get; set; }

    [Required]
    public string RequestedBy { get; set; } = string.Empty;

    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(2048)]
    public string? ReviewComment { get; set; }

    public DateTime? AppliedAt { get; set; }

    public string? AppliedBy { get; set; }

    /// <summary>
    /// Approve this change request.
    /// </summary>
    public void Approve(string reviewerId, string? comment = null)
    {
        Status = ChangeRequestStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        ReviewComment = comment;
    }

    /// <summary>
    /// Reject this change request.
    /// </summary>
    public void Reject(string reviewerId, string? comment = null)
    {
        Status = ChangeRequestStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        ReviewComment = comment;
    }

    /// <summary>
    /// Mark the change request as applied after its payload has been executed.
    /// </summary>
    public void MarkApplied(string appliedBy)
    {
        Status = ChangeRequestStatus.Applied;
        AppliedBy = appliedBy;
        AppliedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancel this change request.
    /// </summary>
    public void Cancel()
    {
        Status = ChangeRequestStatus.Cancelled;
    }
}
