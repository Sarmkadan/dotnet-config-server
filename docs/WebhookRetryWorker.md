# WebhookRetryWorker

The `WebhookRetryWorker` type is a hosted background service (`BackgroundService`) that periodically finds failed webhook deliveries and retries them. Each retry is delayed by an exponential backoff so that a struggling external endpoint is not overwhelmed. It is registered in `Program.cs` with `AddHostedService<WebhookRetryWorker>()` and runs for the lifetime of the application.

## Purpose

Webhook deliveries can fail for transient reasons (network errors, the receiving endpoint being temporarily unavailable, non-success HTTP status codes). `WebhookRetryWorker` gives those deliveries another chance without any manual intervention. It does not send new webhooks itself; it only re-attempts deliveries that are already recorded as `Failed` in the database, and it delegates the actual send to `IWebhookService.RetryWebhookDeliveryAsync`.

## Retry behavior

The worker runs a loop with the following steps:

1. **Wait for the check interval.** The loop runs one batch, then waits 5 minutes (`_checkInterval`) before the next batch. The first batch runs immediately after the service starts.
2. **Query eligible deliveries.** A new DI scope is created for each batch. The worker calls `IWebhookDeliveryRepository.GetFailedDeliveriesAsync(maxRetries, maxAgeHours)`, which returns deliveries that match all of the following:
   - `Status` is `Failed`.
   - `AttemptNumber` is less than `maxRetries` (5).
   - `CreatedAt` is within the last `maxAgeHours` (24) hours.
   - `NextRetryAt` is either unset or less than or equal to the current UTC time.

   The results are ordered by `NextRetryAt`, oldest first.
3. **Retry each delivery sequentially.** For every delivery, the worker:
   - Computes a backoff delay from the delivery's current `AttemptNumber`, waits for it, and then calls `IWebhookService.RetryWebhookDeliveryAsync(delivery.Id)`.
   - Logs a success message if the call returns `true`, or a warning ("still failing") if it returns `false`.
   - Logs an error and continues with the next delivery if the call throws.
4. **Log the batch summary.** The number of deliveries tried and the number that succeeded are logged once the batch completes.

### Backoff schedule

The delay before each retry is calculated by `GetExponentialBackoffDelay`:

```
delay = min(1000 ms × 2^AttemptNumber, 300000 ms)
```

Because the delay is based on the attempt number, the waits grow as follows for the attempts that are still eligible for retry:

| AttemptNumber | Delay before retry |
|---------------|--------------------|
| 1             | 2 s                |
| 2             | 4 s                |
| 3             | 8 s                |
| 4             | 16 s               |

The 5-minute cap is never reached with the current `maxRetries` of 5, but it protects the calculation if the limit is raised later.

Delays are applied one delivery at a time, so a batch with several eligible deliveries takes the sum of their delays plus the time each send takes.

### Failure handling

- `RetryWebhookDeliveryAsync` increments `AttemptNumber`, attempts the send, and on failure records the failure and schedules the next retry. A `false` return value means the delivery is still failing, or that its subscription is missing or inactive.
- If a batch query fails, the error is logged and rethrown. The outer loop in `ExecuteAsync` catches it, logs it, and continues after the check interval, so one failing batch does not stop the worker.
- Once a delivery reaches `maxRetries` or is older than `maxAgeHours`, it is no longer selected and is not retried again.

## Configuration

`WebhookRetryWorker` has no entry in `DotnetConfigServer:*` options and does not read from `appsettings.json`. Its limits are private fields with fixed values:

| Setting | Value | Meaning |
|---------|-------|---------|
| `_checkInterval` | 5 minutes | Time between batches |
| `_maxRetries` | 5 | A delivery is retried only while `AttemptNumber` is below this value |
| `_maxAgeHours` | 24 | Deliveries created more than this many hours ago are ignored |
| Base backoff | 1000 ms | Multiplied by `2^AttemptNumber` |
| Maximum backoff | 300000 ms (5 minutes) | Upper bound for a single delay |

To change these values, edit the fields in `BackgroundWorkers/WebhookRetryWorker.cs`. Making them configurable would require binding them to an options class and injecting it into the constructor.

## Public API

### WebhookRetryWorker (constructor)

**Purpose**  
Creates a new instance of the worker. The constructor is called by the DI container when the hosted service starts.

**Parameters**  
- `serviceProvider`: The root `IServiceProvider`. It is used to create a scope for each batch, so that scoped services such as the repository and webhook service are resolved per batch.
- `logger`: The `ILogger<WebhookRetryWorker>` used for structured logging.

**Return value**  
A new `WebhookRetryWorker` object.

**Exceptions**  
- `ArgumentNullException` if `serviceProvider` or `logger` is `null`.

### ExecuteAsync

**Purpose**  
The main loop of the hosted service. It logs a start message, runs `RetryFailedDeliveriesAsync` followed by a wait of `_checkInterval`, and repeats until the host requests shutdown. Exceptions from a batch are logged and do not end the loop.

**Return value**  
A `Task` that completes when the host stops the service.

**Exceptions**  
- `OperationCanceledException` (`TaskCanceledException`) is thrown from the wait when `stoppingToken` is cancelled. This is the normal shutdown path.

### RetryFailedDeliveriesAsync (private)

**Purpose**  
Runs a single batch: creates a scope, loads eligible failed deliveries, and retries each one with its backoff delay.

**Return value**  
A `Task` that completes when the batch has been processed.

**Exceptions**  
- Any exception raised while loading the batch is logged and rethrown to `ExecuteAsync`.
- Exceptions from an individual delivery are caught and logged, and the batch continues.

### GetExponentialBackoffDelay (private)

**Purpose**  
Calculates the delay in milliseconds for a retry, as `min(1000 × 2^retryCount, 300000)`.

**Parameters**  
- `retryCount`: The delivery's `AttemptNumber` at the time of the retry.

**Return value**  
The delay in milliseconds as an `int`.

## Usage

### Registering the worker

The worker is registered in `Program.cs` alongside the other hosted services:

```csharp
builder.Services.AddBusinessServices();
builder.Services.AddWebhookClient();
// ...
builder.Services.AddHostedService<WebhookRetryWorker>();
```

No additional configuration is required. The worker needs `IWebhookDeliveryRepository` and `IWebhookService` to be registered, which `AddBusinessServices` and the repository registrations provide.

### Triggering a retry manually

The worker has no public method to trigger a batch on demand. To retry a single delivery immediately, call the webhook service directly:

```csharp
var success = await webhookService.RetryWebhookDeliveryAsync(deliveryId);
```

## Notes

- Only deliveries with `Status == Failed` are picked up. Deliveries in `Retry` or `Pending` state are not handled by this worker.
- `NextRetryAt` is used only as a filter in the repository query. The backoff delay in this worker is calculated from `AttemptNumber`, so a delivery can be retried a few seconds after it becomes eligible.
- Deliveries in a batch are processed one after another, so a slow endpoint delays the rest of the batch. The 5-minute wait starts only after the batch finishes, so a long batch also pushes back the next one.
- During shutdown, the cancellation raised by a delivery's backoff delay is caught by the per-delivery handler and logged as an error before the worker exits. This is harmless, but it can appear in logs as an error on every shutdown while a batch is in progress.
- The worker uses only the injected `IServiceProvider` and creates a scope per batch. It does not hold references to scoped services between batches, so it is safe to run alongside the other hosted services.
