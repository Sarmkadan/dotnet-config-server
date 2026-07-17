# INotificationService

Represents a notification message that can be dispatched through the configuration server’s notification infrastructure. The type encapsulates all data required for a notification—identifier, type, message text, severity, creation timestamp, and optional metadata—and exposes two asynchronous methods to send the notification. The `NotificationService` property provides access to the underlying service instance that manages the notification lifecycle.

## API

### `Guid Id`
Gets the unique identifier of the notification. This value is typically assigned at creation time and remains immutable for the lifetime of the instance.

### `string Type`
Gets or sets the notification type (e.g., `"ConfigurationChange"`, `"Error"`). The type is used for routing and filtering by subscribers.

### `string Message`
Gets or sets the human-readable message body of the notification.

### `string Severity`
Gets or sets the severity level (e.g., `"Info"`, `"Warning"`, `"Error"`). This value influences how the notification is processed and displayed.

### `DateTime CreatedAt`
Gets the timestamp when the notification was created. The value is set to `DateTime.UtcNow` at instantiation and is read-only.

### `Dictionary<string, object> Metadata`
Gets or sets a dictionary of arbitrary key-value pairs that provide additional context for the notification. Keys are case-sensitive strings; values can be any object.

### `NotificationService NotificationService`
Gets the `NotificationService` instance associated with this notification. This property is set during construction and provides access to the service that will dispatch the notification when `NotifyAsync` is called.

### `Task NotifyAsync()`
Sends the notification using the current property values. The method uses the default cancellation token (`CancellationToken.None`).

- **Returns:** A `Task` that completes when the notification has been dispatched.
- **Throws:** `InvalidOperationException` if the `NotificationService` property is `null` or if the service is not in a valid state to send notifications.

### `Task NotifyAsync(CancellationToken cancellationToken)`
Sends the notification with support for cancellation.

- **Parameters:**
  - `cancellationToken` – A `CancellationToken` that can be used to cancel the send operation.
- **Returns:** A `Task` that completes when the notification has been dispatched or the operation is cancelled.
- **Throws:** `InvalidOperationException` if the `NotificationService` property is `null` or the service is not ready. `OperationCanceledException` if the cancellation token is triggered before the operation completes.

## Usage

### Example 1: Sending a simple notification

```csharp
var notification = new INotificationService
{
    Type = "ConfigurationChange",
    Message = "Connection string updated for database 'Orders'.",
    Severity = "Info",
    Metadata = new Dictionary<string, object>
    {
        ["Key"] = "ConnectionStrings:Orders",
        ["Environment"] = "Production"
    }
};

await notification.NotifyAsync();
```

### Example 2: Sending a notification with cancellation support

```csharp
var notification = new INotificationService
{
    Type = "Error",
    Message = "Failed to load configuration from remote source.",
    Severity = "Error",
    Metadata = new Dictionary<string, object>
    {
        ["Source"] = "RemoteProvider",
        ["Attempt"] = 3
    }
};

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await notification.NotifyAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Notification send was cancelled.");
}
```

## Notes

- **Thread safety:** The `Id` and `CreatedAt` properties are read-only and safe to access from multiple threads. The mutable properties (`Type`, `Message`, `Severity`, `Metadata`) are not synchronized; concurrent writes may produce inconsistent state. It is recommended to configure all properties before calling `NotifyAsync` and to avoid modifying them after the notification has been dispatched.
- **Null values:** Setting `Type`, `Message`, or `Severity` to `null` may cause the `NotifyAsync` methods to throw an `InvalidOperationException` if the underlying service requires non-null values. The `Metadata` dictionary can contain `null` values, but keys must not be `null`.
- **Cancellation:** The `NotifyAsync(CancellationToken)` overload does not guarantee immediate cancellation; it depends on the implementation of the `NotificationService`. The operation may still complete after the token is cancelled if the service does not honour cancellation.
- **Service dependency:** The `NotificationService` property must be set to a valid instance before calling any `NotifyAsync` overload. If the property is `null`, both methods will throw an `InvalidOperationException`.
