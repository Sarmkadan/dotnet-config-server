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
/// Provides extension methods for <see cref="ConcurrencyException"/> and its derived types
/// to simplify common concurrency conflict handling scenarios.
/// </summary>
public static class ConcurrencyExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception represents an optimistic concurrency conflict.
    /// </summary>
    /// <param name="exception">The concurrency exception to check.</param>
    /// <returns>True if the exception is an <see cref="OptimisticConcurrencyException"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsOptimisticConcurrency(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is OptimisticConcurrencyException;
    }

    /// <summary>
    /// Determines whether the exception represents a circular dependency conflict.
    /// </summary>
    /// <param name="exception">The concurrency exception to check.</param>
    /// <returns>True if the exception is a <see cref="CircularDependencyException"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsCircularDependency(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is CircularDependencyException;
    }

    /// <summary>
    /// Extracts the entity type from an optimistic concurrency exception.
    /// Returns null if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The entity type involved in the conflict, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string? GetEntityType(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OptimisticConcurrencyException optException)
        {
            if (optException.Details is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("EntityType", out var entityType))
                {
                    return entityType?.ToString();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the entity ID from an optimistic concurrency exception.
    /// Returns null if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The entity ID involved in the conflict, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static Guid? GetEntityId(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OptimisticConcurrencyException optException)
        {
            if (optException.Details is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("EntityId", out var entityId))
                {
                    if (entityId is Guid guidId)
                    {
                        return guidId;
                    }

                    if (Guid.TryParse(entityId?.ToString(), out var parsedId))
                    {
                        return parsedId;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the expected version from an optimistic concurrency exception.
    /// Returns null if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The expected version string, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string? GetExpectedVersion(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OptimisticConcurrencyException optException)
        {
            if (optException.Details is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("ExpectedVersion", out var expectedVersion))
                {
                    return expectedVersion?.ToString();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the actual version from an optimistic concurrency exception.
    /// Returns null if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The actual version string, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string? GetActualVersion(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OptimisticConcurrencyException optException)
        {
            if (optException.Details is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue("ActualVersion", out var actualVersion))
                {
                    return actualVersion?.ToString();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a user-friendly message for retry scenarios.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <param name="retryCount">The number of retry attempts made.</param>
    /// <returns>A formatted message suitable for logging or user display.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string ToRetryMessage(this ConcurrencyException exception, int retryCount)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OptimisticConcurrencyException optEx =>
                $"Optimistic concurrency conflict (attempt {retryCount + 1}). Entity: {optEx.GetEntityType()}, ID: {optEx.GetEntityId()}. " +
                $"Expected version: {optEx.GetExpectedVersion()}, Actual: {optEx.GetActualVersion()}. " +
                $"Action: Retry with fresh data or implement merge strategy.",

            CircularDependencyException =>
                $"Circular dependency detected (attempt {retryCount + 1}). " +
                $"Action: Review dependency graph and break the cycle.",

            _ =>
                $"Concurrency conflict detected (attempt {retryCount + 1}). " +
                $"Error: {exception.Message}. Action: Retry operation or investigate resource contention."
        };
    }

    /// <summary>
    /// Determines whether the exception should trigger an automatic retry.
    /// Returns true for optimistic concurrency and circular dependency exceptions.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>True if automatic retry is recommended; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool ShouldRetryAutomatically(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is OptimisticConcurrencyException or CircularDependencyException;
    }

    /// <summary>
    /// Gets all related exception messages in a flat list, including inner exceptions.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>An enumerable of all exception messages in the hierarchy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static IEnumerable<string> GetAllMessages(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var current = exception;
        while (current != null)
        {
            yield return current.Message;
            current = current.InnerException as ConcurrencyException;
        }
    }
}