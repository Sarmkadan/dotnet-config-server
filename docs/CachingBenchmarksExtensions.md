# CachingBenchmarksExtensions

Provides a suite of benchmark scenarios for measuring cache behavior in the configuration server, including cache miss/hit ratios, concurrent access patterns, eviction policies, encryption overhead, and case-insensitive key handling. The extensions also expose a statistics collector for analyzing cache operation metrics across benchmark runs.

## API

### `public static async Task GetConfigurations_CacheMissBatch()`
Executes a batch of configuration requests that all result in cache misses, simulating cold-start or cache-invalidation scenarios. Measures the latency of populating the cache from the underlying store.
- **Returns**: A task that completes when the batch finishes.
- **Throws**: `InvalidOperationException` if the cache or configuration store is not initialized; `OperationCanceledException` if the operation is cancelled.

### `public static async Task GetConfigurations_CacheHitBatch()`
Executes a batch of configuration requests that all result in cache hits, measuring read-through performance when data is already cached.
- **Returns**: A task that completes when the batch finishes.
- **Throws**: `InvalidOperationException` if the cache is not populated; `OperationCanceledException` if the operation is cancelled.

### `public static async Task GetConfigurations_WithCacheAsideBatch()`
Runs a cache-aside pattern benchmark where the caller manually checks the cache, fetches on miss, and stores the result. Exercises the full read/write path.
- **Returns**: A task that completes when the batch finishes.
- **Throws**: `InvalidOperationException` if dependencies are missing; `OperationCanceledException` if cancelled.

### `public static async Task ConcurrentCacheAccess()`
Simulates multiple threads simultaneously reading and writing cache entries to evaluate thread-safety and contention under load.
- **Returns**: A task that completes when all concurrent operations finish.
- **Throws**: `AggregateException` wrapping any exceptions from concurrent tasks; `OperationCanceledException` if cancelled.

### `public static async Task CacheOperations_WithDifferentTTL()`
Executes cache operations using varying time-to-live values to measure expiration behavior and cleanup overhead.
- **Returns**: A task that completes after all TTL scenarios execute.
- **Throws**: `ArgumentOutOfRangeException` if any TTL value is invalid; `OperationCanceledException` if cancelled.

### `public static async Task CacheOperations_WithVaryingSizes()`
Benchmarks cache operations with payloads of different sizes to assess serialization, memory, and network impact.
- **Returns**: A task that completes when all size variations finish.
- **Throws**: `OutOfMemoryException` if a payload exceeds available memory; `OperationCanceledException` if cancelled.

### `public static async Task CacheEvictionPolicy()`
Exercises the configured eviction policy (e.g., LRU, LFU) by filling the cache beyond capacity and accessing entries in patterns that trigger eviction.
- **Returns**: A task that completes after eviction cycles are observed.
- **Throws**: `InvalidOperationException` if no eviction policy is configured; `OperationCanceledException` if cancelled.

### `public static async Task CacheEncryptedData_Multiple()`
Measures the performance impact of encrypting and decrypting multiple configuration payloads stored in the cache.
- **Returns**: A task that completes when all encryption/decryption cycles finish.
- **Throws**: `CryptographicException` if encryption fails; `OperationCanceledException` if cancelled.

### `public static async Task AllCacheMissScenarios()`
Runs a composite benchmark covering all cache-miss code paths (cold start, invalidation, eviction, expiration) in sequence.
- **Returns**: A task that completes when all scenarios finish.
- **Throws**: `InvalidOperationException` if any scenario cannot be initialized; `OperationCanceledException` if cancelled.

### `public static async Task AllCacheHitScenarios()`
Runs a composite benchmark covering all cache-hit code paths (direct hit, refresh-ahead, stale-while-revalidate) in sequence.
- **Returns**: A task that completes when all scenarios finish.
- **Throws**: `InvalidOperationException` if the cache is not primed; `OperationCanceledException` if cancelled.

### `public static async Task CacheOperations_CaseInsensitiveKeys()`
Verifies and measures cache operations using keys that differ only in casing, ensuring case-insensitive lookup behaves correctly.
- **Returns**: A task that completes after case-variation tests finish.
- **Throws**: `ArgumentException` if key normalization fails; `OperationCanceledException` if cancelled.

### `public static IReadOnlyList<CacheOperationStats> GetCacheOperationStats()`
Retrieves aggregated statistics collected during benchmark execution, including hit/miss counts, latency percentiles, eviction counts, and encryption overhead.
- **Returns**: An immutable list of `CacheOperationStats` records; empty if no benchmarks have run.
- **Throws**: `InvalidOperationException` if the statistics collector is not initialized.

### `public sealed record CacheOperationStats`
Immutable snapshot of cache operation metrics for a single benchmark scenario.

**Properties**:
- `string ScenarioName` — Name of the benchmark scenario.
- `long TotalOperations` — Total cache operations performed.
- `long CacheHits` — Number of cache hits.
- `long CacheMisses` — Number of cache misses.
- `double HitRatio` — Cache hit ratio (0.0–1.0).
- `TimeSpan AverageLatency` — Mean operation latency.
- `TimeSpan P50Latency` — Median latency.
- `TimeSpan P95Latency` — 95th-percentile latency.
- `TimeSpan P99Latency` — 99th-percentile latency.
- `long EvictionCount` — Number of entries evicted.
- `long EncryptionOperations` — Number of encrypt/decrypt cycles.
- `TimeSpan EncryptionOverhead` — Total time spent in encryption.

## Usage

```csharp
using DotNetConfigServer.Benchmarks;

await CachingBenchmarksExtensions.GetConfigurations_CacheMissBatch();
await CachingBenchmarksExtensions.GetConfigurations_CacheHitBatch();
await CachingBenchmarksExtensions.ConcurrentCacheAccess();

var stats = CachingBenchmarksExtensions.GetCacheOperationStats();
foreach (var s in stats)
{
    Console.WriteLine($"{s.ScenarioName}: HitRatio={s.HitRatio:P2}, P99={s.P99Latency.TotalMs}ms");
}
```

```csharp
using DotNetConfigServer.Benchmarks;

await CachingBenchmarksExtensions.CacheOperations_WithDifferentTTL();
await CachingBenchmarksExtensions.CacheOperations_WithVaryingSizes();
await CachingBenchmarksExtensions.CacheEvictionPolicy();
await CachingBenchmarksExtensions.CacheEncryptedData_Multiple();
await CachingBenchmarksExtensions.CacheOperations_CaseInsensitiveKeys();

var allStats = CachingBenchmarksExtensions.GetCacheOperationStats();
var encryptionStats = allStats.Where(s => s.EncryptionOperations > 0).ToList();
Console.WriteLine($"Encryption overhead across scenarios: {encryptionStats.Sum(s => s.EncryptionOverhead.TotalMilliseconds)}ms");
```

## Notes

- All benchmark methods are stateless and can be invoked in any order; however, `GetCacheOperationStats` only returns data for scenarios that have already executed.
- The methods are thread-safe for concurrent invocation, but running multiple benchmarks simultaneously may skew latency measurements due to resource contention.
- `CacheOperationStats` records are allocated per scenario; the list returned by `GetCacheOperationStats` is a snapshot and does not update automatically if additional benchmarks run afterward.
- Encryption benchmarks (`CacheEncryptedData_Multiple`) require a configured `IDataProtectionProvider`; otherwise they throw `CryptographicException`.
- Case-insensitive key benchmarks assume the underlying cache provider normalizes keys via `StringComparer.OrdinalIgnoreCase`; behavior is undefined if the provider uses case-sensitive keys.
- TTL and size variation benchmarks may allocate significant memory; ensure the test environment has sufficient headroom to avoid `OutOfMemoryException` distorting results.
- Eviction policy benchmarks depend on the cache implementation honoring the configured policy; if the provider uses a no-op eviction, `EvictionCount` will remain zero.
