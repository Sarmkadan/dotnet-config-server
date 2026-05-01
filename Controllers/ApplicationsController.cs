#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for managing applications.
/// Applications are the top-level containers for configurations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
sealed public class ApplicationsController : ControllerBase
{
    private readonly IApplicationRepository _repository;
    private readonly IConfigurationRepository _configRepository;
    private readonly ILogger<ApplicationsController> _logger;

    public ApplicationsController(
        IApplicationRepository repository,
        IConfigurationRepository configRepository,
        ILogger<ApplicationsController> logger)
    {
        _repository = repository;
        _configRepository = configRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new application.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Application), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Application application)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(application.Name))
                return BadRequest(new { error = "Application name is required" });

            application.Id = Guid.NewGuid();
            application.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(application);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Application {AppId} created: {AppName}", application.Id, application.Name);
            return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets an application by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var application = await _repository.GetByIdAsync(id);
            if (application is null)
                return NotFound(new { error = "Application not found" });

            return Ok(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application {AppId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all applications with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<Application>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var apps = await _repository.GetAllAsync();
            var total = apps.Count;
            var items = apps.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new PaginatedResult<Application>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates an application.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] Application application)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { error = "Application not found" });

            existing.Name = application.Name;
            existing.Description = application.Description;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Application {AppId} updated", id);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application {AppId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes an application and all its configurations.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { error = "Application not found" });

            await _repository.DeleteAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Application {AppId} deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting application {AppId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all configurations for an application.
    /// </summary>
    [HttpGet("{applicationId}/configurations")]
    [ProducesResponseType(typeof(List<Configuration>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigurations([FromRoute] Guid applicationId)
    {
        try
        {
            var configs = await _configRepository.GetByApplicationIdAsync(applicationId);
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for application {AppId}", applicationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

sealed public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
