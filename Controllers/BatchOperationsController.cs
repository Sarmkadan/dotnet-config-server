#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for batch operations on configurations and keys.
/// Enables efficient bulk updates, deletes, and imports.
/// </summary>
[ApiController]
[Route("api/v1/batch")]
[Produces("application/json")]
sealed public class BatchOperationsController : ControllerBase
{
    private readonly IBatchOperationService _batchService;
    private readonly ILogger<BatchOperationsController> _logger;

    public BatchOperationsController(
        IBatchOperationService batchService,
        ILogger<BatchOperationsController> logger)
    {
        _batchService = batchService;
        _logger = logger;
    }

    /// <summary>
    /// Updates multiple configuration keys in a batch operation.
    /// </summary>
    [HttpPost("keys/update")]
    [ProducesResponseType(typeof(BatchOperationResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateKeys([FromBody] List<KeyUpdateRequest> updates)
    {
        try
        {
            if (updates == null || updates.Count == 0)
                return BadRequest(new { error = "At least one update is required" });

            if (updates.Count > 1000)
                return BadRequest(new { error = "Maximum 1000 updates per request" });

            var userId = User.Identity?.Name ?? "system";
            var result = await _batchService.UpdateKeysAsync(updates, userId);

            _logger.LogInformation("Batch update operation {OpId} initiated with {Count} updates", result.OperationId, updates.Count);
            return AcceptedAtAction(nameof(GetOperationStatus), new { operationId = result.OperationId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating batch update");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes multiple configuration keys in a batch operation.
    /// </summary>
    [HttpPost("keys/delete")]
    [ProducesResponseType(typeof(BatchOperationResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteKeys([FromBody] List<Guid> keyIds)
    {
        try
        {
            if (keyIds == null || keyIds.Count == 0)
                return BadRequest(new { error = "At least one key ID is required" });

            if (keyIds.Count > 1000)
                return BadRequest(new { error = "Maximum 1000 deletions per request" });

            var userId = User.Identity?.Name ?? "system";
            var result = await _batchService.DeleteKeysAsync(keyIds, userId);

            _logger.LogInformation("Batch delete operation {OpId} initiated for {Count} keys", result.OperationId, keyIds.Count);
            return AcceptedAtAction(nameof(GetOperationStatus), new { operationId = result.OperationId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating batch delete");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the status of a batch operation.
    /// </summary>
    [HttpGet("operations/{operationId}/status")]
    [ProducesResponseType(typeof(BatchOperationStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOperationStatus([FromRoute] Guid operationId)
    {
        try
        {
            var status = await _batchService.GetStatusAsync(operationId);

            if (status.Status == "not_found")
                return NotFound(new { error = "Operation not found" });

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operation status {OpId}", operationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a pending batch operation.
    /// </summary>
    [HttpPost("operations/{operationId}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOperation([FromRoute] Guid operationId)
    {
        try
        {
            await _batchService.CancelAsync(operationId);

            _logger.LogInformation("Batch operation {OpId} cancellation requested", operationId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling operation {OpId}", operationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
