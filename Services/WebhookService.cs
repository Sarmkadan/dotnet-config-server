#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Events;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for managing webhooks and their delivery
/// </summary>
public sealed class WebhookService : IWebhookService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookDeliveryRepository _deliveryRepository;
    private readonly ILogger<WebhookService> _logger;
    private readonly HttpClient _httpClient;

    // In-flight guard: prevents duplicate deliveries when NotifyAsync is called concurrently
    // for the same (eventId, subscriptionId) pair before the DB record is persisted.
    private readonly ConcurrentDictionary<(Guid eventId, Guid subscriptionId), bool> _pendingDeliveries = new();

    public WebhookService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookDeliveryRepository deliveryRepository,
        ILogger<WebhookService> logger,
        HttpClient httpClient)
    {
        _subscriptionRepository = subscriptionRepository;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(AppConstants.Webhook.TimeoutSeconds);
    }

    /// <summary>
    /// Creates a webhook subscription
    /// </summary>
    public async Task<WebhookSubscription> CreateSubscriptionAsync(WebhookSubscription subscription, string userId)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        subscription.Validate();
        subscription.CreatedBy = userId;

        if (subscription.VerifySignature && string.IsNullOrEmpty(subscription.Secret))
        {
            subscription.Secret = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        }

        await _subscriptionRepository.AddAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription {WebhookId} created by {UserId}",
            subscription.Id, userId);

        return subscription;
    }

    /// <summary>
    /// Gets a webhook subscription
    /// </summary>
    public async Task<WebhookSubscription?> GetSubscriptionAsync(Guid subscriptionId)
    {
        return await _subscriptionRepository.GetByIdAsync(subscriptionId);
    }

    /// <summary>
    /// Gets all webhooks for a configuration
    /// </summary>
    public async Task<List<WebhookSubscription>> GetSubscriptionsAsync(Guid configurationId)
    {
        return await _subscriptionRepository.GetByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Updates a webhook subscription
    /// </summary>
    public async Task<WebhookSubscription> UpdateSubscriptionAsync(Guid subscriptionId, WebhookSubscription subscription, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var existing = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (existing is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        existing.Name = subscription.Name;
        existing.Url = subscription.Url;
        existing.Description = subscription.Description;
        existing.UpdatedBy = userId;
        existing.UpdatedAt = DateTime.UtcNow;

        existing.Validate();

        await _subscriptionRepository.UpdateAsync(existing);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription {WebhookId} updated by {UserId}",
            subscriptionId, userId);

        return existing;
    }

    /// <summary>
    /// Deletes a webhook subscription
    /// </summary>
    public async Task DeleteSubscriptionAsync(Guid subscriptionId, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        subscription.IsActive = false;
        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Webhook subscription {WebhookId} deleted by {UserId}",
            subscriptionId, userId);
    }

    /// <summary>
    /// Delivers webhook to a specific subscription
    /// </summary>
    public async Task<WebhookDelivery> DeliverAsync(Guid subscriptionId, string payload, Guid versionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentOutOfRangeException.ThrowIfEqual(versionId, Guid.Empty);
        
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        var delivery = new WebhookDelivery
        {
            WebhookSubscriptionId = subscriptionId,
            ConfigurationVersionId = versionId,
            Payload = payload,
            Status = Common.WebhookDeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            AttemptNumber = 1
        };

        await _deliveryRepository.AddAsync(delivery);
        await _deliveryRepository.SaveChangesAsync();

        try
        {
            await SendWebhookAsync(subscription, delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook {WebhookId}", subscriptionId);
            delivery.MarkFailed(ex.Message);
            delivery.ScheduleRetry();
            await _deliveryRepository.UpdateAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();
        }

        return delivery;
    }
    
    /// <summary>
    /// Notifies relevant webhook subscriptions about a domain event.
    /// </summary>
    public async Task NotifyAsync(string eventType, DomainEvent payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(payload);
        
        var subscriptions = await _subscriptionRepository.GetActiveWebhooksAsync();
        var serializedPayload = JsonSerializer.Serialize(payload);

        foreach (var subscription in subscriptions)
        {
            if (subscription.TriggerEvents.Count > 0 && !subscription.TriggerEvents.Contains(eventType))
                continue;

            var guardKey = (payload.Id, subscription.Id);

            // Fast in-memory check: skip if another concurrent call is already processing
            // this (eventId, subscriptionId) pair before the DB record is committed.
            if (!_pendingDeliveries.TryAdd(guardKey, true))
            {
                _logger.LogInformation(
                    "Skipping concurrent duplicate webhook delivery for event {EventId} to subscription {SubscriptionId}.",
                    payload.Id, subscription.Id);
                continue;
            }

            try
            {
                // Persistent idempotency check: skip if delivery already recorded in DB.
                var existingDelivery = await _deliveryRepository.GetByEventAndSubscriptionAsync(payload.Id, subscription.Id);
                if (existingDelivery is not null &&
                    (existingDelivery.Status == WebhookDeliveryStatus.Pending || existingDelivery.Status == WebhookDeliveryStatus.Success))
                {
                    _logger.LogInformation(
                        "Skipping duplicate webhook delivery for event {EventId} to subscription {SubscriptionId}. Existing status: {Status}",
                        payload.Id, subscription.Id, existingDelivery.Status);
                    continue;
                }

                var delivery = new WebhookDelivery
                {
                    WebhookSubscriptionId = subscription.Id,
                    Payload = serializedPayload,
                    EventType = eventType,
                    Url = subscription.Url,
                    NextRetryAt = DateTime.UtcNow,
                    EventId = payload.Id,
                    ConfigurationVersionId = payload is ConfigurationVersionCreatedEvent versionEvent
                                             ? versionEvent.VersionId
                                             : (payload is ConfigurationUpdatedEvent updatedEvent
                                                ? updatedEvent.ConfigurationId
                                                : Guid.Empty)
                };

                await _deliveryRepository.AddAsync(delivery);
                await _deliveryRepository.SaveChangesAsync();

                try
                {
                    await SendWebhookAsync(subscription, delivery);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to deliver webhook for event {EventId} to subscription {SubscriptionId}",
                        payload.Id, subscription.Id);
                    delivery.MarkFailed(ex.Message);
                    delivery.ScheduleRetry();
                    await _deliveryRepository.UpdateAsync(delivery);
                }

                await _deliveryRepository.SaveChangesAsync();
            }
            finally
            {
                _pendingDeliveries.TryRemove(guardKey, out _);
            }
        }
    }


    /// <summary>
    /// Gets webhook delivery history
    /// </summary>
    public async Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        
        return await _deliveryRepository.GetBySubscriptionAsync(subscriptionId);
    }

    /// <summary>
    /// Retries failed webhook deliveries
    /// </summary>
    public async Task<int> RetryFailedDeliveriesAsync(int maxRetries = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetries, 1);
        
        var failedDeliveries = await _deliveryRepository.GetFailedDeliveriesAsync();
        int retryCount = 0;

        foreach (var delivery in failedDeliveries)
        {
            if (delivery.AttemptNumber < maxRetries)
            {
                var subscription = await _subscriptionRepository.GetByIdAsync(delivery.WebhookSubscriptionId);
                if (subscription is not null && subscription.IsActive)
                {
                    try
                    {
                        await SendWebhookAsync(subscription, delivery);
                        retryCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Retry failed for delivery {DeliveryId}", delivery.Id);
                        delivery.ScheduleRetry();
                        await _deliveryRepository.UpdateAsync(delivery);
                    }
                }
            }
        }

        await _deliveryRepository.SaveChangesAsync();
        return retryCount;
    }

    /// <summary>
    /// Activates a webhook subscription
    /// </summary>
    public async Task<WebhookSubscription> ActivateAsync(Guid subscriptionId, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        subscription.Activate();
        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        return subscription;
    }

    /// <summary>
    /// Deactivates a webhook subscription
    /// </summary>
    public async Task<WebhookSubscription> DeactivateAsync(Guid subscriptionId, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(subscriptionId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        subscription.Deactivate();
        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        return subscription;
    }

    /// <summary>
    /// Retries a single webhook delivery.
    /// </summary>
    /// <param name="deliveryId">The ID of the delivery to retry.</param>
    /// <returns>True if the delivery succeeded, otherwise false.</returns>
    /// <exception cref="ConfigurationNotFoundException">Thrown when the delivery does not exist.</exception>
    public async Task<bool> RetryWebhookDeliveryAsync(Guid deliveryId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(deliveryId, Guid.Empty);

        var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
        if (delivery is null)
            throw new ConfigurationNotFoundException(deliveryId.ToString());

        var subscription = await _subscriptionRepository.GetByIdAsync(delivery.WebhookSubscriptionId);
        if (subscription is null || !subscription.IsActive)
            return false;

        delivery.AttemptNumber++;

        try
        {
            await SendWebhookAsync(subscription, delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retry failed for delivery {DeliveryId}", deliveryId);
            delivery.MarkFailed(ex.Message);
            delivery.ScheduleRetry();
            await _deliveryRepository.UpdateAsync(delivery);
        }

        await _deliveryRepository.SaveChangesAsync();
        return delivery.Status == WebhookDeliveryStatus.Success;
    }

    /// <summary>
    /// Sends a test payload to a webhook subscription without recording a delivery.
    /// </summary>
    /// <param name="subscription">The subscription to test.</param>
    /// <param name="payload">The test payload to send.</param>
    /// <returns>True if the endpoint responded with a success status code.</returns>
    public async Task<bool> TestWebhookAsync(WebhookSubscription subscription, object payload)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(payload);

        var body = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        if (subscription.VerifySignature && !string.IsNullOrEmpty(subscription.Secret))
            request.Headers.Add("X-Webhook-Signature", subscription.GenerateSignature(body));

        try
        {
            using var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Webhook test failed for subscription {WebhookId}", subscription.Id);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Webhook test timed out for subscription {WebhookId}", subscription.Id);
            return false;
        }
    }

    private async Task SendWebhookAsync(WebhookSubscription subscription, WebhookDelivery delivery)
    {
        var startTime = DateTime.UtcNow;
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
        };

        // Add custom headers
        if (subscription.CustomHeaders is not null)
        {
            foreach (var header in subscription.CustomHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        // Add signature if configured
        if (subscription.VerifySignature && !string.IsNullOrEmpty(subscription.Secret))
        {
            var signature = subscription.GenerateSignature(delivery.Payload);
            request.Headers.Add("X-Webhook-Signature", signature);
        }

        using var response = await _httpClient.SendAsync(request);
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            delivery.MarkSuccess((int)response.StatusCode, (int)elapsed, responseBody);
            subscription.ResetRetryCount((int)response.StatusCode);

            _logger.LogInformation("Webhook {WebhookId} delivered successfully in {Elapsed}ms",
                subscription.Id, elapsed);
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            delivery.MarkFailed(errorBody, (int)response.StatusCode, (int)elapsed);
            subscription.IncrementRetryCount((int)response.StatusCode);

            _logger.LogWarning("Webhook {WebhookId} delivery failed with status {StatusCode}",
                subscription.Id, response.StatusCode);
        }

        await _deliveryRepository.UpdateAsync(delivery);
        await _subscriptionRepository.UpdateAsync(subscription);
    }
}
