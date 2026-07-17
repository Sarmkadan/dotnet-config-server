# WebhookSubscription

Represents a subscription that defines how and when the configuration server delivers webhook notifications to an external endpoint. It encapsulates the target URL, security settings, delivery tracking, and event filtering rules that govern outbound HTTP callbacks when configuration changes occur.

## API

### Properties

#### `public Guid Id`
Unique identifier for the subscription. Assigned at creation and immutable thereafter.

#### `public string Name`
Human-readable label for the subscription. Must be non-null and non-empty. Used in logs and management interfaces to distinguish subscriptions.

#### `public string Url`
The fully qualified HTTPS endpoint to which webhook payloads are delivered. Must be a valid absolute URI. The property itself does not enforce the scheme, but `Validate` rejects non-HTTPS URLs unless explicitly allowed by server configuration.

#### `public string? Description`
Optional free-text description of the subscription's purpose. May be `null`.

#### `public Guid ConfigurationId`
Identifies the configuration resource whose changes trigger this webhook. The subscription monitors events scoped to this configuration.

#### `public WebhookStatus Status`
Current lifecycle state of the subscription. The `WebhookStatus` enum defines values such as `Active`, `Paused`, `Failing`, and `Disabled`. The server uses this to decide whether to attempt delivery.

#### `public DateTime CreatedAt`
UTC timestamp of when the subscription was first persisted.

#### `public DateTime UpdatedAt`
UTC timestamp of the most recent modification to any property of the subscription.

#### `public DateTime? LastDeliveryAt`
UTC timestamp of the last attempted delivery, regardless of success or failure. `null` if no delivery has ever been attempted.

#### `public int LastDeliveryStatusCode`
The HTTP status code returned by the last delivery attempt. A value of `0` indicates no attempt has been made or the attempt failed before receiving a response.

#### `public int RetryCount`
The number of consecutive failed delivery attempts since the last successful delivery. Reset to `0` upon a successful delivery.

#### `public int MaxRetries`
The maximum number of consecutive retry attempts allowed before the subscription transitions to a `Failing` or `Disabled` status. Must be a non-negative integer.

#### `public bool IsActive`
Convenience flag indicating whether the subscription is eligible for delivery. Typically `true` when `Status == WebhookStatus.Active` and `RetryCount < MaxRetries`, though the exact logic is managed by the delivery service.

#### `public string CreatedBy`
The identity (username, service principal ID, or system identifier) of the principal that created the subscription. Must be non-null.

#### `public string? UpdatedBy`
The identity of the principal that last modified the subscription. `null` if the subscription has never been updated after creation.

#### `public string? Secret`
A shared secret used to compute HMAC signatures on outbound payloads. When `VerifySignature` is `true`, this value must be non-null and non-empty. Stored securely and never returned in plaintext through unencrypted channels.

#### `public bool VerifySignature`
When `true`, the delivery service includes a signature header (e.g., `X-Config-Signature`) in each outbound request, computed using the `Secret`. The receiver can validate the payload integrity using this signature.

#### `public Dictionary<string, string> CustomHeaders`
A dictionary of additional HTTP headers to include in every webhook delivery. Keys are header names, values are header values. Both must be non-null strings. Common uses include authentication tokens (`Authorization`), content-type overrides, or custom tracing headers.

#### `public List<string> TriggerEvents`
A list of event names that cause a delivery. Typical values include `"ConfigurationCreated"`, `"ConfigurationUpdated"`, `"ConfigurationDeleted"`. An empty list means no events trigger delivery. Duplicates are allowed but are typically normalized by the delivery service.

### Methods

#### `public void Validate()`
Performs a synchronous validation pass over the subscription's properties. Throws a `ValidationException` (or a derived type) if any constraint is violated.

**Throws:**
- `ValidationException` when:
  - `Name` is `null`, empty, or exceeds the maximum length.
  - `Url` is `null`, empty, not a valid absolute URI, or uses a disallowed scheme.
  - `ConfigurationId` is `Guid.Empty`.
  - `MaxRetries` is negative.
  - `VerifySignature` is `true` but `Secret` is `null` or empty.
  - `CustomHeaders` contains a `null` key or value.
  - `TriggerEvents` is `null` or contains `null` entries.

**Remarks:**
This method does not check uniqueness constraints (e.g., duplicate `Name`), which are enforced at the persistence layer. It also does not validate that the referenced `ConfigurationId` exists.

## Usage

### Example 1: Creating a basic subscription with signature verification

```csharp
var subscription = new WebhookSubscription
{
    Id = Guid.NewGuid(),
    Name = "CI/CD Pipeline Notifier",
    Url = "https://ci.example.com/hooks/config-updates",
    Description = "Notifies the CI/CD pipeline when the production config changes.",
    ConfigurationId = prodConfigId,
    Status = WebhookStatus.Active,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    CreatedBy = "admin-user",
    Secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    VerifySignature = true,
    MaxRetries = 5,
    TriggerEvents = new List<string> { "ConfigurationUpdated" },
    CustomHeaders = new Dictionary<string, string>
    {
        ["X-Pipeline-Environment"] = "production"
    }
};

subscription.Validate();
await repository.AddAsync(subscription);
```

### Example 2: Pausing a subscription after repeated failures

```csharp
var subscription = await repository.GetByIdAsync(subscriptionId);

if (subscription.RetryCount >= subscription.MaxRetries)
{
    subscription.Status = WebhookStatus.Paused;
    subscription.UpdatedAt = DateTime.UtcNow;
    subscription.UpdatedBy = "system";
    subscription.IsActive = false;

    await repository.UpdateAsync(subscription);

    logger.LogWarning(
        "Webhook {Name} paused after {RetryCount} failed deliveries",
        subscription.Name,
        subscription.RetryCount);
}
```

## Notes

- **Thread safety:** This type is not inherently thread-safe. If multiple threads mutate the same instance (e.g., updating `RetryCount` during concurrent delivery attempts), external synchronization is required. The delivery service typically loads a fresh instance from the store for each attempt to avoid race conditions.
- **Validation scope:** `Validate()` checks only the immediate properties of the instance. It does not verify that `ConfigurationId` references an existing configuration, nor does it check for duplicate subscription names. Those checks must be performed by the caller or repository layer.
- **Secret handling:** The `Secret` property is sensitive. When serializing or logging, ensure it is masked or excluded. The `Validate()` method does not enforce minimum entropy, but operators should use cryptographically random values of at least 256 bits.
- **CustomHeaders constraints:** The dictionary allows any key-value pairs, but the delivery service may drop or override headers that conflict with internally managed headers (e.g., `Content-Type`, `Content-Length`). Avoid setting headers that the HTTP client manages automatically.
- **TriggerEvents normalization:** An empty list means no events trigger delivery. A `null` list is rejected by `Validate()`. Duplicate entries are not removed automatically; the delivery service typically deduplicates before evaluating triggers.
- **Status transitions:** The `Status` property is a mutable enum. Changing it directly does not automatically update `IsActive` or `UpdatedAt`. Callers must keep these in sync when manually altering status.
- **RetryCount semantics:** `RetryCount` is a counter, not a timestamp-based window. It increments on each failure and resets to `0` on the first success. If the delivery service is restarted, the count persists and resumes from the stored value.
