# ConfigurationClientFactory

The `ConfigurationClientFactory` class serves as a factory for creating HTTP and typed clients to interact with a configuration server. It abstracts the instantiation and configuration of clients, including retry policies for transient fault handling, enabling seamless communication with the `dotnet-config-server` backend.

## API

### `ConfigurationClientFactory`

**Purpose**
Initializes a new instance of the `ConfigurationClientFactory` class. This constructor is typically used to prepare the factory for client creation.

---

### `HttpClient CreateClient()`

**Purpose**
Creates and returns an `HttpClient` configured with default settings, including a retry handler for transient fault tolerance.

**Returns**
An `HttpClient` instance ready for HTTP requests to the configuration server.

**Throws**
- `InvalidOperationException`: If the factory has not been properly initialized or if underlying dependencies are misconfigured.

---

### `IConfigurationServerClient CreateTypedClient()`

**Purpose**
Creates and returns a strongly-typed client implementing `IConfigurationServerClient`, which provides high-level methods for interacting with the configuration server.

**Returns**
An instance of `IConfigurationServerClient` with methods for configuration management.

**Throws**
- `InvalidOperationException`: If the factory or its dependencies are not properly initialized.

---

### `RetryHandler(HttpMessageHandler innerHandler, int maxRetries) : DelegatingHandler`

**Purpose**
A nested `DelegatingHandler` that implements retry logic for transient HTTP failures. This handler wraps an inner `HttpMessageHandler` and retries failed requests up to the specified maximum retry count.

**Parameters**
- `innerHandler`: The inner `HttpMessageHandler` to which requests are forwarded after retry logic is applied.
- `maxRetries`: The maximum number of retry attempts for transient failures.

**Returns**
A configured `DelegatingHandler` instance that can be used with `HttpClient`.

**Notes**
- Retries are applied only for transient HTTP errors (e.g., 5xx, 429).
- Exponential backoff is used between retry attempts.

---

### `ConfigurationServerClient`

**Purpose**
A concrete implementation of `IConfigurationServerClient` that provides methods for managing configurations, keys, and health checks on the configuration server.

---

### `Task<Configuration> GetConfigurationAsync()`

**Purpose**
Retrieves the full configuration for the associated application and environment.

**Returns**
A `Task<Configuration>` representing the asynchronous operation, yielding the requested `Configuration` object.

**Throws**
- `HttpRequestException`: If the request fails due to network issues or server errors.
- `InvalidOperationException`: If the response cannot be deserialized or is malformed.

---

### `Task<Configuration> CreateConfigurationAsync()`

**Purpose**
Creates a new configuration entry on the server for the associated application and environment.

**Returns**
A `Task<Configuration>` representing the asynchronous operation, yielding the created `Configuration` object.

**Throws**
- `HttpRequestException`: If the request fails due to network issues or server errors.
- `InvalidOperationException`: If the response is invalid or the configuration already exists.

---

### `Task<ConfigurationKey> AddKeyAsync(ConfigurationKey key)`

**Purpose**
Adds a new key-value pair to an existing configuration.

**Parameters**
- `key`: The `ConfigurationKey` object containing the key, value, and metadata to be added.

**Returns**
A `Task<ConfigurationKey>` representing the asynchronous operation, yielding the added `ConfigurationKey`.

**Throws**
- `HttpRequestException`: If the request fails due to network issues or server errors.
- `ArgumentNullException`: If `key` is `null`.
- `InvalidOperationException`: If the configuration does not exist or the key already exists.

---

### `Task<bool> HealthCheckAsync()`

**Purpose**
Performs a health check on the configuration server to verify connectivity and availability.

**Returns**
A `Task<bool>` representing the asynchronous operation, returning `true` if the server is healthy, `false` otherwise.

**Throws**
- `HttpRequestException`: If the request fails due to network issues.

---

### `Guid Id`

**Purpose**
Gets the unique identifier of the configuration.

---

### `Guid ApplicationId`

**Purpose**
Gets the unique identifier of the application associated with the configuration.

---

### `string Environment`

**Purpose**
Gets the environment name (e.g., "Production", "Staging") for which the configuration is defined.

---

### `string Description`

**Purpose**
Gets or sets a human-readable description of the configuration.

---

### `List<ConfigurationKey> Keys`

**Purpose**
Gets the collection of key-value pairs (`ConfigurationKey` objects) associated with the configuration.

---

### `Guid Id` (ConfigurationKey)

**Purpose**
Gets the unique identifier of the configuration key.

---

### `string Key` (ConfigurationKey)

**Purpose**
Gets the name of the configuration key.

---

### `string Value` (ConfigurationKey)

**Purpose**
Gets the value associated with the configuration key.

---

### `bool IsEncrypted` (ConfigurationKey)

**Purpose**
Indicates whether the value of the configuration key is encrypted.

---

### `string Description` (ConfigurationKey)

**Purpose**
Gets or sets a human-readable description of the configuration key.

---

### `Guid ApplicationId` (ConfigurationKey)

**Purpose**
Gets the unique identifier of the application associated with the configuration key.

## Usage

### Example 1: Retrieving Configuration Using Typed Client
