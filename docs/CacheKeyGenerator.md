# CacheKeyGenerator

A static utility class (`DotnetConfigServer.Caching`) that builds the cache keys used by the configuration server for configurations, configuration keys, versions, diffs, applications, webhook subscriptions, and search results. It also returns the key lists that event handlers remove when related data changes.

All identifying parameters are `Guid` values, except the search query and the `GetAllApplicationsKey` method, which takes no identifier.

## Key format

Keys are colon-separated (`:`), prefixed by the entity type, and built from the entity's `Guid` id:

| Method | Key format |
|--------|------------|
| `GetConfigurationKey` | `config:{configurationId}` |
| `GetApplicationConfigurationsKey` | `app:{applicationId}:configs` |
| `GetConfigurationKeysKey` | `config:{configurationId}:keys` |
| `GetConfigurationKeyKey` | `key:{keyId}` |
| `GetConfigurationVersionsKey` | `config:{configurationId}:versions` |
| `GetConfigurationVersionKey` | `version:{versionId}` |
| `GetConfigurationDiffKey` | `diff:{fromVersionId}:{toVersionId}` |
| `GetApplicationKey` | `app:{applicationId}` |
| `GetAllApplicationsKey` | `apps{Separator}all` (see the note on `GetAllApplicationsKey` below) |
| `GetWebhookSubscriptionsKey` | `webhooks:{applicationId}` |
| `GetWebhookSubscriptionKey` | `webhook:{subscriptionId}` |
| `GetSearchKey` | `search:{Uri.EscapeDataString(query)}` with an optional `:{applicationId}` suffix |

## API

### GetConfigurationKey

```csharp
public static string GetConfigurationKey(Guid configurationId)
```

Generates the primary cache key for a configuration.

**Parameters:**
- `configurationId` — The unique identifier of the configuration.

**Return value:** A key in the form `config:{configurationId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetApplicationConfigurationsKey

```csharp
public static string GetApplicationConfigurationsKey(Guid applicationId)
```

Generates a cache key for the collection of all configurations belonging to an application.

**Parameters:**
- `applicationId` — The unique identifier of the application.

**Return value:** A key in the form `app:{applicationId}:configs`.

**Throws:** Nothing. The method does not validate its input.

---

### GetConfigurationKeysKey

```csharp
public static string GetConfigurationKeysKey(Guid configurationId)
```

Generates a cache key for the list of configuration keys (the entries inside a configuration), as opposed to the configuration object itself.

**Parameters:**
- `configurationId` — The unique identifier of the configuration.

**Return value:** A key in the form `config:{configurationId}:keys`.

**Throws:** Nothing. The method does not validate its input.

---

### GetConfigurationKeyKey

```csharp
public static string GetConfigurationKeyKey(Guid keyId)
```

Generates a cache key for a single configuration key entry.

**Parameters:**
- `keyId` — The unique identifier of the configuration key.

**Return value:** A key in the form `key:{keyId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetConfigurationVersionsKey

```csharp
public static string GetConfigurationVersionsKey(Guid configurationId)
```

Generates a cache key for the version history of a configuration.

**Parameters:**
- `configurationId` — The unique identifier of the configuration.

**Return value:** A key in the form `config:{configurationId}:versions`.

**Throws:** Nothing. The method does not validate its input.

---

### GetConfigurationVersionKey

```csharp
public static string GetConfigurationVersionKey(Guid versionId)
```

Generates a cache key for a single configuration version.

**Parameters:**
- `versionId` — The unique identifier of the configuration version.

**Return value:** A key in the form `version:{versionId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetConfigurationDiffKey

```csharp
public static string GetConfigurationDiffKey(Guid fromVersionId, Guid toVersionId)
```

Generates a cache key for the computed diff between two versions.

**Parameters:**
- `fromVersionId` — The identifier of the source version.
- `toVersionId` — The identifier of the target version.

**Return value:** A key in the form `diff:{fromVersionId}:{toVersionId}`. The order matters: the diff from A to B has a different key than the diff from B to A.

**Throws:** Nothing. The method does not validate its input.

---

### GetApplicationKey

```csharp
public static string GetApplicationKey(Guid applicationId)
```

Generates a cache key for a single application.

**Parameters:**
- `applicationId` — The unique identifier of the application.

**Return value:** A key in the form `app:{applicationId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetAllApplicationsKey

```csharp
public static string GetAllApplicationsKey()
```

Generates a cache key for the collection of all registered applications.

**Return value:** A constant key for the application list. The current implementation returns the literal text `apps{Separator}all`, not `apps:all`, because the string is not interpolated. Callers should use the return value of this method rather than hard-coding the text; the value will change when the bug is fixed in the source.

**Throws:** Nothing.

---

### GetWebhookSubscriptionsKey

```csharp
public static string GetWebhookSubscriptionsKey(Guid applicationId)
```

Generates a cache key for the webhook subscriptions associated with an application.

**Parameters:**
- `applicationId` — The unique identifier of the application.

**Return value:** A key in the form `webhooks:{applicationId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetWebhookSubscriptionKey

```csharp
public static string GetWebhookSubscriptionKey(Guid subscriptionId)
```

Generates a cache key for a single webhook subscription.

**Parameters:**
- `subscriptionId` — The unique identifier of the webhook subscription.

**Return value:** A key in the form `webhook:{subscriptionId}`.

**Throws:** Nothing. The method does not validate its input.

---

### GetSearchKey

```csharp
public static string GetSearchKey(string query, Guid? applicationId = null)
```

Generates a cache key for a configuration search result.

**Parameters:**
- `query` — The search query string. It is escaped with `Uri.EscapeDataString`, so characters such as `:` cannot collide with the key separator.
- `applicationId` — Optional. When provided, the key is scoped to that application by appending `:{applicationId}`. When omitted, the key covers the unscoped search.

**Return value:** A key in the form `search:{escapedQuery}`, or `search:{escapedQuery}:{applicationId}` when an application is given.

**Throws:**
- `ArgumentNullException` if `query` is `null`.
- `ArgumentException` if `query` is empty.

---

### GetInvalidationPatternsForConfiguration

```csharp
public static IEnumerable<string> GetInvalidationPatternsForConfiguration(Guid configurationId, Guid applicationId)
```

Returns the entries that should be evicted when a configuration changes. The result is a lazily evaluated sequence, containing:

1. `config:{configurationId}`
2. `config:{configurationId}:keys`
3. `config:{configurationId}:versions`
4. `app:{applicationId}:configs`
5. `search*` — a wildcard entry meant to match all search results (see the Notes)

Individual `version:{versionId}` and `diff:{from}:{to}` entries are **not** included, because the caller does not have their identifiers.

**Parameters:**
- `configurationId` — The unique identifier of the configuration that changed.
- `applicationId` — The unique identifier of the application that owns the configuration.

**Return value:** An enumerable of keys and wildcard patterns to remove from the cache.

**Throws:** Nothing. The method does not validate its input.

---

### GetInvalidationPatternsForApplication

```csharp
public static IEnumerable<string> GetInvalidationPatternsForApplication(Guid applicationId)
```

Returns the entries that should be evicted when an application changes. The result is a lazily evaluated sequence, containing:

1. `app:{applicationId}`
2. `app:{applicationId}:configs`
3. The value of `GetAllApplicationsKey()`
4. `app:{applicationId}*` — a wildcard entry meant to match all keys for the application (see the Notes)

Webhook subscription keys (`webhooks:{applicationId}`, `webhook:{subscriptionId}`) are **not** included. Remove them separately when an application changes.

**Parameters:**
- `applicationId` — The unique identifier of the application that changed.

**Return value:** An enumerable of keys and wildcard patterns to remove from the cache.

**Throws:** Nothing. The method does not validate its input.

## Usage

The examples use `ICacheService` from `DotnetConfigServer.Caching`.

### Example 1: Caching a Configuration and Invalidating on Change

```csharp
public async Task<Configuration?> GetConfigurationAsync(
    ICacheService cache,
    IConfigurationRepository repository,
    Guid configurationId)
{
    var key = CacheKeyGenerator.GetConfigurationKey(configurationId);
    var cached = await cache.GetAsync<Configuration>(key);

    if (cached is not null)
        return cached;

    var config = await repository.GetConfigurationAsync(configurationId);

    if (config is not null)
        await cache.SetAsync(key, config);

    return config;
}

public async Task UpdateConfigurationAsync(
    ICacheService cache,
    IConfigurationRepository repository,
    Guid applicationId,
    Guid configurationId,
    Configuration updated)
{
    await repository.SaveConfigurationAsync(configurationId, updated);

    // Evict the configuration, its keys and versions, the application's configuration list, and search results
    var keys = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(configurationId, applicationId);
    await cache.RemoveAsync(keys);
}
```

### Example 2: Retrieving All Applications with Webhook Subscriptions

```csharp
public async Task<IEnumerable<Application>> GetAllApplicationsAsync(
    ICacheService cache,
    IApplicationRepository repository)
{
    var key = CacheKeyGenerator.GetAllApplicationsKey();
    var cached = await cache.GetAsync<IEnumerable<Application>>(key);

    if (cached is not null)
        return cached;

    var applications = await repository.GetAllApplicationsAsync();
    await cache.SetAsync(key, applications);

    return applications;
}

public async Task<IEnumerable<WebhookSubscription>> GetWebhookSubscriptionsAsync(
    ICacheService cache,
    IWebhookRepository repository,
    Guid applicationId)
{
    var key = CacheKeyGenerator.GetWebhookSubscriptionsKey(applicationId);
    var cached = await cache.GetAsync<IEnumerable<WebhookSubscription>>(key);

    if (cached is not null)
        return cached;

    var subscriptions = await repository.GetSubscriptionsAsync(applicationId);
    await cache.SetAsync(key, subscriptions);

    return subscriptions;
}
```

## Notes

- **Thread safety:** All methods are static and operate purely on their input parameters without accessing shared state. They are inherently thread-safe and can be called concurrently from any number of threads without synchronization.

- **Key format stability:** The string keys produced by these methods are intended to remain stable across application versions. Changing the key generation logic would effectively invalidate all existing cache entries, which may be acceptable during major version upgrades but should be avoided in minor patches. The table in the Key format section is the reference for the current format.

- **Input validation:** Identifier parameters are `Guid` values, so they cannot be `null`, and the methods do not check them. The only validation is in `GetSearchKey`, which rejects a `null` or empty query. Callers that build a `Guid` from external input should parse it with `Guid.TryParse` before calling these methods.

- **Invalidation patterns:** `GetInvalidationPatternsForConfiguration` and `GetInvalidationPatternsForApplication` return a mix of exact keys and wildcard entries (`search*`, `app:{applicationId}*`). Exact keys are removed by name. Wildcard entries are removed only if the `ICacheService` implementation expands them. `MemoryCacheService.RemoveAsync` removes exact keys only, so with the current in-memory cache the wildcard entries are no-ops and the search and per-application entries must be removed in another way. Check the cache implementation before relying on these patterns.

- **Key collisions:** Each entity type has its own prefix (`config:`, `app:`, `key:`, `version:`, `diff:`, `webhooks:`, `webhook:`, `search:`), and the `:keys`, `:versions`, and `:configs` suffixes separate the collections from the entities. Because the identifiers are `Guid` values, keys for different entities cannot collide. The search key is the exception: an unescaped separator in the query could create ambiguity, which is why the query is escaped with `Uri.EscapeDataString`.

- **`GetAllApplicationsKey`:** This method returns a fixed key that represents the global application list. Because it is a global collection, any operation that modifies the application registry should also remove this key. The current return value is the literal string `apps{Separator}all` rather than an interpolated `apps:all`. This is a defect in the source, and it does not break callers that use the method's return value consistently.
