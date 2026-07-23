#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Middleware.RateLimiting;

/// <summary>
/// Abstraction over the backing store used to track and enforce rate-limit
/// counters for a client key. Implementations decide whether counters are
/// scoped to a single process (<see cref="InMemoryRateLimitStore"/>) or shared
/// across every instance sitting behind a load balancer
/// (<see cref="DistributedRateLimitStore"/>), which is what keeps the
/// effective limit at N requests per window instead of N * instance-count.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Atomically registers one request attempt for <paramref name="clientKey"/>
    /// against a fixed time window and reports whether it is allowed.
    /// </summary>
    /// <param name="clientKey">Stable identifier for the caller (API key or client id, falling back to remote IP).</param>
    /// <param name="limit">Maximum number of requests allowed within <paramref name="window"/>.</param>
    /// <param name="window">Duration of the fixed rate-limit window.</param>
    /// <param name="cancellationToken">Token used to cancel the store operation.</param>
    /// <returns>A <see cref="RateLimitDecision"/> describing whether the request is allowed and the current counter state.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientKey"/> is null or empty, or when <paramref name="limit"/> is not positive.</exception>
    Task<RateLimitDecision> EvaluateAsync(string clientKey, int limit, TimeSpan window, CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of a single <see cref="IRateLimitStore.EvaluateAsync"/> call, carrying
/// everything needed to populate the standard rate-limit response headers.
/// </summary>
/// <param name="IsAllowed">Whether the request is within the allotted limit.</param>
/// <param name="Limit">The configured maximum requests for the window.</param>
/// <param name="Remaining">Requests remaining in the current window (never negative).</param>
/// <param name="ResetsAtUtc">UTC instant at which the current window ends and the counter resets.</param>
public readonly record struct RateLimitDecision(bool IsAllowed, int Limit, int Remaining, DateTimeOffset ResetsAtUtc)
{
    /// <summary>
    /// Time remaining until the window resets, suitable for the Retry-After header. Never negative.
    /// </summary>
    public TimeSpan RetryAfter
    {
        get
        {
            var remaining = ResetsAtUtc - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
