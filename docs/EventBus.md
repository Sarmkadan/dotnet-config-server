# EventBus

The `EventBus` class provides a lightweight, in-memory publish–subscribe mechanism for decoupling event producers from event consumers. It allows components to subscribe to events of a specific type, publish events asynchronously to all registered subscribers, and manage subscriptions dynamically at runtime.

## API

### `public EventBus()`

Initializes a new instance of the `EventBus` class with no subscribers.

- **Parameters:** None.
- **Return value:** None.
- **Throws:** None.

---

### `public async Task PublishAsync<T>(T eventData)`

Publishes an event of type `T` to all currently subscribed handlers. Each handler is awaited in sequence.

- **Parameters:**
  - `eventData` – The event payload to deliver to subscribers.
- **Return value:** A `Task` that completes when all subscribers have finished processing the event.
- **Throws:**
  - `ArgumentNullException` – If `eventData` is `null`.

---

### `public void Subscribe<T>(Func<T, Task> handler)`

Registers a handler to be invoked whenever an event of type `T` is published.

- **Parameters:**
  - `handler` – An asynchronous delegate that processes the event.
- **Return value:** None.
- **Throws:**
  - `ArgumentNullException` – If `handler` is `null`.

---

### `public void Unsubscribe<T>(Func<T, Task> handler)`

Removes a previously registered handler for events of type `T`. If the handler was not subscribed, the method does nothing.

- **Parameters:**
  - `handler` – The delegate to remove from the subscription list.
- **Return value:** None.
- **Throws:**
  - `ArgumentNullException` – If `handler` is `null`.

---

### `public IEnumerable<Func<T, Task>> GetSubscribers<T>()`

Returns a snapshot of all currently registered handlers for events of type `T`.

- **Parameters:** None.
- **Return value:** An `IEnumerable<Func<T, Task>>` containing the subscribed handlers. If no handlers are registered, the sequence is empty.
- **Throws:** None.

---

### `public void Clear()`

Removes all subscriptions for every event type.

- **Parameters:** None.
- **Return value:** None.
- **Throws:** None.

## Usage

### Example 1: Basic publish–subscribe

```csharp
using System;
using System.Threading.Tasks;

public class OrderPlaced
{
    public int OrderId { get; set; }
}

public class OrderService
{
    private readonly EventBus _eventBus;

    public OrderService(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PlaceOrderAsync(int orderId)
    {
        // Business logic...
        await _eventBus.PublishAsync(new OrderPlaced { OrderId = orderId });
    }
}

public class NotificationService
{
    public NotificationService(EventBus eventBus)
    {
        eventBus.Subscribe<OrderPlaced>(async e =>
        {
            Console.WriteLine($"Sending notification for order {e.OrderId}");
            await Task.CompletedTask;
        });
    }
}

// Usage
var bus = new EventBus();
var notification = new NotificationService(bus);
var orderService = new OrderService(bus);

await orderService.PlaceOrderAsync(42);
// Output: Sending notification for order 42
```

### Example 2: Multiple subscribers and unsubscription

```csharp
using System;
using System.Threading.Tasks;

public class TemperatureReading
{
    public double Celsius { get; set; }
}

public class Logger
{
    public async Task LogTemperature(TemperatureReading reading)
    {
        Console.WriteLine($"Log: {reading.Celsius}°C");
        await Task.CompletedTask;
    }
}

public class AlertSystem
{
    public async Task CheckForHighTemperature(TemperatureReading reading)
    {
        if (reading.Celsius > 40)
        {
            Console.WriteLine("Alert: High temperature detected!");
        }
        await Task.CompletedTask;
    }
}

// Usage
var bus = new EventBus();
var logger = new Logger();
var alert = new AlertSystem();

Func<TemperatureReading, Task> logHandler = logger.LogTemperature;
Func<TemperatureReading, Task> alertHandler = alert.CheckForHighTemperature;

bus.Subscribe(logHandler);
bus.Subscribe(alertHandler);

await bus.PublishAsync(new TemperatureReading { Celsius = 38.5 });
// Output:
// Log: 38.5°C
// (no alert)

await bus.PublishAsync(new TemperatureReading { Celsius = 42.0 });
// Output:
// Log: 42°C
// Alert: High temperature detected!

// Unsubscribe the alert handler
bus.Unsubscribe(alertHandler);

await bus.PublishAsync(new TemperatureReading { Celsius = 45.0 });
// Output:
// Log: 45°C
// (no alert)
```

## Notes

- **Thread safety:** The `EventBus` is not thread-safe. Concurrent calls to `Subscribe`, `Unsubscribe`, `Clear`, or `PublishAsync` from multiple threads may lead to race conditions or undefined behavior. External synchronization (e.g., a lock) is required when the bus is accessed from multiple threads.
- **Handler execution order:** Subscribers are invoked in the order they were registered. The `PublishAsync` method awaits each handler before invoking the next, so a long-running or blocking handler will delay subsequent handlers.
- **Null handlers:** All methods that accept a handler delegate throw `ArgumentNullException` if the argument is `null`. The `PublishAsync` method also throws if `eventData` is `null`.
- **Unsubscribing a non‑existent handler:** Calling `Unsubscribe` with a handler that was never subscribed (or has already been removed) is a no‑op and does not throw.
- **Empty subscriptions:** Publishing an event type that has no subscribers completes immediately without error.
- **Reference equality for unsubscription:** The `Unsubscribe` method uses reference equality to identify the handler to remove. If the same logical handler is represented by different delegate instances, each must be unsubscribed separately.
- **Clearing subscriptions:** `Clear` removes all subscriptions for all event types. After calling `Clear`, the bus behaves as if newly created.
