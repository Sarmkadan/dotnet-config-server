#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

public sealed class ConfigurationModelTests
{
    private static Configuration CreateValidConfiguration() => new()
    {
        Name = "app-settings",
        ApplicationId = Guid.NewGuid(),
        CreatedBy = "user1"
    };

    [Fact]
    public void Validate_EmptyName_ThrowsValidationExceptionWithNameError()
    {
        var config = CreateValidConfiguration();
        config.Name = string.Empty;

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_WhitespaceName_ThrowsValidationException()
    {
        var config = CreateValidConfiguration();
        config.Name = "   ";

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_EmptyApplicationId_ThrowsValidationExceptionWithApplicationIdError()
    {
        var config = CreateValidConfiguration();
        config.ApplicationId = Guid.Empty;

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("ApplicationId");
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ThrowsValidationException()
    {
        var config = CreateValidConfiguration();
        config.Name = new string('x', AppConstants.Configuration.MaxKeyLength + 1);

        var act = () => config.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void Validate_ValidConfiguration_DoesNotThrow()
    {
        var config = CreateValidConfiguration();

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateNewVersion_IncrementsVersionNumberAndPreservesIdentity()
    {
        var config = CreateValidConfiguration();
        config.Name = "production-config";
        config.VersionNumber = 3;
        var originalId = config.Id;
        var originalAppId = config.ApplicationId;

        var next = config.CreateNewVersion();

        next.VersionNumber.Should().Be(4);
        next.Id.Should().NotBe(originalId);
        next.Name.Should().Be("production-config");
        next.ApplicationId.Should().Be(originalAppId);
    }

    [Fact]
    public void Delete_SetsIsActiveFalseAndRecordsDeletedBy()
    {
        var config = CreateValidConfiguration();
        var before = DateTime.UtcNow;

        config.Delete("admin-user");

        config.IsActive.Should().BeFalse();
        config.DeletedBy.Should().Be("admin-user");
        config.DeletedAt.Should().NotBeNull();
        config.DeletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SetEncryption_WithAes256_SetsIsEncryptedTrueAndStoresKeyId()
    {
        var config = CreateValidConfiguration();

        config.SetEncryption(EncryptionAlgorithm.AES256, "key-abc-123");

        config.IsEncrypted.Should().BeTrue();
        config.EncryptionAlgorithm.Should().Be(EncryptionAlgorithm.AES256);
        config.EncryptionKeyId.Should().Be("key-abc-123");
    }

    [Fact]
    public void SetEncryption_WithNone_ClearsEncryptionFlag()
    {
        var config = CreateValidConfiguration();
        config.SetEncryption(EncryptionAlgorithm.AES256, "key-xyz");

        config.SetEncryption(EncryptionAlgorithm.None, null);

        config.IsEncrypted.Should().BeFalse();
        config.EncryptionKeyId.Should().BeNull();
        config.EncryptionAlgorithm.Should().Be(EncryptionAlgorithm.None);
    }

    [Fact]
    public void GetSummary_ReturnsSnapshotMatchingCurrentState()
    {
        var config = CreateValidConfiguration();
        config.Name = "staging-config";
        config.VersionNumber = 7;

        var summary = config.GetSummary();

        summary.Id.Should().Be(config.Id);
        summary.Name.Should().Be("staging-config");
        summary.VersionNumber.Should().Be(7);
        summary.IsEncrypted.Should().BeFalse();
        summary.Environment.Should().Be(config.Environment);
    }
}
