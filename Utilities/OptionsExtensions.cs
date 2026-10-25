#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for validating and normalizing configuration option values.
/// Intended for use inside options classes and option validation code paths.
/// </summary>
public static class OptionsExtensions
{
    /// <summary>
    /// Returns the value if present, otherwise the supplied fallback.
    /// </summary>
    /// <typeparam name="T">The underlying value type of the option.</typeparam>
    /// <param name="value">The nullable option value.</param>
    /// <param name="fallback">The value to use when <paramref name="value"/> is null.</param>
    /// <returns>The option value, or <paramref name="fallback"/> if it is null.</returns>
    public static T OrDefault<T>(this T? value, T fallback) where T : struct
    {
        return value ?? fallback;
    }

    /// <summary>
    /// Ensures a required reference-type option has been configured.
    /// </summary>
    /// <typeparam name="T">The reference type of the option.</typeparam>
    /// <param name="value">The option value to check.</param>
    /// <param name="optionName">The configuration key name used in the error message.</param>
    /// <returns>The non-null option value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is null.</exception>
    public static T RequireValue<T>(this T? value, string optionName) where T : class
    {
        ArgumentNullException.ThrowIfNull(optionName);

        return value ?? throw new InvalidOperationException($"Required option '{optionName}' is not configured.");
    }

    /// <summary>
    /// Ensures a duration option is strictly greater than zero (e.g., polling or timeout intervals).
    /// </summary>
    /// <param name="value">The duration to check.</param>
    /// <param name="optionName">The configuration key name used in the error message.</param>
    /// <returns>The same duration, so the call can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="optionName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is zero or negative.</exception>
    public static TimeSpan EnsurePositive(this TimeSpan value, string optionName)
    {
        ArgumentNullException.ThrowIfNull(optionName);

        if (value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(optionName, value, $"Option '{optionName}' must be a positive duration.");

        return value;
    }
}
