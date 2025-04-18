#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Events;

/// <summary>
/// In-memory implementation of the event bus.
/// Manages subscription and publication of domain events.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;
    private readonly object _lock = new();

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event) where T : DomainEvent
    {
        var eventType = typeof(T);

        List<Delegate> snapshot;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                _logger.LogDebug("No handlers registered for event type {EventType}", eventType.Name);
                return;
            }

            snapshot = handlers.ToList();
        }

        _logger.LogInformation("Publishing event {EventType} with {HandlerCount} handlers", eventType.Name, snapshot.Count);

        var tasks = new List<Task>();

        foreach (var handler in snapshot)
        {
            try
            {
                if (handler is Func<T, Task> typedHandler)
                {
                    tasks.Add(typedHandler(@event));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking handler for event {EventType}", eventType.Name);
            }
        }

        await Task.WhenAll(tasks);
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : DomainEvent
    {
        var eventType = typeof(T);

        lock (_lock)
        {
            _subscribers.AddOrUpdate(
                eventType,
                new List<Delegate> { handler },
                (_, list) =>
                {
                    list.Add(handler);
                    return list;
                }
            );
        }

        _logger.LogDebug("Handler registered for event type {EventType}", eventType.Name);
    }

    public void Unsubscribe<T>(Func<T, Task> handler) where T : DomainEvent
    {
        var eventType = typeof(T);

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var list))
            {
                list.Remove(handler);

                if (list.Count == 0)
                {
                    _subscribers.TryRemove(eventType, out _);
                }
            }
        }

        _logger.LogDebug("Handler unregistered for event type {EventType}", eventType.Name);
    }

    public IEnumerable<Func<T, Task>> GetSubscribers<T>() where T : DomainEvent
    {
        var eventType = typeof(T);

        lock (_lock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                return handlers.OfType<Func<T, Task>>().ToList();
            }
        }

        return Enumerable.Empty<Func<T, Task>>();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }

        _logger.LogInformation("All event subscriptions cleared");
    }
}
