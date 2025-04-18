#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Rate limiting middleware using token bucket algorithm per IP address.
/// Prevents abuse by limiting requests per client within a time window.
/// </summary>
public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets;

    public RateLimitingMiddleware(RequestDelegate next, Microsoft.Extensions.Options.IOptions<DotnetConfigServerOptions> options, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _options = options.Value.RateLimit;
        _logger = logger;
        _buckets = new ConcurrentDictionary<string, RateLimitBucket>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var bucket = _buckets.GetOrAdd(clientIp, _ => new RateLimitBucket(_options.RequestsPerMinute));

        // Check if rate limit exceeded
        if (!bucket.TryConsumeToken())
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientIp}", clientIp);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = _options.RetryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }
}

public sealed class RateLimitBucket
{
    private readonly int _capacity;
    private double _tokens;
    private DateTime _lastRefillTime;
    private readonly object _lock = new();

    public RateLimitBucket(int requestsPerMinute)
    {
        _capacity = requestsPerMinute;
        _tokens = requestsPerMinute;
        _lastRefillTime = DateTime.UtcNow;
    }

    public bool TryConsumeToken()
    {
        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }

            return false;
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timePassed = (now - _lastRefillTime).TotalSeconds;
        var tokensToAdd = timePassed * (_capacity / 60.0);

        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
        _lastRefillTime = now;
    }
}
