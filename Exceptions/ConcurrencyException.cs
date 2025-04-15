#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Thrown when concurrency conflicts occur (optimistic concurrency, race conditions)
/// </summary>
sealed public class ConcurrencyException : DotnetConfigServerException
{
    public ConcurrencyException(string message) : base(message, "CONCURRENCY_ERROR")
    {
    }

    public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "CONCURRENCY_ERROR";
    }

    public ConcurrencyException(string message, string errorCode, object? details = null) : base(message, errorCode, details)
    {
    }
}

/// <summary>
/// Thrown when optimistic concurrency check fails
/// </summary>
sealed public class OptimisticConcurrencyException : ConcurrencyException
{
    public OptimisticConcurrencyException(string entityType, Guid entityId, string expectedVersion, string actualVersion)
        : base($"Optimistic concurrency conflict for {entityType} {entityId}. Expected version: {expectedVersion}, Actual version: {actualVersion}",
              "OPTIMISTIC_CONCURRENCY_FAILED",
              new { EntityType = entityType, EntityId = entityId, ExpectedVersion = expectedVersion, ActualVersion = actualVersion })
    {
    }
}

/// <summary>
/// Thrown when a circular dependency is detected
/// </summary>
sealed public class CircularDependencyException : ConcurrencyException
{
    public CircularDependencyException(string message) : base(message, "CIRCULAR_DEPENDENCY")
    {
    }
}