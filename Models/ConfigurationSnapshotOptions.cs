#nullable enable

using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Models;

/// <summary>
/// Configuration options for automatic snapshot creation
/// </summary>
public sealed class ConfigurationSnapshotOptions
{
    /// <summary>
    /// Gets or sets whether automatic snapshots are enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the interval in minutes between automatic snapshots
    /// </summary>
    [Range(5, 1440, ErrorMessage = "Snapshot interval must be between 5 and 1440 minutes (24 hours)")]
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the default user ID to use when creating automatic snapshots
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DefaultUserId { get; set; } = "system";

    /// <summary>
    /// Gets or sets the default reason for automatic snapshots
    /// </summary>
    [StringLength(1024)]
    public string DefaultReason { get; set; } = "Automatic scheduled snapshot";

    /// <summary>
    /// Gets or sets whether to skip snapshots when nothing has changed since the last snapshot
    /// </summary>
    public bool SkipIfUnchanged { get; set; } = true;
}