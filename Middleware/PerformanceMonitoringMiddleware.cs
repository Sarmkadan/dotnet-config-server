#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Middleware that monitors performance metrics like request duration,
/// memory usage, and CPU time. Useful for identifying bottlenecks in the application.
/// </summary>
public sealed class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly PerformanceMetrics _metrics;
    private readonly PerformanceMonitoringOptions _options;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger,
        PerformanceMetrics metrics,
        IOptions<PerformanceMonitoringOptions> options)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);

        _logger.LogInformation("InvokeAsync started for {Method} {Path}", context.Request.Method, context.Request.Path);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during request processing for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            var metric = new RequestMetric
            {
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = memoryUsed,
                Timestamp = DateTime.UtcNow
            };

            _metrics.RecordRequest(metric);

            if (stopwatch.ElapsedMilliseconds > _options.SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "High latency detected on {Method} {Path}: {DurationMs}ms, Memory: {MemoryMb}MB",
                    metric.Method,
                    metric.Path,
                    metric.DurationMs,
                    memoryUsed / (1024 * 1024)
                );
            }

            _logger.LogInformation("InvokeAsync finished for {Method} {Path} with StatusCode {StatusCode} in {DurationMs}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}

public sealed class PerformanceMetrics
{
    private readonly ConcurrentQueue<RequestMetric> _metrics = new();
    private readonly ILogger<PerformanceMetrics> _logger;
    private readonly PerformanceMonitoringOptions _options;

    public PerformanceMetrics(
        ILogger<PerformanceMetrics> logger,
        IOptions<PerformanceMonitoringOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public void RecordRequest(RequestMetric metric)
    {
        _metrics.Enqueue(metric);

        // Keep a bounded queue to prevent memory issues
        while (_metrics.Count > _options.MaxMetrics)
        {
            _metrics.TryDequeue(out _);
        }
    }

    public IEnumerable<RequestMetric> GetRecentMetrics(int count = 100)
    {
        var take = count > 0 ? count : _options.RecentMetricsCount;
        return _metrics.TakeLast(take);
    }

    public double GetAverageDurationMs(string? path = null)
    {
        var query = _metrics.AsEnumerable();
        if (path is not null)
            query = query.Where(m => m.Path == path);

        var snapshot = query.ToList();
        return snapshot.Count > 0 ? snapshot.Average(m => m.DurationMs) : 0;
    }

    public void LogSummary()
    {
        if (_metrics.IsEmpty)
            return;

        var recentMetrics = _metrics.TakeLast(_options.RecentMetricsCount).ToList();
        var avgDuration = recentMetrics.Average(m => m.DurationMs);
        var avgMemory = recentMetrics.Average(m => m.MemoryUsedBytes) / (1024 * 1024);
        var slowRequests = recentMetrics.Count(m => m.DurationMs > _options.SlowRequestThresholdMs);

        _logger.LogInformation(
            "Performance Summary - Avg Duration: {AvgDuration}ms, Avg Memory: {AvgMemory:F2}MB, Slow Requests: {SlowCount}",
            avgDuration,
            avgMemory,
            slowRequests
        );
    }
}

public sealed class RequestMetric
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public DateTime Timestamp { get; set; }

    public override string ToString() => $"PerformanceMonitoringMiddleware {{ Path = {Path}, Method = {Method}, StatusCode = {StatusCode}, DurationMs = {DurationMs}, MemoryUsedBytes = {MemoryUsedBytes}, Timestamp = {Timestamp} }}";
}