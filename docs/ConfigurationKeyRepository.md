# ConfigurationKeyRepository

Central repository for retrieving and querying configuration keys stored in the dotnet-config-server. Provides methods to fetch configuration keys by configuration identity, version, or key name, as well as search capabilities across configurations.

## API

### `public ConfigurationKeyRepository`

Initializes a new instance of the `ConfigurationKeyRepository` class with required services for accessing configuration data.

### `public async Task<List<ConfigurationKey>> GetByConfigurationAsync(Guid configurationId)`

Retrieves all configuration keys associated with the specified configuration identifier.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration to retrieve keys for.
- **Returns**
  - `Task<List<ConfigurationKey>>`: A list of `ConfigurationKey` objects belonging to the configuration.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public async Task<List<ConfigurationKey>> GetByVersionAsync(Guid versionId)`

Retrieves all configuration keys associated with the specified configuration version.

- **Parameters**
  - `versionId` (Guid): The unique identifier of the configuration version to retrieve keys for.
- **Returns**
  - `Task<List<ConfigurationKey>>`: A list of `ConfigurationKey` objects belonging to the version.
- **Exceptions**
  - Throws `ArgumentException` if `versionId` is empty.

### `public async Task<ConfigurationKey?> GetByKeyNameAsync(Guid configurationId, string keyName)`

Retrieves a single configuration key by its name within a specific configuration.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
  - `keyName` (string): The name of the configuration key to retrieve.
- **Returns**
  - `Task<ConfigurationKey?>`: The matching `ConfigurationKey` if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty or `keyName` is null or whitespace.

### `public async Task<List<ConfigurationKey>> SearchAsync(Guid configurationId, string searchTerm)`

Performs a case-insensitive search for configuration keys within a configuration matching the provided search term.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration to search within.
  - `searchTerm` (string): The term to search for in key names or values.
- **Returns**
  - `Task<List<ConfigurationKey>>`: A list of matching `ConfigurationKey` objects.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty or `searchTerm` is null.

### `public ConfigurationVersionRepository`

Provides access to configuration version-related operations. This property exposes a repository instance dedicated to managing and querying configuration versions.

### `public async Task<List<ConfigurationVersion>> GetByConfigurationAsync(Guid configurationId)`

Retrieves all versions associated with the specified configuration.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
- **Returns**
  - `Task<List<ConfigurationVersion>>`: A list of `ConfigurationVersion` objects belonging to the configuration.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public async Task<ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId)`

Retrieves the currently active version for the specified configuration, if one exists.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
- **Returns**
  - `Task<ConfigurationVersion?>`: The active `ConfigurationVersion` if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public async Task<ConfigurationVersion?> GetByVersionNumberAsync(Guid configurationId, string versionNumber)`

Retrieves a configuration version by its version number within a specific configuration.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
  - `versionNumber` (string): The version number to retrieve.
- **Returns**
  - `Task<ConfigurationVersion?>`: The matching `ConfigurationVersion` if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty or `versionNumber` is null or whitespace.

### `public async Task<List<ConfigurationVersion>> GetOlderThanAsync(Guid configurationId, DateTime cutoff)`

Retrieves all configuration versions for a configuration that are older than the specified cutoff date.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
  - `cutoff` (DateTime): The cutoff date; versions older than this will be returned.
- **Returns**
  - `Task<List<ConfigurationVersion>>`: A list of `ConfigurationVersion` objects older than the cutoff.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public WebhookSubscriptionRepository`

Provides access to webhook subscription-related operations. This property exposes a repository instance dedicated to managing and querying webhook subscriptions.

### `public async Task<List<WebhookSubscription>> GetByConfigurationAsync(Guid configurationId)`

Retrieves all webhook subscriptions associated with the specified configuration.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
- **Returns**
  - `Task<List<WebhookSubscription>>`: A list of `WebhookSubscription` objects belonging to the configuration.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public async Task<List<WebhookSubscription>> GetActiveWebhooksAsync(Guid configurationId)`

Retrieves all active webhook subscriptions for the specified configuration.

- **Parameters**
  - `configurationId` (Guid): The unique identifier of the configuration.
- **Returns**
  - `Task<List<WebhookSubscription>>`: A list of active `WebhookSubscription` objects.
- **Exceptions**
  - Throws `ArgumentException` if `configurationId` is empty.

### `public async Task<List<WebhookSubscription>> GetByApplicationIdAsync(Guid applicationId)`

Retrieves all webhook subscriptions associated with the specified application identifier.

- **Parameters**
  - `applicationId` (Guid): The unique identifier of the application.
- **Returns**
  - `Task<List<WebhookSubscription>>`: A list of `WebhookSubscription` objects belonging to the application.
- **Exceptions**
  - Throws `ArgumentException` if `applicationId` is empty.

### `public WebhookDeliveryRepository`

Provides access to webhook delivery-related operations. This property exposes a repository instance dedicated to managing and querying webhook deliveries.

### `public async Task<List<WebhookDelivery>> GetBySubscriptionAsync(Guid subscriptionId)`

Retrieves all delivery attempts for the specified webhook subscription.

- **Parameters**
  - `subscriptionId` (Guid): The unique identifier of the webhook subscription.
- **Returns**
  - `Task<List<WebhookDelivery>>`: A list of `WebhookDelivery` objects for the subscription.
- **Exceptions**
  - Throws `ArgumentException` if `subscriptionId` is empty.

### `public async Task<List<WebhookDelivery>> GetFailedDeliveriesAsync(Guid subscriptionId)`

Retrieves all failed delivery attempts for the specified webhook subscription.

- **Parameters**
  - `subscriptionId` (Guid): The unique identifier of the webhook subscription.
- **Returns**
  - `Task<List<WebhookDelivery>>`: A list of failed `WebhookDelivery` objects.
- **Exceptions**
  - Throws `ArgumentException` if `subscriptionId` is empty.

### `public async Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(Guid subscriptionId)`

Retrieves all pending delivery attempts for the specified webhook subscription.

- **Parameters**
  - `subscriptionId` (Guid): The unique identifier of the webhook subscription.
- **Returns**
  - `Task<List<WebhookDelivery>>`: A list of pending `WebhookDelivery` objects.
- **Exceptions**
  - Throws `ArgumentException` if `subscriptionId` is empty.

### `public async Task<WebhookDelivery?> GetByEventAndSubscriptionAsync(Guid eventId, Guid subscriptionId)`

Retrieves a single webhook delivery attempt by its associated event and subscription identifiers.

- **Parameters**
  - `eventId` (Guid): The unique identifier of the event.
  - `subscriptionId` (Guid): The unique identifier of the webhook subscription.
- **Returns**
  - `Task<WebhookDelivery?>`: The matching `WebhookDelivery` if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentException` if either `eventId` or `subscriptionId` is empty.

### `public async Task<List<WebhookDelivery>> GetFailedDeliveriesAsync()`

Retrieves all failed webhook delivery attempts across all subscriptions.

- **Returns**
  - `Task<List<WebhookDelivery>>`: A list of all failed `WebhookDelivery` objects.

## Usage

```csharp
// Example 1: Fetch all configuration keys for a given configuration
var configId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
var keys = await repository.GetByConfigurationAsync(configId);
foreach (var key in keys)
{
    Console.WriteLine($"{key.KeyName}: {key.Value}");
}

// Example 2: Search for configuration keys containing a specific term
var searchResults = await repository.SearchAsync(configId, "service");
foreach (var key in searchResults)
{
    Console.WriteLine($"Found key: {key.KeyName}");
}
```

## Notes

- All asynchronous methods return `Task` and should be awaited to avoid deadlocks in UI or ASP.NET contexts.
- Methods accepting `Guid` identifiers validate for empty values and throw `ArgumentException`; ensure valid identifiers are provided.
- Repository instances exposed via properties (`ConfigurationVersionRepository`, `WebhookSubscriptionRepository`, `WebhookDeliveryRepository`) are shared and safe for concurrent access.
- Search operations are case-insensitive and may return partial matches depending on the underlying data store.
- Retrieval methods returning `null` indicate absence of the requested entity rather than an error condition.
- Methods returning collections (`List<T>`) never return `null`; an empty list is returned for no matches.
