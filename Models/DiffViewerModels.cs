#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;

namespace DotnetConfigServer.Models;

/// <summary>
/// A configuration diff enriched with full version metadata for viewer display.
/// Uses a cached diff record when one exists; otherwise reflects a live key comparison.
/// </summary>
sealed public class EnrichedDiff
{
    /// <summary>Gets or sets the diff record identifier (may be <see cref="Guid.NewGuid"/> when computed on the fly).</summary>
    public Guid DiffId { get; set; }

    /// <summary>Gets or sets the owning configuration identifier.</summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>Gets or sets the metadata of the source (from) version.</summary>
    public ConfigurationVersionSummary FromVersion { get; set; } = null!;

    /// <summary>Gets or sets the metadata of the target (to) version.</summary>
    public ConfigurationVersionSummary ToVersion { get; set; } = null!;

    /// <summary>Gets or sets all individual key changes between the two versions.</summary>
    public List<DiffEntry> Changes { get; set; } = new();

    /// <summary>Gets or sets the number of keys added in the target version.</summary>
    public int AddedCount { get; set; }

    /// <summary>Gets or sets the number of keys whose values changed.</summary>
    public int ModifiedCount { get; set; }

    /// <summary>Gets or sets the number of keys removed in the target version.</summary>
    public int DeletedCount { get; set; }

    /// <summary>Gets the sum of all added, modified, and deleted changes.</summary>
    public int TotalChanges => AddedCount + ModifiedCount + DeletedCount;

    /// <summary>Gets or sets the UTC timestamp when the diff was computed or retrieved.</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Gets all change entries that represent newly added keys.</summary>
    public IEnumerable<DiffEntry> AddedKeys => Changes.Where(c => c.ChangeType == ChangeType.Added);

    /// <summary>Gets all change entries that represent keys with updated values.</summary>
    public IEnumerable<DiffEntry> ModifiedKeys => Changes.Where(c => c.ChangeType == ChangeType.Modified);

    /// <summary>Gets all change entries that represent removed keys.</summary>
    public IEnumerable<DiffEntry> DeletedKeys => Changes.Where(c => c.ChangeType == ChangeType.Deleted);
}

/// <summary>
/// Preview of what a rollback operation would change relative to the currently active version,
/// computed without persisting any state changes.
/// </summary>
sealed public class RollbackPreview
{
    /// <summary>Gets or sets the configuration identifier the rollback applies to.</summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>Gets or sets the currently active version summary, or <see langword="null"/> if no version is active.</summary>
    public ConfigurationVersionSummary? CurrentVersion { get; set; }

    /// <summary>Gets or sets the version that would be restored by the rollback.</summary>
    public ConfigurationVersionSummary TargetVersion { get; set; } = null!;

    /// <summary>Gets or sets the key changes that the rollback would introduce.</summary>
    public List<DiffEntry> Changes { get; set; } = new();

    /// <summary>Gets or sets the number of keys that would be added by the rollback.</summary>
    public int AddedCount { get; set; }

    /// <summary>Gets or sets the number of keys whose values would change.</summary>
    public int ModifiedCount { get; set; }

    /// <summary>Gets or sets the number of keys that would be removed.</summary>
    public int DeletedCount { get; set; }

    /// <summary>Gets the total number of keys affected by the rollback.</summary>
    public int TotalChanges => AddedCount + ModifiedCount + DeletedCount;

    /// <summary>
    /// Gets or sets whether the rollback is considered safe, i.e. it would not remove
    /// any key marked as required in the active version.
    /// </summary>
    public bool IsRollbackSafe { get; set; }

    /// <summary>Gets or sets human-readable warnings about potentially breaking changes.</summary>
    public List<string> WarningMessages { get; set; } = new();
}

/// <summary>
/// A single point in a configuration version timeline annotated with change statistics
/// relative to the immediately preceding version.
/// </summary>
sealed public class VersionTimelineEntry
{
    /// <summary>Gets or sets the version summary for this timeline point.</summary>
    public ConfigurationVersionSummary Version { get; set; } = null!;

    /// <summary>
    /// Gets or sets the diff summary from the preceding version, or <see langword="null"/>
    /// for the first (oldest) version in the timeline.
    /// </summary>
    public ConfigurationDiffSummary? DiffFromPrevious { get; set; }

    /// <summary>Gets or sets whether this is the first (oldest) version in the timeline.</summary>
    public bool IsFirst { get; set; }
}
