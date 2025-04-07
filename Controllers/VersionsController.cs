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
/// API controller for managing configuration versions
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/[controller]")]
[Produces("application/json")]
sealed public class VersionsController : ControllerBase
{
    private readonly IVersioningService _versioningService;
    private readonly IDiffService _diffService;
    private readonly IDiffViewerService _diffViewerService; // Add IDiffViewerService
    private readonly ILogger<VersionsController> _logger;

    public VersionsController(
        IVersioningService versioningService,
        IDiffService diffService,
        IDiffViewerService diffViewerService, // Inject IDiffViewerService
        ILogger<VersionsController> logger)
    {
        _versioningService = versioningService;
        _diffService = diffService;
        _diffViewerService = diffViewerService; // Assign
        _logger = logger;
    }

    /// <summary>
    /// Gets all versions for a configuration
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ConfigurationVersionSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions([FromRoute] Guid configurationId)
    {
        try
        {
            var history = await _versioningService.GetVersionHistoryAsync(configurationId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving versions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the active version
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveVersion([FromRoute] Guid configurationId)
    {
        try
        {
            var version = await _versioningService.GetActiveVersionAsync(configurationId);
            if (version is null)
                return NotFound(new { error = "No active version found" });

            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific version
    /// </summary>
    [HttpGet("{versionId}")]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersion([FromRoute] Guid configurationId, [FromRoute] Guid versionId)
    {
        try
        {
            var version = await _versioningService.GetVersionAsync(versionId);
            if (version is null || version.ConfigurationId != configurationId)
                return NotFound(new { error = "Version not found" });

            return Ok(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a new version
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVersion([FromRoute] Guid configurationId, [FromBody] CreateVersionRequest request)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var version = await _versioningService.CreateVersionAsync(configurationId, request.ReleaseNotes, userId);
            return CreatedAtAction(nameof(GetVersion), new { configurationId, versionId = version.Id }, version);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Publishes a version
    /// </summary>
    [HttpPost("{versionId}/publish")]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishVersion([FromRoute] Guid configurationId, [FromRoute] Guid versionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var version = await _versioningService.PublishVersionAsync(versionId, userId);
            return Ok(version);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Archives a version
    /// </summary>
    [HttpPost("{versionId}/archive")]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchiveVersion([FromRoute] Guid configurationId, [FromRoute] Guid versionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var version = await _versioningService.ArchiveVersionAsync(versionId, userId);
            return Ok(version);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Rolls back to a previous version
    /// </summary>
    [HttpPost("{previousVersionId}/rollback")]
    [ProducesResponseType(typeof(ConfigurationVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rollback([FromRoute] Guid configurationId, [FromRoute] Guid previousVersionId)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var version = await _versioningService.RollbackAsync(configurationId, previousVersionId, userId);
            return Ok(version);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back version");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets diff between two versions
    /// </summary>
    [HttpGet("{fromVersionId}/diff/{toVersionId}")]
    [ProducesResponseType(typeof(EnrichedDiff), StatusCodes.Status200OK)] // Change return type
    public async Task<IActionResult> GetDiff(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid fromVersionId,
        [FromRoute] Guid toVersionId)
    {
        try
        {
            // Use _diffViewerService to get the enriched diff
            var diff = await _diffViewerService.GetEnrichedDiffAsync(fromVersionId, toVersionId);
            return Ok(diff);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "One or both versions not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating diff");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cleans up old versions
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(CleanupResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cleanup([FromRoute] Guid configurationId, [FromQuery] int maxVersions = 100)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var count = await _versioningService.CleanupOldVersionsAsync(configurationId, maxVersions);
            return Ok(new CleanupResponse { ArchivedCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up versions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for creating a version
/// </summary>
sealed public class CreateVersionRequest
{
    public string? ReleaseNotes { get; set; }
}

/// <summary>
/// Response model for cleanup operation
/// </summary>
sealed public class CleanupResponse
{
    public int ArchivedCount { get; set; }
}
