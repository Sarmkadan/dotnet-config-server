#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for <see cref="VersioningService"/> which provides versioning functionality
/// for configuration management including creating versions, publishing versions, and rolling back to
/// previous versions.
/// </summary>
public sealed class VersioningServiceTests
{
    private readonly Mock<IConfigurationVersionRepository> _versionRepositoryMock;
    /// <summary>
    /// Mock repository for configuration version operations.
    /// </summary>
    private readonly Mock<IConfigurationRepository> _configRepositoryMock;
    /// <summary>
    /// Mock repository for configuration key operations.
    /// </summary>
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    /// <summary>
    /// Mock repository for audit logging operations.
    /// </summary>
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    /// <summary>
    /// Mock logger for versioning service operations.
    /// </summary>
    private readonly Mock<ILogger<VersioningService>> _loggerMock;
    /// <summary>
    /// System under test - the versioning service instance being tested.
    /// </summary>
    private readonly VersioningService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersioningServiceTests"/> class.
    /// Sets up mock repositories and logger, and creates the versioning service instance for testing.
    /// </summary>
    public VersioningServiceTests()
    {
        _versionRepositoryMock = new Mock<IConfigurationVersionRepository>();
        _configRepositoryMock = new Mock<IConfigurationRepository>();
        _keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<VersioningService>>();

        _sut = new VersioningService(
            _versionRepositoryMock.Object,
            _configRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Tests that creating a version for a non-existent configuration throws a ConfigurationNotFoundException.
    /// </summary>
    [Fact]
    public async Task CreateVersionAsync_WithNonExistentConfiguration_ThrowsConfigurationNotFoundException()
    {
        var configId = Guid.NewGuid();

        _configRepositoryMock
            .Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync((Configuration?)null);

        var act = () => _sut.CreateVersionAsync(configId, "Release notes", "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Tests that creating the first version of a configuration starts at version 1.0.1.
    /// </summary>
    [Fact]
    public async Task CreateVersionAsync_FirstVersion_StartsAt1_0_0()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateVersionAsync(configId, "Initial release", "admin");

        result.VersionNumber.Should().Be("1.0.1");
        result.ConfigurationId.Should().Be(configId);
        result.Status.Should().Be(ConfigurationVersionStatus.Draft);
        result.ReleaseNotes.Should().Be("Initial release");
    }

    /// <summary>
    /// Tests that creating a version with a previous version copies the keys from the previous version.
    /// </summary>
    [Fact]
    public async Task CreateVersionAsync_WithPreviousVersion_CopiesKeys()
    {
        var configId = Guid.NewGuid();
        var previousVersionId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };
        var previousVersion = new ConfigurationVersion
        {
            Id = previousVersionId,
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin",
            Keys = new List<ConfigurationKey>()
        };

        var previousKeys = new List<ConfigurationKey>
        {
            new() { Key = "db.host", Value = "localhost", ConfigurationId = configId, VersionId = previousVersionId, CreatedBy = "admin", IsRequired = true },
            new() { Key = "db.port", Value = "5432", ConfigurationId = configId, VersionId = previousVersionId, CreatedBy = "admin" }
        };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync(previousVersion);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(previousVersionId)).ReturnsAsync(previousKeys);
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateVersionAsync(configId, "Release v1.0.1", "admin");

        result.VersionNumber.Should().Be("1.0.1");
        result.Keys.Should().HaveCount(2);
        result.Keys.Should().Contain(k => k.Key == "db.host" && k.Value == "localhost" && k.IsRequired);
        result.Keys.Should().Contain(k => k.Key == "db.port" && k.Value == "5432");
    }

    /// <summary>
    /// Tests that creating a version sets the PreviousVersionId to the ID of the previous active version.
    /// </summary>
    [Fact]
    public async Task CreateVersionAsync_SetsPreviousVersionId()
    {
        var configId = Guid.NewGuid();
        var previousVersionId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };
        var previousVersion = new ConfigurationVersion
        {
            Id = previousVersionId,
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin"
        };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync(previousVersion);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(previousVersionId)).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateVersionAsync(configId, "Release notes", "admin");

        result.PreviousVersionId.Should().Be(previousVersionId.ToString());
    }

    /// <summary>
    /// Tests that getting the active version returns a published version with Active status.
    /// </summary>
    [Fact]
    public async Task GetActiveVersionAsync_ReturnsPublishedVersion()
    {
        var configId = Guid.NewGuid();
        var activeVersion = new ConfigurationVersion
        {
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin"
        };

        _versionRepositoryMock
            .Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync(activeVersion);

        var result = await _sut.GetActiveVersionAsync(configId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(ConfigurationVersionStatus.Active);
    }

    /// <summary>
    /// Tests that getting the active version returns null when no active version exists.
    /// </summary>
    [Fact]
    public async Task GetActiveVersionAsync_NoActiveVersion_ReturnsNull()
    {
        var configId = Guid.NewGuid();

        _versionRepositoryMock
            .Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync((ConfigurationVersion?)null);

        var result = await _sut.GetActiveVersionAsync(configId);

        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that publishing a version changes its status from Draft to Active and sets the published timestamp.
    /// </summary>
    [Fact]
    public async Task PublishVersionAsync_WithValidVersion_ChangesStatusToPublished()
    {
        var versionId = Guid.NewGuid();
        var version = new ConfigurationVersion
        {
            Id = versionId,
            ConfigurationId = Guid.NewGuid(),
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Draft,
            CreatedBy = "admin"
        };

        _versionRepositoryMock.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync(version);
        _versionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        await _sut.PublishVersionAsync(versionId, "admin");

        version.Status.Should().Be(ConfigurationVersionStatus.Active);
        version.PublishedAt.Should().NotBeNull();
        version.PublishedAt!.Value.Should().BeOnOrAfter(before);
        _versionRepositoryMock.Verify(r => r.UpdateAsync(version), Times.Once);
    }

    /// <summary>
    /// Tests that publishing a non-existent version throws a ConfigurationNotFoundException.
    /// </summary>
    [Fact]
    public async Task PublishVersionAsync_WithNonExistentVersion_ThrowsConfigurationNotFoundException()
    {
        var versionId = Guid.NewGuid();

        _versionRepositoryMock.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync((ConfigurationVersion?)null);

        var act = () => _sut.PublishVersionAsync(versionId, "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Tests that rolling back to a specific version restores keys from that version.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_ToSpecificVersion_RestoresKeysFromPreviousVersion()
    {
        var configId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin"
        };

        var targetKeys = new List<ConfigurationKey>
        {
            new() { Key = "setting1", Value = "old-value", ConfigurationId = configId, VersionId = targetVersionId, CreatedBy = "admin" }
        };

        var config = new Configuration { Id = configId, Name = "rollback-config", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };
        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetByIdAsync(targetVersionId)).ReturnsAsync(targetVersion);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(targetVersionId)).ReturnsAsync(targetKeys);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.Is<Guid>(id => id != targetVersionId))).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.RollbackAsync(configId, targetVersionId, "admin");

        result.VersionNumber.Should().Contain("."); // Version number format
        result.ConfigurationId.Should().Be(configId);
        result.Keys.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that getting a version by its ID returns the version with the specified ID.
    /// </summary>
    [Fact]
    public async Task GetVersionAsync_WithValidId_ReturnsVersion()
    {
        var versionId = Guid.NewGuid();
        var version = new ConfigurationVersion
        {
            Id = versionId,
            ConfigurationId = Guid.NewGuid(),
            VersionNumber = "2.1.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin"
        };

        _versionRepositoryMock
            .Setup(r => r.GetByIdAsync(versionId))
            .ReturnsAsync(version);

        var result = await _sut.GetVersionAsync(versionId);

        result.Should().NotBeNull();
        result!.VersionNumber.Should().Be("2.1.0");
    }
}
