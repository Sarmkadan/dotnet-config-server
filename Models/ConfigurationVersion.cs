#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a version of a configuration with associated keys
/// </summary>
sealed public class ConfigurationVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ConfigurationId { get; set; }

    [Required]
    [StringLength(50)]
    public string VersionNumber { get; set; } = "1.0.0";

    [Required]
    public ConfigurationVersionStatus Status { get; set; } = ConfigurationVersionStatus.Draft;

    [StringLength(1024)]
    public string? ReleaseNotes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public string? PublishedBy { get; set; }

    public string? ArchivedBy { get; set; }

    [Required]
    public int KeyCount { get; set; } = 0;

    [Required]
    public bool HasEncryptedKeys { get; set; } = false;

    public string? PreviousVersionId { get; set; }

    public string? ChangesSummary { get; set; }

    public List<ConfigurationKey> Keys { get; set; } = new();

    /// <summary>
    /// Publishes this version, making it the active version
    /// </summary>
    public void Publish(string publishedBy)
    {
        if (Status == ConfigurationVersionStatus.Active)
            throw new Exceptions.ConfigurationException(
                "This version is already published", "VERSION_ALREADY_ACTIVE");

        Status = ConfigurationVersionStatus.Active;
        PublishedAt = DateTime.UtcNow;
        PublishedBy = publishedBy;
    }

    /// <summary>
    /// Archives this version
    /// </summary>
    public void Archive(string archivedBy)
    {
        if (Status == ConfigurationVersionStatus.Archived)
            throw new Exceptions.ConfigurationException(
                "This version is already archived", "VERSION_ALREADY_ARCHIVED");

        Status = ConfigurationVersionStatus.Archived;
        ArchivedAt = DateTime.UtcNow;
        ArchivedBy = archivedBy;
    }

    /// <summary>
    /// Marks version as deprecated
    /// </summary>
    public void Deprecate()
    {
        if (Status != ConfigurationVersionStatus.Active)
            throw new Exceptions.ConfigurationException(
                "Only active versions can be deprecated", "INVALID_VERSION_STATUS");

        Status = ConfigurationVersionStatus.Deprecated;
    }

    /// <summary>
    /// Calculates the current version number by incrementing major, minor, or patch
    /// </summary>
    public static string IncrementVersion(string currentVersion, VersionIncrementType incrementType)
    {
        var parts = currentVersion.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor) || !int.TryParse(parts[2], out var patch))
        {
            throw new Exceptions.ValidationException(
                "VersionNumber", "Invalid version format. Expected format: major.minor.patch");
        }

        return incrementType switch
        {
            VersionIncrementType.Major => $"{major + 1}.0.0",
            VersionIncrementType.Minor => $"{major}.{minor + 1}.0",
            VersionIncrementType.Patch => $"{major}.{minor}.{patch + 1}",
            _ => currentVersion
        };
    }

    /// <summary>
    /// Validates the configuration version
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(VersionNumber))
            errors.AddError("VersionNumber", "Version number is required");

        var versionParts = VersionNumber?.Split('.');
        if (versionParts?.Length != 3 ||
            !int.TryParse(versionParts[0], out _) ||
            !int.TryParse(versionParts[1], out _) ||
            !int.TryParse(versionParts[2], out _))
        {
            errors.AddError("VersionNumber", "Version number must be in format: major.minor.patch");
        }

        if (ReleaseNotes?.Length > 1024)
            errors.AddError("ReleaseNotes", "Release notes cannot exceed 1024 characters");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Configuration version validation failed", errors);
    }

    /// <summary>
    /// Gets a summary of this version
    /// </summary>
    public ConfigurationVersionSummary GetSummary()
    {
        return new ConfigurationVersionSummary
        {
            Id = Id,
            VersionNumber = VersionNumber,
            Status = Status,
            KeyCount = KeyCount,
            CreatedAt = CreatedAt,
            PublishedAt = PublishedAt,
            CreatedBy = CreatedBy
        };
    }
}

/// <summary>
/// Summary view of a configuration version
/// </summary>
sealed public class ConfigurationVersionSummary
{
    public Guid Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public ConfigurationVersionStatus Status { get; set; }
    public int KeyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Specifies which part of the version to increment
/// </summary>
public enum VersionIncrementType
{
    Major,
    Minor,
    Patch
}
