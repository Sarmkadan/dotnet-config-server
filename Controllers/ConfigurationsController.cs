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
public sealed class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IVersioningService _versioningService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<ConfigurationsController> _logger;

    private const string SystemUserId = "system";
    private const string ConfigurationNotFoundMessage = "Configuration not found";
    private const string InternalServerErrorMessage = "Internal server error";

    private string CurrentUserId => User.Identity?.Name ?? SystemUserId;

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
        ArgumentNullException.ThrowIfNull(configuration);
        return await ExecuteAsync(async () =>
        {
            var created = await _configurationService.CreateAsync(configuration, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }, "Error creating configuration");
    }

    /// <summary>
    /// Gets a configuration by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Configuration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        return await ExecuteAsync(async () =>
        {
            var config = await _configurationService.GetByIdAsync(id);
            if (config is null)
                return NotFound(new { error = ConfigurationNotFoundMessage });

            return Ok(config);
        }, $"Error retrieving configuration {id}");
    }

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    [HttpGet("application/{applicationId}")]
    [ProducesResponseType(typeof(List<Configuration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplication([FromRoute] Guid applicationId)
    {
        return await ExecuteAsync(async () =>
        {
            var configs = await _configurationService.GetByApplicationAsync(applicationId);
            return Ok(configs);
        }, $"Error retrieving configurations for application {applicationId}");
    }

    /// <summary>
    /// Updates a configuration
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Configuration), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return await ExecuteAsync(async () =>
        {
            var updated = await _configurationService.UpdateAsync(id, configuration, CurrentUserId);
            return Ok(updated);
        }, $"Error updating configuration {id}");
    }

    /// <summary>
    /// Deletes a configuration
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        return await ExecuteAsync(async () =>
        {
            await _configurationService.DeleteAsync(id, CurrentUserId);
            return NoContent();
        }, $"Error deleting configuration {id}");
    }

    /// <summary>
    /// Gets all keys for a configuration
    /// </summary>
    [HttpGet("{configurationId}/keys")]
    [ProducesResponseType(typeof(List<ConfigurationKey>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeys([FromRoute] Guid configurationId)
    {
        return await ExecuteAsync(async () =>
        {
            var keys = await _configurationService.GetKeysAsync(configurationId);
            return Ok(keys);
        }, "Error retrieving configuration keys");
    }

    /// <summary>
    /// Adds a key to a configuration
    /// </summary>
    [HttpPost("{configurationId}/keys")]
    [ProducesResponseType(typeof(ConfigurationKey), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddKey([FromRoute] Guid configurationId, [FromBody] ConfigurationKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return await ExecuteAsync(async () =>
        {
            var created = await _configurationService.AddKeyAsync(configurationId, key, CurrentUserId);
            return CreatedAtAction(nameof(GetKeys), new { configurationId }, created);
        }, "Error adding configuration key");
    }

    /// <summary>
    /// Searches for configurations
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Configuration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] Guid? applicationId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);
        return await ExecuteAsync(async () =>
        {
            var results = await _configurationService.SearchAsync(query, applicationId);
            return Ok(results);
        }, "Error searching configurations");
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
        return await ExecuteAsync(async () =>
        {
            var results = await _configurationService.SearchKeysAsync(q, prefix, configurationId);
            return Ok(results);
        }, "Error searching configuration keys");
    }

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action, string errorContext)
    {
        try
        {
            return await action();
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = ConfigurationNotFoundMessage });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, errorContext);
            return StatusCode(500, new { error = InternalServerErrorMessage });
        }
    }
}
