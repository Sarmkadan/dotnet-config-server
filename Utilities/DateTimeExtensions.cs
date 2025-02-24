// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for DateTime manipulation and formatting.
/// Provides utilities for common date/time operations and conversions.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Gets a human-readable time span from now (e.g., "2 hours ago", "in 3 days").
    /// </summary>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)timeSpan.TotalMinutes} minute{(int)timeSpan.TotalMinutes != 1 ? "s" : ""} ago",
            < 86400 => $"{(int)timeSpan.TotalHours} hour{(int)timeSpan.TotalHours != 1 ? "s" : ""} ago",
            < 604800 => $"{(int)timeSpan.TotalDays} day{(int)timeSpan.TotalDays != 1 ? "s" : ""} ago",
            < 2592000 => $"{(int)(timeSpan.TotalDays / 7)} week{(int)(timeSpan.TotalDays / 7) != 1 ? "s" : ""} ago",
            < 31536000 => $"{(int)(timeSpan.TotalDays / 30)} month{(int)(timeSpan.TotalDays / 30) != 1 ? "s" : ""} ago",
            _ => $"{(int)(timeSpan.TotalDays / 365)} year{(int)(timeSpan.TotalDays / 365) != 1 ? "s" : ""} ago"
        };
    }

    /// <summary>
    /// Converts DateTime to ISO 8601 string format.
    /// </summary>
    public static string ToIso8601(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Gets the beginning of the day for a DateTime.
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day for a DateTime.
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the week for a DateTime.
    /// </summary>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var diff = dateTime.DayOfWeek - weekStart;
        if (diff < 0)
            diff += 7;

        return dateTime.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gets the end of the week for a DateTime.
    /// </summary>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        return dateTime.StartOfWeek(weekStart).AddDays(7).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the month for a DateTime.
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month for a DateTime.
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the year for a DateTime.
    /// </summary>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Gets the end of the year for a DateTime.
    /// </summary>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, 12, 31).EndOfDay();
    }

    /// <summary>
    /// Checks if a DateTime is between two dates (inclusive).
    /// </summary>
    public static bool IsBetween(this DateTime dateTime, DateTime startDate, DateTime endDate)
    {
        return dateTime >= startDate && dateTime <= endDate;
    }

    /// <summary>
    /// Gets the number of business days between two dates.
    /// </summary>
    public static int GetBusinessDaysBetween(this DateTime startDate, DateTime endDate)
    {
        var days = 0;
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                days++;

            currentDate = currentDate.AddDays(1);
        }

        return days;
    }

    /// <summary>
    /// Checks if a year is a leap year.
    /// </summary>
    public static bool IsLeapYear(this DateTime dateTime)
    {
        return DateTime.IsLeapYear(dateTime.Year);
    }

    /// <summary>
    /// Gets the age in years from a birth date to today.
    /// </summary>
    public static int GetAge(this DateTime birthDate)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age;
    }
}
