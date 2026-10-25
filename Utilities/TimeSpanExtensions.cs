#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for TimeSpan formatting and comparison.
/// Provides utilities for durations such as TTLs, timeouts and polling intervals.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Formats a TimeSpan as a compact human-readable string (e.g., "2d 3h 15m", "45s", "-1h 30m").
    /// Zero-valued components are omitted. Sub-second precision is truncated, so a zero duration
    /// (or anything shorter than one second) is formatted as "0s".
    /// </summary>
    /// <param name="timeSpan">The duration to format.</param>
    /// <returns>A compact, human-readable representation of the duration.</returns>
    public static string ToFriendlyString(this TimeSpan timeSpan)
    {
        var parts = new List<string>(4);

        if (timeSpan.Days != 0)
            parts.Add($"{Math.Abs(timeSpan.Days)}d");
        if (timeSpan.Hours != 0)
            parts.Add($"{Math.Abs(timeSpan.Hours)}h");
        if (timeSpan.Minutes != 0)
            parts.Add($"{Math.Abs(timeSpan.Minutes)}m");
        if (timeSpan.Seconds != 0)
            parts.Add($"{Math.Abs(timeSpan.Seconds)}s");

        if (parts.Count == 0)
            return "0s";

        var sign = timeSpan < TimeSpan.Zero ? "-" : string.Empty;
        return sign + string.Join(" ", parts);
    }

    /// <summary>
    /// Checks if a TimeSpan is strictly greater than zero.
    /// </summary>
    /// <param name="timeSpan">The duration to check.</param>
    /// <returns>True if the duration is positive; otherwise false.</returns>
    public static bool IsPositive(this TimeSpan timeSpan)
    {
        return timeSpan > TimeSpan.Zero;
    }

    /// <summary>
    /// Restricts a TimeSpan to the inclusive range [min, max].
    /// </summary>
    /// <param name="timeSpan">The duration to clamp.</param>
    /// <param name="min">The lower bound (inclusive).</param>
    /// <param name="max">The upper bound (inclusive).</param>
    /// <returns><paramref name="min"/> if the value is below it, <paramref name="max"/> if above it; otherwise the value itself.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when min is greater than max.</exception>
    public static TimeSpan Clamp(this TimeSpan timeSpan, TimeSpan min, TimeSpan max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(min), "The minimum bound must be less than or equal to the maximum bound.");

        if (timeSpan < min)
            return min;

        return timeSpan > max ? max : timeSpan;
    }
}
