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
using System.Text.Json;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for the <see cref="RollbackService"/> class.
/// </summary>
public sealed class RollbackServiceTests
{
    private readonly Mock<IVersioningService> _versioningServiceMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<RollbackService>> _loggerMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    private readonly RollbackService _sut;

    /// <summary>
    /// Initializes the mock dependencies and creates an instance of <see cref="RollbackService"/>
    /// to be used in the tests.
    /// </summary>
    public RollbackServiceTests()
    {
        _versioningServiceMock = new Mock<IVersioningService>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _loggerMock = new Mock<ILogger<RollbackService>>();
        _keyRepositoryMock = new Mock<IConfigurationKeyRepository>();

        _sut = new RollbackService(
            _versioningServiceMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object,
            _keyRepositoryMock.Object);
    }

    /// <summary>
    /// Verifies that when the target version cannot be found,
    /// <see cref="RollbackService.ExecuteRollbackAsync"/> throws a <see cref="ConfigurationNotFoundException"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteRollbackAsync_VersionNotFound_ThrowsConfigurationNotFoundException()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();

        _versioningServiceMock
            .Setup(s => s.GetVersionAsync(targetVersionId))
            .ReturnsAsync((ConfigurationVersion?)null);

        var act = () => _sut.ExecuteRollbackAsync(configurationId, targetVersionId, "bad deploy", "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Verifies that a valid rollback creates a new draft version, publishes it,
    /// and returns a <see cref="RollbackResult"/> containing the expected data.
    /// The test also checks that an audit log entry is added with the correct details.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteRollbackAsync_ValidRollback_ReturnsRollbackResult()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Deprecated,
            CreatedBy = "admin",
            KeyCount = 2
        };
        var createdVersion = new ConfigurationVersion
        {
            Id = newVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.1",
            Status = ConfigurationVersionStatus.Draft,
            CreatedBy = "admin",
            KeyCount = 2
        };
        var publishedVersion = new ConfigurationVersion
        {
            Id = newVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.1",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin",
            KeyCount = 2
        };

        _versioningServiceMock.Setup(s => s.GetVersionAsync(targetVersionId)).ReturnsAsync(targetVersion);
        _versioningServiceMock.Setup(s => s.RollbackAsync(configurationId, targetVersionId, "admin")).ReturnsAsync(createdVersion);
        _versioningServiceMock.Setup(s => s.PublishVersionAsync(newVersionId, "admin")).ReturnsAsync(publishedVersion);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteRollbackAsync(configurationId, targetVersionId, "bad deploy", "admin");

        result.ConfigurationId.Should().Be(configurationId);
        result.NewVersion.Id.Should().Be(newVersionId);
        result.RestoredFromVersion.Id.Should().Be(targetVersionId);
        result.Reason.Should().Be("bad deploy");
        result.PerformedBy.Should().Be("admin");
        result.KeysRestored.Should().Be(2);

        _versioningServiceMock.Verify(s => s.RollbackAsync(configurationId, targetVersionId, "admin"), Times.Once);
        _versioningServiceMock.Verify(s => s.PublishVersionAsync(newVersionId, "admin"), Times.Once);
        _auditLogRepositoryMock.Verify(r => r.AddAsync(It.Is<AuditLog>(log =>
            log.ConfigurationId == configurationId &&
            log.EntityType == "Rollback" &&
            log.EntityId == newVersionId.ToString() &&
            log.ActionType == AuditActionType.ConfigurationUpdated &&
            log.Details == "bad deploy")), Times.Once);
    }

    /// <summary>
    /// Verifies that rollback to a previous version works correctly and creates proper audit entries.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteRollbackAsync_ToPreviousVersion_CreatesAuditEntryWithMetadata()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Deprecated,
            CreatedBy = "admin",
            KeyCount = 3
        };
        var createdVersion = new ConfigurationVersion
        {
            Id = newVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.1",
            Status = ConfigurationVersionStatus.Draft,
            CreatedBy = "admin",
            KeyCount = 3
        };
        var publishedVersion = new ConfigurationVersion
        {
            Id = newVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.1",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = "admin",
            KeyCount = 3
        };

        _versioningServiceMock.Setup(s => s.GetVersionAsync(targetVersionId)).ReturnsAsync(targetVersion);
        _versioningServiceMock.Setup(s => s.RollbackAsync(configurationId, targetVersionId, "admin")).ReturnsAsync(createdVersion);
        _versioningServiceMock.Setup(s => s.PublishVersionAsync(newVersionId, "admin")).ReturnsAsync(publishedVersion);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteRollbackAsync(configurationId, targetVersionId, "reverting bad changes", "admin");

        result.ConfigurationId.Should().Be(configurationId);
        result.NewVersion.Id.Should().Be(newVersionId);
        result.RestoredFromVersion.Id.Should().Be(targetVersionId);
        result.Reason.Should().Be("reverting bad changes");
        result.PerformedBy.Should().Be("admin");
        result.KeysRestored.Should().Be(3);

        _versioningServiceMock.Verify(s => s.RollbackAsync(configurationId, targetVersionId, "admin"), Times.Once);
        _versioningServiceMock.Verify(s => s.PublishVersionAsync(newVersionId, "admin"), Times.Once);
        _auditLogRepositoryMock.Verify(r => r.AddAsync(It.Is<AuditLog>(log =>
            log.ConfigurationId == configurationId &&
            log.EntityType == "Rollback" &&
            log.EntityId == newVersionId.ToString() &&
            log.ActionType == AuditActionType.ConfigurationUpdated &&
            log.Details == "reverting bad changes")), Times.Once);

        _auditLogRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that GetRollbackHistoryAsync returns rollback records correctly.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task GetRollbackHistoryAsync_ReturnsRollbackRecords()
    {
        var configurationId = Guid.NewGuid();
        var versionId1 = Guid.NewGuid();
        var versionId2 = Guid.NewGuid();
        var rollbackId1 = Guid.NewGuid();
        var rollbackId2 = Guid.NewGuid();

        var rollbackMetadata1 = new
        {
            RestoredFromVersionId = Guid.NewGuid(),
            Reason = "fixing configuration",
            PerformedBy = "admin",
            PerformedAt = DateTime.UtcNow.AddDays(-2),
            KeysRestored = 5
        };

        var rollbackMetadata2 = new
        {
            RestoredFromVersionId = Guid.NewGuid(),
            Reason = "reverting bad deploy",
            PerformedBy = "user1",
            PerformedAt = DateTime.UtcNow.AddDays(-1),
            KeysRestored = 3
        };

        var auditLogs = new List<AuditLog>
        {
            new AuditLog
            {
                Id = rollbackId1,
                ActionType = AuditActionType.ConfigurationUpdated,
                EntityType = "Rollback",
                EntityId = versionId1.ToString(),
                EntityName = null,
                Timestamp = DateTime.UtcNow.AddDays(-2),
                UserId = "admin",
                Status = "Success",
                Details = "fixing configuration",
                NewValues = JsonSerializer.Serialize(rollbackMetadata1),
                ConfigurationId = configurationId
            },
            new AuditLog
            {
                Id = rollbackId2,
                ActionType = AuditActionType.ConfigurationUpdated,
                EntityType = "Rollback",
                EntityId = versionId2.ToString(),
                EntityName = null,
                Timestamp = DateTime.UtcNow.AddDays(-1),
                UserId = "user1",
                Status = "Success",
                Details = "reverting bad deploy",
                NewValues = JsonSerializer.Serialize(rollbackMetadata2),
                ConfigurationId = configurationId
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = AuditActionType.ConfigurationUpdated,
                EntityType = "SomeOtherAction",
                EntityId = Guid.NewGuid().ToString(),
                EntityName = null,
                Timestamp = DateTime.UtcNow,
                UserId = "admin",
                Status = "Success",
                Details = "some other action",
                ConfigurationId = configurationId
            }
        };

        _auditLogRepositoryMock.Setup(r => r.GetByConfigurationAsync(configurationId))
            .ReturnsAsync(auditLogs);

        var result = await _sut.GetRollbackHistoryAsync(configurationId);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(rollbackId2);
        result[1].Id.Should().Be(rollbackId1);
        result[0].ConfigurationId.Should().Be(configurationId);
        result[0].NewVersionId.Should().Be(versionId2);
        result[0].RestoredFromVersionId.Should().NotBe(Guid.Empty);
        result[0].Reason.Should().Be("reverting bad deploy");
        result[0].PerformedBy.Should().Be("user1");
    }

    /// <summary>
    /// Verifies that PreviewRollbackAsync returns correct changes and detects unsafe rollbacks.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task PreviewRollbackAsync_ReturnsChangesAndDetectsUnsafeRollbacks()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var activeVersionId = Guid.NewGuid();

        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Deprecated,
            KeyCount = 2
        };

        var activeVersion = new ConfigurationVersion
        {
            Id = activeVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.2",
            Status = ConfigurationVersionStatus.Active,
            KeyCount = 3
        };

        var targetKeys = new List<ConfigurationKey>
        {
            new ConfigurationKey { Key = "key1", Value = "value1", IsRequired = false },
            new ConfigurationKey { Key = "key2", Value = "value2", IsRequired = false }
        };

        var activeKeys = new List<ConfigurationKey>
        {
            new ConfigurationKey { Key = "key1", Value = "value1", IsRequired = false },
            new ConfigurationKey { Key = "key2", Value = "value2-changed", IsRequired = false },
            new ConfigurationKey { Key = "key3", Value = "value3", IsRequired = true }
        };

        _versioningServiceMock.Setup(s => s.GetVersionAsync(targetVersionId)).ReturnsAsync(targetVersion);
        _versioningServiceMock.Setup(s => s.GetActiveVersionAsync(configurationId)).ReturnsAsync(activeVersion);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(targetVersionId)).ReturnsAsync(targetKeys);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(activeVersionId)).ReturnsAsync(activeKeys);

        var preview = await _sut.PreviewRollbackAsync(configurationId, targetVersionId, "admin");

        preview.ConfigurationId.Should().Be(configurationId);
        preview.CurrentVersion.Should().NotBeNull();
        preview.CurrentVersion!.Id.Should().Be(activeVersionId);
        preview.TargetVersion.Id.Should().Be(targetVersionId);
        preview.TotalChanges.Should().Be(2);
        preview.AddedCount.Should().Be(0);
        preview.ModifiedCount.Should().Be(1);
        preview.DeletedCount.Should().Be(1);
        preview.IsRollbackSafe.Should().BeFalse();
        preview.WarningMessages.Should().ContainSingle();
    }

    /// <summary>
    /// Verifies that PreviewRollbackAsync detects unsafe rollbacks that would delete required keys.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task PreviewRollbackAsync_DetectsUnsafeRollback_WhenRequiredKeyWouldBeDeleted()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var activeVersionId = Guid.NewGuid();

        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Deprecated,
            KeyCount = 1
        };

        var activeVersion = new ConfigurationVersion
        {
            Id = activeVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.0.2",
            Status = ConfigurationVersionStatus.Active,
            KeyCount = 2
        };

        var targetKeys = new List<ConfigurationKey>
        {
            new ConfigurationKey { Key = "key1", Value = "value1", IsRequired = false }
        };

        var activeKeys = new List<ConfigurationKey>
        {
            new ConfigurationKey { Key = "key1", Value = "value1", IsRequired = false },
            new ConfigurationKey { Key = "required_key", Value = "required_value", IsRequired = true }
        };

        _versioningServiceMock.Setup(s => s.GetVersionAsync(targetVersionId)).ReturnsAsync(targetVersion);
        _versioningServiceMock.Setup(s => s.GetActiveVersionAsync(configurationId)).ReturnsAsync(activeVersion);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(targetVersionId)).ReturnsAsync(targetKeys);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(activeVersionId)).ReturnsAsync(activeKeys);

        var preview = await _sut.PreviewRollbackAsync(configurationId, targetVersionId, "admin");

        preview.ConfigurationId.Should().Be(configurationId);
        preview.IsRollbackSafe.Should().BeFalse();
        preview.WarningMessages.Should().ContainSingle();
        preview.WarningMessages[0].Should().Contain("required_key");
    }
}
