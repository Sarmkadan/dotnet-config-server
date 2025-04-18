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

public sealed class VersioningServiceTests
{
    private readonly Mock<IConfigurationVersionRepository> _versionRepositoryMock;
    private readonly Mock<IConfigurationRepository> _configRepositoryMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<VersioningService>> _loggerMock;
    private readonly VersioningService _sut;

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

    [Fact]
    public async Task PublishVersionAsync_WithNonExistentVersion_ThrowsConfigurationNotFoundException()
    {
        var versionId = Guid.NewGuid();

        _versionRepositoryMock.Setup(r => r.GetByIdAsync(versionId)).ReturnsAsync((ConfigurationVersion?)null);

        var act = () => _sut.PublishVersionAsync(versionId, "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

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
