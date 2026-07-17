# ExternalApiClient

Provides a resilient HTTP client wrapper for communicating with external configuration services. It encapsulates retry logic, configurable timeouts, and typed serialization/deserialization of JSON payloads, allowing consumers to perform common HTTP operations (`GET`, `POST`, `PUT`, `DELETE`) against remote endpoints without manually handling transient failures or response parsing.

## API

### `public ExternalApiClient`

Initializes a new instance of the `ExternalApiClient` class. The constructor accepts the necessary dependencies (such as an `HttpClient` or `HttpMessageHandler`) and configuration parameters required to establish the base address and default request headers for the external service.

### `public async Task<T?> GetAsync<T>(string requestUri)`

Sends a `GET` request to the specified URI and deserializes a successful JSON response body into an instance of `T`.

- **Parameters:**
  - `requestUri` (`string`): The relative or absolute URI to send the request to.
- **Returns:** A `Task<T?>` representing the asynchronous operation. The result is the deserialized object of type `T`, or `null` if the response indicates a non-success status code or the body is empty.
- **Exceptions:** Throws an `HttpRequestException` if the request fails after all configured retries, or a `TaskCanceledException` if the timeout elapses. Throws a `JsonException` if the response body cannot be deserialized into `T`.

### `public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content)`

Sends a `POST` request with a JSON-serialized body to the specified URI and deserializes a successful JSON response body into an instance of `TResponse`.

- **Parameters:**
  - `requestUri` (`string`): The URI to send the request to.
  - `content` (`TRequest`): The object to serialize as the JSON request body.
- **Returns:** A `Task<TResponse?>` representing the asynchronous operation. The result is the deserialized response object, or `null` if the response indicates no content.
- **Exceptions:** Throws an `HttpRequestException` if the request fails after all retries. Throws a `JsonException` if serialization of the request body or deserialization of the response body fails.

### `public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content)`

Sends a `PUT` request with a JSON-serialized body to the specified URI and deserializes a successful JSON response body into an instance of `TResponse`.

- **Parameters:**
  - `requestUri` (`string`): The URI to send the request to.
  - `content` (`TRequest`): The object to serialize as the JSON request body.
- **Returns:** A `Task<TResponse?>` representing the asynchronous operation. The result is the deserialized response object, or `null` if the response indicates no content.
- **Exceptions:** Throws an `HttpRequestException` if the request fails after all retries. Throws a `JsonException` if serialization or deserialization fails.

### `public async Task DeleteAsync(string requestUri)`

Sends a `DELETE` request to the specified URI. This method does not expect a response body.

- **Parameters:**
  - `requestUri` (`string`): The URI to send the request to.
- **Returns:** A `Task` representing the asynchronous operation.
- **Exceptions:** Throws an `HttpRequestException` if the request fails after all configured retries.

### `public TimeSpan Timeout { get; set; }`

Gets or sets the overall timeout applied to each individual HTTP request attempt. The default value is typically 30 seconds. When a request exceeds this duration, it is canceled and, if retries remain, re-attempted.

### `public int MaxRetries { get; set; }`

Gets or sets the maximum number of retry attempts for failed or timed-out requests. A value of `0` means no retries are performed. Transient failures (such as 5xx status codes or `HttpRequestException`) trigger a retry.

### `public int RetryDelay { get; set; }`

Gets or sets the delay in milliseconds between retry attempts. This is a fixed delay applied before each subsequent attempt.

## Usage

### Basic GET Request with Retry Configuration

```csharp
var client = new ExternalApiClient(httpClient, baseUrl)
{
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetries = 3,
    RetryDelay = 500
};

var config = await client.GetAsync<ServerConfig>("api/v1/config/current");
if (config != null)
{
    Console.WriteLine($"Config version: {config.Version}");
}
```

### Conditional Update with POST and PUT

```csharp
var client = new ExternalApiClient(httpClient, baseUrl);

// Create a new resource
var newEntry = new ConfigEntry { Key = "feature-flag", Value = "enabled" };
var created = await client.PostAsync<ConfigEntry, ConfigEntry>("api/v1/entries", newEntry);

if (created != null)
{
    // Update the resource
    created.Value = "disabled";
    var updated = await client.PutAsync<ConfigEntry, ConfigEntry>(
        $"api/v1/entries/{created.Id}", created);
}

// Clean up
await client.DeleteAsync($"api/v1/entries/{created?.Id}");
```

## Notes

- **Thread Safety:** Instance methods are not guaranteed to be thread-safe. Concurrent calls to the same `ExternalApiClient` instance that modify properties (`Timeout`, `MaxRetries`, `RetryDelay`) while requests are in flight may lead to race conditions. It is recommended to configure the client once during initialization and treat it as read-only thereafter, or use separate instances per thread.
- **Retry Behavior:** Retries are performed only on transient failures (network errors, timeouts, and server-side 5xx responses). Client errors (4xx) are not retried and will result in an `HttpRequestException` being thrown immediately.
- **Null Returns:** `GetAsync`, `PostAsync`, and `PutAsync` return `null` when the server responds with a successful status code but an empty body, or when the response status code indicates no content (e.g., 204 No Content). Callers must perform null checks on the result.
- **Serialization:** All request and response bodies are assumed to be JSON. The client uses a default or injected `System.Text.Json` serializer. Circular references or non-serializable types will cause `JsonException` to be thrown.
- **Timeout Scope:** The `Timeout` property applies per attempt, not to the cumulative duration of all retries. A request with `Timeout = 5s` and `MaxRetries = 2` could take up to 15 seconds plus retry delays before failing definitively.
