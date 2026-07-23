#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Middleware.RateLimiting;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Rate limiting middleware using a fixed-window algorithm keyed per client.
/// Prevents abuse by limiting requests per client within a time window.
/// Counter storage is delegated to an <see cref="IRateLimitStore"/>, which lets
/// the same middleware enforce either a per-instance limit
/// (<see cref="InMemoryRateLimitStore"/>) or a single combined limit shared
/// across every instance behind a load balancer
/// (<see cref="DistributedRateLimitStore"/>).
/// </summary>
public sealed class RateLimitingMiddleware
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IRateLimitStore _store;

    /// <summary>
    /// Creates a new <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">Next delegate in the middleware pipeline.</param>
    /// <param name="options">Application options, from which rate-limit settings are read.</param>
    /// <param name="logger">Logger used to record rate-limit rejections.</param>
    /// <param name="store">
    /// Backing counter store. When not supplied by the DI container (or not registered),
    /// a fresh process-local <see cref="InMemoryRateLimitStore"/> is used, matching prior behavior.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.</exception>
    public RateLimitingMiddleware(
        RequestDelegate next,
        Microsoft.Extensions.Options.IOptions<DotnetConfigServerOptions> options,
        ILogger<RateLimitingMiddleware> logger,
        IRateLimitStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options.Value.RateLimit;
        _logger = logger;
        _store = store ?? new InMemoryRateLimitStore();
    }

    /// <summary>
    /// Evaluates the rate limit for the current request's client key and either
    /// forwards it to the next delegate or short-circuits with a 429 response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableRateLimiting)
        {
            await _next(context);
            return;
        }

        var clientKey = ResolveClientKey(context);
        var decision = await _store.EvaluateAsync(clientKey, _options.RequestsPerMinute, Window, context.RequestAborted);

        context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = decision.Remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = decision.ResetsAtUtc.ToUnixTimeSeconds().ToString();

        if (!decision.IsAllowed)
        {
            var retryAfterSeconds = Math.Max(_options.RetryAfterSeconds, (int)Math.Ceiling(decision.RetryAfter.TotalSeconds));

            _logger.LogWarning("Rate limit exceeded for client {ClientKey}", clientKey);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Resolves the identity used to key rate-limit counters for a request:
    /// the caller's API key when present (services behind NAT share a single
    /// IP, so IP alone would let them collectively exceed the intended limit
    /// or throttle each other unfairly), falling back to the remote IP address
    /// when no API key header is supplied.
    /// </summary>
    private static string ResolveClientKey(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(AppConstants.Api.ApiKeyHeader, out var apiKeyValues))
        {
            var apiKey = apiKeyValues.ToString();
            if (!string.IsNullOrWhiteSpace(apiKey))
                return $"apikey:{apiKey}";
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{clientIp}";
    }
}
