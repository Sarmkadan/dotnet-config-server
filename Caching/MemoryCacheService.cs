#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Caching;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// Suitable for single-instance deployments. For distributed scenarios, use Redis.
/// </summary>
sealed public class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private CacheStats _stats = new();
    private readonly object _statsLock = new();
    private Timer? _cleanupTimer;

    public MemoryCacheService(ILogger<MemoryCacheService> logger)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                RecordMiss();
                return default;
            }

            RecordHit();
            return (T?)entry.Value;
        }

        RecordMiss();
        return await Task.FromResult(default(T?));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var entry = new CacheEntry(value, expiration);
        _cache[key] = entry;
        RecordSet();
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        RecordDelete();
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }

        RecordDelete();
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
                return await Task.FromResult(true);

            _cache.TryRemove(key, out _);
        }

        return await Task.FromResult(false);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }

    public async Task ClearAsync()
    {
        _cache.Clear();
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern)
    {
        return await Task.FromResult(
            _cache.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
        );
    }

    public Task<CacheStats> GetStatsAsync()
    {
        lock (_statsLock)
        {
            _stats.Size = _cache.Count;
            return Task.FromResult(_stats);
        }
    }

    private void RecordHit()
    {
        lock (_statsLock)
        {
            _stats.Hits++;
        }
    }

    private void RecordMiss()
    {
        lock (_statsLock)
        {
            _stats.Misses++;
        }
    }

    private void RecordSet()
    {
        lock (_statsLock)
        {
            _stats.Sets++;
        }
    }

    private void RecordDelete()
    {
        lock (_statsLock)
        {
            _stats.Deletes++;
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private class CacheEntry
    {
        private readonly DateTime _createdAt = DateTime.UtcNow;
        private readonly TimeSpan? _expiration;

        public object? Value { get; }

        public CacheEntry(object? value, TimeSpan? expiration)
        {
            Value = value;
            _expiration = expiration;
        }

        public bool IsExpired
        {
            get
            {
                if (_expiration is null)
                    return false;

                return DateTime.UtcNow - _createdAt > _expiration;
            }
        }
    }
}
