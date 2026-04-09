#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.RegularExpressions;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

namespace DotnetConfigServer.Services;

/// <summary>
/// Manages validation rules and validates configuration values against them.
/// </summary>
sealed public class ValidationRuleService : IValidationRuleService
{
    private readonly IValidationRuleRepository _validationRuleRepository;
    private readonly IConfigurationService _configurationService;
    private readonly IVersioningService _versioningService;
    private readonly ILogger<ValidationRuleService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleService"/>.
    /// </summary>
    public ValidationRuleService(
        IValidationRuleRepository validationRuleRepository,
        IConfigurationService configurationService,
        IVersioningService versioningService,
        ILogger<ValidationRuleService> logger)
    {
        _validationRuleRepository = validationRuleRepository;
        _configurationService = configurationService;
        _versioningService = versioningService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidationRule> CreateRuleAsync(ValidationRule rule, string userId)
    {
        ValidateRuleDefinition(rule);

        rule.Id = rule.Id == Guid.Empty ? Guid.NewGuid() : rule.Id;
        rule.CreatedBy = userId;
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = rule.CreatedAt;

        await _validationRuleRepository.AddAsync(rule);
        await _validationRuleRepository.SaveChangesAsync();

        _logger.LogInformation("Validation rule {RuleId} created by {UserId}", rule.Id, userId);
        return rule;
    }

    /// <inheritdoc />
    public async Task<ValidationRule?> GetRuleAsync(Guid ruleId)
    {
        return await _validationRuleRepository.GetByIdAsync(ruleId);
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetRulesAsync(Guid configurationId)
    {
        return await _validationRuleRepository.GetByConfigurationAsync(configurationId);
    }

    /// <inheritdoc />
    public async Task<ValidationRule> UpdateRuleAsync(Guid ruleId, ValidationRule updated, string userId)
    {
        var existing = await _validationRuleRepository.GetByIdAsync(ruleId);
        if (existing is null)
            throw new ConfigurationNotFoundException(ruleId.ToString());

        existing.Name = updated.Name;
        existing.Description = updated.Description;
        existing.RuleType = updated.RuleType;
        existing.Parameters = updated.Parameters;
        existing.IsActive = updated.IsActive;
        existing.TargetKeyPattern = updated.TargetKeyPattern;
        existing.UpdatedAt = DateTime.UtcNow;

        ValidateRuleDefinition(existing);

        await _validationRuleRepository.UpdateAsync(existing);
        await _validationRuleRepository.SaveChangesAsync();

        _logger.LogInformation("Validation rule {RuleId} updated by {UserId}", ruleId, userId);
        return existing;
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(Guid ruleId)
    {
        var existing = await _validationRuleRepository.GetByIdAsync(ruleId);
        if (existing is null)
            throw new ConfigurationNotFoundException(ruleId.ToString());

        await _validationRuleRepository.DeleteAsync(existing);
        await _validationRuleRepository.SaveChangesAsync();

        _logger.LogInformation("Validation rule {RuleId} deleted", ruleId);
    }

    /// <inheritdoc />
    public async Task<ValidationRuleResult> ValidateConfigurationAsync(Guid configurationId, Guid? versionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var effectiveVersionId = versionId;
        if (!effectiveVersionId.HasValue)
        {
            var activeVersion = await _versioningService.GetActiveVersionAsync(configurationId);
            effectiveVersionId = activeVersion?.Id;
        }

        var keys = await _configurationService.GetKeysAsync(configurationId, effectiveVersionId, true);
        var rules = await _validationRuleRepository.GetApplicableRulesAsync(configurationId);
        var violations = new List<ValidationViolation>();

        foreach (var rule in rules)
        {
            ct.ThrowIfCancellationRequested();
            var matchingKeys = GetMatchingKeys(keys, rule).ToList();
            violations.AddRange(EvaluateRule(rule, matchingKeys, keys));
        }

        return new ValidationRuleResult
        {
            IsValid = violations.Count == 0,
            Violations = violations
        };
    }

    private static IEnumerable<ConfigurationKey> GetMatchingKeys(IEnumerable<ConfigurationKey> keys, ValidationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.TargetKeyPattern))
            return keys;

        var regex = new Regex(rule.TargetKeyPattern, RegexOptions.Compiled);
        return keys.Where(key => regex.IsMatch(key.Key));
    }

    private static List<ValidationViolation> EvaluateRule(
        ValidationRule rule,
        List<ConfigurationKey> matchingKeys,
        List<ConfigurationKey> allKeys)
    {
        return rule.RuleType switch
        {
            ValidationRuleType.Required => EvaluateRequiredRule(rule, matchingKeys),
            ValidationRuleType.Regex => EvaluateValueRule(rule, matchingKeys, value => Regex.IsMatch(value, rule.Parameters ?? string.Empty),
                $"Value must match regex pattern '{rule.Parameters}'."),
            ValidationRuleType.MinLength => EvaluateValueRule(rule, matchingKeys,
                value => value.Length >= int.Parse(rule.Parameters ?? "0"),
                $"Value must be at least {rule.Parameters} characters long."),
            ValidationRuleType.MaxLength => EvaluateValueRule(rule, matchingKeys,
                value => value.Length <= int.Parse(rule.Parameters ?? "0"),
                $"Value must be at most {rule.Parameters} characters long."),
            ValidationRuleType.AllowedValues => EvaluateAllowedValuesRule(rule, matchingKeys),
            ValidationRuleType.NumericRange => EvaluateNumericRangeRule(rule, matchingKeys),
            ValidationRuleType.Url => EvaluateValueRule(rule, matchingKeys,
                value => Uri.TryCreate(value, UriKind.Absolute, out _),
                "Value must be a well-formed absolute URL."),
            ValidationRuleType.Json => EvaluateJsonRule(rule, matchingKeys),
            ValidationRuleType.CrossKey => EvaluateCrossKeyRule(rule, matchingKeys, allKeys),
            _ => new List<ValidationViolation>()
        };
    }

    private static List<ValidationViolation> EvaluateRequiredRule(ValidationRule rule, List<ConfigurationKey> matchingKeys)
    {
        var violations = new List<ValidationViolation>();
        if (matchingKeys.Count == 0)
        {
            violations.Add(CreateViolation(rule, rule.TargetKeyPattern ?? "*", "Required key is missing."));
            return violations;
        }

        foreach (var key in matchingKeys.Where(key => string.IsNullOrWhiteSpace(key.Value)))
            violations.Add(CreateViolation(rule, key.Key, "Value is required."));

        return violations;
    }

    private static List<ValidationViolation> EvaluateValueRule(
        ValidationRule rule,
        List<ConfigurationKey> matchingKeys,
        Func<string, bool> isValid,
        string message)
    {
        return matchingKeys
            .Where(key => !isValid(key.Value))
            .Select(key => CreateViolation(rule, key.Key, message))
            .ToList();
    }

    private static List<ValidationViolation> EvaluateAllowedValuesRule(ValidationRule rule, List<ConfigurationKey> matchingKeys)
    {
        var allowedValues = (rule.Parameters ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return matchingKeys
            .Where(key => !allowedValues.Contains(key.Value, StringComparer.OrdinalIgnoreCase))
            .Select(key => CreateViolation(rule, key.Key, $"Value must be one of: {string.Join(", ", allowedValues)}."))
            .ToList();
    }

    private static List<ValidationViolation> EvaluateNumericRangeRule(ValidationRule rule, List<ConfigurationKey> matchingKeys)
    {
        var parameters = JsonSerializer.Deserialize<NumericRangeParameters>(rule.Parameters ?? "{}") ?? new NumericRangeParameters();
        var violations = new List<ValidationViolation>();

        foreach (var key in matchingKeys)
        {
            if (!decimal.TryParse(key.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                violations.Add(CreateViolation(rule, key.Key, "Value must be numeric."));
                continue;
            }

            if (parameters.Min.HasValue && value < parameters.Min.Value)
                violations.Add(CreateViolation(rule, key.Key, $"Value must be greater than or equal to {parameters.Min}."));

            if (parameters.Max.HasValue && value > parameters.Max.Value)
                violations.Add(CreateViolation(rule, key.Key, $"Value must be less than or equal to {parameters.Max}."));
        }

        return violations;
    }

    private static List<ValidationViolation> EvaluateJsonRule(ValidationRule rule, List<ConfigurationKey> matchingKeys)
    {
        var violations = new List<ValidationViolation>();
        foreach (var key in matchingKeys)
        {
            try
            {
                JsonDocument.Parse(key.Value);
            }
            catch (JsonException)
            {
                violations.Add(CreateViolation(rule, key.Key, "Value must be valid JSON."));
            }
        }

        return violations;
    }

    private static List<ValidationViolation> EvaluateCrossKeyRule(
        ValidationRule rule,
        List<ConfigurationKey> matchingKeys,
        List<ConfigurationKey> allKeys)
    {
        var parameters = JsonSerializer.Deserialize<CrossKeyParameters>(rule.Parameters ?? "{}") ?? new CrossKeyParameters();
        if (string.IsNullOrWhiteSpace(parameters.OtherKey))
            return new List<ValidationViolation>();

        var otherKey = allKeys.FirstOrDefault(key => string.Equals(key.Key, parameters.OtherKey, StringComparison.OrdinalIgnoreCase));
        if (otherKey is null)
            return new List<ValidationViolation>();

        return matchingKeys
            .Where(key => !IsCrossKeyValid(key.Value, otherKey.Value, parameters.Operator))
            .Select(key => CreateViolation(rule, key.Key, parameters.Message ?? $"Cross-key validation failed against '{parameters.OtherKey}'."))
            .ToList();
    }

    private static bool IsCrossKeyValid(string currentValue, string otherValue, string? comparisonOperator)
    {
        return comparisonOperator?.ToLowerInvariant() switch
        {
            "equals" => string.Equals(currentValue, otherValue, StringComparison.OrdinalIgnoreCase),
            "notequals" => !string.Equals(currentValue, otherValue, StringComparison.OrdinalIgnoreCase),
            "requiresifpresent" => !string.IsNullOrWhiteSpace(otherValue) ? !string.IsNullOrWhiteSpace(currentValue) : true,
            _ => true
        };
    }

    private static ValidationViolation CreateViolation(ValidationRule rule, string keyName, string message)
    {
        return new ValidationViolation
        {
            KeyName = keyName,
            RuleName = rule.Name,
            Message = message,
            RuleId = rule.Id
        };
    }

    private static void ValidateRuleDefinition(ValidationRule rule)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(rule.Name))
            errors["Name"] = new List<string> { "Rule name is required." };

        if (!string.IsNullOrWhiteSpace(rule.TargetKeyPattern))
        {
            try
            {
                _ = new Regex(rule.TargetKeyPattern);
            }
            catch (ArgumentException ex)
            {
                errors["TargetKeyPattern"] = new List<string> { $"Invalid target key pattern: {ex.Message}" };
            }
        }

        switch (rule.RuleType)
        {
            case ValidationRuleType.Regex:
                ValidateRegexParameters(rule, errors);
                break;
            case ValidationRuleType.MinLength:
            case ValidationRuleType.MaxLength:
                ValidateIntegerParameters(rule, errors);
                break;
            case ValidationRuleType.AllowedValues:
                if (string.IsNullOrWhiteSpace(rule.Parameters))
                    errors["Parameters"] = new List<string> { "AllowedValues rule requires a comma-separated parameter list." };
                break;
            case ValidationRuleType.NumericRange:
                ValidateNumericRangeParameters(rule, errors);
                break;
            case ValidationRuleType.CrossKey:
                ValidateCrossKeyParameters(rule, errors);
                break;
        }

        if (errors.Count > 0)
            throw new ValidationException("Validation rule definition is invalid", errors);
    }

    private static void ValidateRegexParameters(ValidationRule rule, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(rule.Parameters))
        {
            errors["Parameters"] = new List<string> { "Regex rule requires a regex pattern." };
            return;
        }

        try
        {
            _ = new Regex(rule.Parameters);
        }
        catch (ArgumentException ex)
        {
            errors["Parameters"] = new List<string> { $"Invalid regex pattern: {ex.Message}" };
        }
    }

    private static void ValidateIntegerParameters(ValidationRule rule, Dictionary<string, List<string>> errors)
    {
        if (!int.TryParse(rule.Parameters, out _))
            errors["Parameters"] = new List<string> { "Length rule requires an integer parameter." };
    }

    private static void ValidateNumericRangeParameters(ValidationRule rule, Dictionary<string, List<string>> errors)
    {
        try
        {
            _ = JsonSerializer.Deserialize<NumericRangeParameters>(rule.Parameters ?? "{}");
        }
        catch (JsonException ex)
        {
            errors["Parameters"] = new List<string> { $"Invalid numeric range parameters: {ex.Message}" };
        }
    }

    private static void ValidateCrossKeyParameters(ValidationRule rule, Dictionary<string, List<string>> errors)
    {
        try
        {
            var parameters = JsonSerializer.Deserialize<CrossKeyParameters>(rule.Parameters ?? "{}") ?? new CrossKeyParameters();
            if (string.IsNullOrWhiteSpace(parameters.OtherKey))
                errors["Parameters"] = new List<string> { "CrossKey rule requires an otherKey parameter." };
        }
        catch (JsonException ex)
        {
            errors["Parameters"] = new List<string> { $"Invalid cross-key parameters: {ex.Message}" };
        }
    }

    private sealed class NumericRangeParameters
    {
        public decimal? Min { get; set; }

        public decimal? Max { get; set; }
    }

    private sealed class CrossKeyParameters
    {
        public string? OtherKey { get; set; }

        public string? Operator { get; set; }

        public string? Message { get; set; }
    }
}
