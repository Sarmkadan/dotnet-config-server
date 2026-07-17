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

/// <summary>
/// Contains unit tests for the <see cref="DiffViewerService"/> class.
/// Tests the rollback preview functionality and version handling scenarios.
/// </summary>
public sealed class DiffViewerServiceTests
{
	/// <summary>
	/// Mock implementation of <see cref="IVersioningService"/> for testing version-related operations.
	/// </summary>
	private readonly Mock<IVersioningService> _versioningServiceMock;

	/// <summary>
	/// Mock implementation of <see cref="IConfigurationDiffRepository"/> for testing configuration diff operations.
	/// </summary>
	private readonly Mock<IConfigurationDiffRepository> _diffRepositoryMock;

	/// <summary>
	/// Mock implementation of <see cref="IConfigurationKeyRepository"/> for testing configuration key operations.
	/// </summary>
	private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;

	/// <summary>
	/// Mock implementation of <see cref="IConfigurationService"/> for testing configuration operations.
	/// </summary>
	private readonly Mock<IConfigurationService> _configurationServiceMock;

	/// <summary>
	/// Mock implementation of <see cref="ILogger{DiffViewerService}"/> for testing logging functionality.
	/// </summary>
	private readonly Mock<ILogger<DiffViewerService>> _loggerMock;

	/// <summary>
	/// Instance of the service under test - <see cref="DiffViewerService"/>.
	/// </summary>
	private readonly DiffViewerService _sut;

	/// <summary>
	/// Initializes a new instance of the <see cref="DiffViewerServiceTests"/> class.
	/// Sets up mock dependencies for testing the <see cref="DiffViewerService"/> functionality.
	/// </summary>
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

	/// <summary>
	/// Tests that <see cref="DiffViewerService.GetRollbackPreviewAsync"/> throws <see cref="ConfigurationNotFoundException"/> when the target version is not found.
	/// </summary>
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

	/// <summary>
	/// Tests that <see cref="DiffViewerService.GetRollbackPreviewAsync"/> returns a preview with no current version when there is no active version.
	/// Verifies that the service correctly handles the case where a configuration has no active version.
	/// </summary>
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