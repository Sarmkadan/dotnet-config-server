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
/// Represents a single key-value pair within a configuration
/// </summary>
sealed public class ConfigurationKey
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string? DefaultValue { get; set; }

    [StringLength(1024)]
    public string? Description { get; set; }

    [Required]
    public ConfigurationValueType ValueType { get; set; } = ConfigurationValueType.String;

    [Required]
    public Guid ConfigurationId { get; set; }

    [Required]
    public Guid VersionId { get; set; }

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
    public bool IsEncrypted { get; set; } = false;

    [Required]
    public bool IsRequired { get; set; } = false;

    [Required]
    public bool IsSensitive { get; set; } = false;

    public string? ValidationRegex { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinLength { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Validates the configuration key
    /// </summary>
    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(Key))
            errors.AddError("Key", "Key is required");

        if (!System.Text.RegularExpressions.Regex.IsMatch(Key, AppConstants.Validation.KeyPattern))
            errors.AddError("Key", "Key can only contain alphanumeric characters, underscores, hyphens, and dots");

        if (Key?.Length > AppConstants.Configuration.MaxKeyLength)
            errors.AddError("Key", $"Key cannot exceed {AppConstants.Configuration.MaxKeyLength} characters");

        if (string.IsNullOrEmpty(Value) && !IsRequired)
            errors.AddError("Value", "Value is required");

        if (Value?.Length > AppConstants.Configuration.MaxValueLength)
            errors.AddError("Value", $"Value cannot exceed {AppConstants.Configuration.MaxValueLength} characters");

        if (ValidationRegex is not null)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(ValidationRegex);
                if (!regex.IsMatch(Value))
                    errors.AddError("Value", "Value does not match the validation pattern");
            }
            catch (Exception ex)
            {
                errors.AddError("ValidationRegex", $"Invalid regex pattern: {ex.Message}");
            }
        }

        if (MinLength.HasValue && Value?.Length < MinLength.Value)
            errors.AddError("Value", $"Value length must be at least {MinLength} characters");

        if (MaxLength.HasValue && Value?.Length > MaxLength.Value)
            errors.AddError("Value", $"Value length cannot exceed {MaxLength} characters");

        if (errors.Count > 0)
            throw new Exceptions.ValidationException("Configuration key validation failed", errors);
    }

    /// <summary>
    /// Attempts to parse the value as the specified type
    /// </summary>
    public object? GetTypedValue()
    {
        try
        {
            return ValueType switch
            {
                ConfigurationValueType.String => Value,
                ConfigurationValueType.Integer => int.Parse(Value),
                ConfigurationValueType.Boolean => bool.Parse(Value),
                ConfigurationValueType.Json => System.Text.Json.JsonSerializer.Deserialize<object>(Value),
                ConfigurationValueType.Decimal => decimal.Parse(Value),
                ConfigurationValueType.DateTime => DateTime.Parse(Value),
                _ => Value
            };
        }
        catch (Exception ex)
        {
            throw new Exceptions.ConfigurationException(
                $"Failed to parse value '{Value}' as {ValueType}: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the key's value and metadata
    /// </summary>
    public void Update(string value, string? description, string updatedBy)
    {
        Value = value;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the key as deleted (soft delete)
    /// </summary>
    public void Delete()
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a summary representation of this key
    /// </summary>
    public ConfigurationKeySummary GetSummary()
    {
        return new ConfigurationKeySummary
        {
            Id = Id,
            Key = Key,
            ValueType = ValueType,
            IsEncrypted = IsEncrypted,
            IsSensitive = IsSensitive,
            IsRequired = IsRequired,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}

/// <summary>
/// Summary view of a configuration key
/// </summary>
sealed public class ConfigurationKeySummary
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public ConfigurationValueType ValueType { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
