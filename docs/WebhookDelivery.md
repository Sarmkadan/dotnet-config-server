# WebhookDelivery

Represents a single delivery attempt of a webhook event to a subscriber endpoint. Each instance tracks the request payload, response details, retry state, and the outcome of the attempt. Instances are created by the system when a webhook subscription triggers and are used to record the result of the HTTP call or to schedule retries on failure.

## API

### Properties

- **`Id`** (`Guid`)  
  Unique identifier for this delivery record.

- **`WebhookSubscriptionId`** (`Guid`)  
  The identifier of the webhook subscription that generated this delivery.

- **`ConfigurationVersionId`** (`Guid`)  
  The identifier of the configuration version that was the source of the event payload.

- **`Status`** (`WebhookDeliveryStatus`)  
  Current status of the delivery (e.g., `Pending`, `Success`, `Failed`). Updated by `MarkSuccess`, `MarkFailed`, or `ScheduleRetry`.

- **`EventId`** (`Guid`)  
  Unique identifier for the event that triggered this delivery.

- **`CreatedAt`** (`DateTime`)  
  Timestamp when the delivery record was created.

- **`SentAt`** (`DateTime?`)  
  Timestamp when the HTTP request was actually sent. `null` if the delivery has not yet been attempted.

- **`AttemptNumber`** (`int`)  
  Zero‑based attempt index. The first attempt is `0`, each subsequent retry increments this value.

- **`StatusCode`** (`int`)  
  HTTP status code returned by the subscriber endpoint. `0` if no response was received.

- **`ResponseTimeMs`** (`int?`)  
  Response time in milliseconds. `null` if the request timed out or failed before a response.

- **`ResponseBody`** (`string?`)  
  Body of the HTTP response from the subscriber. `null` if no response body was captured.

- **`ErrorMessage`** (`string?`)  
  Description of the error that occurred, if any. `null` on success.

- **`Payload`** (`string`)  
  The JSON payload that was sent to the subscriber.

- **`EventType`** (`string?`)  
  The type of event that triggered the delivery (e.g., `"configuration.updated"`). `null` if not set.

- **`Url`** (`string?`)  
  The subscriber endpoint URL to which the payload was sent. `null` if not resolved at creation time.

- **`NextRetryAt`** (`DateTime?`)  
  Scheduled time for the next retry attempt. `null` if no retry is pending or if the delivery has reached its maximum retry count.

### Methods

- **`void MarkSuccess()`**  
  Records the delivery as successful. Sets `Status` to `Success` and updates `SentAt` to the current time.  
  **Throws:** `InvalidOperationException` if the delivery is already in a terminal state (`Success` or `Failed` after all retries exhausted).

- **`void MarkFailed()`**  
  Records the delivery as failed for the current attempt. Sets `Status` to `Failed` and updates `SentAt` to the current time. Does **not** automatically schedule a retry.  
  **Throws:** `InvalidOperationException` if the delivery is already in a terminal state.

- **`void ScheduleRetry()`**  
  Schedules the next retry attempt by setting `NextRetryAt` to a future time based on the current attempt number and the subscription’s retry policy. Increments `AttemptNumber` and resets `StatusCode`, `ResponseTimeMs`, `ResponseBody`, and `ErrorMessage` for the next attempt.  
  **Throws:** `InvalidOperationException` if the delivery is already in a terminal state or if the maximum number of retries has been reached.

- **`bool ShouldRetry()`**  
  Returns `true` if the delivery is eligible for another retry attempt (i.e., status is `Failed` and the maximum retry count has not been exceeded). Does not modify state.

## Usage

### Example 1: Processing a delivery after sending the HTTP request

```csharp
var delivery = new WebhookDelivery
{
    Id = Guid.NewGuid(),
    WebhookSubscriptionId = subscriptionId,
    ConfigurationVersionId = versionId,
    EventId = eventId,
    Payload = payload,
    Url = subscriberUrl,
    EventType = "configuration.updated",
    CreatedAt = DateTime.UtcNow,
    AttemptNumber = 0
};

// Simulate sending the webhook
HttpResponseMessage response = await httpClient.PostAsync(delivery.Url, new StringContent(delivery.Payload));
delivery.StatusCode = (int)response.StatusCode;
delivery.ResponseTimeMs = (int)elapsedMs;
delivery.ResponseBody = await response.Content.ReadAsStringAsync();

if (response.IsSuccessStatusCode)
{
    delivery.MarkSuccess();
}
else
{
    delivery.MarkFailed();
    if (delivery.ShouldRetry())
    {
        delivery.ScheduleRetry();
        // Persist delivery with updated NextRetryAt for background retry worker
    }
}
```

### Example 2: Retry logic in a background worker

```csharp
// Load a delivery that is pending retry
WebhookDelivery delivery = await repository.GetNextPendingRetryAsync();

if (delivery == null) return;

// Send the webhook again
HttpResponseMessage response = await httpClient.PostAsync(delivery.Url, new StringContent(delivery.Payload));
delivery.StatusCode = (int)response.StatusCode;
delivery.ResponseTimeMs = (int)elapsedMs;
delivery.ResponseBody = await response.Content.ReadAsStringAsync();

if (response.IsSuccessStatusCode)
{
    delivery.MarkSuccess();
}
else
{
    delivery.MarkFailed();
    if (delivery.ShouldRetry())
    {
        delivery.ScheduleRetry();
    }
    // else: delivery is terminal, no further retries
}

await repository.SaveAsync(delivery);
```

## Notes

- **Thread safety:** Instances of `WebhookDelivery` are not designed for concurrent access. All property modifications and method calls should be performed from a single thread or protected by external synchronization.
- **Nullability:** Properties `SentAt`, `ResponseTimeMs`, `ResponseBody`, `ErrorMessage`, `EventType`, `Url`, and `NextRetryAt` may be `null`. Code consuming these values should check for `null` before use.
- **Method preconditions:** `MarkSuccess`, `MarkFailed`, and `ScheduleRetry` throw `InvalidOperationException` if called on a delivery that is already in a terminal state (status `Success` or `Failed` after all retries exhausted). Always check `ShouldRetry` before calling `ScheduleRetry`.
- **Retry policy:** The exact delay calculated by `ScheduleRetry` depends on the subscription’s retry configuration (e.g., exponential backoff). The method does not enforce a maximum retry count; that is the caller’s responsibility via `ShouldRetry`.
- **AttemptNumber:** Starts at `0` for the first attempt. After `ScheduleRetry` is called, `AttemptNumber` is incremented. The maximum allowed attempts is typically defined externally (e.g., in the subscription settings).
