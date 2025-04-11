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
    /// <param name="subscription">The webhook subscription to create.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The created webhook subscription.</returns>
    Task<WebhookSubscription> CreateSubscriptionAsync(WebhookSubscription subscription, string userId);

    /// <summary>
    /// Gets a webhook subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to retrieve.</param>
    /// <returns>The webhook subscription if found, otherwise null.</returns>
    Task<WebhookSubscription?> GetSubscriptionAsync(Guid subscriptionId);

    /// <summary>
    /// Gets all webhooks for a configuration
    /// </summary>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <returns>A list of webhook subscriptions.</returns>
    Task<List<WebhookSubscription>> GetSubscriptionsAsync(Guid configurationId);

    /// <summary>
    /// Updates a webhook subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to update.</param>
    /// <param name="subscription">The webhook subscription data.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The updated webhook subscription.</returns>
    Task<WebhookSubscription> UpdateSubscriptionAsync(Guid subscriptionId, WebhookSubscription subscription, string userId);

    /// <summary>
    /// Deletes a webhook subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to delete.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    Task DeleteSubscriptionAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Delivers webhook to a specific subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="payload">The webhook payload.</param>
    /// <param name="versionId">The ID of the version.</param>
    /// <returns>The webhook delivery record.</returns>
    Task<WebhookDelivery> DeliverAsync(Guid subscriptionId, string payload, Guid versionId);

    /// <summary>
    /// Gets webhook delivery history
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <returns>A list of webhook delivery records.</returns>
    Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId);

    /// <summary>
    /// Retries failed webhook deliveries
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <returns>The number of retries performed.</returns>
    Task<int> RetryFailedDeliveriesAsync(int maxRetries = 5);

    /// <summary>
    /// Activates a webhook subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to activate.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The activated webhook subscription.</returns>
    Task<WebhookSubscription> ActivateAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Deactivates a webhook subscription
    /// </summary>
    /// <param name="subscriptionId">The ID of the subscription to deactivate.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The deactivated webhook subscription.</returns>
    Task<WebhookSubscription> DeactivateAsync(Guid subscriptionId, string userId);

    /// <summary>
    /// Notifies relevant webhook subscriptions about a domain event.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="payload">The domain event payload.</param>
    Task NotifyAsync(string eventType, DomainEvent payload);
}
