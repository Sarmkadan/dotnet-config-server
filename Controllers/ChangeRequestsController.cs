#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for the configuration change approval workflow.
/// Changes submitted here are held as pending until an authorised reviewer
/// approves or rejects them. Approved changes are applied atomically.
/// </summary>
[ApiController]
[Route("api/v1/change-requests")]
[Produces("application/json")]
public sealed class ChangeRequestsController : ControllerBase
{
    private readonly ChangeRequestService _service;
    private readonly ILogger<ChangeRequestsController> _logger;

    public ChangeRequestsController(ChangeRequestService service, ILogger<ChangeRequestsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new configuration change request for approval.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChangeRequest), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] ChangeRequest request)
    {
        try
        {
            request.RequestedBy = User.Identity?.Name ?? "system";
            var created = await _service.SubmitAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting change request");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a single change request by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChangeRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var request = await _service.GetByIdAsync(id);
        return request is null ? NotFound(new { error = "Change request not found" }) : Ok(request);
    }

    /// <summary>
    /// Lists all pending change requests.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<ChangeRequest>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending()
    {
        var requests = await _service.GetPendingAsync();
        return Ok(requests);
    }

    /// <summary>
    /// Lists change requests for a specific configuration, optionally filtered by status.
    /// </summary>
    [HttpGet("configuration/{configurationId}")]
    [ProducesResponseType(typeof(List<ChangeRequest>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByConfiguration(
        [FromRoute] Guid configurationId,
        [FromQuery] ChangeRequestStatus? status = null)
    {
        var requests = await _service.GetByConfigurationAsync(configurationId, status);
        return Ok(requests);
    }

    /// <summary>
    /// Approves a pending change request and optionally applies it immediately.
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ChangeRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] ReviewDecisionRequest decision)
    {
        try
        {
            var reviewerId = User.Identity?.Name ?? "system";
            var result = await _service.ApproveAsync(id, reviewerId, decision.Comment, decision.ApplyImmediately);
            return Ok(result);
        }
        catch (ConfigurationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConfigurationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving change request {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Rejects a pending change request.
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ChangeRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] ReviewDecisionRequest decision)
    {
        try
        {
            var reviewerId = User.Identity?.Name ?? "system";
            var result = await _service.RejectAsync(id, reviewerId, decision.Comment);
            return Ok(result);
        }
        catch (ConfigurationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConfigurationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting change request {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a pending change request.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ChangeRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var result = await _service.CancelAsync(id, userId);
            return Ok(result);
        }
        catch (ConfigurationNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConfigurationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling change request {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request body for approve/reject decisions.
/// </summary>
public sealed record ReviewDecisionRequest(
    string? Comment = null,
    bool ApplyImmediately = true);
