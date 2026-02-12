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
/// API controller for retrieving audit logs.
/// Provides read-only access to audit trail for compliance and debugging.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
sealed public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogRepository repository, ILogger<AuditLogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets audit logs for a specific entity with pagination.
    /// </summary>
    [HttpGet("entity/{entityId}")]
    [ProducesResponseType(typeof(PaginatedResult<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(
        [FromRoute] string entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _repository.GetByEntityIdAsync(entityId);
            var total = logs.Count;
            var items = logs
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation("Retrieved audit logs for entity {EntityId}", entityId);

            return Ok(new PaginatedResult<AuditLog>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for entity {EntityId}", entityId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets audit logs by user with pagination.
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(PaginatedResult<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(
        [FromRoute] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _repository.GetByUserIdAsync(userId);
            var total = logs.Count;
            var items = logs
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PaginatedResult<AuditLog>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all audit logs with date filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AuditLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _repository.GetAllAsync();

            // Apply filters
            if (from.HasValue)
                logs = logs.Where(l => l.CreatedAt >= from).ToList();

            if (to.HasValue)
                logs = logs.Where(l => l.CreatedAt <= to).ToList();

            if (!string.IsNullOrEmpty(action))
                logs = logs.Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase)).ToList();

            var total = logs.Count;
            var items = logs
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PaginatedResult<AuditLog>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a specific audit log by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuditLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var log = await _repository.GetByIdAsync(id);
            if (log is null)
                return NotFound(new { error = "Audit log not found" });

            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {LogId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a summary of recent changes.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AuditSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] int days = 7)
    {
        try
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var logs = await _repository.GetAllAsync();
            var recentLogs = logs.Where(l => l.CreatedAt >= since).ToList();

            var summary = new AuditSummary
            {
                TotalChanges = recentLogs.Count,
                CreateCount = recentLogs.Count(l => l.Action.Contains("Created")),
                UpdateCount = recentLogs.Count(l => l.Action.Contains("Updated")),
                DeleteCount = recentLogs.Count(l => l.Action.Contains("Deleted")),
                UniqueUsers = recentLogs.Select(l => l.UserId).Distinct().Count(),
                LastChange = recentLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault()?.CreatedAt
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit summary");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

sealed public class AuditSummary
{
    public int TotalChanges { get; set; }
    public int CreateCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime? LastChange { get; set; }
}
