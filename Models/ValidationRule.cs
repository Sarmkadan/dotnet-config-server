#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Models;

/// <summary>
/// Represents the supported validation rule types.
/// </summary>
public enum ValidationRuleType
{
    Required = 0,
    Regex = 1,
    MinLength = 2,
    MaxLength = 3,
    AllowedValues = 4,
    NumericRange = 5,
    Url = 6,
    Json = 7,
    CrossKey = 8
}

/// <summary>
/// Represents a reusable configuration validation rule.
/// </summary>
public sealed class ValidationRule
{
    /// <summary>Gets or sets the rule identifier.</summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the rule name.</summary>
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional rule description.</summary>
    [StringLength(1024)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration identifier, or <see langword="null"/> for a global rule.</summary>
    public Guid? ConfigurationId { get; set; }

    /// <summary>Gets or sets the rule type.</summary>
    [Required]
    public ValidationRuleType RuleType { get; set; }

    /// <summary>Gets or sets serialized parameters for the rule.</summary>
    public string? Parameters { get; set; }

    /// <summary>Gets or sets whether the rule is active.</summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the creator identifier.</summary>
    [Required]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when the rule was created.</summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the rule was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the regex pattern that selects matching keys.</summary>
    public string? TargetKeyPattern { get; set; }
}

/// <summary>
/// Represents the result of validating a configuration against active rules.
/// </summary>
public sealed class ValidationRuleResult
{
    /// <summary>Gets or sets whether the configuration is valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets all validation violations.</summary>
    public List<ValidationViolation> Violations { get; set; } = new();
}

/// <summary>
/// Represents a single validation rule violation.
/// </summary>
public sealed class ValidationViolation
{
    /// <summary>Gets or sets the key name that violated the rule.</summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule name.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the violation message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule identifier.</summary>
    public Guid RuleId { get; set; }
}
