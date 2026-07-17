#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for string manipulation and validation.
/// Provides common utilities for sanitization, validation, and transformation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null, empty, or contains only whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise false.</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Safely truncates a string to a maximum length with optional suffix.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">Maximum length of the resulting string.</param>
    /// <param name="suffix">Suffix to append when truncating (default: "...").</param>
    /// <returns>The truncated string or original if not needed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..Math.Min(maxLength - suffix.Length, value.Length)] + suffix;
    }

    /// <summary>
    /// Converts a string to a sanitized filename by removing invalid characters.
    /// </summary>
    /// <param name="value">The string to sanitize.</param>
    /// <returns>A filename-safe string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToSafeFileName(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if (!invalidChars.Contains(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a string to kebab-case (e.g., "HelloWorld" -> "hello-world").
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The kebab-cased string, or original if null/empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToKebabCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
            return value;

        var pascalPattern = new Regex("(?<!^)(?=[A-Z])");
        return pascalPattern.Replace(value, "-").ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to PascalCase (e.g., "hello-world" or "hello_world" -> "HelloWorld").
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The Pascal-cased string, or original if null/empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToPascalCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
            return value;

        var parts = Regex.Split(value, @"[-_\s]+");
        return string.Concat(parts.Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>
    /// Converts a string to snake_case (e.g., "HelloWorld" -> "hello_world").
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The snake-cased string, or original if null/empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToSnakeCase(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
            return value;

        var pascalPattern = new Regex("(?<!^)(?=[A-Z])");
        return pascalPattern.Replace(value, "_").ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a string matches a regex pattern.
    /// </summary>
    /// <param name="value">The string to match.</param>
    /// <param name="pattern">The regex pattern to match against.</param>
    /// <returns>True if the string matches the pattern; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> or <paramref name="pattern"/> is null.</exception>
    public static bool MatchesPattern(this string value, string pattern)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(pattern);

        return Regex.IsMatch(value, pattern);
    }

    /// <summary>
    /// Strips all whitespace from a string.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <returns>A string with all whitespace removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string RemoveWhitespace(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
    }

    /// <summary>
    /// Repeats a string n times.
    /// </summary>
    /// <param name="value">The string to repeat.</param>
    /// <param name="count">Number of times to repeat.</param>
    /// <returns>The repeated string, or empty if count is 0 or negative.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string Repeat(this string value, int count)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (count <= 0)
            return string.Empty;

        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Checks if a string is a valid email address.
    /// </summary>
    /// <param name="value">The email address to validate.</param>
    /// <returns>True if the string is a valid email address; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValidEmail(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely encodes a string for use in URLs.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <returns>The URL-encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string UrlEncode(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Safely decodes a URL-encoded string.
    /// </summary>
    /// <param name="value">The string to decode.</param>
    /// <returns>The URL-decoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string UrlDecode(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    /// Gets the common prefix of two strings.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The common prefix of both strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="a"/> or <paramref name="b"/> is null.</exception>
    public static string CommonPrefix(this string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var minLength = Math.Min(a.Length, b.Length);
        var i = 0;

        while (i < minLength && a[i] == b[i])
            i++;

        return a[..i];
    }
}