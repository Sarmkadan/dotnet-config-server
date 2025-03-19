#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class DateTimeExtensionsTests
{
    // ── ToRelativeTime ───────────────────────────────────────────────────────

    [Fact]
    public void ToRelativeTime_JustNow_ReturnsJustNow()
    {
        var dt = DateTime.UtcNow.AddSeconds(-5);

        dt.ToRelativeTime().Should().Be("just now");
    }

    [Fact]
    public void ToRelativeTime_OneMinuteAgo_ReturnsSingularForm()
    {
        var dt = DateTime.UtcNow.AddMinutes(-1);

        dt.ToRelativeTime().Should().Be("1 minute ago");
    }

    [Fact]
    public void ToRelativeTime_TwoMinutesAgo_ReturnsPluralForm()
    {
        var dt = DateTime.UtcNow.AddMinutes(-2);

        dt.ToRelativeTime().Should().Be("2 minutes ago");
    }

    [Fact]
    public void ToRelativeTime_OneHourAgo_ReturnsSingularForm()
    {
        var dt = DateTime.UtcNow.AddHours(-1);

        dt.ToRelativeTime().Should().Be("1 hour ago");
    }

    [Fact]
    public void ToRelativeTime_ThreeHoursAgo_ReturnsPluralForm()
    {
        var dt = DateTime.UtcNow.AddHours(-3);

        dt.ToRelativeTime().Should().Be("3 hours ago");
    }

    [Fact]
    public void ToRelativeTime_OneDayAgo_ReturnsDayAgo()
    {
        var dt = DateTime.UtcNow.AddDays(-1);

        dt.ToRelativeTime().Should().Be("1 day ago");
    }

    [Fact]
    public void ToRelativeTime_OneWeekAgo_ReturnsWeekAgo()
    {
        var dt = DateTime.UtcNow.AddDays(-7);

        dt.ToRelativeTime().Should().Be("1 week ago");
    }

    // ── ToIso8601 ────────────────────────────────────────────────────────────

    [Fact]
    public void ToIso8601_KnownDate_ReturnsRoundTripFormat()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var iso = dt.ToIso8601();

        iso.Should().StartWith("2024-06-15T10:30:00");
    }

    [Fact]
    public void ToIso8601_ParsedBack_MatchesOriginal()
    {
        var original = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var iso = original.ToIso8601();
        var parsed = DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind);

        parsed.Should().Be(original);
    }

    // ── StartOfDay / EndOfDay ────────────────────────────────────────────────

    [Fact]
    public void StartOfDay_AnyTime_ReturnsDateAtMidnight()
    {
        var dt = new DateTime(2024, 3, 15, 14, 30, 45);

        dt.StartOfDay().Should().Be(new DateTime(2024, 3, 15, 0, 0, 0));
    }

    [Fact]
    public void EndOfDay_AnyTime_ReturnsLastTickOfDay()
    {
        var dt = new DateTime(2024, 3, 15, 14, 30, 45);
        var end = dt.EndOfDay();

        end.Date.Should().Be(new DateTime(2024, 3, 15));
        end.TimeOfDay.Should().Be(new TimeSpan(23, 59, 59) + TimeSpan.FromTicks(TimeSpan.TicksPerSecond - 1));
    }

    // ── StartOfWeek ──────────────────────────────────────────────────────────

    [Fact]
    public void StartOfWeek_Wednesday_ReturnsMonday()
    {
        var wednesday = new DateTime(2024, 3, 20); // Wednesday

        var startOfWeek = wednesday.StartOfWeek(DayOfWeek.Monday);

        startOfWeek.DayOfWeek.Should().Be(DayOfWeek.Monday);
        startOfWeek.Date.Should().Be(new DateTime(2024, 3, 18));
    }

    [Fact]
    public void StartOfWeek_Sunday_ReturnsStartOfPreviousWeek()
    {
        var sunday = new DateTime(2024, 3, 17); // Sunday

        var start = sunday.StartOfWeek(DayOfWeek.Monday);

        start.DayOfWeek.Should().Be(DayOfWeek.Monday);
        start.Date.Should().Be(new DateTime(2024, 3, 11));
    }

    // ── StartOfMonth / EndOfMonth ────────────────────────────────────────────

    [Fact]
    public void StartOfMonth_MidMonth_ReturnsFirstDayOfMonth()
    {
        var dt = new DateTime(2024, 7, 15);

        dt.StartOfMonth().Should().Be(new DateTime(2024, 7, 1));
    }

    [Fact]
    public void EndOfMonth_February2024_ReturnsLastDayOfFebruaryLeapYear()
    {
        var feb = new DateTime(2024, 2, 10);

        var end = feb.EndOfMonth();

        end.Date.Should().Be(new DateTime(2024, 2, 29));
    }

    // ── StartOfYear / EndOfYear ──────────────────────────────────────────────

    [Fact]
    public void StartOfYear_MidYear_ReturnsJanuaryFirst()
    {
        new DateTime(2023, 8, 20).StartOfYear().Should().Be(new DateTime(2023, 1, 1));
    }

    [Fact]
    public void EndOfYear_MidYear_ReturnsDecemberThirtyFirst()
    {
        var end = new DateTime(2023, 5, 10).EndOfYear();

        end.Date.Should().Be(new DateTime(2023, 12, 31));
    }

    // ── IsBetween ────────────────────────────────────────────────────────────

    [Fact]
    public void IsBetween_DateWithinRange_ReturnsTrue()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);
        var mid = new DateTime(2024, 6, 15);

        mid.IsBetween(start, end).Should().BeTrue();
    }

    [Fact]
    public void IsBetween_DateAtBoundary_ReturnsTrue()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);

        start.IsBetween(start, end).Should().BeTrue();
        end.IsBetween(start, end).Should().BeTrue();
    }

    [Fact]
    public void IsBetween_DateOutsideRange_ReturnsFalse()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);

        new DateTime(2023, 12, 31).IsBetween(start, end).Should().BeFalse();
        new DateTime(2025, 1, 1).IsBetween(start, end).Should().BeFalse();
    }

    // ── GetBusinessDaysBetween ───────────────────────────────────────────────

    [Fact]
    public void GetBusinessDaysBetween_WeekdaySpan_ExcludesWeekends()
    {
        var monday = new DateTime(2024, 3, 18); // Monday
        var friday = new DateTime(2024, 3, 22); // Friday

        var days = monday.GetBusinessDaysBetween(friday);

        days.Should().Be(5);
    }

    [Fact]
    public void GetBusinessDaysBetween_SingleDay_ReturnsOne()
    {
        var monday = new DateTime(2024, 3, 18);

        var days = monday.GetBusinessDaysBetween(monday);

        days.Should().Be(1);
    }

    [Fact]
    public void GetBusinessDaysBetween_WeekendOnly_ReturnsZero()
    {
        var saturday = new DateTime(2024, 3, 16);
        var sunday = new DateTime(2024, 3, 17);

        saturday.GetBusinessDaysBetween(sunday).Should().Be(0);
    }

    // ── IsLeapYear ───────────────────────────────────────────────────────────

    [Fact]
    public void IsLeapYear_LeapYear_ReturnsTrue()
    {
        new DateTime(2024, 1, 1).IsLeapYear().Should().BeTrue();
    }

    [Fact]
    public void IsLeapYear_NonLeapYear_ReturnsFalse()
    {
        new DateTime(2023, 1, 1).IsLeapYear().Should().BeFalse();
    }

    // ── GetAge ───────────────────────────────────────────────────────────────

    [Fact]
    public void GetAge_BirthDateInPast_ReturnsCorrectAge()
    {
        var today = DateTime.UtcNow;
        var birthDate = today.AddYears(-30).AddDays(1); // birthday is tomorrow

        var age = birthDate.GetAge();

        age.Should().Be(29);
    }
}
