#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <param name="value">The string value to validate.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    public static ValidationResult ValidateNotEmpty(this string? value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" };

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a string meets a minimum length requirement.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minLength"/> is negative.</exception>
    public static ValidationResult ValidateMinLength(this string value, int minLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);

        return value?.Length >= minLength
            ? new ValidationResult { IsValid = true }
            : new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must be at least {minLength} characters" };
    }

    /// <summary>
    /// Validates that a string meets a maximum length requirement.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static ValidationResult ValidateMaxLength(this string value, int maxLength, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        return value?.Length <= maxLength
            ? new ValidationResult { IsValid = true }
            : new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must not exceed {maxLength} characters" };
    }

    /// <summary>
    /// Validates that a value is within a specified range.
    /// </summary>
    /// <typeparam name="T">The type of values being compared, must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static ValidationResult ValidateRange<T>(this T value, T min, T max, string fieldName) where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(min);
        ArgumentNullException.ThrowIfNull(max);

        if (min.CompareTo(max) > 0)
            throw new ArgumentException($"Minimum value cannot be greater than maximum value", nameof(min));

        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0
            ? new ValidationResult { IsValid = true }
            : new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} must be between {min} and {max}" };
    }

    /// <summary>
    /// Validates that a string matches a regular expression pattern.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="pattern">The regular expression pattern to match against.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/>, <paramref name="pattern"/>, or <paramref name="fieldName"/> is null.</exception>
    public static ValidationResult ValidatePattern(this string value, string pattern, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(fieldName);

        return Regex.IsMatch(value, pattern)
            ? new ValidationResult { IsValid = true }
            : new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} has invalid format" };
    }

    /// <summary>
    /// Validates that a string is a valid email address.
    /// </summary>
    /// <param name="value">The email address to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static ValidationResult ValidateEmail(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        const string emailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        return value.ValidatePattern(emailPattern, "Email");
    }

    /// <summary>
    /// Validates that a string is a valid URL.
    /// </summary>
    /// <param name="value">The URL to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static ValidationResult ValidateUrl(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

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
    /// <param name="value">The GUID value to validate.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    public static ValidationResult ValidateNotEmpty(this Guid value, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        return value == Guid.Empty
            ? new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" }
            : new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="fieldName">The name of the field being validated, used in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    public static ValidationResult ValidateNotEmpty<T>(this IEnumerable<T>? collection, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        return collection is null || !collection.Any()
            ? new ValidationResult { IsValid = false, ErrorMessage = $"{fieldName} cannot be empty" }
            : new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Combines multiple validation results.
    /// </summary>
    /// <param name="results">The validation results to combine.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether all validations passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
    public static ValidationResult Combine(this IEnumerable<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var errors = results.Where(r => !r.IsValid).Select(r => r.ErrorMessage).ToList();

        return errors.Count == 0
            ? new ValidationResult { IsValid = true }
            : new ValidationResult
            {
                IsValid = false,
                ErrorMessage = string.Join("; ", errors)
            };
    }

    /// <summary>
    /// Validates the first condition and returns the result or throws.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ValidationException">Thrown when the validation result is invalid.</exception>
    public static void ThrowIfInvalid(this ValidationResult result)
    {
        if (!result.IsValid)
            throw new ValidationException("Validation", result.ErrorMessage ?? "Validation failed");
    }

    /// <summary>
    /// Validates a condition and returns a result.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="errorMessage">The error message to return if validation fails.</param>
    /// <param name="fieldName">Optional field name for context in error messages.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether validation passed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errorMessage"/> is null.</exception>
    public static ValidationResult ValidateCondition(bool condition, string errorMessage, string fieldName = "")
    {
        ArgumentNullException.ThrowIfNull(errorMessage);

        return !condition
            ? new ValidationResult { IsValid = false, ErrorMessage = errorMessage }
            : new ValidationResult { IsValid = true };
    }
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}