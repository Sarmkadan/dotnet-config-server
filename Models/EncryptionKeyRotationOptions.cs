#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Models;

/// <summary>
/// Configuration options for the background job that migrates stored ciphertext
/// onto the newest active encryption key of each configuration.
/// </summary>
public sealed class EncryptionKeyRotationOptions
{
    /// <summary>
    /// Gets or sets whether the automatic re-encryption sweep is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the interval in minutes between re-encryption sweeps.
    /// </summary>
    [Range(5, 1440, ErrorMessage = "Rotation sweep interval must be between 5 and 1440 minutes (24 hours)")]
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the identifier recorded as the actor performing automatic re-encryption.
    /// </summary>
    [Required]
    [StringLength(256)]
    public string DefaultUserId { get; set; } = "system";
}
