#nullable enable

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
public sealed class VersioningServiceTests_v2
{
    private readonly Mock<IConfigurationVersionRepository> _versionRepositoryMock;
    private readonly Mock<IConfigurationRepository> _configRepositoryMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<VersioningService>> _loggerMock;
    private readonly VersioningService _sut;

    public VersioningServiceTests_v2()
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
    public async Task CreateVersionAsync_VersionNumbersIncrementCorrectly()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };

        // Setup mocks to return sequential versions
        var callCount = 0;
        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) return null; // First call - no previous version
                if (callCount == 2) return new ConfigurationVersion { VersionNumber = "1.0.1" };
                if (callCount == 3) return new ConfigurationVersion { VersionNumber = "1.0.2" };
                return null;
            });
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // First version starts at 1.0.1 (1.0.0 + patch increment)
        var v1 = await _sut.CreateVersionAsync(configId, "Initial version", "admin");
        v1.VersionNumber.Should().Be("1.0.1");

        // Second version increments patch from 1.0.1 to 1.0.2
        var v2 = await _sut.CreateVersionAsync(configId, "Second version", "admin");
        v2.VersionNumber.Should().Be("1.0.2");

        // Third version increments patch from 1.0.2 to 1.0.3
        var v3 = await _sut.CreateVersionAsync(configId, "Third version", "admin");
        v3.VersionNumber.Should().Be("1.0.3");
    }

    [Fact]
    public void IncrementVersion_WithDifferentTypes_ProducesCorrectVersions()
    {
        // Test patch increment
        var patchVersion = ConfigurationVersion.IncrementVersion("1.0.0", VersionIncrementType.Patch);
        patchVersion.Should().Be("1.0.1");

        // Test minor increment
        var minorVersion = ConfigurationVersion.IncrementVersion("1.0.0", VersionIncrementType.Minor);
        minorVersion.Should().Be("1.1.0");

        // Test major increment
        var majorVersion = ConfigurationVersion.IncrementVersion("1.0.0", VersionIncrementType.Major);
        majorVersion.Should().Be("2.0.0");

        // Test chained increments
        var v1 = ConfigurationVersion.IncrementVersion("1.0.0", VersionIncrementType.Patch); // 1.0.1
        var v2 = ConfigurationVersion.IncrementVersion(v1, VersionIncrementType.Minor); // 1.1.0
        var v3 = ConfigurationVersion.IncrementVersion(v2, VersionIncrementType.Patch); // 1.1.1
        var v4 = ConfigurationVersion.IncrementVersion(v3, VersionIncrementType.Major); // 2.0.0

        v1.Should().Be("1.0.1");
        v2.Should().Be("1.1.0");
        v3.Should().Be("1.1.1");
        v4.Should().Be("2.0.0");
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsSpecificVersionById()
    {
        var versionId = Guid.NewGuid();
        var version = new ConfigurationVersion
        {
            Id = versionId,
            ConfigurationId = Guid.NewGuid(),
            VersionNumber = "3.2.1",
            Status = ConfigurationVersionStatus.Draft,
            ReleaseNotes = "Test version",
            CreatedBy = "testuser",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _versionRepositoryMock
            .Setup(r => r.GetByIdAsync(versionId))
            .ReturnsAsync(version);

        var result = await _sut.GetVersionAsync(versionId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(versionId);
        result.VersionNumber.Should().Be("3.2.1");
        result.Status.Should().Be(ConfigurationVersionStatus.Draft);
        result.ReleaseNotes.Should().Be("Test version");
        result.CreatedBy.Should().Be("testuser");
    }

    [Fact]
    public async Task GetVersionHistoryAsync_ReturnsVersionsInDescendingOrder()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Create versions
        var v1 = await _sut.CreateVersionAsync(configId, "First version", "admin");
        var v2 = await _sut.CreateVersionAsync(configId, "Second version", "admin");
        var v3 = await _sut.CreateVersionAsync(configId, "Third version", "admin");

        // Mock GetVersionsAsync to return versions in reverse order (simulating database returning unordered results)
        _versionRepositoryMock.Setup(r => r.GetByConfigurationAsync(configId))
            .ReturnsAsync(new List<ConfigurationVersion> { v3, v1, v2 });

        var result = await _sut.GetVersionHistoryAsync(configId);

        result.Should().HaveCount(3);
        result[0].VersionNumber.Should().Be("1.0.3"); // newest first
        result[1].VersionNumber.Should().Be("1.0.2");
        result[2].VersionNumber.Should().Be("1.0.1"); // oldest last

        // Verify CreatedAt order - newest first (CreatedAt is set by service, so v3 > v2 > v1)
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
        result[1].CreatedAt.Should().BeAfter(result[2].CreatedAt);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersionsWithoutOrdering()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "test", ApplicationId = Guid.NewGuid(), CreatedBy = "admin" };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Create versions
        var v1 = await _sut.CreateVersionAsync(configId, "First", "admin");
        var v2 = await _sut.CreateVersionAsync(configId, "Second", "admin");
        var v3 = await _sut.CreateVersionAsync(configId, "Third", "admin");

        // Mock the repository to return versions in any order
        _versionRepositoryMock.Setup(r => r.GetByConfigurationAsync(configId))
            .ReturnsAsync(new List<ConfigurationVersion> { v3, v1, v2 });

        var result = await _sut.GetVersionsAsync(configId);

        result.Should().HaveCount(3);
        // GetVersionsAsync returns unordered list from repository
        result.Should().Contain(v => v.Id == v1.Id);
        result.Should().Contain(v => v.Id == v2.Id);
        result.Should().Contain(v => v.Id == v3.Id);
    }
}
