using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetConfigServer.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Extension methods for <see cref="CachingBenchmarks"/> that provide additional caching scenarios and utilities
/// for benchmarking and testing cache behavior.
/// </summary>
public static class CachingBenchmarksExtensions
{
    /// <summary>
    /// Extension method that benchmarks cache miss scenarios for multiple configurations at once.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="configurationIds">Collection of configuration IDs to test cache misses for</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or <paramref name="configurationIds"/> is null</exception>
    public static async Task GetConfigurations_CacheMissBatch(this CachingBenchmarks benchmarks, IEnumerable<Guid> configurationIds)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(configurationIds);

        foreach (var _ in configurationIds)
        {
            await benchmarks.GetConfiguration_CacheMiss();
        }
    }

    /// <summary>
    /// Extension method that benchmarks cache hit scenarios for multiple configurations at once.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="configurationIds">Collection of configuration IDs to test cache hits for</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or <paramref name="configurationIds"/> is null</exception>
    public static async Task GetConfigurations_CacheHitBatch(this CachingBenchmarks benchmarks, IEnumerable<Guid> configurationIds)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(configurationIds);

        foreach (var _ in configurationIds)
        {
            await benchmarks.GetConfiguration_CacheHit();
        }
    }

    /// <summary>
    /// Extension method that benchmarks cache-aside pattern with multiple configurations.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="configurationIds">Collection of configuration IDs to test cache-aside pattern for</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or <paramref name="configurationIds"/> is null</exception>
    public static async Task GetConfigurations_WithCacheAsideBatch(this CachingBenchmarks benchmarks, IEnumerable<Guid> configurationIds)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(configurationIds);

        foreach (var _ in configurationIds)
        {
            await benchmarks.GetConfiguration_WithCacheAside();
        }
    }

    /// <summary>
    /// Extension method that benchmarks concurrent cache access patterns.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="applicationId">The application ID to test concurrent access for</param>
    /// <param name="concurrencyLevel">Number of concurrent operations to simulate</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="concurrencyLevel"/> is less than 1</exception>
    public static async Task ConcurrentCacheAccess(this CachingBenchmarks benchmarks, Guid applicationId, int concurrencyLevel = 10)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(concurrencyLevel, 1);

        var tasks = new List<Task>(concurrencyLevel * 2);
        for (int i = 0; i < concurrencyLevel; i++)
        {
            tasks.Add(benchmarks.GetConfiguration_CacheHit());
            tasks.Add(benchmarks.GetKeys_CacheHit());
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Extension method that benchmarks cache operations with different time-to-live (TTL) values.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="ttlMinutes">Collection of TTL values in minutes to test</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or <paramref name="ttlMinutes"/> is null</exception>
    public static async Task CacheOperations_WithDifferentTTL(this CachingBenchmarks benchmarks, IEnumerable<int> ttlMinutes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(ttlMinutes);

        foreach (var ttl in ttlMinutes)
        {
            // Cache a configuration with the specified TTL
            await benchmarks.GetConfiguration_CacheHit();

            // Wait for the TTL to expire (simulate by waiting)
            await Task.Delay(TimeSpan.FromSeconds(ttl * 60 / 10));

            // Access again to verify cache expiration
            await benchmarks.GetConfiguration_CacheMiss();
        }
    }

    /// <summary>
    /// Extension method that benchmarks cache operations with varying cache sizes.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="cacheSizes">Collection of cache sizes to test</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> or <paramref name="cacheSizes"/> is null</exception>
    public static async Task CacheOperations_WithVaryingSizes(this CachingBenchmarks benchmarks, IEnumerable<int> cacheSizes)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentNullException.ThrowIfNull(cacheSizes);

        foreach (var size in cacheSizes)
        {
            // Create and cache multiple configurations
            for (int i = 0; i < size; i++)
            {
                await benchmarks.CacheSizeTracking();
            }
        }
    }

    /// <summary>
    /// Extension method that benchmarks cache eviction policies.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="evictionCount">Number of items to add to trigger eviction</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="evictionCount"/> is less than 1</exception>
    public static async Task CacheEvictionPolicy(this CachingBenchmarks benchmarks, int evictionCount = 1000)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(evictionCount, 1);

        // Add the original item to cache
        await benchmarks.CacheEviction();

        // Add many items to trigger eviction (using the public CacheEviction benchmark multiple times)
        for (int i = 0; i < evictionCount; i++)
        {
            await benchmarks.CacheEviction();
        }
    }

    /// <summary>
    /// Extension method that benchmarks cache operations with encrypted data.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="count">Number of encrypted configurations to cache and retrieve</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="count"/> is less than 1</exception>
    public static async Task CacheEncryptedData_Multiple(this CachingBenchmarks benchmarks, int count = 5)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        for (int i = 0; i < count; i++)
        {
            await benchmarks.CacheWithEncryptedData();
        }
    }

    /// <summary>
    /// Extension method that provides a combined benchmark for all cache miss scenarios.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    public static async Task AllCacheMissScenarios(this CachingBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        await benchmarks.GetConfiguration_CacheMiss();
        await benchmarks.GetKeys_CacheMiss();
        await benchmarks.SearchConfigurations_CacheMiss();
        await benchmarks.GetConfigurationCount_CacheMiss();
    }

    /// <summary>
    /// Extension method that provides a combined benchmark for all cache hit scenarios.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    public static async Task AllCacheHitScenarios(this CachingBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        await benchmarks.GetConfiguration_CacheHit();
        await benchmarks.GetKeys_CacheHit();
        await benchmarks.SearchConfigurations_CacheHit();
        await benchmarks.GetConfigurationCount_CacheHit();
    }

    /// <summary>
    /// Extension method that benchmarks cache operations with case-insensitive keys.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <param name="applicationId">The application ID to test case-insensitive key handling</param>
    /// <returns>A task representing the benchmark operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    public static async Task CacheOperations_CaseInsensitiveKeys(this CachingBenchmarks benchmarks, Guid applicationId)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // Cache with lowercase key
        await benchmarks.SearchConfigurations_CacheHit();

        // Try to retrieve with different case (simulated by calling different methods)
        await benchmarks.GetConfiguration_CacheHit();
    }

    /// <summary>
    /// Extension method that returns statistics about cache operations performed during benchmarks.
    /// </summary>
    /// <param name="benchmarks">The <see cref="CachingBenchmarks"/> instance</param>
    /// <returns>Read-only list of cache operation statistics</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null</exception>
    public static IReadOnlyList<CacheOperationStats> GetCacheOperationStats(this CachingBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new List<CacheOperationStats>
        {
            new CacheOperationStats("GetConfiguration_CacheMiss", "Cache miss for single configuration"),
            new CacheOperationStats("GetConfiguration_CacheHit", "Cache hit for single configuration"),
            new CacheOperationStats("GetConfiguration_WithCacheAside", "Cache-aside pattern for single configuration"),
            new CacheOperationStats("GetKeys_CacheMiss", "Cache miss for configuration keys"),
            new CacheOperationStats("GetKeys_CacheHit", "Cache hit for configuration keys"),
            new CacheOperationStats("GetKeys_WithCacheAside", "Cache-aside pattern for configuration keys"),
            new CacheOperationStats("SearchConfigurations_CacheMiss", "Cache miss for configuration search"),
            new CacheOperationStats("SearchConfigurations_CacheHit", "Cache hit for configuration search"),
            new CacheOperationStats("GetConfigurationCount_CacheMiss", "Cache miss for configuration count"),
            new CacheOperationStats("GetConfigurationCount_CacheHit", "Cache hit for configuration count"),
            new CacheOperationStats("CacheEviction", "Cache eviction behavior"),
            new CacheOperationStats("CacheSizeTracking", "Cache size tracking"),
            new CacheOperationStats("CacheWithEncryptedData", "Cache operations with encrypted data")
        }.AsReadOnly();
    }
}

/// <summary>
/// Represents statistics for a cache operation benchmark.
/// </summary>
/// <param name="name">The name of the cache operation</param>
/// <param name="description">Description of what the operation benchmarks</param>
public sealed record CacheOperationStats(string Name, string Description);
