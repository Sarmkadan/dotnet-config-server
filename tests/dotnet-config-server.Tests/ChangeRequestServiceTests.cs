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

public sealed class ChangeRequestServiceTests
{
    private readonly Mock<IChangeRequestRepository> _repositoryMock;
    private readonly Mock<IConfigurationService> _configServiceMock;
    private readonly Mock<ILogger<ChangeRequestService>> _loggerMock;
    private readonly ChangeRequestService _sut;

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

    private static ChangeRequest CreateValidRequest() => new()
    {
        ConfigurationId = Guid.NewGuid(),
        RequestedBy = "developer",
        Operation = ChangeRequestOperation.UpdateKey,
        Payload = """{"Value":"new-value"}""",
        Summary = "Update connection string"
    };

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

    [Fact]
    public async Task SubmitAsync_WithEmptyRequestedBy_ThrowsValidationException()
    {
        var request = CreateValidRequest();
        request.RequestedBy = string.Empty;

        var act = () => _sut.SubmitAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SubmitAsync_WithWhitespaceRequestedBy_ThrowsValidationException()
    {
        var request = CreateValidRequest();
        request.RequestedBy = "   ";

        var act = () => _sut.SubmitAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

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

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

        var act = () => _sut.ApproveAsync(id, "reviewer");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

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

    [Fact]
    public async Task RejectAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

        var act = () => _sut.RejectAsync(id, "reviewer");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

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

    [Fact]
    public async Task CancelAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ChangeRequest?)null);

        var act = () => _sut.CancelAsync(id, "user");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

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

    [Fact]
    public void ChangeRequest_Reject_SetsReviewerAndStatus()
    {
        var request = CreateValidRequest();

        request.Reject("reviewer-2", "Security risk");

        request.Status.Should().Be(ChangeRequestStatus.Rejected);
        request.ReviewedBy.Should().Be("reviewer-2");
        request.ReviewComment.Should().Be("Security risk");
    }

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

    [Fact]
    public void ChangeRequest_Cancel_SetsStatusToCancelled()
    {
        var request = CreateValidRequest();

        request.Cancel();

        request.Status.Should().Be(ChangeRequestStatus.Cancelled);
    }
}
