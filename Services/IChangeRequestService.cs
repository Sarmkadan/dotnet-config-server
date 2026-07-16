#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Contract for the configuration change request approval workflow.
/// </summary>
public interface IChangeRequestService
{
    /// <summary>
    /// Submits a new change request for review.
    /// </summary>
    Task<ChangeRequest> SubmitAsync(ChangeRequest request);

    /// <summary>
    /// Gets change requests for a configuration, optionally filtered by status.
    /// </summary>
    Task<List<ChangeRequest>> GetByConfigurationAsync(Guid configurationId, ChangeRequestStatus? status = null);

    /// <summary>
    /// Gets all pending change requests.
    /// </summary>
    Task<List<ChangeRequest>> GetPendingAsync();

    /// <summary>
    /// Gets a change request by id.
    /// </summary>
    Task<ChangeRequest?> GetByIdAsync(Guid id);

    /// <summary>
    /// Approves a change request and optionally applies it immediately.
    /// </summary>
    Task<ChangeRequest> ApproveAsync(Guid id, string reviewerId, string? comment = null, bool applyImmediately = true);

    /// <summary>
    /// Rejects a change request.
    /// </summary>
    Task<ChangeRequest> RejectAsync(Guid id, string reviewerId, string? comment = null);

    /// <summary>
    /// Cancels a pending change request.
    /// </summary>
    Task<ChangeRequest> CancelAsync(Guid id, string userId);
}
