#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;

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
    /// <returns><see langword="true"/> if the exception is an <see cref="OptimisticConcurrencyException"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool IsOptimisticConcurrency(this ConcurrencyException exception)
    => ArgumentNullException.ThrowIfNull(exception) is OptimisticConcurrencyException;

    /// <summary>
    /// Determines whether the exception represents a circular dependency conflict.
    /// </summary>
    /// <param name="exception">The concurrency exception to check.</param>
    /// <returns><see langword="true"/> if the exception is a <see cref="CircularDependencyException"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static bool IsCircularDependency(this ConcurrencyException exception)
        => ArgumentNullException.ThrowIfNull(exception) is CircularDependencyException;

    /// <summary>
    /// Extracts the entity type from an optimistic concurrency exception.
    /// Returns <see langword="null"/> if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The entity type involved in the conflict, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string? GetEntityType(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OptimisticConcurrencyException optException when optException.Details is IDictionary<string, object> dict
                => dict.TryGetValue("EntityType", out var entityType) ? entityType?.ToString() : null,
            _ => null
        };
    }

    /// <summary>
    /// Extracts the entity ID from an optimistic concurrency exception.
    /// Returns <see langword="null"/> if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The entity ID involved in the conflict, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static Guid? GetEntityId(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OptimisticConcurrencyException optException when optException.Details is IDictionary<string, object> dict
                => dict.TryGetValue("EntityId", out var entityId)
                    ? entityId switch
                      {
                          Guid guidId => guidId,
                          _ when Guid.TryParse(entityId?.ToString(), out var parsedId) => parsedId,
                          _ => null
                      }
                    : null,
            _ => null
        };
    }

    /// <summary>
    /// Extracts the expected version from an optimistic concurrency exception.
    /// Returns <see langword="null"/> if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The expected version string, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string? GetExpectedVersion(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OptimisticConcurrencyException optException when optException.Details is IDictionary<string, object> dict
                => dict.TryGetValue("ExpectedVersion", out var expectedVersion) ? expectedVersion?.ToString() : null,
            _ => null
        };
    }

    /// <summary>
    /// Extracts the actual version from an optimistic concurrency exception.
    /// Returns <see langword="null"/> if the exception is not an optimistic concurrency exception.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns>The actual version string, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static string? GetActualVersion(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OptimisticConcurrencyException optException when optException.Details is IDictionary<string, object> dict
                => dict.TryGetValue("ActualVersion", out var actualVersion) ? actualVersion?.ToString() : null,
            _ => null
        };
    }

    /// <summary>
    /// Creates a user-friendly message for retry scenarios.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <param name="retryCount">The number of retry attempts made.</param>
    /// <returns>A formatted message suitable for logging or user display.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
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
    /// Returns <see langword="true"/> for optimistic concurrency and circular dependency exceptions.
    /// </summary>
    /// <param name="exception">The concurrency exception.</param>
    /// <returns><see langword="true"/> if automatic retry is recommended; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static IEnumerable<string> GetAllMessages(this ConcurrencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception; current != null; current = current.InnerException as ConcurrencyException)
        {
            yield return current.Message;
        }
    }
}