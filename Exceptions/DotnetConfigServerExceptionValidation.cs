#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="DotnetConfigServerException"/> instances.
/// </summary>
public static class DotnetConfigServerExceptionValidation
{
    /// <summary>
    /// Validates the specified exception instance.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <returns>A list of validation problems; empty if the exception is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate([NotNull] this DotnetConfigServerException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate ErrorCode if it's set
        if (!string.IsNullOrEmpty(value.ErrorCode))
        {
            if (string.IsNullOrWhiteSpace(value.ErrorCode))
            {
                problems.Add("ErrorCode cannot be whitespace or empty string.");
            }
        }

        // Validate Details if it's set
        if (value.Details is not null)
        {
            // Details can be any object, so we just check if it's null
            // No further validation needed for arbitrary objects
        }

        // Validate base Exception members
        if (string.IsNullOrWhiteSpace(value.Message))
        {
            problems.Add("Message cannot be null, empty, or whitespace.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified exception instance is valid.
    /// </summary>
    /// <param name="value">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid([NotNullWhen(true)] this DotnetConfigServerException? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified exception instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the exception is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid([NotNull] this DotnetConfigServerException? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DotnetConfigServerException is invalid. Problems: {string.Join(" ", problems)}");
        }
    }
}