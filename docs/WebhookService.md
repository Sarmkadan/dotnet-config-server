# WebhookService

Central service for managing webhook subscriptions, deliveries, and retry logic in the dotnet-config-server. It handles the lifecycle of subscriptions (create, read, update, delete), tracks delivery attempts, and provides mechanisms for retrying failed deliveries and testing endpoints.

## API

### `WebhookService`

Constructor for the service. Requires dependencies for subscription storage, delivery retry policy, and an HTTP client for outbound calls.

### `async Task<WebhookSubscription> CreateSubscriptionAsync(string url, string? secret = null, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)`

Creates a new webhook subscription with the specified target URL, optional secret for HMAC signing, and metadata. The subscription is initially inactive and must be activated before deliveries occur.

- **Parameters**
  - `url`: Absolute HTTPS URL to receive webhook payloads.
  - `secret`: Optional secret used to sign payloads via HMAC-SHA256.
  - `metadata`: Optional key-value pairs for additional context.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: The created subscription with assigned identifier and initial state.
- **Throws**
  - `ArgumentException`: If `url` is null, empty, or not HTTPS.
  - `InvalidOperationException`: If a subscription with the same URL already exists.

### `async Task<WebhookSubscription?> GetSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)`

Retrieves a subscription by its unique identifier.

- **Parameters**
  - `id`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: The subscription if found; otherwise `null`.
- **Throws**: None.

### `async Task<List<WebhookSubscription>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)`

Returns all active and inactive subscriptions.

- **Parameters**
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: List of all subscriptions.
- **Throws**: None.

### `async Task<WebhookSubscription> UpdateSubscriptionAsync(Guid id, string? url = null, string? secret = null, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)`

Updates an existing subscription’s URL, secret, or metadata. Only provided values are changed; omitted fields remain unchanged.

- **Parameters**
  - `id`: Subscription identifier.
  - `url`: New target URL (must be HTTPS if provided).
  - `secret`: New secret for HMAC signing.
  - `metadata`: Updated metadata dictionary.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: The updated subscription.
- **Throws**
  - `ArgumentException`: If `url` is provided and invalid.
  - `KeyNotFoundException`: If no subscription exists with `id`.

### `async Task DeleteSubscriptionAsync(Guid id, CancellationToken cancellationToken = default)`

Removes a subscription and all associated delivery records.

- **Parameters**
  - `id`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Throws**: `KeyNotFoundException` if the subscription does not exist.

### `async Task<WebhookDelivery> DeliverAsync(Guid subscriptionId, string eventType, string payload, CancellationToken cancellationToken = default)`

Initiates a delivery attempt for a specific event to a subscription. Automatically records the attempt and schedules retries if delivery fails.

- **Parameters**
  - `subscriptionId`: Target subscription identifier.
  - `eventType`: Semantic type of the event (e.g., "config-updated").
  - `payload`: Serialized event data.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: Delivery record with status and attempt metadata.
- **Throws**
  - `KeyNotFoundException`: If no subscription exists with `subscriptionId`.
  - `InvalidOperationException`: If the subscription is inactive.

### `async Task NotifyAsync(string eventType, string payload, CancellationToken cancellationToken = default)`

Broadcasts an event to all active subscriptions. Initiates delivery attempts for each eligible subscription.

- **Parameters**
  - `eventType`: Semantic type of the event.
  - `payload`: Serialized event data.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: None.
- **Throws**: None.

### `async Task<List<WebhookDelivery>> GetDeliveriesAsync(Guid subscriptionId, CancellationToken cancellationToken = default)`

Returns all delivery attempts for a subscription.

- **Parameters**
  - `subscriptionId`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: List of delivery records, ordered by attempt time descending.
- **Throws**: `KeyNotFoundException` if the subscription does not exist.

### `async Task<int> RetryFailedDeliveriesAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)`

Retries all failed deliveries up to `maxAttempts` times. Each retry increments the attempt count and updates status.

- **Parameters**
  - `maxAttempts`: Maximum number of delivery attempts per failed record.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: Number of deliveries retried.
- **Throws**: None.

### `async Task<WebhookSubscription> ActivateAsync(Guid id, CancellationToken cancellationToken = default)`

Activates a subscription so that future events are delivered.

- **Parameters**
  - `id`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: The activated subscription.
- **Throws**
  - `KeyNotFoundException`: If no subscription exists with `id`.
  - `InvalidOperationException`: If the subscription is already active.

### `async Task<WebhookSubscription> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)`

Deactivates a subscription so that no further events are delivered.

- **Parameters**
  - `id`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: The deactivated subscription.
- **Throws**
  - `KeyNotFoundException`: If no subscription exists with `id`.
  - `InvalidOperationException`: If the subscription is already inactive.

### `async Task<bool> RetryWebhookDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken = default)`

Retries a specific failed delivery attempt once.

- **Parameters**
  - `deliveryId`: Delivery record identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: `true` if a retry was initiated; `false` if the delivery is not in a retryable state.
- **Throws**: `KeyNotFoundException` if no delivery exists with `deliveryId`.

### `async Task<bool> TestWebhookAsync(Guid subscriptionId, CancellationToken cancellationToken = default)`

Sends a lightweight test payload to the subscription’s URL to verify connectivity and response handling. Does not record a delivery.

- **Parameters**
  - `subscriptionId`: Subscription identifier.
  - `cancellationToken`: Propagates notification that the operation should be canceled.
- **Returns**: `true` if the endpoint responded with HTTP 2xx; otherwise `false`.
- **Throws**
  - `KeyNotFoundException`: If no subscription exists with `subscriptionId`.
  - `InvalidOperationException`: If the subscription is inactive.

## Usage
