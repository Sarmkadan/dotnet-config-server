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
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Safely truncates a string to a maximum length with optional suffix.
    /// </summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - suffix.Length) + suffix;
    }

    /// <summary>
    /// Converts a string to a sanitized filename by removing invalid characters.
    /// </summary>
    public static string ToSafeFileName(this string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();

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
    public static string ToKebabCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var pascalPattern = new Regex(@"(?<!^)(?=[A-Z])");
        return pascalPattern.Replace(value, "-").ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to PascalCase (e.g., "hello-world" or "hello_world" -> "HelloWorld").
    /// </summary>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var parts = Regex.Split(value, @"[-_\s]+");
        return string.Concat(parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
    }

    /// <summary>
    /// Converts a string to snake_case (e.g., "HelloWorld" -> "hello_world").
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var pascalPattern = new Regex(@"(?<!^)(?=[A-Z])");
        return pascalPattern.Replace(value, "_").ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a string matches a regex pattern.
    /// </summary>
    public static bool MatchesPattern(this string value, string pattern)
    {
        return Regex.IsMatch(value, pattern);
    }

    /// <summary>
    /// Strips all whitespace from a string.
    /// </summary>
    public static string RemoveWhitespace(this string value)
    {
        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    /// <summary>
    /// Repeats a string n times.
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        return count <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Checks if a string is a valid email address.
    /// </summary>
    public static bool IsValidEmail(this string value)
    {
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
    public static string UrlEncode(this string value)
    {
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Safely decodes a URL-encoded string.
    /// </summary>
    public static string UrlDecode(this string value)
    {
        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    /// Gets the common prefix of two strings.
    /// </summary>
    public static string CommonPrefix(this string a, string b)
    {
        var minLength = Math.Min(a.Length, b.Length);
        var i = 0;

        while (i < minLength && a[i] == b[i])
            i++;

        return a.Substring(0, i);
    }
}
