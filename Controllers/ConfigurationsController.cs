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
/// API controller for managing configurations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
sealed public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IVersioningService _versioningService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<ConfigurationsController> _logger;

    public ConfigurationsController(
        IConfigurationService configurationService,
        IVersioningService versioningService,
        IWebhookService webhookService,
        ILogger<ConfigurationsController> logger)
    {
        _configurationService = configurationService;
        _versioningService = versioningService;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Configuration), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Configuration configuration)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var created = await _configurationService.CreateAsync(configuration, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Configuration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var config = await _configurationService.GetByIdAsync(id);
            if (config is null)
                return NotFound(new { error = "Configuration not found" });

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {ConfigId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    [HttpGet("application/{applicationId}")]
    [ProducesResponseType(typeof(List<Configuration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplication([FromRoute] Guid applicationId)
    {
        try
        {
            var configs = await _configurationService.GetByApplicationAsync(applicationId);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for application {AppId}", applicationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a configuration
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Configuration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Configuration configuration)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var updated = await _configurationService.UpdateAsync(id, configuration, userId);
            return Ok(updated);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration not found" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {ConfigId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a configuration
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            await _configurationService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {ConfigId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all keys for a configuration
    /// </summary>
    [HttpGet("{configurationId}/keys")]
    [ProducesResponseType(typeof(List<ConfigurationKey>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeys([FromRoute] Guid configurationId)
    {
        try
        {
            var keys = await _configurationService.GetKeysAsync(configurationId);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration keys");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Adds a key to a configuration
    /// </summary>
    [HttpPost("{configurationId}/keys")]
    [ProducesResponseType(typeof(ConfigurationKey), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddKey([FromRoute] Guid configurationId, [FromBody] ConfigurationKey key)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            var created = await _configurationService.AddKeyAsync(configurationId, key, userId);
            return CreatedAtAction(nameof(GetKeys), new { configurationId }, created);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration not found" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding configuration key");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Searches for configurations
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Configuration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] Guid? applicationId = null)
    {
        try
        {
            var results = await _configurationService.SearchAsync(query, applicationId);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configurations");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Searches configuration keys by text and/or key prefix
    /// </summary>
    [HttpGet("keys/search")]
    [ProducesResponseType(typeof(List<ConfigurationKey>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchKeys(
        [FromQuery] string? q = null,
        [FromQuery] string? prefix = null,
        [FromQuery] Guid? configurationId = null)
    {
        try
        {
            var results = await _configurationService.SearchKeysAsync(q, prefix, configurationId);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configuration keys");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
