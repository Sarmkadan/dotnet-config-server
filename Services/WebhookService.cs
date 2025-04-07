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
using DotnetConfigServer.Events; // Add this using directive for DomainEvent types

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for managing webhooks and their delivery
/// </summary>
sealed public class WebhookService : IWebhookService
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

                _ = SendWebhookAsync(subscription, delivery);
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
        return await _deliveryRepository.GetBySubscriptionAsync(subscriptionId);
    }

    /// <summary>
    /// Retries failed webhook deliveries
    /// </summary>
    public async Task<int> RetryFailedDeliveriesAsync(int maxRetries = 5)
    {
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
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription is null)
            throw new ConfigurationNotFoundException(subscriptionId.ToString());

        subscription.Deactivate();
        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        return subscription;
    }

    private async Task SendWebhookAsync(WebhookSubscription subscription, WebhookDelivery delivery)
    {
        var startTime = DateTime.UtcNow;
        var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
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

        var response = await _httpClient.SendAsync(request);
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