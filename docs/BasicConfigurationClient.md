# BasicConfigurationClient

`BasicConfigurationClient` is a minimal HTTP client for reading configurations from dotnet-config-server. It wraps three endpoints (`GET /api/v1/configurations/{id}`, `GET /api/v1/configurations?applicationId=...` and `GET /health`) and deserializes the responses into the `Configuration`, `ConfigurationKey` and `PagedResult<T>` models.

The class is defined in `examples/BasicConfigurationClient.cs` in namespace `DotnetConfigServer.Examples`. The main project excludes `examples/**/*.cs` from compilation (`<Compile Remove="examples/**/*.cs" />` in `dotnet-config-server.csproj`), so the file is not part of the library build. Copy it into your own project to use it.

## API

### Constructor

#### `public BasicConfigurationClient(string baseUrl)`
Creates a client bound to the server's base address. Internally creates a new `HttpClient` with `BaseAddress = new Uri(baseUrl)`.
*   **Parameters**: `baseUrl` - absolute URI of the server, for example `https://localhost:5001`.
*   **Exceptions**: `ArgumentNullException` if `baseUrl` is `null`; `ArgumentException` if it is empty; `UriFormatException` if it is not a valid absolute URI.

### Methods

#### `public async Task<Configuration> GetConfigurationAsync(string configurationId)`
Sends `GET /api/v1/configurations/{configurationId}` and returns the full configuration, including all keys.
*   **Parameters**: `configurationId` - identifier of the configuration (a GUID string). Must not be `null` or empty.
*   **Return Value**: A `Task` resulting in a `Configuration`.
*   **Exceptions**: `ArgumentNullException` / `ArgumentException` for invalid input; `HttpRequestException` on a network failure or a non-success status code; `JsonException` if the body cannot be deserialized.

#### `public async Task<string> GetConfigurationKeyAsync(string configurationId, string keyName)`
Loads the configuration with `GetConfigurationAsync` and returns the value of the key whose `Key` equals `keyName` (ordinal, case-sensitive comparison). Every call performs a full fetch; nothing is cached.
*   **Parameters**: `configurationId` - as above. `keyName` - name of the key, for example `Database:Host`. Must not be `null` or empty.
*   **Return Value**: A `Task` resulting in the key's `Value` string, returned as the server sent it.
*   **Exceptions**: `KeyNotFoundException` if no key has that name, or if the key's `Value` is `null`. Also throws the exceptions of `GetConfigurationAsync`.

#### `public async Task<IEnumerable<Configuration>> GetApplicationConfigurationsAsync(string applicationId)`
Sends `GET /api/v1/configurations?applicationId={applicationId}`, deserializes the body as `PagedResult<Configuration>` (case-insensitive property matching) and returns its `Items`.
*   **Parameters**: `applicationId` - identifier of the application (a GUID string). Must not be `null` or empty.
*   **Return Value**: A `Task` resulting in an `IEnumerable<Configuration>` (the `Items` list).
*   **Paging**: No `pageNumber` or `pageSize` is sent, so the server's default page is returned. `TotalCount`, `PageSize` and `PageNumber` are discarded; only `Items` is returned.
*   **Exceptions**: `ArgumentNullException` / `ArgumentException` for invalid input; `HttpRequestException`; `JsonException`; `NullReferenceException` if the response body deserializes to `null`.

#### `public async Task<bool> IsHealthyAsync()`
Sends `GET /health`.
*   **Return Value**: A `Task` resulting in `true` if the response has a 2xx status code, otherwise `false`.
*   **Exceptions**: None. Any exception raised while sending the request (connection, DNS, TLS, timeout) is caught and reported as `false`.

### Properties

The class has no public properties. Its only state is two private `readonly` fields, `_httpClient` and `_baseUrl`.

## Models

These types are defined in the same file, in namespace `DotnetConfigServer.Examples`.

### `Configuration`
| Member | Type | Description |
|---|---|---|
| `Id` | `Guid` | Configuration identifier. |
| `ApplicationId` | `Guid` | Owning application identifier. |
| `Environment` | `string` | Environment name, for example `Development` or `Production`. |
| `Description` | `string` | Free-text description. |
| `Keys` | `List<ConfigurationKey>` | Keys of the configuration. Initialized to an empty list. |
| `CreatedAt` | `DateTime` | Creation timestamp. |
| `LastModifiedAt` | `DateTime` | Last modification timestamp. |

Methods: `string GetKeyValue(string keyName)` returns the `Value` of the first key with the given name, or throws `KeyNotFoundException`. `ToString()` returns a summary of the fields.

### `ConfigurationKey`
| Member | Type | Description |
|---|---|---|
| `Id` | `Guid` | Key entry identifier. |
| `Key` | `string` | Key name. |
| `Value` | `string` | Stored value. |
| `IsEncrypted` | `bool` | Whether the value is stored encrypted. |
| `Description` | `string` | Free-text description. |

### `PagedResult<T>`
| Member | Type | Description |
|---|---|---|
| `Items` | `List<T>` | Items on the current page. |
| `TotalCount` | `int` | Total number of matching items. |
| `PageSize` | `int` | Page size. |
| `PageNumber` | `int` | Current page number. |

## Usage

### Example 1: Health Check and Full Configuration

```csharp
using System;
using System.Threading.Tasks;
using DotnetConfigServer.Examples;

public class ConfigService
{
    private readonly BasicConfigurationClient _client = new("https://localhost:5001");

    public async Task LoadConfigurationAsync(string configurationId)
    {
        if (!await _client.IsHealthyAsync())
        {
            Console.WriteLine("Configuration server is unavailable.");
            return;
        }

        var configuration = await _client.GetConfigurationAsync(configurationId);

        Console.WriteLine($"Environment: {configuration.Environment}");
        Console.WriteLine($"Total Keys: {configuration.Keys.Count}");
        Console.WriteLine($"Last Modified: {configuration.LastModifiedAt}");
    }
}
```

### Example 2: Reading Individual Keys

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetConfigServer.Examples;

public class KeyInspector
{
    private readonly BasicConfigurationClient _client;

    public KeyInspector(BasicConfigurationClient client) => _client = client;

    public async Task InspectKeysAsync(string configurationId, string applicationId)
    {
        // List every configuration of the application (first page only)
        IEnumerable<Configuration> configurations =
            await _client.GetApplicationConfigurationsAsync(applicationId);

        foreach (var configuration in configurations)
        {
            Console.WriteLine($"{configuration.Environment}: {configuration.Keys.Count} keys");
        }

        // Read a single key by name
        try
        {
            string host = await _client.GetConfigurationKeyAsync(configurationId, "Database:Host");
            Console.WriteLine($"Database Host: {host}");
        }
        catch (KeyNotFoundException)
        {
            Console.WriteLine("Key 'Database:Host' is missing or has no value.");
        }
    }
}
```

## Notes

*   **Thread Safety**: After construction, the instance's state is never modified: both fields are `readonly`, and no method writes to instance state. One client can therefore be used from several threads at once, and the underlying `HttpClient` is designed for concurrent requests. Do not create a new client per request: each instance creates its own `HttpClient`, and the class never disposes it (it does not implement `IDisposable`). Create one instance and reuse it for the lifetime of the application, for example as a singleton.
*   **Cancellation and Timeouts**: No method accepts a `CancellationToken`. Requests are bounded only by the `HttpClient` default timeout of 100 seconds.
*   **Casing of Response JSON**: `GetConfigurationAsync` deserializes with default `JsonSerializerOptions`, which match property names case-sensitively. `GetApplicationConfigurationsAsync` uses `PropertyNameCaseInsensitive = true`. ASP.NET Core emits camelCase by default, so if the server sends camelCase names, the `Configuration` results from `GetConfigurationAsync` (and therefore `GetConfigurationKeyAsync`) will not bind their PascalCase properties.
*   **URL Construction**: `configurationId` and `applicationId` are inserted into the request URL without escaping. Pass GUID strings. A value containing `/`, `?`, `&` or `#` changes the request target.
*   **Encryption**: The client does not decrypt anything. `IsEncrypted` only reports how the value is stored, and `Value` is returned exactly as the server sends it.
*   **Repeated Lookups**: `GetConfigurationKeyAsync` fetches the whole configuration on every call. If you read many keys, call `GetConfigurationAsync` once and use `Configuration.GetKeyValue` on the result.
*   **Health Check Semantics**: `IsHealthyAsync` never throws, so it can gate startup logic without a `try` block. A `false` result means either the server is unhealthy or it could not be reached.
