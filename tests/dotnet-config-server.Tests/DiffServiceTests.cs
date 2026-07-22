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
/// Contains unit tests for <see cref="DiffService"/> class that verify diff generation functionality.
/// </summary>
public sealed class DiffServiceTests
{
    private readonly Mock<IConfigurationDiffRepository> _diffRepositoryMock = new();
    private readonly Mock<IConfigurationVersionRepository> _versionRepositoryMock = new();
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock = new();
    private readonly Mock<ILogger<DiffService>> _loggerMock = new();
    private readonly DiffService _diffService;

    public DiffServiceTests()
    {
        _diffService = new DiffService(
            _diffRepositoryMock.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Creates a test configuration version with the specified ID and configuration ID.
    /// </summary>
    private static ConfigurationVersion CreateVersion(Guid id, Guid configurationId, string versionNumber = "1.0.0")
    {
        return new ConfigurationVersion
        {
            Id = id,
            ConfigurationId = configurationId,
            VersionNumber = versionNumber,
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "test-user"
        };
    }

    /// <summary>
    /// Creates a test configuration key with the specified parameters.
    /// </summary>
    private static ConfigurationKey CreateKey(Guid versionId, string key, string value, Guid configurationId)
    {
        return new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            ConfigurationId = configurationId,
            VersionId = versionId,
            CreatedBy = "test-user"
        };
    }

    /// <summary>
    /// Creates an empty configuration diff for testing.
    /// </summary>
    private static ConfigurationDiff CreateEmptyDiff(Guid configurationId, Guid fromVersionId, Guid toVersionId)
    {
        return new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Tests that GenerateDiffAsync throws ConfigurationNotFoundException when from version is not found.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_FromVersionNotFound_ThrowsConfigurationNotFoundException()
    {
        // Arrange
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";
        var configurationId = Guid.NewGuid();

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync((ConfigurationVersion)null!);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(CreateVersion(toVersionId, configurationId));

        // Act
        var act = () => _diffService.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Tests that GenerateDiffAsync throws ConfigurationNotFoundException when to version is not found.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_ToVersionNotFound_ThrowsConfigurationNotFoundException()
    {
        // Arrange
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";
        var configurationId = Guid.NewGuid();

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(CreateVersion(fromVersionId, configurationId));
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync((ConfigurationVersion)null!);

        // Act
        var act = () => _diffService.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Tests that GenerateDiffAsync returns empty diff when both versions have identical keys and values.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_IdenticalVersions_ReturnsEmptyDiff()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var keys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "value2", configurationId),
            CreateKey(fromVersionId, "key3", "value3", configurationId)
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(keys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(keys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        result.ConfigurationId.Should().Be(configurationId);
        result.FromVersionId.Should().Be(fromVersionId);
        result.ToVersionId.Should().Be(toVersionId);
        result.CreatedBy.Should().Be(userId);
        result.AddedCount.Should().Be(0);
        result.ModifiedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
        result.TotalChanges.Should().Be(0);
        result.Changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GenerateDiffAsync detects added keys between versions.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_AddsKeys_DetectsAddedKeys()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "value2", configurationId)
        };

        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "value2", configurationId),
            CreateKey(toVersionId, "key3", "value3", configurationId), // Added
            CreateKey(toVersionId, "key4", "value4", configurationId)  // Added
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        result.AddedCount.Should().Be(2);
        result.ModifiedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
        result.TotalChanges.Should().Be(2);
        result.Changes.Should().HaveCount(2);
        result.Changes.Should().ContainSingle(c => c.Key == "key3" && c.ChangeType == ChangeType.Added);
        result.Changes.Should().ContainSingle(c => c.Key == "key4" && c.ChangeType == ChangeType.Added);
    }

    /// <summary>
    /// Tests that GenerateDiffAsync detects deleted keys between versions.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_RemovesKeys_DetectsDeletedKeys()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "value2", configurationId),
            CreateKey(fromVersionId, "key3", "value3", configurationId), // Will be deleted
            CreateKey(fromVersionId, "key4", "value4", configurationId)  // Will be deleted
        };

        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(toVersionId, "key1", "value1", configurationId),
            CreateKey(toVersionId, "key2", "value2", configurationId)
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        result.AddedCount.Should().Be(0);
        result.ModifiedCount.Should().Be(0);
        result.DeletedCount.Should().Be(2);
        result.TotalChanges.Should().Be(2);
        result.Changes.Should().HaveCount(2);
        result.Changes.Should().ContainSingle(c => c.Key == "key3" && c.ChangeType == ChangeType.Deleted);
        result.Changes.Should().ContainSingle(c => c.Key == "key4" && c.ChangeType == ChangeType.Deleted);
    }

    /// <summary>
    /// Tests that GenerateDiffAsync detects modified keys between versions.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_ChangesValues_DetectsModifiedKeys()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "old-value", configurationId),
            CreateKey(fromVersionId, "key3", "value3", configurationId)
        };

        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(toVersionId, "key1", "value1", configurationId),
            CreateKey(toVersionId, "key2", "new-value", configurationId), // Modified
            CreateKey(toVersionId, "key3", "value3", configurationId)
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        result.AddedCount.Should().Be(0);
        result.ModifiedCount.Should().Be(1);
        result.DeletedCount.Should().Be(0);
        result.TotalChanges.Should().Be(1);
        result.Changes.Should().HaveCount(1);
        result.Changes[0].Key.Should().Be("key2");
        result.Changes[0].ChangeType.Should().Be(ChangeType.Modified);
        result.Changes[0].OldValue.Should().Be("old-value");
        result.Changes[0].NewValue.Should().Be("new-value");
    }

    /// <summary>
    /// Tests that GenerateDiffAsync with ignoreWhitespaceAndBlankLines treats whitespace differences as equal.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_IgnoreWhitespace_WhitespaceChangesNotDetected()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "  value1  ", configurationId), // Whitespace
            CreateKey(fromVersionId, "key2", "value2", configurationId),
            CreateKey(fromVersionId, "key3", "", configurationId) // Empty
        };

        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(toVersionId, "key1", "value1", configurationId), // Trimmed
            CreateKey(toVersionId, "key2", "  value2  ", configurationId), // Whitespace
            CreateKey(toVersionId, "key3", "   ", configurationId) // Blank line
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId, ignoreWhitespaceAndBlankLines: true);

        // Assert
        result.TotalChanges.Should().Be(0);
        result.Changes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GenerateDiffAsync with ignoreWhitespaceAndBlankLines detects actual value changes.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_IgnoreWhitespace_ActualChangesStillDetected()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>
        {
            CreateKey(fromVersionId, "key1", "value1", configurationId),
            CreateKey(fromVersionId, "key2", "old-value", configurationId)
        };

        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(toVersionId, "key1", "value1", configurationId),
            CreateKey(toVersionId, "key2", "new-value", configurationId) // Actually different
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId, ignoreWhitespaceAndBlankLines: true);

        // Assert
        result.TotalChanges.Should().Be(1);
        result.ModifiedCount.Should().Be(1);
        result.Changes.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that ComparVersionsAsync returns correct summary for versions with changes.
    /// </summary>
    [Fact]
    public async Task ComparVersionsAsync_WithChanges_ReturnsCorrectSummary()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var version1Id = Guid.NewGuid();
        var version2Id = Guid.NewGuid();

        var version1 = CreateVersion(version1Id, configurationId);
        var version2 = CreateVersion(version2Id, configurationId);

        var keys1 = new List<ConfigurationKey>
        {
            CreateKey(version1Id, "key1", "value1", configurationId),
            CreateKey(version1Id, "key2", "value2", configurationId),
            CreateKey(version1Id, "key3", "value3", configurationId)
        };

        var keys2 = new List<ConfigurationKey>
        {
            CreateKey(version2Id, "key1", "value1", configurationId),
            CreateKey(version2Id, "key2", "modified-value", configurationId), // Modified
            CreateKey(version2Id, "key4", "value4", configurationId)  // Added
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(version1Id))
            .ReturnsAsync(version1);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(version2Id))
            .ReturnsAsync(version2);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(version1Id))
            .ReturnsAsync(keys1);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(version2Id))
            .ReturnsAsync(keys2);

        var mockDiffRepo = new Mock<IConfigurationDiffRepository>();

        var service = new DiffService(
            mockDiffRepo.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ComparVersionsAsync(version1Id, version2Id);

        // Assert
        result.TotalChanges.Should().Be(3);
        result.AddedCount.Should().Be(1);
        result.ModifiedCount.Should().Be(1);
        result.DeletedCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that ComparVersionsAsync with ignoreWhitespaceAndBlankLines handles whitespace correctly.
    /// </summary>
    [Fact]
    public async Task ComparVersionsAsync_IgnoreWhitespace_CalculatesCorrectly()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var version1Id = Guid.NewGuid();
        var version2Id = Guid.NewGuid();

        var version1 = CreateVersion(version1Id, configurationId);
        var version2 = CreateVersion(version2Id, configurationId);

        var keys1 = new List<ConfigurationKey>
        {
            CreateKey(version1Id, "key1", "value1", configurationId),
            CreateKey(version1Id, "key2", "  value2  ", configurationId) // Whitespace
        };

        var keys2 = new List<ConfigurationKey>
        {
            CreateKey(version2Id, "key1", "value1", configurationId),
            CreateKey(version2Id, "key2", "value2", configurationId) // Trimmed
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(version1Id))
            .ReturnsAsync(version1);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(version2Id))
            .ReturnsAsync(version2);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(version1Id))
            .ReturnsAsync(keys1);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(version2Id))
            .ReturnsAsync(keys2);

        var mockDiffRepo = new Mock<IConfigurationDiffRepository>();

        var service = new DiffService(
            mockDiffRepo.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.ComparVersionsAsync(version1Id, version2Id, ignoreWhitespaceAndBlankLines: true);

        // Assert
        result.TotalChanges.Should().Be(0);
        result.AddedCount.Should().Be(0);
        result.ModifiedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that diff detects added keys when comparing versions.
    /// </summary>
    [Fact]
    public async Task GenerateDiffAsync_DetectsAddedKeys_ThroughPublicAPI()
    {
        // Arrange
        var configurationId = Guid.NewGuid();
        var fromVersionId = Guid.NewGuid();
        var toVersionId = Guid.NewGuid();
        var userId = "test-user";

        var fromVersion = CreateVersion(fromVersionId, configurationId);
        var toVersion = CreateVersion(toVersionId, configurationId);

        var fromKeys = new List<ConfigurationKey>();
        var toKeys = new List<ConfigurationKey>
        {
            CreateKey(toVersionId, "key1", "value1", configurationId),
            CreateKey(toVersionId, "key2", "value2", configurationId)
        };

        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(fromVersionId))
            .ReturnsAsync(fromVersion);
        _versionRepositoryMock
            .Setup(v => v.GetByIdAsync(toVersionId))
            .ReturnsAsync(toVersion);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(fromVersionId))
            .ReturnsAsync(fromKeys);
        _keyRepositoryMock
            .Setup(k => k.GetByVersionAsync(toVersionId))
            .ReturnsAsync(toKeys);

        var mockDiff = new Mock<IConfigurationDiffRepository>();
        var diff = new ConfigurationDiff
        {
            ConfigurationId = configurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        mockDiff.Setup(d => d.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        mockDiff.Setup(d => d.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new DiffService(
            mockDiff.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.GenerateDiffAsync(fromVersionId, toVersionId, userId);

        // Assert
        result.AddedCount.Should().Be(2);
        result.TotalChanges.Should().Be(2);
    }
}