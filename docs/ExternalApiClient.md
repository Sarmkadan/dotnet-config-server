# ExternalApiClient

`ExternalApiClient` is a thin HTTP client wrapper for calling external JSON APIs. It sends `GET`, `POST`, `PUT` and `DELETE` requests through an injected `HttpClient`, retries failed connection-level attempts with exponential backoff, serializes request bodies and deserializes response bodies with `System.Text.Json`, and logs each request through `ILogger<ExternalApiClient>`.

The class is defined in `Integration/ExternalApiClient.cs` in namespace `DotnetConfigServer.Integration`, together with its settings class `ExternalApiClientOptions`. It is registered in `Program.cs` as a typed `HttpClient` (`AddHttpClient<ExternalApiClient>()`), and `ExternalApiClientOptions` is registered as a singleton with default values. Inject `ExternalApiClient` where it is needed.

## API

### Constructor

#### `public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger, ExternalApiClientOptions options)`

Creates a client around an existing `HttpClient`. The constructor validates `options` and then sets `httpClient.Timeout` to `options.Timeout`. This overwrites any timeout configured on the `HttpClient` earlier, including the 30-second value set in `Program.cs`.

*   **Parameters**:
    *   `httpClient` - the `HttpClient` used for all requests. The client does not dispose it.
    *   `logger` - logger used for debug, information, warning and error messages.
    *   `options` - retry and timeout settings. See `ExternalApiClientOptions`.
*   **Exceptions**: `ArgumentNullException` if any argument is `null`. `ArgumentOutOfRangeException` if `options` fails `Validate()`.

### Methods

#### `public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)`

Sends a `GET` request to `url` and deserializes the JSON response body into `T`.

*   **Parameters**:
    *   `url` - the absolute URL, or a URL relative to the `HttpClient.BaseAddress`. Must not be `null`, empty or whitespace.
    *   `headers` - optional request headers, added to every attempt.
    *   `cancellationToken` - cancels the request, the retry delays and the body read.
*   **Return Value**: A `Task<T?>` whose result is the deserialized body. The method does not check for empty bodies or `204 No Content`; the JSON reader decides the result (an empty body generally throws `JsonException`).
*   **Exceptions**: `ArgumentException` if `url` is `null`, empty or whitespace. `HttpRequestException` if the final attempt fails or the response has a non-success status code. `JsonException` if the body cannot be deserialized into `T`. `TaskCanceledException` if the per-attempt timeout elapses or the caller cancels.

#### `public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)`

Sends a `POST` request with `data` serialized as JSON and deserializes the JSON response body into `TResponse`.

*   **Parameters**:
    *   `url` - as in `GetAsync`.
    *   `data` - the object serialized as the request body. A new `JsonContent` is created for each attempt.
    *   `headers` - optional request headers.
    *   `cancellationToken` - as in `GetAsync`.
*   **Return Value**: A `Task<TResponse?>` whose result is the deserialized response body. Same empty-body behavior as `GetAsync`.
*   **Exceptions**: The same as `GetAsync`. `JsonException` is also thrown if `data` cannot be serialized.

#### `public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)`

Sends a `PUT` request with `data` serialized as JSON and deserializes the JSON response body into `TResponse`. Parameters, return value and exceptions are the same as for `PostAsync`.

#### `public async Task DeleteAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)`

Sends a `DELETE` request to `url`. The response body is not read.

*   **Parameters**: As in `GetAsync`.
*   **Return Value**: A `Task` that completes when the request succeeds.
*   **Exceptions**: `ArgumentException` if `url` is `null`, empty or whitespace. `HttpRequestException` if the final attempt fails or the response has a non-success status code. `TaskCanceledException` if the per-attempt timeout elapses or the caller cancels.

#### `public override string ToString()`

Returns a summary of the current settings, for example `ExternalApiClient { Timeout = 00:00:30, MaxRetries = 3, RetryDelay = 1000 }`.

### ExternalApiClientOptions

#### `public sealed class ExternalApiClientOptions`

Settings for `ExternalApiClient`. All properties have public setters.

| Property | Type | Default | Description |
|---|---|---|---|
| `Timeout` | `TimeSpan` | `30 seconds` | Timeout for each individual HTTP attempt. Read once, when the `ExternalApiClient` is constructed. |
| `MaxRetries` | `int` | `3` | Maximum number of attempts in total, including the first one. Must be at least `1`. |
| `RetryDelay` | `int` | `1000` | Base delay in milliseconds before a retry. Must not be negative. Read before each retry. |

#### `public void Validate()`

Checks the settings and throws `ArgumentOutOfRangeException` if any value is out of range:

*   `MaxRetries` must be at least `1`.
*   `RetryDelay` must not be negative.
*   `Timeout` must be greater than zero.

`ExternalApiClient`'s constructor calls `Validate()`. Changes made to an options object after construction are not validated.

## Usage

### Injecting the client and calling a GET endpoint

```csharp
using DotnetConfigServer.Integration;

public class RemoteConfigSource
{
    private readonly ExternalApiClient _client;

    public RemoteConfigSource(ExternalApiClient client) => _client = client;

    public async Task<ServerConfig?> LoadAsync(string apiKey, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Api-Key"] = apiKey
        };

        return await _client.GetAsync<ServerConfig>(
            "https://config.example.com/api/v1/config/current",
            headers,
            cancellationToken);
    }
}
```

### Creating a client manually with custom settings

```csharp
var options = new ExternalApiClientOptions
{
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetries = 4,
    RetryDelay = 500
};

using var httpClient = new HttpClient();
var client = new ExternalApiClient(httpClient, logger, options);

// Attempts: up to 4, delays before retries: 500 ms, 1000 ms, 2000 ms
var created = await client.PostAsync<NewEntry, EntryDto>(
    "https://config.example.com/api/v1/entries",
    new NewEntry { Key = "feature-flag", Value = "enabled" });

await client.DeleteAsync("https://config.example.com/api/v1/entries/42");
```

## Notes

*   **Retry Scope**: A retry happens only when the `HttpClient` call throws `HttpRequestException`, which covers connection-level failures such as refused connections and DNS errors. The following are not retried:
    *   HTTP error status codes (4xx and 5xx). The status code is checked after the retry loop, so the `HttpRequestException` it raises is thrown immediately.
    *   Per-attempt timeouts. These surface as `TaskCanceledException`, not `HttpRequestException`.
    *   Caller cancellation.
*   **Attempt Count**: `MaxRetries` is the total number of attempts, not the number of retries. With `MaxRetries = 1` the request is sent once and no retry happens.
*   **Backoff**: The delay before retry *n* (starting at 1) is `RetryDelay * 2^(n-1)`, so it doubles after each failed attempt. With `RetryDelay = 1000` the delays are 1 s, 2 s, 4 s, and so on.
*   **Timeout Scope**: `Timeout` is applied by `HttpClient` to each `SendAsync` call, so it limits one attempt, not the total time across retries. With `Timeout = 5s` and `MaxRetries = 3`, a call can take up to about 15 seconds plus the retry delays before it fails.
*   **Request Rebuilding**: `HttpRequestMessage` cannot be sent twice, so a new request, its headers and its JSON content are built for every attempt.
*   **Headers**: Headers are added with `TryAddWithoutValidation`, so invalid header names or values are not rejected by the client.
*   **Shared Options**: `ExternalApiClientOptions` is a mutable reference type, and `Program.cs` registers one instance as a singleton. `MaxRetries` and `RetryDelay` are read on each retry, so changing them affects every in-flight and future call that uses that instance. `Timeout` is only applied in the constructor, so changing it later has no effect on existing clients. Treat the options as read-only after startup.
*   **Thread Safety**: `ExternalApiClient` has no mutable state of its own, and `HttpClient` is safe to use from several threads, so one client can serve concurrent requests. Concurrent changes to the shared options object are not synchronized.
*   **Disposal**: `ExternalApiClient` does not implement `IDisposable`. The `HttpClient` it wraps is owned by the caller or by the `IHttpClientFactory` when the client is registered with `AddHttpClient<T>()`.
*   **Logging**: Each call logs a debug message when it starts, an information message with the status code and elapsed time on success, a warning for each retry (with the attempt number, delay and error message), and an error with the exception when the call fails. The request URL is included in every message.
*   **Serialization**: Request and response bodies are JSON, handled by `System.Text.Json` through `JsonContent.Create` and `ReadFromJsonAsync`. Circular references and non-serializable types cause `JsonException`.
