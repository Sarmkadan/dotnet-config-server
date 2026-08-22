#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for <see cref="ValidationRuleService"/> that verify
/// validation rule processing, rule creation, and overall validation behavior.
/// </summary>
public sealed class ValidationRuleServiceTests
{
    private readonly Mock<IValidationRuleRepository> _validationRuleRepositoryMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly Mock<IVersioningService> _versioningServiceMock;
    private readonly Mock<ILogger<ValidationRuleService>> _loggerMock;
    private readonly ValidationRuleService _sut;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleServiceTests"/> and
    /// sets up the required mock dependencies for the <see cref="ValidationRuleService"/>
    /// under test.
    /// </summary>
    public ValidationRuleServiceTests()
    {
        _validationRuleRepositoryMock = new Mock<IValidationRuleRepository>();
        _configurationServiceMock = new Mock<IConfigurationService>();
        _versioningServiceMock = new Mock<IVersioningService>();
        _loggerMock = new Mock<ILogger<ValidationRuleService>>();

        _sut = new ValidationRuleService(
            _validationRuleRepositoryMock.Object,
            _configurationServiceMock.Object,
            _versioningServiceMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that when a configuration contains a key that violates a
    /// regular‑expression validation rule, <see cref="ValidationRuleService.ValidateConfigurationAsync"/>
    /// returns a result indicating the configuration is invalid and includes the expected violation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidateConfigurationAsync_WithRegexRule_DetectsViolation()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        _loggerMock.Object.LogInformation(
            "Validating configuration {ConfigurationId} version {VersionId} against regex rule {RuleId}",
            configurationId,
            versionId,
            ruleId);
        var keys = new List<ConfigurationKey>
        {
            new()
            {
                Key = "ApiKey",
                Value = "invalid-key",
                ConfigurationId = configurationId,
                VersionId = versionId,
                CreatedBy = "admin",
                ValueType = ConfigurationValueType.String
            }
        };
        var rules = new List<ValidationRule>
        {
            new()
            {
                Id = ruleId,
                Name = "API key format",
                ConfigurationId = configurationId,
                RuleType = ValidationRuleType.Regex,
                Parameters = "^[A-Z]{3}-\\d{3}$",
                TargetKeyPattern = "^ApiKey$",
                CreatedBy = "admin"
            }
        };

        _versioningServiceMock.Setup(s => s.GetActiveVersionAsync(configurationId)).ReturnsAsync(new ConfigurationVersion
        {
            Id = versionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.0",
            CreatedBy = "admin"
        });
        _configurationServiceMock.Setup(s => s.GetKeysAsync(configurationId, versionId, true)).ReturnsAsync(keys);
        _validationRuleRepositoryMock.Setup(r => r.GetApplicableRulesAsync(configurationId)).ReturnsAsync(rules);

        var result = await _sut.ValidateConfigurationAsync(configurationId, null);

        _loggerMock.Object.LogWarning(
            "Configuration {ConfigurationId} violated regex rule {RuleId} for key {KeyName}",
            configurationId,
            ruleId,
            "ApiKey");
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle();
        result.Violations[0].RuleId.Should().Be(ruleId);
        result.Violations[0].KeyName.Should().Be("ApiKey");
        _loggerMock.Object.LogInformation(
            "Completed validation for configuration {ConfigurationId}: detected {ViolationCount} violation(s)",
            configurationId,
            result.Violations.Count);
    }

    /// <summary>
    /// Ensures that when all configuration keys satisfy the applicable validation rules,
    /// <see cref="ValidationRuleService.ValidateConfigurationAsync"/> reports the configuration as valid
    /// and returns an empty collection of violations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidateConfigurationAsync_AllKeysValid_ReturnsNoViolations()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _loggerMock.Object.LogInformation(
            "Validating configuration {ConfigurationId} version {VersionId} expecting all keys to satisfy rules",
            configurationId,
            versionId);
        var keys = new List<ConfigurationKey>
        {
            new()
            {
                Key = "ApiKey",
                Value = "ABC-123",
                ConfigurationId = configurationId,
                VersionId = versionId,
                CreatedBy = "admin",
                ValueType = ConfigurationValueType.String
            }
        };
        var rules = new List<ValidationRule>
        {
            new()
            {
                Name = "API key format",
                ConfigurationId = configurationId,
                RuleType = ValidationRuleType.Regex,
                Parameters = "^[A-Z]{3}-\\d{3}$",
                TargetKeyPattern = "^ApiKey$",
                CreatedBy = "admin"
            }
        };

        _configurationServiceMock.Setup(s => s.GetKeysAsync(configurationId, versionId, true)).ReturnsAsync(keys);
        _validationRuleRepositoryMock.Setup(r => r.GetApplicableRulesAsync(configurationId)).ReturnsAsync(rules);

        var result = await _sut.ValidateConfigurationAsync(configurationId, versionId);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
        _loggerMock.Object.LogInformation(
            "Completed validation for configuration {ConfigurationId}: no violations detected",
            configurationId);
    }

    /// <summary>
    /// Confirms that creating a new <see cref="ValidationRule"/> via
    /// <see cref="ValidationRuleService.CreateRuleAsync"/> populates audit fields,
    /// persists the rule using the repository, and returns the created rule instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CreateRuleAsync_ValidRule_ReturnsCreatedRule()
    {
        var configurationId = Guid.NewGuid();
        var rule = new ValidationRule
        {
            Name = "URL validation",
            ConfigurationId = configurationId,
            RuleType = ValidationRuleType.Url,
            TargetKeyPattern = "^ServiceUrl$"
        };

        _loggerMock.Object.LogInformation(
            "Creating validation rule {RuleName} for configuration {ConfigurationId} by {CreatedBy}",
            rule.Name,
            configurationId,
            "admin");

        _validationRuleRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ValidationRule>())).Returns(Task.CompletedTask);
        _validationRuleRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateRuleAsync(rule, "admin");

        result.CreatedBy.Should().Be("admin");
        result.ConfigurationId.Should().Be(configurationId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _validationRuleRepositoryMock.Verify(r => r.AddAsync(It.Is<ValidationRule>(created => created.Name == "URL validation")), Times.Once);
        _validationRuleRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _loggerMock.Object.LogInformation(
            "Created validation rule {RuleName} for configuration {ConfigurationId}",
            result.Name,
            result.ConfigurationId);
    }

    /// <summary>
    /// Ensures that when a configuration key is missing and a required rule applies to it,
    /// <see cref="ValidationRuleService.ValidateConfigurationAsync"/> reports a violation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidateConfigurationAsync_MissingRequiredKey_ReturnsViolation()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        _loggerMock.Object.LogInformation(
            "Validating configuration {ConfigurationId} version {VersionId} expecting missing required key",
            configurationId,
            versionId);
        var keys = new List<ConfigurationKey>();
        var rules = new List<ValidationRule>
        {
            new()
            {
                Name = "Required API key",
                ConfigurationId = configurationId,
                RuleType = ValidationRuleType.Required,
                TargetKeyPattern = "^ApiKey$"
            }
        };

        _configurationServiceMock.Setup(s => s.GetKeysAsync(configurationId, versionId, true)).ReturnsAsync(keys);
        _validationRuleRepositoryMock.Setup(r => r.GetApplicableRulesAsync(configurationId)).ReturnsAsync(rules);

        var result = await _sut.ValidateConfigurationAsync(configurationId, versionId);

        _loggerMock.Object.LogWarning(
            "Configuration {ConfigurationId} is missing required key matching pattern {TargetKeyPattern}",
            configurationId,
            "^ApiKey$");
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle();
        result.Violations[0].KeyName.Should().Be("^ApiKey$");
        result.Violations[0].Message.Should().Be("Required key is missing.");
        _loggerMock.Object.LogInformation(
            "Completed validation for configuration {ConfigurationId}: missing required key reported",
            configurationId);
    }

    /// <summary>
    /// Verifies that when multiple rules apply to a configuration, all violations are collected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidateConfigurationAsync_MultipleRules_AggregatesViolations()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var keys = new List<ConfigurationKey>
        {
            new() { Key = "MinLengthKey", Value = "ab" },
            new() { Key = "MaxLengthKey", Value = "toolongvalue" }
        };
        var rules = new List<ValidationRule>
        {
            new()
            {
                Name = "Min length",
                ConfigurationId = configurationId,
                RuleType = ValidationRuleType.MinLength,
                Parameters = "5",
                TargetKeyPattern = "MinLengthKey"
            },
            new()
            {
                Name = "Max length",
                ConfigurationId = configurationId,
                RuleType = ValidationRuleType.MaxLength,
                Parameters = "10",
                TargetKeyPattern = "MaxLengthKey"
            }
        };

        _loggerMock.Object.LogInformation(
            "Validating configuration {ConfigurationId} version {VersionId} against {RuleCount} rules",
            configurationId,
            versionId,
            rules.Count);

        _configurationServiceMock.Setup(s => s.GetKeysAsync(configurationId, versionId, true)).ReturnsAsync(keys);
        _validationRuleRepositoryMock.Setup(r => r.GetApplicableRulesAsync(configurationId)).ReturnsAsync(rules);

        var result = await _sut.ValidateConfigurationAsync(configurationId, versionId);

        _loggerMock.Object.LogWarning(
            "Configuration {ConfigurationId} produced {ViolationCount} violations across multiple rules",
            configurationId,
            result.Violations.Count);
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCount(2);
        _loggerMock.Object.LogInformation(
            "Completed validation for configuration {ConfigurationId}: aggregated {ViolationCount} violations",
            configurationId,
            result.Violations.Count);
    }

    /// <summary>
    /// Confirms that updating an existing <see cref="ValidationRule"/> via
    /// <see cref="ValidationRuleService.UpdateRuleAsync"/> updates the rule fields and audit information.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task UpdateRuleAsync_ExistingRule_UpdatesFields()
    {
        var ruleId = Guid.NewGuid();
        var existingRule = new ValidationRule
        {
            Id = ruleId,
            Name = "Old name",
            ConfigurationId = Guid.NewGuid(),
            RuleType = ValidationRuleType.Regex,
            Parameters = "^old$",
            TargetKeyPattern = "^oldKey$",
            IsActive = true,
            CreatedBy = "admin"
        };
        var updatedRule = new ValidationRule
        {
            Id = ruleId,
            Name = "New name",
            ConfigurationId = existingRule.ConfigurationId,
            RuleType = ValidationRuleType.Url,
            Parameters = null,
            TargetKeyPattern = "^newKey$",
            IsActive = false
        };

        _loggerMock.Object.LogInformation(
            "Updating validation rule {RuleId} from {OldName} to {NewName}",
            ruleId,
            existingRule.Name,
            updatedRule.Name);

        _validationRuleRepositoryMock.Setup(r => r.GetByIdAsync(ruleId)).ReturnsAsync(existingRule);
        _validationRuleRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ValidationRule>())).Returns(Task.CompletedTask);
        _validationRuleRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.UpdateRuleAsync(ruleId, updatedRule, "admin");

        result.Name.Should().Be("New name");
        result.RuleType.Should().Be(ValidationRuleType.Url);
        result.Parameters.Should().BeNull();
        result.TargetKeyPattern.Should().Be("^newKey$");
        result.IsActive.Should().BeFalse();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _loggerMock.Object.LogInformation(
            "Updated validation rule {RuleId}; new type {RuleType}, active {IsActive}",
            ruleId,
            result.RuleType,
            result.IsActive);
    }

    /// <summary>
    /// Ensures that attempting to update a non-existent rule throws a <see cref="ConfigurationNotFoundException"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task UpdateRuleAsync_NonExistentRule_Throws()
    {
        var ruleId = Guid.NewGuid();
        var updatedRule = new ValidationRule { Id = ruleId, Name = "Test" };

        _loggerMock.Object.LogInformation(
            "Attempting to update validation rule {RuleId} that may not exist",
            ruleId);

        _validationRuleRepositoryMock.Setup(r => r.GetByIdAsync(ruleId)).ReturnsAsync((ValidationRule?)null);

        _loggerMock.Object.LogWarning(
            "Update aborted: validation rule {RuleId} was not found",
            ruleId);
        await Assert.ThrowsAsync<DotnetConfigServer.Exceptions.ConfigurationNotFoundException>(
            () => _sut.UpdateRuleAsync(ruleId, updatedRule, "admin"));
    }

    /// <summary>
    /// Verifies that deleting an existing rule succeeds and persists the deletion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DeleteRuleAsync_ExistingRule_DeletesSuccessfully()
    {
        var ruleId = Guid.NewGuid();
        var existingRule = new ValidationRule { Id = ruleId, Name = "Test" };

        _loggerMock.Object.LogInformation(
            "Deleting validation rule {RuleId}",
            ruleId);

        _validationRuleRepositoryMock.Setup(r => r.GetByIdAsync(ruleId)).ReturnsAsync(existingRule);
        _validationRuleRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<ValidationRule>())).Returns(Task.CompletedTask);
        _validationRuleRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.DeleteRuleAsync(ruleId);

        _validationRuleRepositoryMock.Verify(r => r.DeleteAsync(It.Is<ValidationRule>(r => r.Id == ruleId)), Times.Once);
        _validationRuleRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _loggerMock.Object.LogInformation(
            "Deleted validation rule {RuleId}",
            ruleId);
    }

    /// <summary>
    /// Ensures that attempting to delete a non-existent rule throws a <see cref="ConfigurationNotFoundException"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DeleteRuleAsync_NonExistentRule_Throws()
    {
        var ruleId = Guid.NewGuid();

        _loggerMock.Object.LogInformation(
            "Attempting to delete validation rule {RuleId} that may not exist",
            ruleId);

        _validationRuleRepositoryMock.Setup(r => r.GetByIdAsync(ruleId)).ReturnsAsync((ValidationRule?)null);

        _loggerMock.Object.LogWarning(
            "Delete aborted: validation rule {RuleId} was not found",
            ruleId);
        await Assert.ThrowsAsync<DotnetConfigServer.Exceptions.ConfigurationNotFoundException>(
            () => _sut.DeleteRuleAsync(ruleId));
    }

    /// <summary>
    /// Confirms that when no rules are defined for a configuration,
    /// <see cref="ValidationRuleService.ValidateConfigurationAsync"/> returns a valid result with no violations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ValidateConfigurationAsync_NoRules_ReturnsValid()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        _loggerMock.Object.LogInformation(
            "Validating configuration {ConfigurationId} version {VersionId} with no applicable rules",
            configurationId,
            versionId);

        _configurationServiceMock.Setup(s => s.GetKeysAsync(configurationId, versionId, true)).ReturnsAsync(new List<ConfigurationKey>());
        _validationRuleRepositoryMock.Setup(r => r.GetApplicableRulesAsync(configurationId)).ReturnsAsync(new List<ValidationRule>());

        var result = await _sut.ValidateConfigurationAsync(configurationId, versionId);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
        _loggerMock.Object.LogInformation(
            "Completed validation for configuration {ConfigurationId}: valid with no rules applied",
            configurationId);
    }
}
