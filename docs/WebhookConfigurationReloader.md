# WebhookConfigurationReloader

The `WebhookConfigurationReloader` type is responsible for receiving webhook notifications that signal configuration changes, updating the internal configuration state, and providing access to the latest configuration values. It is typically registered as a hosted service in an ASP.NET Core application and integrates with the configuration pipeline to allow runtime reloads without restarting the process.

## API

### WebhookConfigurationReloader (constructor)

**Purpose**  
Creates a new instance of the `WebhookConfigurationReloader`. The constructor is intended to be used by the dependency injection container; any required dependencies are supplied automatically when the service is resolved.

**Return value**  
A new `WebhookConfigurationReloader` object.

**Exceptions**  
None are thrown by the constructor itself; exceptions may arise during dependency resolution if required services are not registered.

### HandleWebhookAsync

**Purpose**  
Processes an incoming webhook payload that indicates a configuration update. The method updates the internal state (such as `Timestamp`, `Changes`, and related properties) based on the information received.

**Return value**  
A `Task` that completes when the webhook has been processed.

**Exceptions**  
- `OperationCanceledException` if a cancellation token triggers cancellation.  
- `InvalidOperationException` if the webhook payload is malformed or required fields are missing.  
- Any exception thrown by underlying services (e.g., logging or configuration providers) is propagated.

### GetConfigurationValue

**Purpose**  
Retrieves the current value for a configuration key that has been loaded or updated via webhook handling.

**Return value**  
The string value associated with the requested key, or `null` if the key is not present.

**Exceptions**  
- `ArgumentNullException` if the supplied key is `null`.  
- `ObjectDisposedException` if the reloader has been disposed.

### SetInitialConfiguration

**Purpose**  
Populates the reloader with an initial set of configuration values before any webhook events are processed. This is typically called during application startup.

**Return value**  
None (void).

**Exceptions**  
- `ArgumentException` if the provided configuration contains duplicate keys.  
- `InvalidOperationException` if called after the reloader has already started processing webhooks.

### EventId

**Purpose**  
Gets the unique identifier of the most recent webhook event that was processed.

**Return value**  
A string representing the event ID; `null` if no event has been processed yet.

### EventType

**Purpose**  
Gets the type of the most recent webhook event (e.g., `"ConfigurationUpdated"`).

**Return value**  
A string describing the event type; `null` if no event has been processed.

### ConfigurationId

**Purpose**  
Gets the identifier of the configuration to which the latest event pertains.

**Return value**  
A string configuration ID; `null` if unavailable.

### ApplicationId

**Purpose**  
Gets the identifier of the application that originated the webhook.

**Return value**  
A string application ID; `null` if not provided.

### Timestamp

**Purpose**  
Gets the date and time when the latest webhook event was processed.

**Return value**  
A `DateTime` value; defaults to `DateTime.MinValue` if no event has been handled.

### Changes

**Purpose**  
Gets a list of `ConfigurationChange` objects describing the individual configuration modifications included in the latest webhook event.

**Return value**  
A read‑only list of `ConfigurationChange` instances; empty if no changes were reported.

### Key

**Purpose**  
Gets the configuration key associated with the most recent individual change (when accessing a specific `ConfigurationChange`).

**Return value**  
The key string; `null` if not applicable.

### OldValue

**Purpose**  
Gets the previous value of the configuration key before the change was applied.

**Return value**  
The old value as a string; `null` if the key did not previously exist.

### NewValue

**Purpose**  
Gets the new value of the configuration key after the change was applied.

**Return value**  
The new value as a string; `null` if the key was removed.

### ConfigureServices

**Purpose**  
Registers the `WebhookConfigurationReloader` and its dependencies with the supplied `IServiceCollection`. This method is called by the `AddWebhookConfigurationReloader` extension.

**Return value**  
None (void).

**Exceptions**  
- `ArgumentNullException` if the `services` parameter is `null`.  
- Any exception thrown by registered services during registration.

### Configure

**Purpose**  
Configures the ASP.NET Core application pipeline to listen for incoming webhook requests and route them to `HandleWebhookAsync`.

**Return value**  
None (void).

**Exceptions**  
- `ArgumentNullException` if the `app` parameter is `null`.  
- Exceptions thrown by middleware components during pipeline construction.

### AddWebhookConfigurationReloader (static)

**Purpose**  
Extension method that adds the `WebhookConfigurationReloader` as a hosted service and registers necessary supporting services.

**Parameters**  
- `services`: The `IServiceCollection` to which services are added.

**Return value**  
The same `IServiceCollection` instance to allow chaining.

**Exceptions**  
- `ArgumentNullException` if `services` is `null`.

## Usage

### Registering the reloader in an ASP.NET Core application

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetConfigServer.Webhooks; // namespace containing WebhookConfigurationReloader

var builder = WebApplication.CreateBuilder(args);

// Add the webhook configuration reloader as a hosted service
builder.Services.AddWebhookConfigurationReloader();

var app = builder.Build();

// Configure the middleware to listen for webhook POST requests
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/webhook/config") &&
        HttpMethods.IsPost(context.Request.Method))
    {
        var reloader = context.RequestServices.GetRequiredService<WebhookConfigurationReloader>();
        await reloader.HandleWebhookAsync(context.Request);
        return;
    }

    await next();
});

app.Run();
```

### Retrieving a configuration value after a webhook update

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetConfigServer.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebhookConfigurationReloader();

var app = builder.Build();

// Assume the webhook middleware is already configured elsewhere

var reloader = app.Services.GetRequiredService<WebhookConfigurationReloader>();

// Somewhere in your application logic (e.g., a controller endpoint)
string GetFeatureFlag(string flagName)
{
    // Returns the current value; null if the flag is not defined
    return reloader.GetConfigurationValue(flagName);
}

// Example usage
bool isEnabled = GetFeatureFlag("NewFeature") == "true";
```

## Notes

- The `WebhookConfigurationReloader` is **not** thread‑safe for concurrent calls to `HandleWebhookAsync`. If multiple webhook requests may arrive simultaneously, external synchronization (e.g., locking or processing requests sequentially) should be applied.
- Property values such as `EventId`, `EventType`, `Timestamp`, and `Changes` are updated only inside `HandleWebhookAsync`. Reading these properties outside of that method may yield stale data.
- `GetConfigurationValue` returns the **latest** known value for a key; it does not block waiting for a future webhook. Callers must handle the possibility of a `null` return if the key has never been set.
- After `SetInitialConfiguration` is invoked, any subsequent webhook event will **merge** or **replace** the existing configuration according to the implementation’s merge strategy; the exact behavior is internal to the class.
- The `Changes` collection returned by the `Changes` property reflects the modifications from the **most recent** webhook only; historical changes are not retained.
- Instances created via the DI container are scoped as singletons by the `AddWebhookConfigurationReloader` extension; disposing the application’s service provider will also dispose the reloader.
- Exceptions thrown from `HandleWebhookAsync` are not caught by the middleware automatically; it is advisable to wrap the call in a try/catch block and return appropriate HTTP status codes (e.g., 400 for bad payload, 500 for internal errors).
