# EventBusExtensions

The `EventBusExtensions` class provides a set of static utility methods for managing a type-based event bus within the `dotnet-config-server` ecosystem. It facilitates the subscription, publication, and inspection of events using generic type parameters to distinguish event channels, enabling loose coupling between configuration components and event handlers while maintaining type safety.

## API

### `Subscribe<T, TNext>`
Registers a subscriber delegate for a specific event type. This method establishes a link between the event type `T` and the handling logic defined in `TNext`.
*   **Parameters**: Implicitly relies on generic type arguments `T` (the event type) and `TNext` (typically the handler type or delegate signature) to route the subscription.
*   **Return Value**: `void`.
*   **Exceptions**: May throw if the internal subscription registry is in an invalid state or if duplicate registration rules are violated by the underlying implementation.

### `GetSubscriberCount<T>`
Retrieves the current number of active subscribers registered for a specific event type.
*   **Parameters**: Generic type `T` representing the event type to query.
*   **Return Value**: `int` indicating the count of subscribers. Returns `0` if no subscribers exist.
*   **Exceptions**: Generally does not throw unless the internal dictionary is corrupted.

### `HasSubscribers<T>`
Determines whether any subscribers are currently registered for a specific event type.
*   **Parameters**: Generic type `T` representing the event type to check.
*   **Return Value**: `bool` returning `true` if at least one subscriber exists, otherwise `false`.
*   **Exceptions**: None expected under normal operation.

### `PublishAsyncWithCount<T>`
Asynchronously publishes an event instance to all registered subscribers for type `T` and returns the number of subscribers that were notified.
*   **Parameters**: Generic type `T` representing the event type; accepts an event instance (implied by usage patterns of similar buses) to dispatch.
*   **Return Value**: `Task<int>` which resolves to the count of subscribers that successfully received the event.
*   **Exceptions**: May throw `AggregateException` or propagate exceptions from individual subscriber handlers if error handling is not suppressed internally. Awaits asynchronous handlers.

### `UnsubscribeAll<T>`
Removes all registered subscribers for a specific event type, effectively clearing the channel for type `T`.
*   **Parameters**: Generic type `T` representing the event type to clear.
*   **Return Value**: `void`.
*   **Exceptions**: None expected; safe to call even if no subscribers exist.

### `GetAllSubscriberCounts`
Provides a snapshot of subscriber counts for all currently active event types in the system.
*   **Parameters**: None.
*   **Return Value**: `IReadOnlyDictionary<string, int>` where the key is the fully qualified type name of the event and the value is the subscriber count.
*   **Exceptions**: None expected.

## Usage

### Basic Subscription and Publication
The following example demonstrates subscribing to a configuration change event and publishing it asynchronously.

```csharp
using System;
using System.Threading.Tasks;

// Define an event type
public class ConfigUpdatedEvent { public string Key { get; set; } }

public class ConfigHandler
{
    public static async Task Run()
    {
        // Subscribe to the event
        EventBusExtensions.Subscribe<ConfigUpdatedEvent, Action<ConfigUpdatedEvent>>(
            (e) => Console.WriteLine($"Config updated: {e.Key}")
        );

        // Check for subscribers before publishing
        if (EventBusExtensions.HasSubscribers<ConfigUpdatedEvent>())
        {
            var eventInstance = new ConfigUpdatedEvent { Key = "DatabaseConnectionString" };
            
            // Publish and await the count of notified subscribers
            int notifiedCount = await EventBusExtensions.PublishAsyncWithCount<ConfigUpdatedEvent>(eventInstance);
            Console.WriteLine($"Notified {notifiedCount} subscribers.");
        }
    }
}
```

### Monitoring and Cleanup
This example illustrates inspecting global subscription stats and clearing specific event channels.

```csharp
using System;
using System.Collections.Generic;

public class SubscriptionMonitor
{
    public static void InspectAndCleanup()
    {
        // Get counts for all event types
        IReadOnlyDictionary<string, int> allCounts = EventBusExtensions.GetAllSubscriberCounts;
        
        foreach (var kvp in allCounts)
        {
            Console.WriteLine($"Event: {kvp.Key}, Subscribers: {kvp.Value}");
        }

        // Specific count check
        int currentCount = EventBusExtensions.GetSubscriberCount<ConfigUpdatedEvent>();
        
        if (currentCount > 0)
        {
            // Unsubscribe all listeners for this specific event type
            EventBusExtensions.UnsubscribeAll<ConfigUpdatedEvent>();
            Console.WriteLine("All ConfigUpdatedEvent subscribers removed.");
        }
    }
}
```

## Notes

*   **Thread Safety**: As the methods expose static state management for subscriptions, concurrent calls to `Subscribe`, `UnsubscribeAll`, and `PublishAsyncWithCount` should be considered potentially racy unless the underlying implementation utilizes concurrent collections. It is recommended to manage subscription lifecycle during application startup or within controlled synchronization contexts.
*   **Type Matching**: Subscription and publication rely strictly on the generic type parameter `T`. Subscribing to a base class type does not automatically capture events published as a derived type, and vice versa; the types must match exactly.
*   **Async Execution**: `PublishAsyncWithCount` awaits the execution of handlers. If a handler throws an unhandled exception, it may interrupt the publication flow or result in an aggregated exception depending on the internal iterator strategy.
*   **Memory Management**: Failure to call `UnsubscribeAll<T>` for long-lived static subscribers in scenarios where handlers hold references to disposable resources may lead to memory leaks, as the static event bus holds strong references to delegates.
