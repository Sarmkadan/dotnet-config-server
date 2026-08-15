#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Middleware;

/// <summary>
/// Configuration options for performance monitoring middleware and related services.
/// </summary>
public sealed class PerformanceMonitoringOptions
{
    /// <summary>
    /// Threshold in milliseconds for logging a high‑latency request.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SlowRequestThresholdMs { get; set; } = 500;

    /// <summary>
    /// Maximum number of request metrics to retain in memory.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxMetrics { get; set; } = 1000;

    /// <summary>
    /// Number of recent metrics to return when querying or summarising.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int RecentMetricsCount { get; set; } = 100;
}
