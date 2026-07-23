#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace DotnetConfigServer.Middleware.RateLimiting;

/// <summary>
/// Fixed-window rate limit store backed by <see cref="IDistributedCache"/> so
/// counters are shared by every instance of the application sitting behind a
/// load balancer, enforcing a single combined limit instead of
/// N * instance-count. The concrete backend (Redis, SQL Server, etc.) is
/// whatever <see cref="IDistributedCache"/> implementation is registered in
/// the host - this class only depends on the abstraction.
/// </summary>
/// <remarks>
/// <see cref="IDistributedCache"/> does not expose an atomic increment or
/// compare-and-swap primitive, so counters are updated with a plain
/// read-increment-write sequence. Under heavy concurrent load from many
/// instances hitting the same client key in the same instant, two requests
/// can race between the read and the write and both be admitted, letting a
/// handful of extra requests through right at the margin. For a hard
/// guarantee, back this store with a cache implementation that performs the
/// increment atomically on the server (e.g. Redis INCR/Lua) at the
/// infrastructure layer.
/// </remarks>
public sealed class DistributedRateLimitStore : IRateLimitStore
{
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Creates a new <see cref="DistributedRateLimitStore"/> over the given distributed cache.
    /// </summary>
    /// <param name="cache">Shared distributed cache backend used to store per-client counters.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cache"/> is null.</exception>
    public DistributedRateLimitStore(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    /// <summary>
    /// Registers one request attempt for <paramref name="clientKey"/> against a
    /// fixed window counter shared through the distributed cache backend.
    /// </summary>
    /// <param name="clientKey">Stable identifier for the caller.</param>
    /// <param name="limit">Maximum number of requests allowed within <paramref name="window"/>.</param>
    /// <param name="window">Duration of the fixed rate-limit window.</param>
    /// <param name="cancellationToken">Token used to cancel the underlying cache calls.</param>
    /// <returns>A <see cref="RateLimitDecision"/> describing whether the request is allowed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientKey"/> is null/empty or <paramref name="limit"/> is not positive.</exception>
    public async Task<RateLimitDecision> EvaluateAsync(string clientKey, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientKey);
        if (limit <= 0)
            throw new ArgumentException("Limit must be a positive number.", nameof(limit));

        var cacheKey = $"ratelimit:{clientKey}";
        var now = DateTimeOffset.UtcNow;

        var existingBytes = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        var state = existingBytes is { Length: > 0 }
            ? JsonSerializer.Deserialize<WindowState>(existingBytes) ?? new WindowState(now, 0)
            : new WindowState(now, 0);

        if (now - state.WindowStart >= window)
            state = new WindowState(now, 0);

        var resetsAt = state.WindowStart + window;

        if (state.Count >= limit)
            return new RateLimitDecision(false, limit, 0, resetsAt);

        var updated = state with { Count = state.Count + 1 };
        var payload = JsonSerializer.SerializeToUtf8Bytes(updated);

        await _cache.SetAsync(cacheKey, payload, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = resetsAt
        }, cancellationToken).ConfigureAwait(false);

        return new RateLimitDecision(true, limit, Math.Max(0, limit - updated.Count), resetsAt);
    }

    private sealed record WindowState(DateTimeOffset WindowStart, int Count);
}
