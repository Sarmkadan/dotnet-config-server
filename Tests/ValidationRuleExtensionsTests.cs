using System;
using DotnetConfigServer.Models;
using Xunit;

namespace DotnetConfigServer.Tests
{
    public class ValidationRuleExtensionsTests
    {
        [Fact]
        public void AppliesTo_ReturnsTrue_WhenPatternIsNullOrEmpty()
        {
            var rule = new ValidationRule
            {
                Name = "AnyKeyRule",
                RuleType = ValidationRuleType.Required,
                TargetKeyPattern = null
            };

            Assert.True(rule.AppliesTo("any_key"));
        }

        [Fact]
        public void AppliesTo_EvaluatesRegexPattern()
        {
            var rule = new ValidationRule
            {
                Name = "AppKeyRule",
                RuleType = ValidationRuleType.Regex,
                TargetKeyPattern = "^app_.*$"
            };

            Assert.True(rule.AppliesTo("app_setting"));
            Assert.False(rule.AppliesTo("service_setting"));
        }

        [Fact]
        public void Describe_IncludesNameTypeAndStatus()
        {
            var rule = new ValidationRule
            {
                Name = "RequiredName",
                RuleType = ValidationRuleType.Required,
                IsActive = true,
                TargetKeyPattern = null
            };

            var description = rule.Describe();

            Assert.Contains("RequiredName", description);
            Assert.Contains("Required", description);
            Assert.Contains("Active", description);
        }

        [Fact]
        public void Describe_IncludesPatternWhenPresent()
        {
            var rule = new ValidationRule
            {
                Name = "PatternRule",
                RuleType = ValidationRuleType.Regex,
                IsActive = false,
                TargetKeyPattern = "^test_.*$"
            };

            var description = rule.Describe();

            Assert.Contains("Pattern: ^test_.*$", description);
            Assert.Contains("Inactive", description);
        }

        [Fact]
        public void IsStricterThan_ReturnsFalse_WhenRuleTypesDiffer()
        {
            var ruleA = new ValidationRule
            {
                Name = "MinLengthRule",
                RuleType = ValidationRuleType.MinLength
            };

            var ruleB = new ValidationRule
            {
                Name = "MaxLengthRule",
                RuleType = ValidationRuleType.MaxLength
            };

            Assert.False(ruleA.IsStricterThan(ruleB));
        }

        [Fact]
        public void IsStricterThan_UsesEnumOrder_ForSameType()
        {
            var ruleA = new ValidationRule
            {
                Name = "RuleA",
                RuleType = ValidationRuleType.MaxLength
            };

            var ruleB = new ValidationRule
            {
                Name = "RuleB",
                RuleType = ValidationRuleType.MaxLength
            };

            // Since both have the same enum value, they are not stricter than each other.
            Assert.False(ruleA.IsStricterThan(ruleB));
            Assert.False(ruleB.IsStricterThan(ruleA));
        }

        [Fact]
        public void IsStricterThan_ReturnsTrue_WhenEnumValueHigher()
        {
            var stricter = new ValidationRule
            {
                Name = "StricterRule",
                RuleType = ValidationRuleType.Url // enum value 6
            };

            var lessStrict = new ValidationRule
            {
                Name = "LessStrictRule",
                RuleType = ValidationRuleType.Regex // enum value 1
            };

            // Different types -> false per implementation
            Assert.False(stricter.IsStricterThan(lessStrict));

            // Same type with higher enum (artificial scenario)
            var rule1 = new ValidationRule
            {
                Name = "Rule1",
                RuleType = ValidationRuleType.CrossKey // enum value 8
            };
            var rule2 = new ValidationRule
            {
                Name = "Rule2",
                RuleType = ValidationRuleType.Url // enum value 6
            };

            Assert.True(rule1.IsStricterThan(rule2));
        }
    }
}
