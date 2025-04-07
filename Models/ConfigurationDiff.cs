#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents the difference between two configuration versions
/// </summary>
sealed public class ConfigurationDiff
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ConfigurationId { get; set; }

    [Required]
    public Guid FromVersionId { get; set; }

    [Required]
    public Guid ToVersionId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public int TotalChanges { get; set; } = 0;

    [Required]
    public int AddedCount { get; set; } = 0;

    [Required]
    public int ModifiedCount { get; set; } = 0;

    [Required]
    public int DeletedCount { get; set; } = 0;

    public List<DiffEntry> Changes { get; set; } = new();

    /// <summary>
    /// Adds a new change entry to the diff
    /// </summary>
    public void AddChange(string key, ChangeType changeType, string? oldValue = null, string? newValue = null)
    {
        var entry = new DiffEntry
        {
            Id = Guid.NewGuid(),
            DiffId = Id,
            Key = key,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        };

        Changes.Add(entry);

        switch (changeType)
        {
            case ChangeType.Added:
                AddedCount++;
                break;
            case ChangeType.Modified:
                ModifiedCount++;
                break;
            case ChangeType.Deleted:
                DeletedCount++;
                break;
        }

        TotalChanges = AddedCount + ModifiedCount + DeletedCount;
    }

    /// <summary>
    /// Gets changes by type
    /// </summary>
    public List<DiffEntry> GetChangesByType(ChangeType changeType)
    {
        return Changes.Where(c => c.ChangeType == changeType).ToList();
    }

    /// <summary>
    /// Gets a summary of the diff
    /// </summary>
    public ConfigurationDiffSummary GetSummary()
    {
        return new ConfigurationDiffSummary
        {
            Id = Id,
            TotalChanges = TotalChanges,
            AddedCount = AddedCount,
            ModifiedCount = ModifiedCount,
            DeletedCount = DeletedCount,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy
        };
    }

    /// <summary>
    /// Generates a human-readable summary of the changes
    /// </summary>
    public string GetChangesSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"Total changes: {TotalChanges}");
        summary.AppendLine($"  Added: {AddedCount}");
        summary.AppendLine($"  Modified: {ModifiedCount}");
        summary.AppendLine($"  Deleted: {DeletedCount}");
        return summary.ToString();
    }
}

/// <summary>
/// Represents a single change in a diff
/// </summary>
sealed public class DiffEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DiffId { get; set; }

    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public ChangeType ChangeType { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DiffEntryOrigin Origin { get; set; } = DiffEntryOrigin.Direct;

    /// <summary>
    /// Validates the diff entry
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Key))
            errors.AddError("Key", "Key is required");

        if (ChangeType == ChangeType.Modified && (string.IsNullOrEmpty(OldValue) || string.IsNullOrEmpty(NewValue)))
            errors.AddError("Values", "Both old and new values are required for modified entries");

        if (ChangeType == ChangeType.Added && string.IsNullOrEmpty(NewValue))
            errors.AddError("NewValue", "New value is required for added entries");

        if (ChangeType == ChangeType.Deleted && string.IsNullOrEmpty(OldValue))
            errors.AddError("OldValue", "Old value is required for deleted entries");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Diff entry validation failed", errors);
    }

    /// <summary>
    /// Gets a display-friendly representation of the change
    /// </summary>
    public string GetChangeDescription()
    {
        return ChangeType switch
        {
            ChangeType.Added => $"Added: {Key} = {NewValue}",
            ChangeType.Modified => $"Modified: {Key} ('{OldValue}' → '{NewValue}')",
            ChangeType.Deleted => $"Deleted: {Key}",
            _ => $"Unknown change: {Key}"
        };
    }
}

/// <summary>
/// Summary view of a configuration diff
/// </summary>
sealed public class ConfigurationDiffSummary
{
    public Guid Id { get; set; }
    public int TotalChanges { get; set; }
    public int AddedCount { get; set; }
    public int ModifiedCount { get; set; }
    public int DeletedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
