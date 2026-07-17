#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Events;

/// <summary>
/// Validation extension methods for <see cref="EventBusExtensions"/> that validate the extension methods
/// themselves and their usage patterns.
/// </summary>
public static class EventBusExtensionsValidation
{
    /// <summary>
    /// Validates the EventBusExtensions extension methods by testing them against a test event bus instance.
    /// This ensures the extension methods are callable and behave as expected.
    /// </summary>
    /// <param name="value">The event bus instance to validate against.</param>
    /// <param name="_extensions">Reserved parameter to distinguish from other Validate methods.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EventBus value, bool _extensions = true)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Test Subscribe<T, TNext> extension method
        try
        {
            var testHandler = new Func<object, Task>((e) => Task.CompletedTask);
            value.Subscribe<DomainEvent, DomainEvent>(testHandler);

            var countAfterSubscribe = value.GetSubscriberCount<DomainEvent>();
            if (countAfterSubscribe < 2)
            {
                problems.Add("Subscribe<T, TNext> did not register both event types");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"Subscribe<T, TNext> extension method failed: {ex.Message}");
        }

        // Test GetSubscriberCount<T> extension method
        try
        {
            var count = value.GetSubscriberCount<DomainEvent>();
            if (count < 0)
            {
                problems.Add("GetSubscriberCount<T> returned negative value");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"GetSubscriberCount<T> extension method failed: {ex.Message}");
        }

        // Test HasSubscribers<T> extension method
        try
        {
            var hasSubscribers = value.HasSubscribers<DomainEvent>();
            if (!hasSubscribers && hasSubscribers is not false)
            {
                problems.Add("HasSubscribers<T> did not return a boolean value");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"HasSubscribers<T> extension method failed: {ex.Message}");
        }

        // Test PublishAsyncWithCount<T> extension method
        try
        {
            var testEvent = new TestDomainEvent();
            var publishTask = value.PublishAsyncWithCount(testEvent);
            if (publishTask is null)
            {
                problems.Add("PublishAsyncWithCount<T> returned null");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"PublishAsyncWithCount<T> extension method failed: {ex.Message}");
        }

        // Test UnsubscribeAll<T> extension method
        try
        {
            value.UnsubscribeAll<DomainEvent>();
        }
        catch (Exception ex)
        {
            problems.Add($"UnsubscribeAll<T> extension method failed: {ex.Message}");
        }

        // Test GetAllSubscriberCounts extension method
        try
        {
            var allCounts = value.GetAllSubscriberCounts();
            if (allCounts is null)
            {
                problems.Add("GetAllSubscriberCounts returned null");
            }
            else if (allCounts is not IReadOnlyDictionary<string, int>)
            {
                problems.Add($"GetAllSubscriberCounts returned wrong type: {allCounts.GetType().FullName}");
            }
        }
        catch (Exception ex)
        {
            problems.Add($"GetAllSubscriberCounts extension method failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if the EventBusExtensions are valid by testing them against a test event bus instance.
    /// </summary>
    /// <param name="value">The event bus instance to check.</param>
    /// <returns>True if the extension methods are valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this EventBus value)
    {
        return value.Validate(_extensions: true).Count == 0;
    }

    /// <summary>
    /// Ensures the EventBusExtensions are valid by testing them against a test event bus instance,
    /// throwing an exception if any extension method fails.
    /// </summary>
    /// <param name="value">The event bus instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any extension method fails validation.</exception>
    public static void EnsureValid(this EventBus value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"EventBusExtensions validation failed:\n{string.Join("\n", problems)}");
        }
    }

    /// <summary>
    /// Test domain event for validation purposes.
    /// </summary>
    private sealed class TestDomainEvent : DomainEvent;
}