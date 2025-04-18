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
/// Represents an encryption key used for configuration encryption
/// </summary>
public sealed class EncryptionKey
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string KeyId { get; set; } = string.Empty;

    [Required]
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;

    [Required]
    public byte[] EncryptedKey { get; set; } = [];

    [Required]
    public byte[] Salt { get; set; } = [];

    [StringLength(1024)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RotatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsPrimary { get; set; } = false;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    public string? RotatedBy { get; set; }

    [Required]
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Checks if this key is still valid
    /// </summary>
    public bool IsValid()
    {
        return IsActive && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Checks if the key is near expiration (within 30 days)
    /// </summary>
    public bool IsNearExpiration()
    {
        var daysUntilExpiration = (ExpiresAt - DateTime.UtcNow).TotalDays;
        return daysUntilExpiration <= 30 && daysUntilExpiration > 0;
    }

    /// <summary>
    /// Marks the key as inactive
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the key
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Increments the usage counter
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
    }

    /// <summary>
    /// Marks the key as rotated
    /// </summary>
    public void MarkAsRotated(string rotatedBy)
    {
        RotatedAt = DateTime.UtcNow;
        RotatedBy = rotatedBy;
        IsPrimary = false;
    }

    /// <summary>
    /// Sets this key as the primary key
    /// </summary>
    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Validates the encryption key
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.AddError("Name", "Key name is required");

        if (string.IsNullOrWhiteSpace(KeyId))
            errors.AddError("KeyId", "Key ID is required");

        if (EncryptedKey?.Length == 0)
            errors.AddError("EncryptedKey", "Encrypted key data is required");

        if (Salt?.Length == 0)
            errors.AddError("Salt", "Salt is required");

        if (ExpiresAt <= DateTime.UtcNow)
            errors.AddError("ExpiresAt", "Expiration date must be in the future");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Encryption key validation failed", errors);
    }

    /// <summary>
    /// Gets a summary of the encryption key (without sensitive data)
    /// </summary>
    public EncryptionKeySummary GetSummary()
    {
        return new EncryptionKeySummary
        {
            Id = Id,
            Name = Name,
            KeyId = KeyId,
            Algorithm = Algorithm,
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            IsActive = IsActive,
            IsPrimary = IsPrimary,
            IsNearExpiration = IsNearExpiration(),
            UsageCount = UsageCount
        };
    }
}

/// <summary>
/// Summary view of an encryption key (safe for sharing)
/// </summary>
public sealed class EncryptionKeySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public EncryptionAlgorithm Algorithm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsNearExpiration { get; set; }
    public int UsageCount { get; set; }
}
