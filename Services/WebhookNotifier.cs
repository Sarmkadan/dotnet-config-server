#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotnetConfigServer.Events;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// High-level notifier that listens to domain events via <see cref="IEventBus"/> and
/// fans them out to <see cref="IWebhookService"/> subscriptions. Acts as the glue
/// between the event system and webhook delivery, handling serialization and error isolation.
/// </summary>
public sealed class WebhookNotifier : IDisposable
{
    private readonly IWebhookService _webhookService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WebhookNotifier> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookNotifier"/> class.
    /// </summary>
    /// <param name="webhookService">The webhook service for subscription lookup and delivery.</param>
    /// <param name="eventBus">The event bus to subscribe to.</param>
    /// <param name="logger">The logger instance.</param>
    public WebhookNotifier(
        IWebhookService webhookService,
        IEventBus eventBus,
        ILogger<WebhookNotifier> logger)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Handles a domain event by serializing it and forwarding to the webhook service.
    /// Errors are logged but never rethrown to avoid breaking the event pipeline.
    /// </summary>
    /// <param name="domainEvent">The event to notify subscribers about.</param>
    public async Task NotifyAsync(DomainEvent domainEvent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType().Name;

        try
        {
            _logger.LogInformation(ConfigServerTelemetry.WebhookEvents.NotificationStarted,
                "Processing webhook notification for event {EventType} ({EventId}), configuration {ConfigurationId}",
                eventType, domainEvent.Id, domainEvent.ConfigurationId);

            // Per-delivery outcomes (webhook-sent / webhook-failed) are logged and counted by
            // WebhookService, which performs the HTTP calls; this method only sees fan-out errors.
            await _webhookService.NotifyAsync(eventType, domainEvent).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ConfigServerTelemetry.WebhookEvents.NotificationFailed, ex,
                "Failed to send webhook notification for event {EventType} ({EventId})",
                eventType, domainEvent.Id);
        }
    }

    /// <summary>
    /// Sends a test notification to a specific subscription to verify connectivity.
    /// </summary>
    /// <param name="subscriptionId">The subscription to test.</param>
    /// <returns><c>true</c> if the test delivery succeeded.</returns>
    public async Task<bool> TestSubscriptionAsync(Guid subscriptionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var subscription = await _webhookService.GetSubscriptionAsync(subscriptionId).ConfigureAwait(false);
        if (subscription is null)
        {
            _logger.LogWarning("Cannot test subscription {SubscriptionId}: not found", subscriptionId);
            return false;
        }

        var testPayload = new
        {
            type = "test",
            timestamp = DateTime.UtcNow,
            subscriptionId
        };

        var result = await _webhookService.TestWebhookAsync(subscription, testPayload).ConfigureAwait(false);
        _logger.LogInformation("Test webhook to {Url}: {Result}",
            subscription.Url, result ? "success" : "failed");
        return result;
    }

    /// <summary>
    /// Retries all failed deliveries up to the specified limit.
    /// </summary>
    /// <param name="maxRetries">Maximum retries per delivery.</param>
    /// <returns>The number of deliveries retried.</returns>
    public async Task<int> RetryFailedAsync(int maxRetries = 3)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _logger.LogInformation("Starting failed webhook delivery retry (max {MaxRetries})", maxRetries);
        var count = await _webhookService.RetryFailedDeliveriesAsync(maxRetries).ConfigureAwait(false);
        _logger.LogInformation("Retried {Count} failed webhook deliveries", count);
        return count;
    }

    /// <summary>
    /// Serializes an object to JSON using the notifier's configured serializer options.
    /// </summary>
    /// <param name="payload">The object to serialize.</param>
    /// <returns>JSON string.</returns>
    public string SerializePayload(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return JsonSerializer.Serialize(payload, _jsonOptions);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("WebhookNotifier disposed");
    }
}
