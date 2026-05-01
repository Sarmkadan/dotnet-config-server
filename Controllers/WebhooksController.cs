#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for managing webhook subscriptions.
/// Webhooks allow external systems to be notified of configuration changes.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
sealed public class WebhooksController : ControllerBase
{
    private readonly IWebhookSubscriptionRepository _repository;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookSubscriptionRepository repository,
        IWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _repository = repository;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new webhook subscription.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] WebhookSubscription subscription)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subscription.Url))
                return BadRequest(new { error = "Webhook URL is required" });

            if (!Uri.TryCreate(subscription.Url, UriKind.Absolute, out _))
                return BadRequest(new { error = "Invalid webhook URL" });

            subscription.Id = Guid.NewGuid();
            subscription.CreatedAt = DateTime.UtcNow;
            subscription.IsActive = true;

            await _repository.AddAsync(subscription);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription {SubId} created for {Url}", subscription.Id, subscription.Url);
            return CreatedAtAction(nameof(GetById), new { id = subscription.Id }, subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook subscription");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a webhook subscription by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var subscription = await _repository.GetByIdAsync(id);
            if (subscription is null)
                return NotFound(new { error = "Webhook subscription not found" });

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook subscription {SubId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all webhook subscriptions for an application.
    /// </summary>
    [HttpGet("application/{applicationId}")]
    [ProducesResponseType(typeof(List<WebhookSubscription>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplication([FromRoute] Guid applicationId)
    {
        try
        {
            var subscriptions = await _repository.GetByApplicationIdAsync(applicationId);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for application {AppId}", applicationId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates a webhook subscription.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(WebhookSubscription), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] WebhookSubscription subscription)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null)
                return NotFound(new { error = "Webhook subscription not found" });

            existing.Url = subscription.Url;
            existing.Events = subscription.Events;
            existing.Secret = subscription.Secret;
            existing.IsActive = subscription.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription {SubId} updated", id);
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook subscription {SubId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deletes a webhook subscription.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            var subscription = await _repository.GetByIdAsync(id);
            if (subscription is null)
                return NotFound(new { error = "Webhook subscription not found" });

            await _repository.DeleteAsync(subscription);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription {SubId} deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook subscription {SubId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Tests a webhook subscription by sending a test payload.
    /// </summary>
    [HttpPost("{id}/test")]
    [ProducesResponseType(typeof(WebhookTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Test([FromRoute] Guid id)
    {
        try
        {
            var subscription = await _repository.GetByIdAsync(id);
            if (subscription is null)
                return NotFound(new { error = "Webhook subscription not found" });

            var testPayload = new { message = "Webhook test", timestamp = DateTime.UtcNow };
            var result = await _webhookService.TestWebhookAsync(subscription, testPayload);

            return Ok(new WebhookTestResult
            {
                Success = result,
                Timestamp = DateTime.UtcNow,
                Message = result ? "Test successful" : "Test failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook subscription {SubId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets recent deliveries for a webhook subscription.
    /// </summary>
    [HttpGet("{id}/deliveries")]
    [ProducesResponseType(typeof(List<WebhookDelivery>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveries([FromRoute] Guid id, [FromQuery] int limit = 20)
    {
        try
        {
            var deliveries = await _repository.GetDeliveriesBySubscriptionAsync(id);
            var recent = deliveries.OrderByDescending(d => d.DeliveredAt).Take(limit).ToList();

            return Ok(recent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deliveries for webhook {SubId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

sealed public class WebhookTestResult
{
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}
