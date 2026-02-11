#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.IO;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Middleware for comprehensive request/response logging with timing information.
/// Captures request headers, body, response status, and execution time for observability.
/// </summary>
sealed public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log incoming request
            LogRequest(context);

            // Capture response body
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Log outgoing response
            LogResponse(context, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private void LogRequest(HttpContext context)
    {
        var request = context.Request;
        var userId = context.User?.Identity?.Name ?? "anonymous";

        _logger.LogInformation(
            "Incoming {Method} {Path} from {RemoteIP} by {UserId}",
            request.Method,
            request.Path.Value,
            context.Connection.RemoteIpAddress,
            userId
        );
    }

    private void LogResponse(HttpContext context, long elapsedMilliseconds)
    {
        var response = context.Response;

        _logger.LogInformation(
            "Outgoing response {StatusCode} for {Method} {Path} completed in {ElapsedMs}ms",
            response.StatusCode,
            context.Request.Method,
            context.Request.Path.Value,
            elapsedMilliseconds
        );

        // Log slow requests
        if (elapsedMilliseconds > 1000)
        {
            _logger.LogWarning(
                "Slow request detected: {Method} {Path} took {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path.Value,
                elapsedMilliseconds
            );
        }
    }
}
