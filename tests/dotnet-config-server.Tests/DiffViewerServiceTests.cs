#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class DiffViewerServiceTests
{
    private readonly Mock<IVersioningService> _versioningServiceMock;
    private readonly Mock<IConfigurationDiffRepository> _diffRepositoryMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly Mock<ILogger<DiffViewerService>> _loggerMock;
    private readonly DiffViewerService _sut;

    public DiffViewerServiceTests()
    {
        _versioningServiceMock = new Mock<IVersioningService>();
        _diffRepositoryMock = new Mock<IConfigurationDiffRepository>();
        _keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
        _configurationServiceMock = new Mock<IConfigurationService>();
        _loggerMock = new Mock<ILogger<DiffViewerService>>();

        _sut = new DiffViewerService(
            _versioningServiceMock.Object,
            _diffRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _configurationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetRollbackPreviewAsync_VersionNotFound_ThrowsConfigurationNotFoundException()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();

        _versioningServiceMock
            .Setup(s => s.GetVersionAsync(targetVersionId))
            .ReturnsAsync((ConfigurationVersion?)null);

        var act = () => _sut.GetRollbackPreviewAsync(configurationId, targetVersionId);

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    [Fact]
    public async Task GetRollbackPreviewAsync_NoActiveVersion_ReturnsPreviewWithNoCurrentVersion()
    {
        var configurationId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var targetVersion = new ConfigurationVersion
        {
            Id = targetVersionId,
            ConfigurationId = configurationId,
            VersionNumber = "1.2.3",
            CreatedBy = "admin",
            KeyCount = 0
        };

        _versioningServiceMock
            .Setup(s => s.GetVersionAsync(targetVersionId))
            .ReturnsAsync(targetVersion);
        _versioningServiceMock
            .Setup(s => s.GetActiveVersionAsync(configurationId))
            .ReturnsAsync((ConfigurationVersion?)null);

        var result = await _sut.GetRollbackPreviewAsync(configurationId, targetVersionId);

        result.ConfigurationId.Should().Be(configurationId);
        result.CurrentVersion.Should().BeNull();
        result.TargetVersion.Id.Should().Be(targetVersionId);
        result.IsRollbackSafe.Should().BeTrue();
        result.Changes.Should().BeEmpty();
        result.WarningMessages.Should().ContainSingle();
    }
}
