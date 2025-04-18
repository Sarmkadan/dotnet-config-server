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
/// Represents an application that can have configurations
/// </summary>
public sealed class Application
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Description { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string? SecretKey { get; set; }

    [Required]
    public int ConfigurationCount { get; set; } = 0;

    public string? WebhookUrl { get; set; }

    [Required]
    public bool EnableAutoReload { get; set; } = false;

    [Required]
    public int MaxVersionHistory { get; set; } = AppConstants.Versioning.MaxVersionHistory;

    public DateTime? LastAccessedAt { get; set; }

    public List<Configuration> Configurations { get; set; } = new();

    /// <summary>
    /// Validates the application
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.AddError("Name", "Application name is required");

        if (Name?.Length > 256)
            errors.AddError("Name", "Application name cannot exceed 256 characters");

        if (string.IsNullOrWhiteSpace(Slug))
            errors.AddError("Slug", "Application slug is required");

        if (!System.Text.RegularExpressions.Regex.IsMatch(Slug ?? "", @"^[a-z0-9\-]+$"))
            errors.AddError("Slug", "Slug can only contain lowercase letters, numbers, and hyphens");

        if (Slug?.Length > 256)
            errors.AddError("Slug", "Slug cannot exceed 256 characters");

        if (Description?.Length > 1024)
            errors.AddError("Description", "Description cannot exceed 1024 characters");

        if (string.IsNullOrWhiteSpace(ApiKey))
            errors.AddError("ApiKey", "API key is required");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Application validation failed", errors);
    }

    /// <summary>
    /// Generates a new API key
    /// </summary>
    public void GenerateNewApiKey()
    {
        ApiKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Generates a new secret key
    /// </summary>
    public void GenerateNewSecretKey()
    {
        SecretKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Updates last accessed timestamp
    /// </summary>
    public void UpdateLastAccess()
    {
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the application
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        UpdatedBy = deactivatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the application
    /// </summary>
    public void Activate(string activatedBy)
    {
        IsActive = true;
        UpdatedBy = activatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a summary of the application
    /// </summary>
    public ApplicationSummary GetSummary()
    {
        return new ApplicationSummary
        {
            Id = Id,
            Name = Name,
            Slug = Slug,
            IsActive = IsActive,
            ConfigurationCount = ConfigurationCount,
            CreatedAt = CreatedAt,
            LastAccessedAt = LastAccessedAt
        };
    }
}

/// <summary>
/// Summary view of an application
/// </summary>
public sealed class ApplicationSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ConfigurationCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
