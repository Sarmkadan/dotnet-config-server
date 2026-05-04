// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Middleware that monitors performance metrics like request duration,
/// memory usage, and CPU time. Useful for identifying bottlenecks in the application.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly PerformanceMetrics _metrics;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, PerformanceMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);

        try
        {
            await _next(context);
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

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "High latency detected on {Method} {Path}: {DurationMs}ms, Memory: {MemoryMb}MB",
                    metric.Method,
                    metric.Path,
                    metric.DurationMs,
                    memoryUsed / (1024 * 1024)
                );
            }
        }
    }
}

public class PerformanceMetrics
{
    private readonly ConcurrentQueue<RequestMetric> _metrics = new();
    private readonly int _maxMetrics = 1000;
    private readonly ILogger<PerformanceMetrics> _logger;

    public PerformanceMetrics(ILogger<PerformanceMetrics> logger)
    {
        _logger = logger;
    }

    public void RecordRequest(RequestMetric metric)
    {
        _metrics.Enqueue(metric);

        // Keep a bounded queue to prevent memory issues
        while (_metrics.Count > _maxMetrics)
        {
            _metrics.TryDequeue(out _);
        }
    }

    public IEnumerable<RequestMetric> GetRecentMetrics(int count = 100)
    {
        return _metrics.TakeLast(count);
    }

    public double GetAverageDurationMs(string? path = null)
    {
        var query = _metrics.AsEnumerable();
        if (path != null)
            query = query.Where(m => m.Path == path);

        return query.Any() ? query.Average(m => m.DurationMs) : 0;
    }

    public void LogSummary()
    {
        if (_metrics.IsEmpty)
            return;

        var recentMetrics = _metrics.TakeLast(100).ToList();
        var avgDuration = recentMetrics.Average(m => m.DurationMs);
        var avgMemory = recentMetrics.Average(m => m.MemoryUsedBytes) / (1024 * 1024);
        var slowRequests = recentMetrics.Count(m => m.DurationMs > 500);

        _logger.LogInformation(
            "Performance Summary - Avg Duration: {AvgDuration}ms, Avg Memory: {AvgMemory:F2}MB, Slow Requests: {SlowCount}",
            avgDuration,
            avgMemory,
            slowRequests
        );
    }
}

public class RequestMetric
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public DateTime Timestamp { get; set; }
}

using System.Collections.Concurrent;
