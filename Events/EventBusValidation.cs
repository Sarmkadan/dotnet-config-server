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

        // Validate internal state consistency
        try
        {
            var subscriberCount = value.GetSubscriberCount<DomainEvent>();
            if (subscriberCount < 0)
            {
                problems.Add("Subscriber count cannot be negative");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"Internal state validation failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EventBus"/> instance is valid.
    /// </summary>
    /// <param name="value">The event bus instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this EventBus? value) => value?.Validate()?.Count == 0;

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