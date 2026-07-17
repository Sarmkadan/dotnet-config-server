#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="ConcurrencyException"/> and its derived types.
/// </summary>
public static class ConcurrencyExceptionExtensionsValidation
{
    /// <summary>
    /// Validates that a <see cref="ConcurrencyException"/> instance is valid.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <returns>A list of validation errors; empty if valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ConcurrencyException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate base exception properties
        if (string.IsNullOrWhiteSpace(value.Message))
        {
            errors.Add("Message cannot be null, empty, or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.ErrorCode))
        {
            errors.Add("ErrorCode cannot be null, empty, or whitespace.");
        }

        // Validate OptimisticConcurrencyException specific properties
        if (value is OptimisticConcurrencyException optimisticException)
        {
            ValidateOptimisticConcurrencyException(optimisticException, errors);
        }

        // Validate CircularDependencyException specific properties
        if (value is CircularDependencyException circularException)
        {
            ValidateCircularDependencyException(circularException, errors);
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ConcurrencyException"/> instance is valid.
    /// </summary>
    /// <param name="value">The exception to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsValid(this ConcurrencyException value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that a <see cref="ConcurrencyException"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The exception to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the exception is invalid, containing validation errors.</exception>
    public static void EnsureValid(this ConcurrencyException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ConcurrencyException validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static void ValidateOptimisticConcurrencyException(
        OptimisticConcurrencyException exception,
        List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errors);

        var entityType = exception.GetEntityType();
        if (string.IsNullOrWhiteSpace(entityType))
        {
            errors.Add("EntityType cannot be null, empty, or whitespace for OptimisticConcurrencyException.");
        }

        var entityId = exception.GetEntityId();
        if (entityId == null || entityId == Guid.Empty)
        {
            errors.Add("EntityId must be a valid GUID for OptimisticConcurrencyException.");
        }

        var expectedVersion = exception.GetExpectedVersion();
        if (string.IsNullOrWhiteSpace(expectedVersion))
        {
            errors.Add("ExpectedVersion cannot be null, empty, or whitespace for OptimisticConcurrencyException.");
        }

        var actualVersion = exception.GetActualVersion();
        if (string.IsNullOrWhiteSpace(actualVersion))
        {
            errors.Add("ActualVersion cannot be null, empty, or whitespace for OptimisticConcurrencyException.");
        }
    }

    private static void ValidateCircularDependencyException(
        CircularDependencyException exception,
        List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errors);

        // No additional CircularDependencyException-specific validation needed
    }
}
