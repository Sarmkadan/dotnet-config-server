#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DotnetConfigServer.Middleware;
using DotnetConfigServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Unit tests for <see cref="RateLimitingMiddleware"/> covering:
///   • Requests under the limit are allowed.
///   • Requests over the limit receive a 429 response with the expected JSON shape.
///   • Rate‑limit buckets are isolated per client IP address.
/// </summary>
public sealed class RateLimitingMiddlewareTests
{
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
    private readonly IOptions<DotnetConfigServerOptions> _options;

    public RateLimitingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();

        // Configure a tiny limit (2 requests per minute) for fast testing.
        var configOptions = new DotnetConfigServerOptions
        {
            RateLimit = new RateLimitOptions
            {
                RequestsPerMinute = 2,
                RetryAfterSeconds = 5
            }
        };
        _options = Options.Create(configOptions);
    }

    private static HttpContext CreateContext(string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task UnderLimit_AllowsRequest()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _options, _loggerMock.Object);
        var context = CreateContext("127.0.0.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled, "Next delegate should have been invoked for a request under the limit.");
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task OverLimit_Returns429WithErrorPayload()
    {
        // Arrange
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _options, _loggerMock.Object);
        var context = CreateContext("10.0.0.1");

        // Exhaust the bucket (2 allowed requests)
        await middleware.InvokeAsync(context);
        await middleware.InvokeAsync(context);

        // Third request should be blocked
        var blockedContext = CreateContext("10.0.0.1");
        await middleware.InvokeAsync(blockedContext);

        // Assert status code
        Assert.Equal(StatusCodes.Status429TooManyRequests, blockedContext.Response.StatusCode);

        // Assert JSON shape { error: "Rate limit exceeded" }
        var body = await ReadResponseBodyAsync(blockedContext.Response);
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("error", out var errorProp), "Response should contain an 'error' property.");
        Assert.Equal("Rate limit exceeded", errorProp.GetString());
    }

    [Fact]
    public async Task PerClient_Isolation_SeparateBuckets()
    {
        // Arrange
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(next, _options, _loggerMock.Object);

        // Client A makes two allowed requests
        var clientA1 = CreateContext("192.168.0.1");
        var clientA2 = CreateContext("192.168.0.1");
        await middleware.InvokeAsync(clientA1);
        await middleware.InvokeAsync(clientA2);
        Assert.Equal(StatusCodes.Status200OK, clientA2.Response.StatusCode);

        // Client B should still have a full bucket
        var clientB1 = CreateContext("192.168.0.2");
        await middleware.InvokeAsync(clientB1);
        Assert.Equal(StatusCodes.Status200OK, clientB1.Response.StatusCode);

        // Client A third request -> blocked
        var clientA3 = CreateContext("192.168.0.1");
        await middleware.InvokeAsync(clientA3);
        Assert.Equal(StatusCodes.Status429TooManyRequests, clientA3.Response.StatusCode);

        // Client B second request -> allowed (still within its limit)
        var clientB2 = CreateContext("192.168.0.2");
        await middleware.InvokeAsync(clientB2);
        Assert.Equal(StatusCodes.Status200OK, clientB2.Response.StatusCode);
    }
}
