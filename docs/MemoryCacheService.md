# MemoryCacheService

The `MemoryCacheService` provides an asynchronous, in‑memory caching layer backed by `Microsoft.Extensions.Caching.Memory.IMemoryCache`. It is intended for storing configuration data or other transient objects with optional expiration policies, and it exposes a straightforward async‑first API that integrates naturally with async/await patterns in .NET applications.

## API

### MemoryCacheService()
Creates a new instance of the cache service. No parameters are required. The constructor does not throw under normal circumstances; if the underlying cache cannot be initialized, an `InvalidOperationException` may be propagated.

### GetAsync<T>(string key)
Retrieves a cached value associated with `key`.  
- **Parameters**  
  - `key`: The identifier of the cache entry. Must not be `null`.  
- **Return value**  
  - A `Task<T?>` that completes with the cached value if present, otherwise `default(T?)` (i.e., `null` for reference types).  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.  
  - Any exception thrown by the underlying cache implementation (e.g., `InvalidOperationException` if the cache has been disposed).

### SetAsync<T>(string key, T value)
Inserts or updates a cache entry for `key` with the supplied `value`.  
- **Parameters**  
  - `key`: The identifier for the entry. Must not be `null`.  
  - `value`: The object to store. May be `null` to explicitly store a null reference.  
- **Return value**  
  - A `Task` that completes when the operation finishes.  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.  
  - Any exception from the underlying cache (e.g., if the cache has been disposed).

### RemoveAsync(string key)
Removes a single cache entry identified by `key`.  
- **Parameters**  
  - `key`: The identifier of the entry to remove. Must not be `null`.  
- **Return value**  
  - A `Task` that completes when the removal is finished.  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.  
  - Any exception from the underlying cache (e.g., if disposed).

### RemoveAsync(IEnumerable<string> keys)
Removes multiple cache entries specified by the `keys` collection.  
- **Parameters**  
  - `keys`: A sequence of cache keys to remove. Must not be `null` and must not contain `null` elements.  
- **Return value**  
  - A `Task` that completes when all removals have been processed.  
- **Throws**  
  - `ArgumentNullException` if `keys` is `null`.  
  - `ArgumentException` if any element in `keys` is `null`.  
  - Any exception from the underlying cache (e.g., if disposed).

### ExistsAsync(string key)
Checks whether a cache entry for `key` is present.  
- **Parameters**  
  - `key`: The identifier to test. Must not be `null`.  
- **Return value**  
  - A `Task<bool>` that yields `true` if an entry exists, otherwise `false`.  
- **Throws**  
  - `ArgumentNullException` if `key` is `null`.  
  - Any exception from the underlying cache (e.g., if disposed).

### GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
Retrieves an existing entry for `key`; if none exists, invokes `factory` to create a value, stores it, and returns it.  
- **Parameters**  
  - `key`: The identifier for the entry. Must not be `null`.  
  - `factory`: An asynchronous function that produces the value when the cache miss occurs. Must not be `null`.  
- **Return value**  
  - A `Task<T>` that completes with the cached or newly created value.  
- **Throws**  
  - `ArgumentNullException` if `key` or `factory` is `null`.  
  - Any exception thrown by `factory` or the underlying cache (e.g., if disposed).

### ClearAsync()
Removes all entries from the cache.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task` that completes when the cache has been emptied.  
- **Throws**  
  - Any exception from the underlying cache (e.g., if disposed).

### GetKeysAsync()
Returns all keys currently stored in the cache.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task<IEnumerable<string>>` yielding the set of cache keys.  
- **Throws**  
  - Any exception from the underlying cache (e.g., if disposed).

### GetStatsAsync()
Provides diagnostic information about the cache’s internal state.  
- **Parameters**  
  - None.  
- **Return value**  
  - A `Task<CacheStats>` containing metrics such as entry count, hit/miss ratios, and memory usage.  
- **Throws**  
  - Any exception from the underlying cache (e.g., if disposed).

### Dispose()
Releases resources held by the cache service. After disposal, all subsequent operations will throw an `ObjectDisposedException`.  
- **Parameters**  
  - None.  
- **Return value**  
  - `void`.  
- **Throws**  
  - None (the method itself does not throw, but subsequent calls will).

### Value
Gets the raw object stored in the cache for the most recently accessed key (if any). The property returns `null` when no value is available or after disposal.  
- **Type**  
  - `object?` (read‑only).  
- **Throws**  
  - `ObjectDisposedException` if accessed after the service has been disposed.

### CacheEntry
Provides access to the underlying `ICacheEntry` representation used internally for the most recent operation. This can be useful for advanced inspection or manual manipulation of cache options.  
- **Type**  
  - `CacheEntry` (read‑only).  
- **Throws**  
  - `ObjectDisposedException` if accessed after disposal.

## Usage

### Basic get/set pattern
```csharp
await using var cache = new MemoryCacheService();

// Store a configuration string
await cache.SetAsync("AppName", "MyApplication");

// Retrieve it later
string? appName = await cache.GetAsync<string>("AppName");
Console.WriteLine(appName ?? "Not found");
```

### Using GetOrCreateAsync for lazy loading
```csharp
await using var cache = new MemoryCacheService();

async Task<int> GetExpensiveValueAsync()
{
    // Simulate expensive work
    await Task.Delay(100);
    return 42;
}

// First call triggers the factory; subsequent calls return the cached value
int value1 = await cache.GetOrCreateAsync("ExpensiveKey", GetExpensiveValueAsync);
int value2 = await cache.GetOrCreateAsync("ExpensiveKey", GetExpensiveValueAsync);
// value1 and value2 are both 42, and the factory ran only once
```

## Notes
- All methods that accept a `key` parameter treat a `null` key as a programming error and will throw `ArgumentNullException`.  
- The service is thread‑safe; concurrent calls to any of its public methods are safe and will be coordinated by the underlying `IMemoryCache`.  
- After calling `Dispose`, any further interaction with the service (including accessing `Value` or `CacheEntry`) will result in an `ObjectDisposedException`. It is recommended to use `await using` or explicitly call `Dispose` when the cache is no longer needed.  
- The `Value` and `CacheEntry` properties reflect the state of the most recent successful operation; their values may be stale if other threads modify the cache concurrently.  
- Exceptions originating from the underlying cache (e.g., due to memory pressure or internal faults) are propagated unchanged; callers should handle them according to their application’s error‑handling policy.  
- The `GetStatsAsync` method provides a snapshot; rapid successive calls may show varying results as the cache evolves.  
- Storing `null` as a explicit value is supported via `SetAsync<T>(key, null)`; `GetAsync<T>` will then return `null` for that key.  
- The service does not enforce any particular expiration policy; callers must supply desired options through the overloads of `SetAsync` if they require sliding or absolute expiration (the signatures shown assume such options are encapsulated within the `CacheEntry` type or passed via additional parameters not listed here).  
- When using `GetOrCreateAsync`, the factory delegate should be idempotent or tolerant to being invoked multiple times in rare race conditions, although the implementation guarantees that only one invocation will succeed in storing the value.
