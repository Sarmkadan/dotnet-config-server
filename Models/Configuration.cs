#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotnetConfigServer.Common;
using Environment = DotnetConfigServer.Common.Environment;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents a configuration profile that can contain multiple key-value pairs
/// </summary>
sealed public class Configuration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Description { get; set; }

    [Required]
    public Environment Environment { get; set; } = Environment.Development;

    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }

    public string? DeletedBy { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsEncrypted { get; set; } = false;

    [Required]
    public EncryptionAlgorithm EncryptionAlgorithm { get; set; } = EncryptionAlgorithm.None;

    public string? EncryptionKeyId { get; set; }

    [Required]
    public int VersionNumber { get; set; } = 1;

    public Guid? CurrentVersionId { get; set; }

    public Guid? ParentConfigurationId { get; set; } // Added for hierarchical inheritance


    /// <summary>
    /// Validates the configuration data
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.AddError("Name", "Configuration name is required");

        if (Name?.Length > AppConstants.Configuration.MaxKeyLength)
            errors.AddError("Name", $"Configuration name cannot exceed {AppConstants.Configuration.MaxKeyLength} characters");

        if (Description?.Length > AppConstants.Configuration.MaxDescriptionLength)
            errors.AddError("Description", $"Description cannot exceed {AppConstants.Configuration.MaxDescriptionLength} characters");

        if (ApplicationId == Guid.Empty)
            errors.AddError("ApplicationId", "Application ID is required");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Configuration validation failed", errors);
    }

    /// <summary>
    /// Creates a copy of this configuration with updated timestamp and version
    /// </summary>
    public Configuration CreateNewVersion()
    {
        return new Configuration
        {
            Id = Guid.NewGuid(),
            Name = this.Name,
            Description = this.Description,
            Environment = this.Environment,
            ApplicationId = this.ApplicationId,
            VersionNumber = this.VersionNumber + 1,
            IsEncrypted = this.IsEncrypted,
            EncryptionAlgorithm = this.EncryptionAlgorithm,
            EncryptionKeyId = this.EncryptionKeyId,
            CreatedBy = this.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks configuration as deleted (soft delete)
    /// </summary>
    public void Delete(string deletedBy)
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Updates the configuration metadata
    /// </summary>
    public void Update(string name, string? description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets encryption settings for this configuration
    /// </summary>
    public void SetEncryption(EncryptionAlgorithm algorithm, string? keyId)
    {
        EncryptionAlgorithm = algorithm;
        EncryptionKeyId = keyId;
        IsEncrypted = algorithm != EncryptionAlgorithm.None;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a summary of the configuration
    /// </summary>
    public ConfigurationSummary GetSummary()
    {
        return new ConfigurationSummary
        {
            Id = Id,
            Name = Name,
            Environment = Environment,
            VersionNumber = VersionNumber,
            IsEncrypted = IsEncrypted,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}

/// <summary>
/// Summary view of a configuration
/// </summary>
sealed public class ConfigurationSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Environment Environment { get; set; }
    public int VersionNumber { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Helper extension methods for validation error handling
/// </summary>
internal static class ValidationErrorExtensions
{
    internal static void AddError(this Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new();
        errors[field].Add(message);
    }
}
