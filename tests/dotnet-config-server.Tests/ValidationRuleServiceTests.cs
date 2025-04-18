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

public sealed class ValidationRuleServiceTests
{
    private readonly Mock<IValidationRuleRepository> _validationRuleRepositoryMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly Mock<IVersioningService> _versioningServiceMock;
    private readonly Mock<ILogger<ValidationRuleService>> _loggerMock;
    private readonly ValidationRuleService _sut;

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

    [Fact]
    public async Task ValidateConfigurationAsync_WithRegexRule_DetectsViolation()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
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

        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle();
        result.Violations[0].RuleId.Should().Be(ruleId);
        result.Violations[0].KeyName.Should().Be("ApiKey");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_AllKeysValid_ReturnsNoViolations()
    {
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
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
    }

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

        _validationRuleRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ValidationRule>())).Returns(Task.CompletedTask);
        _validationRuleRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateRuleAsync(rule, "admin");

        result.CreatedBy.Should().Be("admin");
        result.ConfigurationId.Should().Be(configurationId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _validationRuleRepositoryMock.Verify(r => r.AddAsync(It.Is<ValidationRule>(created => created.Name == "URL validation")), Times.Once);
        _validationRuleRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
