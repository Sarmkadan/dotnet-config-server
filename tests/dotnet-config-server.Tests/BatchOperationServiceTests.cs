#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Unit tests for <see cref="BatchOperationService"/> class.
/// Tests batch operations including key updates, deletions, status checking, and cancellation.
/// </summary>
public sealed class BatchOperationServiceTests
{
	/// <summary>
	/// Mock repository for testing configuration key operations.
	/// </summary>
	private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;

	/// <summary>
	/// Mock logger for testing service logging behavior.
	/// </summary>
	private readonly Mock<ILogger<BatchOperationService>> _loggerMock;

	/// <summary>
	/// System under test - the batch operation service being tested.
	/// </summary>
	private readonly BatchOperationService _sut;

	/// <summary>
	/// Initializes a new instance of the <see cref="BatchOperationServiceTests"/> class.
	/// </summary>
	public BatchOperationServiceTests()
	{
		_keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
		_loggerMock = new Mock<ILogger<BatchOperationService>>();
		_sut = new BatchOperationService(_keyRepositoryMock.Object, _loggerMock.Object);
	}

	/// <summary>
	/// Helper method to create test configuration keys.
	/// </summary>
	/// <param name="id">The unique identifier for the configuration key.</param>
	/// <param name="value">The value to set for the configuration key (default: "original").</param>
	/// <returns>A new <see cref="ConfigurationKey"/> instance with the specified properties.</returns>
	private static ConfigurationKey CreateKey(Guid id, string value = "original") => new()
	{
		Id = id,
		Key = $"key.{id}",
		Value = value,
		ConfigurationId = Guid.NewGuid(),
		VersionId = Guid.NewGuid(),
		CreatedBy = "admin"
	};

	// ── UpdateKeysAsync ──────────────────────────────────────────────────────

	/// <summary>
	/// Tests that UpdateKeysAsync returns success with empty operation ID when null input is provided.
	/// </summary>
	[Fact]
	public async Task UpdateKeysAsync_NullInput_ReturnsSuccessWithEmptyOperationId()
	{
		var result = await _sut.UpdateKeysAsync(null!, "user");

		result.Success.Should().BeTrue();
		result.OperationId.Should().Be(Guid.Empty);
	}

	/// <summary>
	/// Tests that UpdateKeysAsync returns success with empty operation ID when empty list is provided.
	/// </summary>
	[Fact]
	public async Task UpdateKeysAsync_EmptyList_ReturnsSuccessWithEmptyOperationId()
	{
		var result = await _sut.UpdateKeysAsync(new List<KeyUpdateRequest>(), "user");

		result.Success.Should().BeTrue();
		result.OperationId.Should().Be(Guid.Empty);
	}

	/// <summary>
	/// Tests that UpdateKeysAsync successfully updates all configuration keys when they are found and returns success.
	/// </summary>
	[Fact]
	public async Task UpdateKeysAsync_AllKeysFound_UpdatesAllAndReturnsSuccess()
	{
		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		var key1 = CreateKey(id1, "old1");
		var key2 = CreateKey(id2, "old2");

		_keyRepositoryMock.Setup(r => r.GetByIdAsync(id1)).ReturnsAsync(key1);
		_keyRepositoryMock.Setup(r => r.GetByIdAsync(id2)).ReturnsAsync(key2);
		_keyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
		_keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var updates = new List<KeyUpdateRequest>
		{
			new() { KeyId = id1, NewValue = "new1" },
			new() { KeyId = id2, NewValue = "new2" }
		};

		var result = await _sut.UpdateKeysAsync(updates, "operator");

		result.Success.Should().BeTrue();
		result.SuccessCount.Should().Be(2);
		result.ErrorCount.Should().Be(0);
		result.OperationId.Should().NotBe(Guid.Empty);

		key1.Value.Should().Be("new1");
		key1.UpdatedBy.Should().Be("operator");
		key2.Value.Should().Be("new2");
	}

	/// <summary>
	/// Tests that UpdateKeysAsync records errors when some configuration keys are not found.
	/// </summary>
	[Fact]
	public async Task UpdateKeysAsync_SomeKeysNotFound_RecordsErrors()
	{
		var existingId = Guid.NewGuid();
		var missingId = Guid.NewGuid();
		var key = CreateKey(existingId);

		_keyRepositoryMock.Setup(r => r.GetByIdAsync(existingId)).ReturnsAsync(key);
		_keyRepositoryMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((ConfigurationKey?)null);
		_keyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
		_keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var updates = new List<KeyUpdateRequest>
		{
			new() { KeyId = existingId, NewValue = "updated" },
			new() { KeyId = missingId, NewValue = "ignored" }
		};

		var result = await _sut.UpdateKeysAsync(updates, "user");

		result.SuccessCount.Should().Be(1);
		result.ErrorCount.Should().Be(1);
		result.Success.Should().BeFalse();
		result.Errors.Should().ContainMatch($"*{missingId}*");
	}

	// ── DeleteKeysAsync ──────────────────────────────────────────────────────

	/// <summary>
	/// Tests that DeleteKeysAsync returns success with empty operation ID when null input is provided.
	/// </summary>
	[Fact]
	public async Task DeleteKeysAsync_NullInput_ReturnsSuccessWithEmptyOperationId()
	{
		var result = await _sut.DeleteKeysAsync(null!, "user");

		result.Success.Should().BeTrue();
		result.OperationId.Should().Be(Guid.Empty);
	}

	/// <summary>
	/// Tests that DeleteKeysAsync returns success with empty operation ID when empty list is provided.
	/// </summary>
	[Fact]
	public async Task DeleteKeysAsync_EmptyList_ReturnsSuccessWithEmptyOperationId()
	{
		var result = await _sut.DeleteKeysAsync(new List<Guid>(), "user");

		result.Success.Should().BeTrue();
	}

	/// <summary>
	/// Tests that DeleteKeysAsync successfully deletes all configuration keys when they are found and returns success.
	/// </summary>
	[Fact]
	public async Task DeleteKeysAsync_AllKeysFound_DeletesAllAndReturnsSuccess()
	{
		var id1 = Guid.NewGuid();
		var id2 = Guid.NewGuid();
		var key1 = CreateKey(id1);
		var key2 = CreateKey(id2);

		_keyRepositoryMock.Setup(r => r.GetByIdAsync(id1)).ReturnsAsync(key1);
		_keyRepositoryMock.Setup(r => r.GetByIdAsync(id2)).ReturnsAsync(key2);
		_keyRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
		_keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.DeleteKeysAsync(new List<Guid> { id1, id2 }, "admin");

		result.Success.Should().BeTrue();
		result.SuccessCount.Should().Be(2);
		result.ErrorCount.Should().Be(0);
		_keyRepositoryMock.Verify(r => r.DeleteAsync(key1), Times.Once);
		_keyRepositoryMock.Verify(r => r.DeleteAsync(key2), Times.Once);
	}

	/// <summary>
	/// Tests that DeleteKeysAsync skips keys that are not found without throwing errors.
	/// </summary>
	[Fact]
	public async Task DeleteKeysAsync_KeyNotFound_SkipsWithNoError()
	{
		var missingId = Guid.NewGuid();
		_keyRepositoryMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((ConfigurationKey?)null);
		_keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.DeleteKeysAsync(new List<Guid> { missingId }, "admin");

		result.SuccessCount.Should().Be(0);
		result.ErrorCount.Should().Be(0);
	}

	// ── GetStatusAsync ───────────────────────────────────────────────────────

	/// <summary>
	/// Tests that GetStatusAsync returns not_found status when querying an unknown operation ID.
	/// </summary>
	[Fact]
	public async Task GetStatusAsync_UnknownOperationId_ReturnsNotFoundStatus()
	{
		var unknownId = Guid.NewGuid();

		var status = await _sut.GetStatusAsync(unknownId);

		status.Status.Should().Be("not_found");
		status.OperationId.Should().Be(unknownId);
	}

	/// <summary>
	/// Tests that GetStatusAsync returns completed status after a successful update operation.
	/// </summary>
	[Fact]
	public async Task GetStatusAsync_AfterUpdate_ReturnsCompletedStatus()
	{
		var id = Guid.NewGuid();
		var key = CreateKey(id);

		_keyRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(key);
		_keyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
		_keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var updateResult = await _sut.UpdateKeysAsync(
			new List<KeyUpdateRequest> { new() { KeyId = id, NewValue = "v" } }, "user");

		var status = await _sut.GetStatusAsync(updateResult.OperationId);

		status.Status.Should().Be("completed");
		status.Progress.Should().BeApproximately(1.0, 0.01);
		status.TotalItems.Should().Be(1);
		status.CompletedAt.Should().NotBeNull();
	}

	// ── CancelAsync ──────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that CancelAsync does not throw when attempting to cancel an unknown operation ID.
	/// </summary>
	[Fact]
	public async Task CancelAsync_UnknownOperationId_DoesNotThrow()
	{
		var act = () => _sut.CancelAsync(Guid.NewGuid());

		await act.Should().NotThrowAsync();
	}

	// ── BatchOperationStatus.Elapsed ─────────────────────────────────────────

	/// <summary>
	/// Tests that BatchOperationStatus.Elapsed returns the correct duration between start and completion.
	/// </summary>
	[Fact]
	public void BatchOperationStatus_Elapsed_CompletedOperation_ReturnsDurationBetweenStartAndCompletion()
	{
		var start = DateTime.UtcNow.AddSeconds(-5);
		var end = DateTime.UtcNow;

		var status = new BatchOperationStatus
		{
			StartedAt = start,
			CompletedAt = end
		};

		status.Elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100));
	}
}