// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Caching;

/// <summary>
/// Interface for distributed caching operations.
/// Abstracts over different cache implementations (Redis, in-memory, etc).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from cache by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Removes multiple values from cache.
    /// </summary>
    Task RemoveAsync(IEnumerable<string> keys);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Gets or creates a value in cache using a factory function.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Clears all entries from cache.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Gets all keys matching a pattern.
    /// </summary>
    Task<IEnumerable<string>> GetKeysAsync(string pattern);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    Task<CacheStats> GetStatsAsync();
}

/// <summary>
/// Cache statistics for monitoring and debugging.
/// </summary>
public class CacheStats
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Sets { get; set; }
    public long Deletes { get; set; }
    public long Size { get; set; }

    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
}
