#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Middleware.RateLimiting;

/// <summary>
/// Process-local fixed-window rate limit store. Counters live in an in-memory
/// dictionary, so this implementation only enforces the configured limit
/// per application instance - behind a load balancer with N instances the
/// effective limit is N * configured value. Use
/// <see cref="DistributedRateLimitStore"/> when running more than one instance.
/// </summary>
public sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, FixedWindowCounter> _counters = new();

    /// <summary>
    /// Registers one request attempt for <paramref name="clientKey"/> against an
    /// in-process fixed window counter.
    /// </summary>
    /// <param name="clientKey">Stable identifier for the caller.</param>
    /// <param name="limit">Maximum number of requests allowed within <paramref name="window"/>.</param>
    /// <param name="window">Duration of the fixed rate-limit window.</param>
    /// <param name="cancellationToken">Token used to cancel the operation (unused - the operation is synchronous and non-blocking).</param>
    /// <returns>A <see cref="RateLimitDecision"/> describing whether the request is allowed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientKey"/> is null/empty or <paramref name="limit"/> is not positive.</exception>
    public Task<RateLimitDecision> EvaluateAsync(string clientKey, int limit, TimeSpan window, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientKey);
        if (limit <= 0)
            throw new ArgumentException("Limit must be a positive number.", nameof(limit));

        var counter = _counters.GetOrAdd(clientKey, _ => new FixedWindowCounter());
        var decision = counter.RegisterAttempt(limit, window);
        return Task.FromResult(decision);
    }

    /// <summary>
    /// Thread-safe fixed-window request counter for a single client key.
    /// </summary>
    private sealed class FixedWindowCounter
    {
        private readonly object _lock = new();
        private int _count;
        private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

        public RateLimitDecision RegisterAttempt(int limit, TimeSpan window)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;

                if (now - _windowStart >= window)
                {
                    _windowStart = now;
                    _count = 0;
                }

                var resetsAt = _windowStart + window;

                if (_count >= limit)
                    return new RateLimitDecision(false, limit, 0, resetsAt);

                _count++;
                return new RateLimitDecision(true, limit, Math.Max(0, limit - _count), resetsAt);
            }
        }
    }
}
