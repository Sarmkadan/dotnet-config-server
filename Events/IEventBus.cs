#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetConfigServer.Events;

/// <summary>
/// Interface for domain event publishing and subscription with resilience features.
/// Enables decoupled communication between different components with retry and dead-letter handling.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered subscribers with retry and dead-letter handling.
    /// Implements exponential backoff with jitter (3 attempts by default) and routes failed events to a dead-letter sink.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="@event">The event to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
    Task PublishAsync<T>(T @event) where T : DomainEvent;

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    void Subscribe<T>(Func<T, Task> handler) where T : DomainEvent;

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="handler">The handler function to remove.</param>
    void Unsubscribe<T>(Func<T, Task> handler) where T : DomainEvent;

    /// <summary>
    /// Gets all subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <returns>An enumerable of handler functions.</returns>
    IEnumerable<Func<T, Task>> GetSubscribers<T>() where T : DomainEvent;

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    void Clear();
}

/// <summary>
/// Event handler interface.
/// </summary>
/// <typeparam name="T">The type of domain event.</typeparam>
public interface IEventHandler<T> where T : DomainEvent
{
    /// <summary>
    /// Handles the specified event.
    /// </summary>
    /// <param name="@event">The event to handle.</param>
    Task HandleAsync(T @event);
}