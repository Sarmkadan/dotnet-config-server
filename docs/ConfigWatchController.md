# ConfigWatchController

The `ConfigWatchController` lets clients watch an application's configuration for changes instead of polling. It offers two transports: long-polling (`GET .../watch`) and Server-Sent Events (`GET .../watch/sse`). A monitoring endpoint (`GET .../watch/status`) lists the watch requests that are currently open. Changes are detected through the in-process event bus: when a `ConfigurationVersionCreatedEvent` is published for the watched application, the waiting requests are completed with the new active version.

Routes are scoped to an application and environment:

```
api/v1/config/{appId:guid}/{env}
```

- `appId`: The GUID of the application to watch.
- `env`: The environment name (`Development`, `Staging`, `Production`).

## API

### `ConfigWatchController`
The controller class is `[ApiController]`, produces `application/json`, and is sealed. Its dependencies are `IEventBus`, `IVersioningService`, `IConfigurationService` and `ILogger<ConfigWatchController>`.

The controller keeps a process-wide `ConcurrentDictionary` of open watch requests, which backs the status endpoint. The state is in memory, so it is not shared between server instances.

### `public async Task<IActionResult> WatchConfigChanges([FromRoute] Guid appId, [FromRoute] Env env, [FromQuery] string? sinceVersion = null, [FromQuery] int? timeout = null)`
Long-polls for the next configuration change of the application. The request is held open until a new version is created or the timeout expires.

**Route**: `GET api/v1/config/{appId}/{env}/watch`

**Parameters**
- `appId`: The application ID to watch (route).
- `env`: The environment to watch (route). Values that are not defined in the `Environment` enum are rejected with `400 BadRequest`.
- `sinceVersion`: The version number the client already has (query, optional). If the active version's major number is greater than this value, the method returns immediately without waiting.
- `timeout`: Wait time in seconds (query, optional). Missing or `<= 0` values use the default of 30 seconds; values `>= 60` are capped at 60 seconds.

**Return value**
- `200 OK` with a `WatchResponse` when a change is detected, or when `sinceVersion` is already behind the active version (`IsImmediateResponse = true`).
- `204 NoContent` when the timeout expires without a change, or when the request is cancelled by the client.
- `400 BadRequest` with `{ "error": "Invalid environment specified" }` if `env` is not a defined environment.
- `404 NotFound` with `{ "error": "No active configuration version found for this application" }` if the application has no active version.
- `500 InternalServerError` with `{ "error": "Internal server error" }` on unexpected failures.

**Exceptions**
- Throws `ArgumentNullException` when `sinceVersion` is not supplied (see Notes).

---

### `public async Task<IActionResult> WatchConfigChangesSse([FromRoute] Guid appId, [FromRoute] Env env, [FromQuery] string? sinceVersion = null)`
Streams configuration changes of the application as Server-Sent Events.

**Route**: `GET api/v1/config/{appId}/{env}/watch/sse`

**Parameters**
- `appId`: The application ID to watch (route).
- `env`: The environment to watch (route). Undefined values are rejected with `400 BadRequest`.
- `sinceVersion`: The version number the client already has (query, optional). Works the same way as in the long-polling endpoint: if the active version is newer, a single `WatchResponse` JSON body is returned immediately instead of opening a stream.

**Return value**
- `200 OK` with a `WatchResponse` JSON body when the client is already behind the active version.
- `200 OK` with `Content-Type: text/event-stream` when the stream is opened. Each change is sent as a `data:` frame (see the SSE format below). The response ends when the server-side wait ends or the client disconnects.
- `400 BadRequest` if `env` is not a defined environment.
- `404 NotFound` if the application has no active version.
- `500 InternalServerError` on unexpected failures.

**Exceptions**
- Throws `ArgumentNullException` when `sinceVersion` is not supplied (see Notes).

**SSE frame format**

Each change is written as one event:

```
data: {"eventType":"config.changed","data":{...WatchResponse...},"id":"<request-id>","retry":10000}

```

The server writes a comment line `: heartbeat` every 5 seconds to keep the connection alive. A stream is closed by the server after 60 seconds, so clients should reconnect and pass the last known version in `sinceVersion`. Errors while handling an event are sent as an `event: error` frame with `{ "error": "<message>" }`.

---

### `public IActionResult GetWatchStatus()`
Lists the watch requests that are currently open in this process. Intended for monitoring.

**Route**: `GET api/v1/config/{appId}/{env}/watch/status`

**Return value**
- `200 OK` with a dictionary keyed by request ID, where each value is a `WatchRequestStatus`.

The route contains `{appId}` and `{env}`, but the method does not filter on them. It returns every open watch request in the process.

---

### `WatchResponse`
The payload returned by both watch endpoints. It is defined in `Controllers/ConfigWatchController.cs`, alongside the controller.

```json
{
  "version": "3.0",
  "changedAt": "2026-10-25T10:15:30Z",
  "configurations": [
    {
      "id": "7d1c0b2e-6f4a-4f5e-9d0a-3b2c1e4f5a6b",
      "application": "MyApp",
      "key": "ApiEndpoint",
      "value": "https://api.example.com"
    }
  ],
  "isImmediateResponse": false
}
```

- `version`: The active version number reported by the versioning service.
- `changedAt`: UTC time when the response was built.
- `configurations`: The current configuration entries for the application (`Configuration` model). May be `null`.
- `isImmediateResponse`: `true` when the response was returned without waiting, `false` when it was produced by a change event.

### `WatchRequestStatus`
An entry in the `watch/status` response.

```json
{
  "applicationId": "7d1c0b2e-6f4a-4f5e-9d0a-3b2c1e4f5a6b",
  "environment": "Production",
  "sinceVersion": "2",
  "startedAt": "2026-10-25T10:14:00Z",
  "duration": "00:00:30",
  "isSse": false
}
```

- `duration`: Time since the request started, serialized as a `TimeSpan`.

---

## Usage

```csharp
// Example 1: Long-poll for the next change, starting from version 2
var appId = Guid.Parse("7d1c0b2e-6f4a-4f5e-9d0a-3b2c1e4f5a6b");

var response = await client.GetAsync(
    $"/api/v1/config/{appId}/Production/watch?sinceVersion=2&timeout=45");

if (response.StatusCode == HttpStatusCode.OK)
{
    var change = await response.Content.ReadFromJsonAsync<WatchResponse>();
    // Apply change.Configurations, then continue from change.Version
}
else if (response.StatusCode == HttpStatusCode.NoContent)
{
    // No change within the timeout - poll again
}
```

```csharp
// Example 2: Consume the SSE stream and reconnect when it closes
using var stream = await client.GetStreamAsync(
    $"/api/v1/config/{appId}/Production/watch/sse?sinceVersion=2");
using var reader = new StreamReader(stream);

while (await reader.ReadLineAsync() is { } line)
{
    if (line.StartsWith("data: ", StringComparison.Ordinal))
    {
        var envelope = JsonSerializer.Deserialize<JsonElement>(line["data: ".Length..]);
        var change = envelope.GetProperty("data").Deserialize<WatchResponse>();
        // Apply change; remember change.Version for the next reconnect
    }
}
// Stream ended after the server-side 60 s window - reconnect with the last version
```

## Notes

- `sinceVersion` is declared as optional, but both watch endpoints call `ArgumentNullException.ThrowIfNull(sinceVersion)` before their `try` block. A request without `sinceVersion` therefore fails with an unhandled `ArgumentNullException` instead of waiting for a change. Clients should always send `sinceVersion` until this is fixed.
- Only the leading segment of `VersionNumber` (the part before the first `.`) is compared against `sinceVersion`, and it must parse as an integer. A change is reported only when that number is strictly greater than `sinceVersion`.
- Timeouts are enforced by the server: `timeout` is clamped to the range 1–60 seconds (missing or non-positive values use 30 seconds). Long-poll requests that time out return `204 NoContent`, not an error.
- Change notifications come from the in-process `IEventBus`. A change published by another server instance does not wake requests waiting in this process.
- The controller does not declare any authorization attribute. Access control for these routes depends on the global middleware and pipeline configuration in `Program.cs`.
- The status endpoint reports requests from this process only and is not filtered by the `appId` and `env` in the route.
- All methods are thread-safe and may be called concurrently. The watch-request registry is a static `ConcurrentDictionary`, so entries are added and removed concurrently without extra locking.
