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
/// API controller for getting configuration diffs between versions
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/[controller]")]
[Produces("application/json")]
public sealed class ConfigurationDiffController : ControllerBase
{
    private readonly IDiffService _diffService;
    private readonly IVersioningService _versioningService;
    private readonly ILogger<ConfigurationDiffController> _logger;

    public ConfigurationDiffController(
        IDiffService diffService,
        IVersioningService versioningService,
        ILogger<ConfigurationDiffController> logger)
    {
        _diffService = diffService;
        _versioningService = versioningService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the diff between two configuration versions
    /// </summary>
    /// <param name="configurationId">The configuration identifier</param>
    /// <param name="fromVersionId">The source version identifier</param>
    /// <param name="toVersionId">The target version identifier</param>
    /// <returns>JSON response listing added/removed/changed keys</returns>
    [HttpGet("compare/{fromVersionId}/{toVersionId}")]
    [ProducesResponseType(typeof(ConfigurationDiff), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfigurationDiff(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid fromVersionId,
        [FromRoute] Guid toVersionId)
    {
        try
        {
            // Validate that both versions belong to the specified configuration
            var fromVersion = await _versioningService.GetVersionAsync(fromVersionId);
            var toVersion = await _versioningService.GetVersionAsync(toVersionId);

            if (fromVersion is null || toVersion is null)
            {
                return NotFound(new { error = "One or both versions not found" });
            }

            if (fromVersion.ConfigurationId != configurationId || toVersion.ConfigurationId != configurationId)
            {
                return NotFound(new { error = "Versions do not belong to the specified configuration" });
            }

            // Get the user ID from the request
            var userId = User.Identity?.Name ?? "system";

            // Generate the diff using DiffService
            var diff = await _diffService.GenerateDiffAsync(fromVersionId, toVersionId, userId);

            return Ok(diff);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration or version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating configuration diff");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a summary of changes between two versions
    /// </summary>
    /// <param name="configurationId">The configuration identifier</param>
    /// <param name="version1Id">First version identifier</param>
    /// <param name="version2Id">Second version identifier</param>
    /// <returns>JSON summary of added/removed/changed counts</returns>
    [HttpGet("summary/{version1Id}/{version2Id}")]
    [ProducesResponseType(typeof(ConfigurationDiffSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfigurationDiffSummary(
        [FromRoute] Guid configurationId,
        [FromRoute] Guid version1Id,
        [FromRoute] Guid version2Id)
    {
        try
        {
            // Validate that both versions belong to the specified configuration
            var version1 = await _versioningService.GetVersionAsync(version1Id);
            var version2 = await _versioningService.GetVersionAsync(version2Id);

            if (version1 is null || version2 is null)
            {
                return NotFound(new { error = "One or both versions not found" });
            }

            if (version1.ConfigurationId != configurationId || version2.ConfigurationId != configurationId)
            {
                return NotFound(new { error = "Versions do not belong to the specified configuration" });
            }

            // Get the diff summary using DiffService
            var summary = await _diffService.ComparVersionsAsync(version1Id, version2Id);

            return Ok(summary);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration or version not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating configuration diff summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for getting configuration diff
/// </summary>
public sealed class GetConfigurationDiffRequest
{
    /// <summary>Source version identifier</summary>
    public Guid FromVersionId { get; set; }

    /// <summary>Target version identifier</summary>
    public Guid ToVersionId { get; set; }
}