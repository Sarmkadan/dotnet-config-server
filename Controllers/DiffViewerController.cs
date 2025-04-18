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
/// API controller for retrieving enriched diffs, timeline data, and rollback previews.
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/diff-viewer")]
[Produces("application/json")]
public sealed class DiffViewerController : ControllerBase
{
    private readonly IDiffViewerService _diffViewerService;
    private readonly ILogger<DiffViewerController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DiffViewerController"/>.
    /// </summary>
    public DiffViewerController(
        IDiffViewerService diffViewerService,
        ILogger<DiffViewerController> logger)
    {
        _diffViewerService = diffViewerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets an enriched diff between two configuration versions.
    /// </summary>
    [HttpGet("{fromVersionId}/{toVersionId}")]
    [ProducesResponseType(typeof(EnrichedDiff), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrichedDiffAsync(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid fromVersionId,
        [FromRoute] Guid toVersionId)
    {
        try
        {
            var diff = await _diffViewerService.GetEnrichedDiffAsync(fromVersionId, toVersionId);
            if (diff.ConfigurationId != configurationId)
                return NotFound(new { error = "One or both versions not found" });

            return Ok(diff);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "One or both versions not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enriched diff for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the version timeline for a configuration.
    /// </summary>
    [HttpGet("timeline")]
    [ProducesResponseType(typeof(List<VersionTimelineEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersionTimelineAsync([FromRoute] Guid configurationId)
    {
        try
        {
            var timeline = await _diffViewerService.GetVersionTimelineAsync(configurationId);
            return Ok(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version timeline for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a rollback preview for the target version.
    /// </summary>
    [HttpGet("rollback-preview/{targetVersionId}")]
    [ProducesResponseType(typeof(RollbackPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRollbackPreviewAsync(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid targetVersionId)
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
