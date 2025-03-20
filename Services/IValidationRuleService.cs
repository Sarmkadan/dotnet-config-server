#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for managing and executing configuration validation rules.
/// </summary>
public interface IValidationRuleService
{
    /// <summary>Creates a validation rule.</summary>
    Task<ValidationRule> CreateRuleAsync(ValidationRule rule, string userId);

    /// <summary>Gets a validation rule by identifier.</summary>
    Task<ValidationRule?> GetRuleAsync(Guid ruleId);

    /// <summary>Gets configuration-scoped validation rules.</summary>
    Task<List<ValidationRule>> GetRulesAsync(Guid configurationId);

    /// <summary>Updates a validation rule.</summary>
    Task<ValidationRule> UpdateRuleAsync(Guid ruleId, ValidationRule updated, string userId);

    /// <summary>Deletes a validation rule.</summary>
    Task DeleteRuleAsync(Guid ruleId);

    /// <summary>Validates a configuration using applicable validation rules.</summary>
    Task<ValidationRuleResult> ValidateConfigurationAsync(Guid configurationId, Guid? versionId, CancellationToken ct = default);
}
