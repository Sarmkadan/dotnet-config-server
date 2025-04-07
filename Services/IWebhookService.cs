#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Events;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for webhook management and delivery
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Creates a webhook subscription
    /// </summary>
    Task<WebhookSubscription> CreateSubscriptionAsync(WebhookSubscription subscription, string userId);

    /// <summary>
    /// Gets a webhook subscription
    /// </summary>
    Task<WebhookSubscription?> GetSubscriptionAsync(Guid subscriptionId);

    /// <summary>
    /// Gets all webhooks for a configuration
    /// </summary>
    Task<List<WebhookSubscription>> GetSubscriptionsAsync(Guid configurationId);

    /// <summary>
    /// Updates a webhook subscription
    /// </summary>
    Task<WebhookSubscription> UpdateSubscriptionAsync(Guid subscriptionId, WebhookSubscription subscription, string userId);

    /// <summary>
    /// Deletes a webhook subscription
    /// </summary>
    Task DeleteSubscriptionAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Delivers webhook to a specific subscription
    /// </summary>
    Task<WebhookDelivery> DeliverAsync(Guid subscriptionId, string payload, Guid versionId);

    /// <summary>
    /// Gets webhook delivery history
    /// </summary>
    Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId);

    /// <summary>
    /// Retries failed webhook deliveries
    /// </summary>
    Task<int> RetryFailedDeliveriesAsync(int maxRetries = 5);

    /// <summary>
    /// Activates a webhook subscription
    /// </summary>
    Task<WebhookSubscription> ActivateAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Deactivates a webhook subscription
    /// </summary>
    Task<WebhookSubscription> DeactivateAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Notifies relevant webhook subscriptions about a domain event.
    /// </summary>
    Task NotifyAsync(string eventType, DomainEvent payload);
}
