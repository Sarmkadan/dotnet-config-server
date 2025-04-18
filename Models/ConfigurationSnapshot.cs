#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a snapshot of a configuration's state at a specific point in time.
/// Includes serialized configuration data and associated keys.
/// </summary>
public sealed class ConfigurationSnapshot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Serialized JSON string of the Configuration object at the time of snapshot.
    /// </summary>
    [Required]
    public string ConfigurationState { get; set; } = string.Empty;

    /// <summary>
    /// Serialized JSON string of the list of ConfigurationKey objects at the time of snapshot.
    /// </summary>
    [Required]
    public string KeysState { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Reason { get; set; }

    /// <summary>
    /// Navigation property to the Configuration this snapshot belongs to.
    /// </summary>
    [JsonIgnore]
    public Configuration? Configuration { get; set; }
}
