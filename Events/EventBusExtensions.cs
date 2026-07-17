#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Events;

/// <summary>
/// Extension methods for <see cref="EventBus"/> that provide additional functionality
/// for event bus management and diagnostics.
/// </summary>
public static class EventBusExtensions
{
    private static readonly ConcurrentDictionary<Type, string> _typeNameCache = new();

    /// <summary>
    /// Subscribes a handler to multiple event types at once.
    /// </summary>
    /// <typeparam name="T">The first event type to subscribe to.</typeparam>
    /// <typeparam name="TNext">The second event type to subscribe to.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <param name="handler">The handler to subscribe.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> or <paramref name="handler"/> is null.</exception>
    public static void Subscribe<T, TNext>(this EventBus bus, Func<object, Task> handler)
        where T : DomainEvent
        where TNext : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);

        bus.Subscribe<T>(h => handler(h));
        bus.Subscribe<TNext>(h => handler(h));
    }

    /// <summary>
    /// Gets the count of subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type to count subscribers for.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>The number of subscribers for the specified event type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public static int GetSubscriberCount<T>(this EventBus bus)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(bus);

        return bus.GetSubscribers<T>().Count();
    }

    /// <summary>
    /// Checks if there are any subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>True if there are subscribers; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public static bool HasSubscribers<T>(this EventBus bus)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(bus);

        return bus.GetSubscribers<T>().Any();
    }

    /// <summary>
    /// Publishes an event and returns the number of handlers that were invoked.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <param name="event">The event to publish.</param>
    /// <returns>The number of handlers that successfully processed the event.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> or <paramref name="event"/> is null.</exception>
    public static async Task<int> PublishAsyncWithCount<T>(this EventBus bus, T @event)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(@event);

        var subscribers = bus.GetSubscribers<T>().ToList();

        if (subscribers.Count == 0)
        {
            return 0;
        }

        await bus.PublishAsync(@event);
        return subscribers.Count;
    }

    /// <summary>
    /// Unsubscribes all handlers of a specific type from the event bus.
    /// </summary>
    /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
    /// <param name="bus">The event bus instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public static void UnsubscribeAll<T>(this EventBus bus)
        where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(bus);

        var subscribers = bus.GetSubscribers<T>().ToList();

        foreach (var subscriber in subscribers)
        {
            bus.Unsubscribe<T>(subscriber);
        }
    }

    /// <summary>
    /// Gets a snapshot of all subscriber counts across all event types.
    /// </summary>
    /// <param name="bus">The event bus instance.</param>
    /// <returns>A dictionary mapping event type names to subscriber counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public static IReadOnlyDictionary<string, int> GetAllSubscriberCounts(this EventBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        var subscribersField = typeof(EventBus).GetField(
            "_subscribers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (subscribersField is null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        var subscribers = (ConcurrentDictionary<Type, List<Delegate>>)subscribersField.GetValue(bus)!;

        return subscribers.ToDictionary(
            kvp => _typeNameCache.GetOrAdd(kvp.Key, static t => t.Name),
            kvp => kvp.Value.Count,
            StringComparer.Ordinal);
    }
}