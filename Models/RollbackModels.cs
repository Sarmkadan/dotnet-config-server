#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents the outcome of an executed rollback.
/// </summary>
public sealed class RollbackResult
{
    /// <summary>Gets or sets the configuration identifier.</summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>Gets or sets the newly created version produced by the rollback.</summary>
    public ConfigurationVersionSummary NewVersion { get; set; } = null!;

    /// <summary>Gets or sets the version that was restored.</summary>
    public ConfigurationVersionSummary RestoredFromVersion { get; set; } = null!;

    /// <summary>Gets or sets the rollback reason.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who performed the rollback.</summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when the rollback was executed.</summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>Gets or sets the number of keys restored into the new version.</summary>
    public int KeysRestored { get; set; }
}

/// <summary>
/// Represents a persisted rollback history entry.
/// </summary>
public sealed class RollbackRecord
{
    /// <summary>Gets or sets the rollback record identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the configuration identifier.</summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>Gets or sets the new version created by the rollback.</summary>
    public Guid NewVersionId { get; set; }

    /// <summary>Gets or sets the restored source version identifier.</summary>
    public Guid RestoredFromVersionId { get; set; }

    /// <summary>Gets or sets the rollback reason.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who performed the rollback.</summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when the rollback was executed.</summary>
    public DateTime PerformedAt { get; set; }
}
