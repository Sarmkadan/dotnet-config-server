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
/// Unit tests for <see cref="ChangeRequestService"/> which handles change request workflows including submission, approval, rejection, and cancellation.
/// Tests cover validation, status transitions, and integration with configuration service.
/// </summary>
public sealed class ChangeRequestServiceTests
{
	/// <summary>
	/// Mock repository for change requests.
	/// </summary>
	private readonly Mock<IChangeRequestRepository> _repositoryMock;

	/// <summary>
	/// Mock configuration service for applying approved changes.
	/// </summary>
	private readonly Mock<IConfigurationService> _configServiceMock;

	/// <summary>
	/// Mock logger for testing service behavior.
	/// </summary>
	private readonly Mock<ILogger<ChangeRequestService>> _loggerMock;

	/// <summary>
	/// System under test - the change request service being tested.
	/// </summary>
	private readonly ChangeRequestService _sut;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChangeRequestServiceTests"/> class with mock dependencies.
	/// </summary>
	public ChangeRequestServiceTests()
	{
		_repositoryMock = new Mock<IChangeRequestRepository>();
		_configServiceMock = new Mock<IConfigurationService>();
		_loggerMock = new Mock<ILogger<ChangeRequestService>>();

		_sut = new ChangeRequestService(
			_repositoryMock.Object,
			_configServiceMock.Object,
			_loggerMock.Object);
	}

	/// <summary>
	/// Creates a valid change request for testing purposes.
	/// </summary>
	/// <returns>A new <see cref="ChangeRequest"/> instance with valid default values.</returns>
	private static ChangeRequest CreateValidRequest() => new()
	{
		ConfigurationId = Guid.NewGuid(),
		RequestedBy = "developer",
		Operation = ChangeRequestOperation.UpdateKey,
		Payload = """{"Value":"new-value"}""",
		Summary = "Update connection string"
	};

	/// <summary>
	/// Tests that submitting a valid change request sets its status to Pending and saves it to the repository.
	/// </summary>
	[Fact]
	public async Task SubmitAsync_WithValidRequest_SetsStatusToPendingAndSaves()
	{
		var request = CreateValidRequest();

		_repositoryMock.Setup(r => r.AddAsync(It.IsAny<ChangeRequest>())).Returns(Task.CompletedTask);
		_repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.SubmitAsync(request);

		result.Status.Should().Be(ChangeRequestStatus.Pending);
		result.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
		_repositoryMock.Verify(r => r.AddAsync(request), Times.Once);
		_repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
	}

	/// <summary>
	/// Tests that submitting a change request with an empty RequestedBy field throws a ValidationException.
	/// </summary>
	[Fact]
	public async Task SubmitAsync_WithEmptyRequestedBy_ThrowsValidationException()
	{
		var request = CreateValidRequest();
		request.RequestedBy = string.Empty;

		var act = () => _sut.SubmitAsync(request);

		await act.Should().ThrowAsync<ValidationException>();
	}

	/// <summary>
	/// Tests that submitting a change request with a whitespace RequestedBy field throws a ValidationException.
	/// </summary>
	[Fact]
	public async Task SubmitAsync_WithWhitespaceRequestedBy_ThrowsValidationException()
	{
		var request = CreateValidRequest();
		request.RequestedBy = " ";

		var act = () => _sut.SubmitAsync(request);

		await act.Should().ThrowAsync<ValidationException>();
	}

	/// <summary>
	/// Tests that retrieving a change request by its ID returns the correct request when it exists.
	/// </summary>
	[Fact]
	public async Task GetByIdAsync_WithValidId_ReturnsRequest()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);

		var result = await _sut.GetByIdAsync(id);

		result.Should().NotBeNull();
		result!.Id.Should().Be(id);
	}

	/// <summary>
	/// Tests that retrieving a non-existent change request by ID returns null.
	/// </summary>
	[Fact]
	public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
	{
		var id = Guid.NewGuid();
		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

		var result = await _sut.GetByIdAsync(id);

		result.Should().BeNull();
	}

	/// <summary>
	/// Tests that approving a non-existent change request throws a ConfigurationNotFoundException.
	/// </summary>
	[Fact]
	public async Task ApproveAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
	{
		var id = Guid.NewGuid();
		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

		var act = () => _sut.ApproveAsync(id, "reviewer");

		await act.Should().ThrowAsync<ConfigurationNotFoundException>();
	}

	/// <summary>
	/// Tests that approving an already approved change request throws a ConfigurationException.
	/// </summary>
	[Fact]
	public async Task ApproveAsync_WithAlreadyApprovedRequest_ThrowsConfigurationException()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Approved;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);

		var act = () => _sut.ApproveAsync(id, "reviewer");

		await act.Should().ThrowAsync<ConfigurationException>();
	}

	/// <summary>
	/// Tests that approving a pending change request sets its status to Applied when applyImmediately is true.
	/// </summary>
	[Fact]
	public async Task ApproveAsync_WithPendingRequest_SetsStatusToApproved()
	{
		var id = Guid.NewGuid();
		var keyId = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Pending;
		request.ConfigurationKeyId = keyId;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);
		_repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ChangeRequest>())).Returns(Task.CompletedTask);
		_repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
		_configServiceMock.Setup(s => s.UpdateKeyAsync(keyId, It.IsAny<string>(), It.IsAny<string>()))
			.ReturnsAsync(new ConfigurationKey { Id = keyId, Key = "key", Value = "new-value", ConfigurationId = Guid.NewGuid(), VersionId = Guid.NewGuid(), CreatedBy = "admin" });

		var result = await _sut.ApproveAsync(id, "reviewer", "Looks good", applyImmediately: true);

		result.Status.Should().Be(ChangeRequestStatus.Applied);
		result.ReviewedBy.Should().Be("reviewer");
		result.ReviewComment.Should().Be("Looks good");
		result.ReviewedAt.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that approving a pending change request with applyImmediately set to false only sets status to Approved without applying the change.
	/// </summary>
	[Fact]
	public async Task ApproveAsync_WithApplyImmediatelyFalse_DoesNotApplyChange()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Pending;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);
		_repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ChangeRequest>())).Returns(Task.CompletedTask);
		_repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.ApproveAsync(id, "reviewer", applyImmediately: false);

		result.Status.Should().Be(ChangeRequestStatus.Approved);
		_configServiceMock.Verify(s => s.UpdateKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
	}

	/// <summary>
	/// Tests that rejecting a non-existent change request throws a ConfigurationNotFoundException.
	/// </summary>
	[Fact]
	public async Task RejectAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
	{
		var id = Guid.NewGuid();
		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

		var act = () => _sut.RejectAsync(id, "reviewer");

		await act.Should().ThrowAsync<ConfigurationNotFoundException>();
	}

	/// <summary>
	/// Tests that rejecting a pending change request sets its status to Rejected with reviewer information.
	/// </summary>
	[Fact]
	public async Task RejectAsync_WithPendingRequest_SetsStatusToRejected()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Pending;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);
		_repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ChangeRequest>())).Returns(Task.CompletedTask);
		_repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.RejectAsync(id, "reviewer", "Does not meet standards");

		result.Status.Should().Be(ChangeRequestStatus.Rejected);
		result.ReviewedBy.Should().Be("reviewer");
		result.ReviewComment.Should().Be("Does not meet standards");
	}

	/// <summary>
	/// Tests that rejecting a non-pending change request throws a ConfigurationException.
	/// </summary>
	[Fact]
	public async Task RejectAsync_WithNonPendingRequest_ThrowsConfigurationException()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Approved;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);

		var act = () => _sut.RejectAsync(id, "reviewer");

		await act.Should().ThrowAsync<ConfigurationException>();
	}

	/// <summary>
	/// Tests that canceling a non-existent change request throws a ConfigurationNotFoundException.
	/// </summary>
	[Fact]
	public async Task CancelAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
	{
		var id = Guid.NewGuid();
		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

		var act = () => _sut.CancelAsync(id, "user");

		await act.Should().ThrowAsync<ConfigurationNotFoundException>();
	}

	/// <summary>
	/// Tests that canceling a pending change request sets its status to Cancelled.
	/// </summary>
	[Fact]
	public async Task CancelAsync_WithPendingRequest_SetsStatusToCancelled()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Pending;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);
		_repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ChangeRequest>())).Returns(Task.CompletedTask);
		_repositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

		var result = await _sut.CancelAsync(id, "requester");

		result.Status.Should().Be(ChangeRequestStatus.Cancelled);
	}

	/// <summary>
	/// Tests that canceling a non-pending change request throws a ConfigurationException.
	/// </summary>
	[Fact]
	public async Task CancelAsync_WithNonPendingRequest_ThrowsConfigurationException()
	{
		var id = Guid.NewGuid();
		var request = CreateValidRequest();
		request.Id = id;
		request.Status = ChangeRequestStatus.Rejected;

		_repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(request);

		var act = () => _sut.CancelAsync(id, "user");

		await act.Should().ThrowAsync<ConfigurationException>();
	}

	/// <summary>
	/// Tests that retrieving pending change requests returns only requests with Pending status.
	/// </summary>
	[Fact]
	public async Task GetPendingAsync_ReturnsPendingRequests()
	{
		var pending = new List<ChangeRequest>
		{
			CreateValidRequest(),
			CreateValidRequest()
		};

		_repositoryMock.Setup(r => r.GetPendingAsync()).ReturnsAsync(pending);

		var result = await _sut.GetPendingAsync();

		result.Should().HaveCount(2);
	}

	/// <summary>
	/// Tests that calling Approve on a ChangeRequest instance sets the reviewer and timestamp correctly.
	/// </summary>
	[Fact]
	public void ChangeRequest_Approve_SetsReviewerAndTimestamp()
	{
		var request = CreateValidRequest();
		var before = DateTime.UtcNow;

		request.Approve("reviewer-1", "LGTM");


		request.Status.Should().Be(ChangeRequestStatus.Approved);
		request.ReviewedBy.Should().Be("reviewer-1");
		request.ReviewComment.Should().Be("LGTM");
		request.ReviewedAt.Should().NotBeNull();
		request.ReviewedAt!.Value.Should().BeOnOrAfter(before);
	}

	/// <summary>
	/// Tests that calling Reject on a ChangeRequest instance sets the reviewer and rejection status.
	/// </summary>
	[Fact]
	public void ChangeRequest_Reject_SetsReviewerAndStatus()
	{
		var request = CreateValidRequest();

		request.Reject("reviewer-2", "Security risk");


		request.Status.Should().Be(ChangeRequestStatus.Rejected);
		request.ReviewedBy.Should().Be("reviewer-2");
		request.ReviewComment.Should().Be("Security risk");
	}

	/// <summary>
	/// Tests that calling MarkApplied on a ChangeRequest instance sets the operator and applied timestamp correctly.
	/// </summary>
	[Fact]
	public void ChangeRequest_MarkApplied_SetsAppliedByAndTimestamp()
	{
		var request = CreateValidRequest();
		request.Status = ChangeRequestStatus.Approved;
		var before = DateTime.UtcNow;


		request.MarkApplied("operator");


		request.Status.Should().Be(ChangeRequestStatus.Applied);
		request.AppliedBy.Should().Be("operator");
		request.AppliedAt.Should().NotBeNull();
		request.AppliedAt!.Value.Should().BeOnOrAfter(before);
	}


	/// <summary>
	/// Tests that calling Cancel on a ChangeRequest instance sets its status to Cancelled.
	/// </summary>
	[Fact]
	public void ChangeRequest_Cancel_SetsStatusToCancelled()
	{
		var request = CreateValidRequest();

		request.Cancel();

		request.Status.Should().Be(ChangeRequestStatus.Cancelled);
	}
}