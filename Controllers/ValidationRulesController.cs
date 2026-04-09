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
/// API controller for managing configuration validation rules.
/// </summary>
[ApiController]
[Route("api/v1/configurations/{configurationId}/validation-rules")]
[Produces("application/json")]
sealed public class ValidationRulesController : ControllerBase
{
    private readonly IValidationRuleService _validationRuleService;
    private readonly ILogger<ValidationRulesController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRulesController"/>.
    /// </summary>
    public ValidationRulesController(
        IValidationRuleService validationRuleService,
        ILogger<ValidationRulesController> logger)
    {
        _validationRuleService = validationRuleService;
        _logger = logger;
    }

    /// <summary>
    /// Lists validation rules for a configuration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ValidationRule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules([FromRoute] Guid configurationId)
    {
        try
        {
            var rules = await _validationRuleService.GetRulesAsync(configurationId);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation rules for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Creates a validation rule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ValidationRule), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRule([FromRoute] Guid configurationId, [FromBody] ValidationRule rule)
    {
        try
        {
            var userId = User.Identity?.Name ?? "system";
            rule.ConfigurationId = configurationId;
            var created = await _validationRuleService.CreateRuleAsync(rule, userId);
            return CreatedAtAction(nameof(GetRule), new { configurationId, ruleId = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating validation rule for configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a validation rule by identifier.
    /// </summary>
    [HttpGet("{ruleId}")]
    [ProducesResponseType(typeof(ValidationRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRule([FromRoute] Guid configurationId, [FromRoute] Guid ruleId)
    {
        try
        {
            var rule = await _validationRuleService.GetRuleAsync(ruleId);
            if (rule is null || rule.ConfigurationId != configurationId)
                return NotFound(new { error = "Validation rule not found" });

            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a validation rule.
    /// </summary>
    [HttpPut("{ruleId}")]
    [ProducesResponseType(typeof(ValidationRule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRule([FromRoute] Guid configurationId, [FromRoute] Guid ruleId, [FromBody] ValidationRule rule)
    {
        try
        {
            var existing = await _validationRuleService.GetRuleAsync(ruleId);
            if (existing is null || existing.ConfigurationId != configurationId)
                return NotFound(new { error = "Validation rule not found" });

            rule.ConfigurationId = configurationId;
            var updated = await _validationRuleService.UpdateRuleAsync(ruleId, rule, User.Identity?.Name ?? "system");
            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.Errors });
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Validation rule not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a validation rule.
    /// </summary>
    [HttpDelete("{ruleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule([FromRoute] Guid configurationId, [FromRoute] Guid ruleId)
    {
        try
        {
            var existing = await _validationRuleService.GetRuleAsync(ruleId);
            if (existing is null || existing.ConfigurationId != configurationId)
                return NotFound(new { error = "Validation rule not found" });

            await _validationRuleService.DeleteRuleAsync(ruleId);
            return NoContent();
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Validation rule not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting validation rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validates a configuration or version against active rules.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationRuleResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateConfiguration(
        [FromRoute] Guid configurationId,
        [FromBody] ValidationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _validationRuleService.ValidateConfigurationAsync(configurationId, request?.VersionId, cancellationToken);
            return Ok(result);
        }
        catch (ConfigurationNotFoundException)
        {
            return NotFound(new { error = "Configuration not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration {ConfigurationId}", configurationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request body for validation execution.
/// </summary>
sealed public class ValidationRequest
{
    /// <summary>
    /// Gets or sets the optional version identifier.
    /// </summary>
    public Guid? VersionId { get; set; }
}
