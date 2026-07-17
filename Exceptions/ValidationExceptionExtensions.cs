#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ValidationException"/> to simplify common validation scenarios
/// </summary>
public static class ValidationExceptionExtensions
{
    /// <summary>
    /// Combines multiple validation errors into a single exception.
    /// </summary>
    /// <param name="exceptions">The validation exceptions to combine</param>
    /// <returns>A new ValidationException containing all errors from the input exceptions</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exceptions"/> is null</exception>
    public static ValidationException Combine(this IEnumerable<ValidationException> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

        var allErrors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var hasErrors = false;

        foreach (var exception in exceptions.Where(ex => ex != null))
        {
            hasErrors = true;
            foreach (var (fieldName, fieldErrors) in exception.Errors)
            {
                if (!allErrors.TryGetValue(fieldName, out var existingErrors))
                {
                    existingErrors = [];
                    allErrors[fieldName] = existingErrors;
                }

                existingErrors.AddRange(fieldErrors);
            }
        }

        return hasErrors
            ? new ValidationException(
                "Multiple validation errors occurred",
                allErrors,
                "VALIDATION_FAILED_MULTIPLE"
            )
            : new ValidationException(
                "No validation errors to combine",
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase),
                "VALIDATION_NO_ERRORS"
            );
    }

    /// <summary>
    /// Checks if the exception contains an error for a specific field.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <param name="fieldName">The field name to check</param>
    /// <returns>True if the field has validation errors; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when exception is null</exception>
    public static bool HasFieldError(this ValidationException exception, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        return exception.Errors.ContainsKey(fieldName);
    }

    /// <summary>
    /// Gets all error messages for a specific field.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <param name="fieldName">The field name to get errors for</param>
    /// <returns>An enumerable of error messages for the field, or empty if field has no errors</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is null or empty</exception>
    public static IEnumerable<string> GetFieldErrors(this ValidationException exception, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        return exception.Errors.TryGetValue(fieldName, out var errors)
            ? errors.AsReadOnly()
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Gets the first error message for a specific field.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <param name="fieldName">The field name to get the first error for</param>
    /// <returns>The first error message for the field, or null if field has no errors</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is null or empty</exception>
    public static string? GetFirstFieldError(this ValidationException exception, string fieldName)
        => exception.Errors.TryGetValue(fieldName, out var errors) && errors.Count > 0
            ? errors[0]
            : null;
}