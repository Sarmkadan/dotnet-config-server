#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetConfigServer.Events;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Watches configuration changes in real time and notifies registered callbacks.
/// Subscribes to <see cref="IEventBus"/> domain events and maintains per-configuration
/// subscriber lists so consumers can react to hot-reload updates without polling.
/// </summary>
public sealed class WatchService : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<WatchService> _logger;
    private readonly ConcurrentDictionary<Guid, List<Func<DomainEvent, Task>>> _subscribers = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchService"/> class.
    /// </summary>
    /// <param name="eventBus">The event bus for subscribing to domain events.</param>
    /// <param name="configurationService">The configuration service for resolving configurations.</param>
    /// <param name="logger">The logger instance.</param>
    public WatchService(
        IEventBus eventBus,
        IConfigurationService configurationService,
        ILogger<WatchService> logger)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a callback that fires whenever the specified configuration changes.
    /// Returns a disposable token that removes the subscription when disposed.
    /// </summary>
    /// <param name="configurationId">The configuration to watch.</param>
    /// <param name="callback">The async callback invoked on change.</param>
    /// <returns>A disposable that unsubscribes the callback.</returns>
    public IDisposable Watch(Guid configurationId, Func<DomainEvent, Task> callback)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(callback);

        var list = _subscribers.GetOrAdd(configurationId, _ => new List<Func<DomainEvent, Task>>());
        lock (list)
        {
            list.Add(callback);
        }

        _logger.LogInformation("Subscriber added for configuration {ConfigurationId}. Total: {Count}",
            configurationId, list.Count);

        return new Unsubscriber(list, callback);
    }

    /// <summary>
    /// Dispatches a domain event to all subscribers watching the event's configuration.
    /// Exceptions from individual callbacks are logged but do not prevent other callbacks from executing.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    public async Task DispatchAsync(DomainEvent domainEvent)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (!_subscribers.TryGetValue(domainEvent.ConfigurationId, out var list))
            return;

        Func<DomainEvent, Task>[] snapshot;
        lock (list)
        {
            snapshot = list.ToArray();
        }

        _logger.LogDebug("Dispatching event {EventId} to {Count} subscriber(s) for configuration {ConfigurationId}",
            domainEvent.Id, snapshot.Length, domainEvent.ConfigurationId);

        foreach (var callback in snapshot)
        {
            try
            {
                await callback(domainEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscriber callback failed for event {EventId}", domainEvent.Id);
            }
        }
    }

    /// <summary>
    /// Returns the number of active subscribers for a given configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID.</param>
    /// <returns>The subscriber count.</returns>
    public int GetSubscriberCount(Guid configurationId)
    {
        if (!_subscribers.TryGetValue(configurationId, out var list))
            return 0;

        lock (list)
        {
            return list.Count;
        }
    }

    /// <summary>
    /// Returns all configuration IDs that currently have at least one subscriber.
    /// </summary>
    /// <returns>An enumerable of watched configuration IDs.</returns>
    public IEnumerable<Guid> GetWatchedConfigurations()
    {
        return _subscribers
            .Where(kv => { lock (kv.Value) { return kv.Value.Count > 0; } })
            .Select(kv => kv.Key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _subscribers.Clear();
        _logger.LogInformation("WatchService disposed, all subscriptions cleared");
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<Func<DomainEvent, Task>> _list;
        private readonly Func<DomainEvent, Task> _callback;

        public Unsubscriber(List<Func<DomainEvent, Task>> list, Func<DomainEvent, Task> callback)
        {
            _list = list;
            _callback = callback;
        }

        public void Dispose()
        {
            lock (_list)
            {
                _list.Remove(_callback);
            }
        }
    }
}
