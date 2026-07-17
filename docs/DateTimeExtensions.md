# DateTimeExtensions

Provides a collection of extension methods for `System.DateTime` that simplify common date and time operations such as relative formatting, ISO 8601 representation, boundary calculations, range checks, and business logic computations. These methods are designed to reduce boilerplate in applications that frequently manipulate dates.

## API

### ToRelativeTime
```csharp
public static string ToRelativeTime(this DateTime dateTime)
```
Returns a human-readable string representing the time elapsed since the given `dateTime` relative to the current system time (e.g., "3 days ago", "just now"). Throws `ArgumentOutOfRangeException` if the date is in the future.

### ToIso8601
```csharp
public static string ToIso8601(this DateTime dateTime)
```
Converts the `DateTime` value to its ISO 8601 string representation (`yyyy-MM-ddTHH:mm:ss`). The `DateTimeKind` of the input is preserved in the output format when relevant (e.g., appending `Z` for UTC).

### StartOfDay
```csharp
public static DateTime StartOfDay(this DateTime dateTime)
```
Returns a new `DateTime` representing the start of the same day (00:00:00.000) while preserving the original `DateTimeKind`.

### EndOfDay
```csharp
public static DateTime EndOfDay(this DateTime dateTime)
```
Returns a new `DateTime` representing the end of the same day (23:59:59.999) while preserving the original `DateTimeKind`.

### StartOfWeek
```csharp
public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
```
Returns a new `DateTime` representing the start of the week containing the given date. The first day of the week defaults to Monday and can be overridden via the `startOfWeek` parameter.

### EndOfWeek
```csharp
public static DateTime EndOfWeek(this DateTime dateTime, DayOfWeek startOfWeek = DayOfWeek.Monday)
```
Returns a new `DateTime` representing the end of the week containing the given date, calculated as the last moment of the day before the next occurrence of `startOfWeek`.

### StartOfMonth
```csharp
public static DateTime StartOfMonth(this DateTime dateTime)
```
Returns a new `DateTime` set to the first day of the month at 00:00:00.000.

### EndOfMonth
```csharp
public static DateTime EndOfMonth(this DateTime dateTime)
```
Returns a new `DateTime` set to the last day of the month at 23:59:59.999.

### StartOfYear
```csharp
public static DateTime StartOfYear(this DateTime dateTime)
```
Returns a new `DateTime` set to January 1st of the same year at 00:00:00.000.

### EndOfYear
```csharp
public static DateTime EndOfYear(this DateTime dateTime)
```
Returns a new `DateTime` set to December 31st of the same year at 23:59:59.999.

### IsBetween
```csharp
public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end, bool inclusive = true)
```
Determines whether the `dateTime` falls within the specified range. When `inclusive` is `true` (default), the boundaries are included in the comparison; otherwise, the check is exclusive of both `start` and `end`. Throws `ArgumentException` if `start` is greater than `end`.

### GetBusinessDaysBetween
```csharp
public static int GetBusinessDaysBetween(this DateTime start, DateTime end)
```
Calculates the number of business days (Monday through Friday) between two dates, inclusive of the start date and exclusive of the end date. Returns a non-negative integer. The order of `start` and `end` is respected; if `end` is before `start`, the result is zero.

### IsLeapYear
```csharp
public static bool IsLeapYear(this DateTime dateTime)
```
Returns `true` if the year component of the `DateTime` is a leap year according to the Gregorian calendar rules.

### GetAge
```csharp
public static int GetAge(this DateTime dateOfBirth, DateTime? referenceDate = null)
```
Calculates the age in full years from the `dateOfBirth` to the `referenceDate`. If `referenceDate` is `null`, the current system date is used. Throws `ArgumentOutOfRangeException` if `dateOfBirth` is later than the reference date.

## Usage

### Calculating a report date range
```csharp
var now = DateTime.UtcNow;
var monthStart = now.StartOfMonth();
var monthEnd = now.EndOfMonth();

Console.WriteLine($"Report period: {monthStart.ToIso8601()} to {monthEnd.ToIso8601()}");
// Output: Report period: 2025-03-01T00:00:00Z to 2025-03-31T23:59:59Z
```

### Checking eligibility based on age and business days
```csharp
var birthDate = new DateTime(1990, 5, 15);
var age = birthDate.GetAge();
var isAdult = age >= 18;

var requestDate = new DateTime(2025, 3, 1);
var deadline = new DateTime(2025, 3, 15);
var remainingDays = requestDate.GetBusinessDaysBetween(deadline);

if (isAdult && remainingDays >= 5)
{
    Console.WriteLine($"Request accepted. {remainingDays} business days remaining.");
}
```

## Notes

- All boundary methods (`StartOfDay`, `EndOfMonth`, etc.) preserve the `DateTimeKind` of the original instance. When performing comparisons or storing results, ensure that `DateTimeKind` mismatches are handled explicitly to avoid unintended time zone conversions.
- `EndOfDay` and similar end-boundary methods use `23:59:59.999` as the maximum representable time for the day. This can cause precision issues when comparing against values with higher fractional-second resolution; consider using exclusive upper bounds (`< nextDay.StartOfDay()`) for range queries.
- `GetBusinessDaysBetween` treats the range as `[start, end)` and does not account for public holidays. For holiday-aware calculations, combine this method with a holiday exclusion set.
- `ToRelativeTime` relies on `DateTime.Now` internally and is therefore sensitive to the system clock and time zone. It is not suitable for persisted or distributed scenarios where consistency across machines is required.
- All methods are stateless and thread-safe. They operate purely on the input parameters and do not mutate shared state.
