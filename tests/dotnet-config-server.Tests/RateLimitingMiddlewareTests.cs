#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Middleware;
using DotnetConfigServer.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for <see cref="RateLimitingMiddleware"/> functionality.
/// Tests cover rate limiting behavior, token bucket algorithm, per-client isolation,
/// and 429 response handling.
/// </summary>
public sealed class RateLimitingMiddlewareTests
{
    private readonly Mock<IOptions<DotnetConfigServerOptions>> _optionsMock;
    private readonly Mock<ILogger<RateLimitingMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly DotnetConfigServerOptions _options;
    private readonly RateLimitOptions _rateLimitOptions;

    public RateLimitingMiddlewareTests()
    {
        _rateLimitOptions = new RateLimitOptions
        {
            RequestsPerMinute = 10,
            RetryAfterSeconds = 30,
            EnableRateLimiting = true
        };

        _options = new DotnetConfigServerOptions
        {
            RateLimit = _rateLimitOptions
        };

        _optionsMock = new Mock<IOptions<DotnetConfigServerOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_options);

        _loggerMock = new Mock<ILogger<RateLimitingMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
    }

    [Fact]
    public async Task InvokeAsync_AllowsRequestsUnderLimit()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_BlocksRequestsOverLimit()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.2");

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Exhaust all tokens by making 11 requests (capacity is 10)
        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Make 10 requests that should succeed
        for (int i = 0; i < 10; i++)
        {
            await middleware.InvokeAsync(context);
        }

        // Reset context for the 11th request
        context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.2");
        context.Response.Body = new MemoryStream();

        // Act - 11th request should be blocked
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Exactly(10));
        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers.RetryAfter.ToString().Should().Be("30");
    }

    [Fact]
    public async Task InvokeAsync_PerClientIsolation_DifferentClientsHaveSeparateBuckets()
    {
        // Arrange
        var client1Context = new DefaultHttpContext();
        client1Context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.10");

        var client2Context = new DefaultHttpContext();
        client2Context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.20");

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Create middleware instance
        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Act - Client 1 makes 10 requests (should exhaust)
        for (int i = 0; i < 10; i++)
        {
            await middleware.InvokeAsync(client1Context);
        }

        // Client 1's next request should be blocked
        client1Context = new DefaultHttpContext();
        client1Context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.10");
        client1Context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(client1Context);
        client1Context.Response.StatusCode.Should().Be(429);

        // Client 2 makes requests (should succeed - different bucket)
        await middleware.InvokeAsync(client2Context);
        client2Context.Response.StatusCode.Should().Be(200);

        var client2Context2 = new DefaultHttpContext();
        client2Context2.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.20");
        await middleware.InvokeAsync(client2Context2);
        client2Context2.Response.StatusCode.Should().Be(200);

        // Assert - verify next was called at least 12 times (10 + 1 blocked for client1, 2 for client2)
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.AtLeast(12));
    }

    [Fact]
    public async Task InvokeAsync_RateLimitDisabled_AllowsAllRequests()
    {
        // Arrange
        _rateLimitOptions.EnableRateLimiting = false;

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.30");

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Make 100 requests
        for (int i = 0; i < 100; i++)
        {
            await middleware.InvokeAsync(context);
        }

        // Assert
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Exactly(100));
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ReturnsCorrectErrorResponseShape()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.40");

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Exhaust tokens
        for (int i = 0; i < 10; i++)
        {
            await middleware.InvokeAsync(context);
        }

        // Reset for blocked request
        context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.40");
        context.Response.Body = new MemoryStream(); // Reset body stream

        // Act
        await middleware.InvokeAsync(context);

        // Assert response shape
        context.Response.StatusCode.Should().Be(429);

        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("error");
        responseBody.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task InvokeAsync_UnknownClientIp_HandledGracefully()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null; // Simulate unknown client

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should still work with "unknown" client
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Once);
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_MultipleRequestsWithinLimit_AllSucceed()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");
        context.Response.Body = new MemoryStream();

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object
        );

        // Make 5 requests within limit
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(context);
            context.Response.Body.SetLength(0); // Clear body for next request
        }

        // All should succeed
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_CustomRateLimitConfiguration_Respected()
    {
        // Arrange - Custom configuration with 5 requests per minute
        var customOptions = new DotnetConfigServerOptions
        {
            RateLimit = new RateLimitOptions
            {
                RequestsPerMinute = 5,
                RetryAfterSeconds = 15,
                EnableRateLimiting = true
            }
        };

        var customOptionsMock = new Mock<IOptions<DotnetConfigServerOptions>>();
        customOptionsMock.Setup(x => x.Value).Returns(customOptions);

        _nextMock.Reset();
        _nextMock.Setup(x => x.Invoke(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.60");

        var middleware = new RateLimitingMiddleware(
            _nextMock.Object,
            customOptionsMock.Object,
            _loggerMock.Object
        );

        // Make 5 requests (should succeed)
        for (int i = 0; i < 5; i++)
        {
            await middleware.InvokeAsync(context);
        }

        // 6th request should be blocked
        context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.60");
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Exactly(5));
        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers.RetryAfter.ToString().Should().Be("15");
    }

    [Fact]
    public async Task RateLimitBucket_TryConsumeToken_ThreadSafety()
    {
        // Arrange
        var bucket = new RateLimitBucket(100);
        var tasks = new List<Task<bool>>();

        // Act - Try to consume 100 tokens from multiple threads simultaneously
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => bucket.TryConsumeToken()));
        }

        await Task.WhenAll(tasks);

        // Assert - exactly 100 tokens should be consumed
        var successfulConsumptions = tasks.Count(t => t.Result);
        successfulConsumptions.Should().Be(100);

        // Next attempt should fail
        bucket.TryConsumeToken().Should().BeFalse();
    }

    [Fact]
    public void RateLimitBucket_TryConsumeToken_ReturnsFalseWhenExhausted()
    {
        // Arrange
        var bucket = new RateLimitBucket(5);

        // Consume all tokens
        for (int i = 0; i < 5; i++)
        {
            bucket.TryConsumeToken();
        }

        // Should be blocked
        bucket.TryConsumeToken().Should().BeFalse();
    }
}