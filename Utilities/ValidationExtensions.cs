#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using DotnetConfigServer.Exceptions;

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for common validation operations.
/// Provides fluent API for validation with meaningful error messages.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    public static ValidationResult ValidateNotEmpty(this string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a string meets a minimum length requirement.
    /// </summary>
    public static ValidationResult ValidateMinLength(this string value, int minLength, string fieldName)
    {
        if (value?.Length < minLength)
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must be at least {minLength} characters" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a string meets a maximum length requirement.
    /// </summary>
    public static ValidationResult ValidateMaxLength(this string value, int maxLength, string fieldName)
    {
        if (value?.Length > maxLength)
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must not exceed {maxLength} characters" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a value is within a specified range.
    /// </summary>
    public static ValidationResult ValidateRange<T>(this T value, T min, T max, string fieldName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must be between {min} and {max}" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a string matches a regular expression pattern.
    /// </summary>
    public static ValidationResult ValidatePattern(this string value, string pattern, string fieldName)
    {
        if (!Regex.IsMatch(value, pattern))
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} has invalid format" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a string is a valid email address.
    /// </summary>
    public static ValidationResult ValidateEmail(this string value)
    {
        const string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        return value.ValidatePattern(emailPattern, "Email");
    }

    /// <summary>
    /// Validates that a string is a valid URL.
    /// </summary>
    public static ValidationResult ValidateUrl(this string value)
    {
        try
        {
            _ = new Uri(value);
            return new ValidationResult { IsValid = true };
        }
        catch
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Invalid URL format" };
        }
    }

    /// <summary>
    /// Validates that a GUID is not empty.
    /// </summary>
    public static ValidationResult ValidateNotEmpty(this Guid value, string fieldName)
    {
        if (value == Guid.Empty)
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    public static ValidationResult ValidateNotEmpty<T>(this IEnumerable<T>? collection, string fieldName)
    {
        if (collection is null || !collection.Any())
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Combines multiple validation results.
    /// </summary>
    public static ValidationResult Combine(this IEnumerable<ValidationResult> results)
    {
        var errors = results.Where(r => !r.IsValid).Select(r => r.ErrorMessage).ToList();

        if (errors.Count == 0)
            return new ValidationResult { IsValid = true };

        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = string.Join("; ", errors)
        };
    }

    /// <summary>
    /// Validates the first condition and returns the result or throws.
    /// </summary>
    public static void ThrowIfInvalid(this ValidationResult result)
    {
        if (!result.IsValid)
            throw new ValidationException("Validation", result.ErrorMessage ?? "Validation failed");
    }

    /// <summary>
    /// Validates a condition and returns a result.
    /// </summary>
    public static ValidationResult ValidateCondition(bool condition, string errorMessage, string fieldName = "")
    {
        if (!condition)
            return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };

        return new ValidationResult { IsValid = true };
    }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
sealed public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}
