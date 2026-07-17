# CachingBenchmarks

The `CachingBenchmarks` class provides a suite of benchmark methods for evaluating the performance of various caching strategies used by the configuration server. It is designed to be used with a benchmarking framework (e.g., BenchmarkDotNet) to measure the latency and throughput of cache operations such as getting configurations, keys, search results, and counts under different cache states (miss, hit, cache-aside). Additional benchmarks cover cache eviction, size tracking, and handling of encrypted data. Each method is asynchronous and requires a prior call to `GlobalSetup` to initialize the test environment.

## API

All methods are `public async Task` and accept no parameters. They return a `Task` representing the asynchronous operation. Exceptions may be thrown if the underlying configuration store or cache encounters an error (e.g., connection failure, serialization error); these are not caught internally and will propagate to the caller.

| Method | Description |
|--------|-------------|
| `GlobalSetup` | Initializes the benchmark environment, including populating the configuration store with test data and configuring the cache. Must be called once before any benchmark method. |
| `GlobalCleanup` | Tears down the benchmark environment, releasing any resources (e.g., disposing of the cache, clearing test data). Should be called after all benchmarks are complete. |
| `GetConfiguration_CacheMiss` | Measures the time to retrieve a configuration value when it is not present in the cache (cache miss). |
| `GetConfiguration_CacheHit` | Measures the time to retrieve a configuration value that is already cached (cache hit). |
| `GetConfiguration_WithCacheAside` | Measures the time to retrieve a configuration value using a cache-aside pattern (check cache, then load from store on miss and populate cache). |
| `GetKeys_CacheMiss` | Measures the time to retrieve all configuration keys when the key list is not cached. |
| `GetKeys_CacheHit` | Measures the time to retrieve all configuration keys when the key list is already cached. |
| `GetKeys_WithCacheAside` | Measures the time to retrieve all configuration keys using a cache-aside pattern. |
| `SearchConfigurations_CacheMiss` | Measures the time to search configurations by a pattern when the search results are not cached. |
| `SearchConfigurations_CacheHit` | Measures the time to search configurations when the search results are already cached. |
| `GetConfigurationCount_CacheMiss` | Measures the time to get the total count of configurations when the count is not cached. |
| `GetConfigurationCount_CacheHit` | Measures the time to get the total count of configurations when the count is already cached. |
| `CacheEviction` | Measures the performance of cache eviction (removing expired or least-recently-used entries). |
| `CacheSizeTracking` | Measures the overhead of tracking the current size of the cache (e.g., memory usage or entry count). |
| `CacheWithEncryptedData` | Measures the performance of caching configuration values that are encrypted, including encryption/decryption overhead. |

## Usage

The class is intended to be used with a benchmarking runner. The following examples demonstrate typical usage.

### Example 1: Running benchmarks with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Running;
using YourNamespace.Benchmarks;

BenchmarkRunner.Run<CachingBenchmarks>();
```

This will execute all benchmark methods in the class, automatically calling `GlobalSetup` before each benchmark run and `GlobalCleanup` after.

### Example 2: Manual invocation for profiling or debugging

```csharp
var benchmarks = new CachingBenchmarks();

// Initialize the environment
await benchmarks.GlobalSetup();

// Run a specific benchmark
await benchmarks.GetConfiguration_CacheHit();
await benchmarks.GetConfiguration_CacheMiss();

// Clean up
await benchmarks.GlobalCleanup();
```

Note that manual invocation may not produce accurate timing results due to warm-up effects; it is primarily useful for verifying correctness or profiling with external tools.

## Notes

- **Edge Cases**:  
  - Cache miss benchmarks assume the cache is empty or the requested data is not present.  
  - Cache hit benchmarks rely on data being pre-populated by `GlobalSetup`.  
  - `CacheEviction` and `CacheSizeTracking` may require the cache to have a specific configuration (e.g., maximum size, TTL) to produce meaningful results.  
  - `CacheWithEncryptedData` assumes encryption keys and algorithms are configured in the test environment.

- **Thread Safety**:  
  The methods are not thread-safe. They are designed to be called sequentially within a single benchmark iteration. Concurrent calls to the same instance may lead to inconsistent cache state or race conditions. If parallel benchmarking is required, each thread should use its own instance of `CachingBenchmarks`.

- **Dependencies**:  
  The class depends on external services (configuration store, cache provider) that must be available during `GlobalSetup`. Ensure that any required connections (e.g., database, Redis) are properly configured before running benchmarks.
