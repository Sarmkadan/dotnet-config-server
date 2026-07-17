#nullable enable
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
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static string ToRelativeTime(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);

        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => Pluralize((int)timeSpan.TotalMinutes, "minute"),
            < 86400 => Pluralize((int)timeSpan.TotalHours, "hour"),
            < 604800 => Pluralize((int)timeSpan.TotalDays, "day"),
            < 2592000 => Pluralize((int)(timeSpan.TotalDays / 7), "week"),
            < 31536000 => Pluralize((int)(timeSpan.TotalDays / 30), "month"),
            _ => Pluralize((int)(timeSpan.TotalDays / 365), "year")
        };
    }

    /// <summary>
    /// Pluralizes a time unit based on count.
    /// </summary>
    /// <param name="count">The count of units.</param>
    /// <param name="unit">The unit name (e.g., "minute").</param>
    /// <returns>A formatted string with the pluralized unit.</returns>
    private static string Pluralize(int count, string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var suffix = count != 1 ? "s" : string.Empty;
        return $"{count} {unit}{suffix} ago";
    }

    /// <summary>
    /// Converts DateTime to ISO 8601 string format.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static string ToIso8601(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Gets the beginning of the day for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the end of the day for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the week for a DateTime.
    /// </summary>
    /// <param name="dateTime">The date to calculate from.</param>
    /// <param name="weekStart">The day of week that represents the start of the week. Defaults to Monday.</param>
    /// <returns>The start of the week.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        ArgumentNullException.ThrowIfNull(dateTime);

        var diff = dateTime.DayOfWeek - weekStart;
        if (diff < 0)
            diff += 7;

        return dateTime.AddDays(-diff).Date;
    }

    /// <summary>
    /// Gets the end of the week for a DateTime.
    /// </summary>
    /// <param name="dateTime">The date to calculate from.</param>
    /// <param name="weekStart">The day of week that represents the start of the week. Defaults to Monday.</param>
    /// <returns>The end of the week.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.StartOfWeek(weekStart).AddDays(7).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the month for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the beginning of the year for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime StartOfYear(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return new DateTime(dateTime.Year, 1, 1);
    }

    /// <summary>
    /// Gets the end of the year for a DateTime.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static DateTime EndOfYear(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return new DateTime(dateTime.Year, 12, 31).EndOfDay();
    }

    /// <summary>
    /// Checks if a DateTime is between two dates (inclusive).
    /// </summary>
    /// <param name="dateTime">The date to check.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>True if the date is between startDate and endDate (inclusive); otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static bool IsBetween(this DateTime dateTime, DateTime startDate, DateTime endDate)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return dateTime >= startDate && dateTime <= endDate;
    }

    /// <summary>
    /// Gets the number of business days between two dates.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>The number of business days between the two dates.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static int GetBusinessDaysBetween(this DateTime startDate, DateTime endDate)
    {
        ArgumentNullException.ThrowIfNull(startDate);
        ArgumentNullException.ThrowIfNull(endDate);

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
    /// <param name="dateTime">The date to check.</param>
    /// <returns>True if the year is a leap year; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dateTime is null.</exception>
    public static bool IsLeapYear(this DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(dateTime);
        return DateTime.IsLeapYear(dateTime.Year);
    }

    /// <summary>
    /// Gets the age in years from a birth date to today.
    /// </summary>
    /// <param name="birthDate">The birth date.</param>
    /// <returns>The age in years.</returns>
    /// <exception cref="ArgumentNullException">Thrown when birthDate is null.</exception>
    public static int GetAge(this DateTime birthDate)
    {
        ArgumentNullException.ThrowIfNull(birthDate);

        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age;
    }
}