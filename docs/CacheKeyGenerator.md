# CacheKeyGenerator

A static utility class that provides a centralized set of methods for constructing consistent cache keys used throughout the configuration server. It generates string keys for storing and retrieving configurations, applications, webhook subscriptions, and search results in the distributed cache, and also provides invalidation patterns to ensure cache entries are properly evicted when related data changes.

## API

### GetConfigurationKey

```csharp
public static string GetConfigurationKey(string applicationName, string configurationName)
```

Generates the primary cache key for a specific configuration resource.

**Parameters:**
- `applicationName` — The name of the application that owns the configuration.
- `configurationName` — The name of the configuration.

**Return value:** A composite string key uniquely identifying the configuration in the cache.

**Throws:** `ArgumentNullException` if either parameter is `null` or empty.

---

### GetApplicationConfigurationsKey

```csharp
public static string GetApplicationConfigurationsKey(string applicationName)
```

Generates a cache key that represents the collection of all configurations belonging to a given application.

**Parameters:**
- `applicationName` — The name of the application.

**Return value:** A string key for the application's configuration collection.

**Throws:** `ArgumentNullException` if `applicationName` is `null` or empty.

---

### GetConfigurationKeysKey

```csharp
public static string GetConfigurationKeysKey(string applicationName)
```

Generates a cache key for the set of configuration names (keys) within an application, as opposed to the full configuration objects.

**Parameters:**
- `applicationName` — The name of the application.

**Return value:** A string key for the list of configuration names.

**Throws:** `ArgumentNullException` if `applicationName` is `null` or empty.

---

### GetConfigurationKeyKey

```csharp
public static string GetConfigurationKeyKey(string applicationName, string configurationName)
```

Generates a cache key specifically for the key identifier of a configuration, distinct from the configuration value itself.

**Parameters:**
- `applicationName` — The name of the application.
- `configurationName` — The name of the configuration.

**Return value:** A string key for the configuration's key metadata.

**Throws:** `ArgumentNullException` if either parameter is `null` or empty.

---

### GetConfigurationVersionsKey

```csharp
public static string GetConfigurationVersionsKey(string applicationName, string configurationName)
```

Generates a cache key for the version history collection of a specific configuration.

**Parameters:**
- `applicationName` — The name of the application.
- `configurationName` — The name of the configuration.

**Return value:** A string key for the configuration's version list.

**Throws:** `ArgumentNullException` if either parameter is `null` or empty.

---

### GetConfigurationVersionKey

```csharp
public static string GetConfigurationVersionKey(string applicationName, string configurationName, string version)
```

Generates a cache key for a single specific version of a configuration.

**Parameters:**
- `applicationName` — The name of the application.
- `configurationName` — The name of the configuration.
- `version` — The version identifier.

**Return value:** A string key for the specific configuration version.

**Throws:** `ArgumentNullException` if any parameter is `null` or empty.

---

### GetConfigurationDiffKey

```csharp
public static string GetConfigurationDiffKey(string applicationName, string configurationName, string fromVersion, string toVersion)
```

Generates a cache key for the computed diff between two versions of a configuration.

**Parameters:**
- `applicationName` — The name of the application.
- `configurationName` — The name of the configuration.
- `fromVersion` — The source version identifier for the diff.
- `toVersion` — The target version identifier for the diff.

**Return value:** A string key for the cached diff result.

**Throws:** `ArgumentNullException` if any parameter is `null` or empty.

---

### GetApplicationKey

```csharp
public static string GetApplicationKey(string applicationName)
```

Generates a cache key for a single application entity.

**Parameters:**
- `applicationName` — The name of the application.

**Return value:** A string key uniquely identifying the application.

**Throws:** `ArgumentNullException` if `applicationName` is `null` or empty.

---

### GetAllApplicationsKey

```csharp
public static string GetAllApplicationsKey()
```

Generates a cache key for the collection of all registered applications.

**Return value:** A constant string key representing the full application list.

---

### GetWebhookSubscriptionsKey

```csharp
public static string GetWebhookSubscriptionsKey(string applicationName)
```

Generates a cache key for the collection of webhook subscriptions associated with an application.

**Parameters:**
- `applicationName` — The name of the application.

**Return value:** A string key for the application's webhook subscription list.

**Throws:** `ArgumentNullException` if `applicationName` is `null` or empty.

---

### GetWebhookSubscriptionKey

```csharp
public static string GetWebhookSubscriptionKey(string applicationName, string subscriptionId)
```

Generates a cache key for a single webhook subscription.

**Parameters:**
- `applicationName` — The name of the application.
- `subscriptionId` — The unique identifier of the webhook subscription.

**Return value:** A string key for the specific subscription.

**Throws:** `ArgumentNullException` if either parameter is `null` or empty.

---

### GetSearchKey

```csharp
public static string GetSearchKey(string query)
```

Generates a cache key for a search query result.

**Parameters:**
- `query` — The search query string.

**Return value:** A string key derived from the search query.

**Throws:** `ArgumentNullException` if `query` is `null` or empty.

---

### GetInvalidationPatternsForConfiguration

```csharp
public static IEnumerable<string> GetInvalidationPatternsForConfiguration(string applicationName, string configurationName)
```

Returns a set of wildcard or prefix patterns that, when used with cache invalidation mechanisms, will remove all cache entries related to a specific configuration. This typically includes the configuration itself, its keys, versions, and diffs.

**Parameters:**
- `applicationName` — The name of the application.
- `configurationName` — The name of the configuration.

**Return value:** An enumerable of pattern strings suitable for bulk cache eviction.

**Throws:** `ArgumentNullException` if either parameter is `null` or empty.

---

### GetInvalidationPatternsForApplication

```csharp
public static IEnumerable<string> GetInvalidationPatternsForApplication(string applicationName)
```

Returns a set of wildcard or prefix patterns that, when used with cache invalidation mechanisms, will remove all cache entries related to an entire application. This typically includes the application entity, all its configurations, configuration keys, versions, webhook subscriptions, and related data.

**Parameters:**
- `applicationName` — The name of the application.

**Return value:** An enumerable of pattern strings suitable for bulk cache eviction.

**Throws:** `ArgumentNullException` if `applicationName` is `null` or empty.

## Usage

### Example 1: Caching a Configuration and Invalidating on Change

```csharp
using Microsoft.Extensions.Caching.Distributed;

public async Task<Configuration> GetConfigurationAsync(
    IDistributedCache cache,
    IConfigurationRepository repository,
    string appName,
    string configName)
{
    var key = CacheKeyGenerator.GetConfigurationKey(appName, configName);
    var cached = await cache.GetStringAsync(key);

    if (cached is not null)
        return JsonSerializer.Deserialize<Configuration>(cached);

    var config = await repository.GetConfigurationAsync(appName, configName);
    var serialized = JsonSerializer.Serialize(config);

    await cache.SetStringAsync(key, serialized);
    return config;
}

public async Task UpdateConfigurationAsync(
    IDistributedCache cache,
    IConfigurationRepository repository,
    string appName,
    string configName,
    Configuration updated)
{
    await repository.SaveConfigurationAsync(appName, configName, updated);

    // Invalidate all cache entries related to this configuration
    var patterns = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(appName, configName);
    foreach (var pattern in patterns)
    {
        await cache.RemoveAsync(pattern);
    }
}
```

### Example 2: Retrieving All Applications with Webhook Subscriptions

```csharp
public async Task<IEnumerable<Application>> GetAllApplicationsAsync(
    IDistributedCache cache,
    IApplicationRepository repository)
{
    var key = CacheKeyGenerator.GetAllApplicationsKey();
    var cached = await cache.GetStringAsync(key);

    if (cached is not null)
        return JsonSerializer.Deserialize<IEnumerable<Application>>(cached);

    var applications = await repository.GetAllApplicationsAsync();
    await cache.SetStringAsync(key, JsonSerializer.Serialize(applications));

    return applications;
}

public async Task<IEnumerable<WebhookSubscription>> GetWebhookSubscriptionsAsync(
    IDistributedCache cache,
    IWebhookRepository repository,
    string appName)
{
    var key = CacheKeyGenerator.GetWebhookSubscriptionsKey(appName);
    var cached = await cache.GetStringAsync(key);

    if (cached is not null)
        return JsonSerializer.Deserialize<IEnumerable<WebhookSubscription>>(cached);

    var subscriptions = await repository.GetSubscriptionsAsync(appName);
    await cache.SetStringAsync(key, JsonSerializer.Serialize(subscriptions));

    return subscriptions;
}
```

## Notes

- **Thread safety:** All methods are static and operate purely on their input parameters without accessing shared state. They are inherently thread-safe and can be called concurrently from any number of threads without synchronization.

- **Key format stability:** The string keys produced by these methods are intended to remain stable across application versions. Changing the key generation logic would effectively invalidate all existing cache entries, which may be acceptable during major version upgrades but should be avoided in minor patches.

- **Null handling:** Every method that accepts string parameters throws `ArgumentNullException` when passed `null` or empty values. Callers must validate inputs before invoking these methods, particularly when data originates from external sources such as HTTP requests or message queues.

- **Invalidation patterns:** The patterns returned by `GetInvalidationPatternsForConfiguration` and `GetInvalidationPatternsForApplication` are designed for use with cache backends that support prefix-based or glob-style removal (e.g., Redis `KEYS` followed by `DEL`, or `IDistributedCache` implementations that support pattern matching). If the underlying cache provider does not support pattern-based removal, these methods may need to be supplemented with manual key tracking.

- **Key collisions:** The naming conventions used internally ensure that keys for different entity types (applications, configurations, versions, webhooks) do not collide, even when the same application and configuration names are used across different key types.

- **`GetAllApplicationsKey`:** This parameterless method returns a fixed, well-known key. Since it represents a global collection, callers should ensure that any operation modifying the application registry also invalidates this key to prevent stale data.
