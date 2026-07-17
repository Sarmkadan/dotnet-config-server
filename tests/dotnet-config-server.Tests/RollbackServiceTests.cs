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
/// Contains unit tests for the <see cref="RollbackService"/> class.
/// </summary>
public sealed class RollbackServiceTests
{
    private readonly Mock<IVersioningService> _versioningServiceMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<ILogger<RollbackService>> _loggerMock;
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

        _sut = new RollbackService(
            _versioningServiceMock.Object,
            _auditLogRepositoryMock.Object,
            _loggerMock.Object);
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
}
