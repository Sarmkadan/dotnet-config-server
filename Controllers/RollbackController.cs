#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for rollback execution, preview, and history.
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/rollback")]
[Produces("application/json")]
public sealed class RollbackController : ControllerBase
{
    private readonly IRollbackService _rollbackService;
    private readonly IDiffViewerService _diffViewerService;
    private readonly ILogger<RollbackController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RollbackController"/>.
    /// </summary>
    public RollbackController(
        IRollbackService rollbackService,
        IDiffViewerService diffViewerService,
        ILogger<RollbackController> logger)
    {
        _rollbackService = rollbackService;
        _diffViewerService = diffViewerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets rollback history for a configuration.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<RollbackRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromRoute] Guid configurationId)
    {
        try
        {
            var history = await _rollbackService.GetRollbackHistoryAsync(configurationId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rollback history for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Executes a rollback to the specified version.
    /// </summary>
    [HttpPost("{targetVersionId}")]
    [ProducesResponseType(typeof(RollbackResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteRollback(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid targetVersionId,
        [FromBody] RollbackRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var result = await _rollbackService.ExecuteRollbackAsync(
                configurationId,
                targetVersionId,
                request.Reason,
                userId);

            return Ok(result);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rollback for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a preview of rolling back to the specified version.
    /// </summary>
    [HttpGet("preview/{targetVersionId}")]
    [ProducesResponseType(typeof(RollbackPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewRollback([FromRoute] Guid configurationId, [FromRoute] Guid targetVersionId)
    {
        try
        {
            var preview = await _diffViewerService.GetRollbackPreviewAsync(configurationId, targetVersionId);
            return Ok(preview);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rollback preview for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request body for executing a rollback.
/// </summary>
public sealed class RollbackRequest
{
    /// <summary>
    /// Gets or sets the reason for the rollback.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
