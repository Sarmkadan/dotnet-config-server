#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;

namespace DotnetConfigServer.Events;

/// <summary>
/// In-memory implementation of the event bus with retry and dead-letter handling.
/// Manages subscription and publication of domain events with resilience features.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ILogger<EventBus> _logger;
    private readonly object _lock = new();
    private readonly FailedEventSink _failedEventSink;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBus"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
        _failedEventSink = new FailedEventSink();
    }

    /// <summary>
    /// Publishes an event to all registered subscribers with retry and dead-letter handling.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="@event">The event to publish.</param>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
    public async Task PublishAsync<T>(T @event) where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

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

        // Apply retry policy with exponential backoff and jitter
        var retryPolicy = new RetryPolicy(3, TimeSpan.FromMilliseconds(100), _logger);
        var publishResult = await retryPolicy.ExecuteAsync(async () =>
        {
            var tasks = new List<Task>();
            var handlerExceptions = new List<Exception>();

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
                    handlerExceptions.Add(ex);
                }
            }

            await Task.WhenAll(tasks);
            return handlerExceptions;
        });

        // Handle failed events after all retries exhausted
        if (publishResult.Count > 0)
        {
            _logger.LogError("Failed to publish event {EventType} after {RetryCount} attempts. Moving to dead-letter sink.",
                eventType.Name, retryPolicy.MaxRetries);

            await _failedEventSink.RecordFailedEventAsync(@event, publishResult);
        }
    }

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="handler">The handler function.</param>
    /// <exception cref="ArgumentNullException">Thrown when handler is null.</exception>
    public void Subscribe<T>(Func<T, Task> handler) where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

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

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <param name="handler">The handler function to remove.</param>
    public void Unsubscribe<T>(Func<T, Task> handler) where T : DomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

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

    /// <summary>
    /// Gets all subscribers for a specific event type.
    /// </summary>
    /// <typeparam name="T">The type of domain event.</typeparam>
    /// <returns>An enumerable of handler functions.</returns>
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

    /// <summary>
    /// Clears all subscriptions.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }

        _logger.LogInformation("All event subscriptions cleared");
    }

    /// <summary>
    /// Retry policy with exponential backoff and jitter for resilient event publishing.
    /// </summary>
    private sealed class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly ILogger _logger;
        private readonly Random _random = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <param name="initialDelay">Initial delay between retries.</param>
        /// <param name="logger">Logger instance.</param>
        public RetryPolicy(int maxRetries, TimeSpan initialDelay, ILogger logger)
        {
            _maxRetries = maxRetries;
            _initialDelay = initialDelay;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the specified action with retry logic.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the action.</returns>
        public async Task<List<Exception>> ExecuteAsync(Func<Task<List<Exception>>> action)
        {
            var exceptions = new List<Exception>();
            var currentDelay = _initialDelay;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    var handlerExceptions = await action();

                    // If no exceptions, return empty list (success)
                    if (handlerExceptions.Count == 0)
                    {
                        return new List<Exception>();
                    }

                    // If we got exceptions but this is the final attempt, return them
                    if (attempt == _maxRetries)
                    {
                        exceptions.AddRange(handlerExceptions);
                        return exceptions;
                    }

                    // Log retry attempt
                    _logger.LogWarning("Retry attempt {Attempt}/{MaxRetries} failed for event publishing. Will retry in {DelayMs}ms.",
                        attempt + 1, _maxRetries, currentDelay.TotalMilliseconds);
                }
                catch (Exception ex) when (attempt < _maxRetries)
                {
                    exceptions.Add(ex);
                    _logger.LogWarning(ex, "Attempt {Attempt} failed. Retrying in {DelayMs}ms...", attempt + 1, currentDelay.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    break;
                }

                // Apply exponential backoff with jitter
                if (attempt < _maxRetries)
                {
                    var jitter = TimeSpan.FromMilliseconds(_random.Next(0, 100));
                    await Task.Delay(currentDelay + jitter);
                    currentDelay = TimeSpan.FromTicks((long)(currentDelay.Ticks * 2.0)); // Double the delay
                }
            }

            return exceptions;
        }

        /// <summary>
        /// Gets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries => _maxRetries;
    }

    /// <summary>
    /// Dead-letter sink for events that fail after all retry attempts are exhausted.
    /// </summary>
    private sealed class FailedEventSink
    {
        private readonly List<FailedEvent> _failedEvents = new();
        private readonly object _lock = new();

        /// <summary>
        /// Records a failed event with associated exceptions.
        /// </summary>
        /// <param name="event">The failed event.</param>
        /// <param name="exceptions">List of exceptions that caused the failure.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RecordFailedEventAsync(DomainEvent @event, List<Exception> exceptions)
        {
            ArgumentNullException.ThrowIfNull(@event);
            ArgumentNullException.ThrowIfNull(exceptions);

            var failedEvent = new FailedEvent
            {
                EventId = @event.Id,
                EventType = @event.GetType().FullName ?? @event.GetType().Name,
                EventData = @event,
                FailedAt = DateTime.UtcNow,
                FailureCount = exceptions.Count,
                Exceptions = exceptions.Select(ex => new ExceptionInfo
                {
                    Type = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                }).ToList()
            };

            lock (_lock)
            {
                _failedEvents.Add(failedEvent);
            }

            // Simulate async operation
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets all failed events.
        /// </summary>
        /// <returns>List of failed events.</returns>
        public List<FailedEvent> GetFailedEvents()
        {
            lock (_lock)
            {
                return new List<FailedEvent>(_failedEvents);
            }
        }

        /// <summary>
        /// Clears all recorded failed events.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _failedEvents.Clear();
            }
        }

        /// <summary>
        /// Failed event record containing event data and failure information.
        /// </summary>
        public sealed class FailedEvent
        {
            public Guid EventId { get; set; }
            public string EventType { get; set; } = string.Empty;
            public DomainEvent EventData { get; set; } = null!;
            public DateTime FailedAt { get; set; }
            public int FailureCount { get; set; }
            public List<ExceptionInfo> Exceptions { get; set; } = new();
        }

        /// <summary>
        /// Exception information for dead-letter events.
        /// </summary>
        public sealed class ExceptionInfo
        {
            public string Type { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string StackTrace { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}