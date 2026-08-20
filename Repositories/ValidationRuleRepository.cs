#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for <see cref="ValidationRule"/> entities.
/// </summary>
public sealed class ValidationRuleRepository : BaseRepository<ValidationRule>, IValidationRuleRepository
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleRepository"/>.
    /// </summary>
    public ValidationRuleRepository(ApplicationDbContext context, ILogger<ValidationRuleRepository> logger)
        : base(context, logger)
    {
        _logger.LogInformation("ValidationRuleRepository initialized");
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetByConfigurationAsync(Guid configurationId)
    {
        _logger.LogInformation("GetByConfigurationAsync called with {ConfigurationId}", configurationId);
        return await _dbSet
            .Where(rule => rule.ConfigurationId == configurationId && rule.IsActive)
            .OrderBy(rule => rule.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetGlobalRulesAsync()
    {
        _logger.LogInformation("GetGlobalRulesAsync called");
        return await _dbSet
            .Where(rule => rule.ConfigurationId == null && rule.IsActive)
            .OrderBy(rule => rule.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetApplicableRulesAsync(Guid configurationId)
    {
        try
        {
            _logger.LogInformation("GetApplicableRulesAsync called with {ConfigurationId}", configurationId);
            return await _dbSet
                .Where(rule => rule.IsActive && (rule.ConfigurationId == null || rule.ConfigurationId == configurationId))
                .OrderBy(rule => rule.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applicable rules");
            throw;
        }
    }
}

/// <summary>
/// Extension methods for <see cref="ValidationRule"/>.
/// </summary>
public static class ValidationRuleExtensions
{
    /// <summary>
    /// Determines whether the rule applies to the supplied configuration key.
    /// If <see cref="ValidationRule.TargetKeyPattern"/> is <c>null</c> or empty,
    /// the rule is considered applicable to all keys.
    /// </summary>
    /// <param name="rule">The validation rule.</param>
    /// <param name="key">The configuration key to test.</param>
    /// <returns><c>true</c> if the rule applies to the key; otherwise <c>false</c>.</returns>
    public static bool AppliesTo(this ValidationRule rule, string key)
    {
        if (string.IsNullOrEmpty(rule.TargetKeyPattern))
            return true;

        return Regex.IsMatch(key, rule.TargetKeyPattern);
    }

    /// <summary>
    /// Provides a human‑readable description of the rule.
    /// Includes the rule name, type, activity status and, when present,
    /// the target‑key pattern.
    /// </summary>
    /// <param name="rule">The validation rule.</param>
    /// <returns>A description string.</returns>
    public static string Describe(this ValidationRule rule)
    {
        var parts = new List<string>
        {
            $"{rule.Name} ({rule.RuleType})",
            rule.IsActive ? "Active" : "Inactive"
        };

        if (!string.IsNullOrWhiteSpace(rule.TargetKeyPattern))
            parts.Add($"Pattern: {rule.TargetKeyPattern}");

        return string.Join(" - ", parts);
    }

    /// <summary>
    /// Determines whether this rule is stricter than another rule.
    /// The comparison is simplified:
    /// <list type="bullet">
    ///   <item>Rules of different <see cref="ValidationRuleType"/> are never considered stricter.</item>
    ///   <item>For rules of the same type, a higher <see cref="ValidationRuleType"/> enum value is treated as stricter.</item>
    /// </list>
    /// This heuristic provides a deterministic result without needing to parse the
    /// <see cref="ValidationRule.Parameters"/> JSON payload.
    /// </summary>
    /// <param name="rule">The rule to evaluate.</param>
    /// <param name="other">The rule to compare against.</param>
    /// <returns><c>true</c> if <paramref name="rule"/> is stricter than <paramref name="other"/>; otherwise <c>false</c>.</returns>
    public static bool IsStricterThan(this ValidationRule rule, ValidationRule other)
    {
        if (rule.RuleType != other.RuleType)
            return false;

        // Simple heuristic: a rule with a higher enum value is considered stricter.
        // This works because the enum is ordered from least to most restrictive.
        return rule.RuleType > other.RuleType;
    }
}