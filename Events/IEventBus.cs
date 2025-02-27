// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Events;

/// <summary>
/// Interface for domain event publishing and subscription.
/// Enables decoupled communication between different components.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// </summary>
    Task PublishAsync<T>(T @event) where T : DomainEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    void Subscribe<T>(Func<T, Task> handler) where T : DomainEvent;

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    void Unsubscribe<T>(Func<T, Task> handler) where T : DomainEvent;

    /// <summary>
    /// Gets all subscribers for a specific event type.
    /// </summary>
    IEnumerable<Func<T, Task>> GetSubscribers<T>() where T : DomainEvent;

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    void Clear();
}

/// <summary>
/// Event handler interface.
/// </summary>
public interface IEventHandler<T> where T : DomainEvent
{
    Task HandleAsync(T @event);
}
