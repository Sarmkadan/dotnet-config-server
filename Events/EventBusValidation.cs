#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Events;

/// <summary>
/// Provides validation helpers for <see cref="EventBus"/> instances.
/// </summary>
public static class EventBusValidation
{
    /// <summary>
    /// Validates the specified <see cref="EventBus"/> instance.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <returns>A list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBus? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // The EventBus has no public properties to validate beyond its constructor dependencies
        // The logger is injected via constructor and validated there
        // All other members are methods that operate on the internal state

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this EventBus? value)
    {
        try
        {
            _ = value?.Validate() ?? throw new ArgumentNullException(nameof(value));
            return true;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance has validation problems.</exception>
    public static void EnsureValid(this EventBus? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"EventBus validation failed: {string.Join("; ", problems)}",
                nameof(value));
        }
    }
}