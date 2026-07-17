# BasicConfigurationClient

The `BasicConfigurationClient` serves as the primary interface for interacting with the configuration server within the `dotnet-config-server` ecosystem. It provides asynchronous methods to retrieve application-specific configurations, fetch individual key values, and verify the health status of the service. In addition to operational methods, the client exposes metadata properties regarding the current environment, application identity, and the state of loaded configuration keys, enabling developers to manage and inspect configuration data programmatically.

## API

### Constructors

#### `public BasicConfigurationClient`
Initializes a new instance of the `BasicConfigurationClient` class. This constructor sets up the internal state required to communicate with the configuration server, typically utilizing default or injected connection parameters depending on the instantiation context.

### Methods

#### `public async Task<Configuration> GetConfigurationAsync`
Retrieves the full configuration object for the current application and environment context.
*   **Parameters**: None.
*   **Return Value**: A `Task` resulting in a `Configuration` object containing the aggregate settings.
*   **Exceptions**: Throws an exception if the network connection fails, the server returns an error status, or the requested application/environment combination does not exist.

#### `public async Task<string> GetConfigurationKeyAsync`
Fetches the string value of a specific configuration key directly.
*   **Parameters**: None (implicitly targets a key based on internal state or requires context not visible in this signature; typically used when the client is scoped to a specific key or requires a key argument in an overload not listed here. Based strictly on the provided signature, it operates on the current instance context).
*   **Return Value**: A `Task` resulting in a `string` representing the value of the configuration key.
*   **Exceptions**: Throws if the key is not found or if decryption fails for encrypted values.

#### `public async Task<IEnumerable<Configuration>> GetApplicationConfigurationsAsync`
Retrieves a collection of configuration objects associated with the current application across various contexts or versions.
*   **Parameters**: None.
*   **Return Value**: A `Task` resulting in an `IEnumerable<Configuration>`.
*   **Exceptions**: Throws if the application ID is invalid or if the server encounters an error while aggregating the results.

#### `public async Task<bool> IsHealthyAsync`
Performs a health check against the configuration server endpoint.
*   **Parameters**: None.
*   **Return Value**: A `Task` resulting in a `bool`. Returns `true` if the server is responsive and operational; otherwise `false`.
*   **Exceptions**: Generally does not throw; network failures usually result in a `false` return, though severe protocol errors may propagate exceptions.

### Properties

#### `public Guid Id`
Gets the unique identifier for this specific client instance or the specific configuration session it represents.

#### `public Guid ApplicationId`
Gets the unique identifier of the application associated with this client. This value scopes all configuration requests to a specific registered application.

#### `public string Environment`
Gets the name of the environment (e.g., "Development", "Production") currently targeted by this client.

#### `public string Description`
Gets a textual description of the current configuration context or the client instance.

#### `public List<ConfigurationKey> Keys`
Gets the list of `ConfigurationKey` objects currently loaded or managed by this client instance.

#### `public DateTime CreatedAt`
Gets the timestamp indicating when this configuration context or client session was created.

#### `public DateTime LastModifiedAt`
Gets the timestamp of the last modification made to the configuration data held by this client.

#### `public string GetKeyValue`
Gets the raw string value of the currently selected or primary configuration key. Note: This appears to be a property accessor rather than a method based on the signature provided.

#### `public Guid Id` (Duplicate Member)
*Note: The signature list contains a duplicate `Id` property. In the context of `ConfigurationKey` (see below), this likely refers to the unique ID of a specific key entry.*
Gets the unique identifier for a specific configuration key entry.

#### `public string Key`
Gets the name of the configuration key.

#### `public string Value`
Gets the stored value associated with the configuration key.

#### `public bool IsEncrypted`
Gets a boolean indicating whether the associated configuration value is stored in an encrypted format.

#### `public string Description` (Duplicate Member)
*Note: Likely refers to the description of a specific `ConfigurationKey`.*
Gets the description text for a specific configuration key.

#### `public List<T> Items`
Gets a generic list of items contained within a configuration set. The type `T` is determined by the specific configuration payload.

#### `public int TotalCount`
Gets the total number of configuration items or keys available in the current context.

## Usage

### Example 1: Initializing and Retrieving Full Configuration
This example demonstrates instantiating the client and fetching the complete configuration set for an application, including a health check prior to data retrieval.

```csharp
using System;
using System.Threading.Tasks;
using DotNetConfigServer;

public class ConfigService
{
    public async Task LoadConfigurationAsync()
    {
        // Initialize the client
        var client = new BasicConfigurationClient();

        // Verify server health before attempting data retrieval
        bool isHealthy = await client.IsHealthyAsync();
        if (!isHealthy)
        {
            Console.WriteLine("Configuration server is unavailable.");
            return;
        }

        // Retrieve the full configuration
        var configuration = await client.GetConfigurationAsync();

        Console.WriteLine($"Loaded config for App: {client.ApplicationId}");
        Console.WriteLine($"Environment: {client.Environment}");
        Console.WriteLine($"Total Keys: {client.Keys.Count}");
        Console.WriteLine($"Last Modified: {client.LastModifiedAt}");
    }
}
```

### Example 2: Fetching Specific Key Values and Handling Encryption
This example illustrates accessing specific key properties, checking encryption status, and retrieving individual key values.

```csharp
using System;
using System.Threading.Tasks;
using System.Linq;

public class KeyInspector
{
    public async Task InspectKeysAsync(BasicConfigurationClient client)
    {
        // Ensure configuration is loaded
        await client.GetConfigurationAsync();

        // Iterate through available keys
        foreach (var key in client.Keys)
        {
            Console.WriteLine($"Key: {key.Key}");
            Console.WriteLine($"Description: {key.Description}");
            
            if (key.IsEncrypted)
            {
                Console.WriteLine("Status: Encrypted value detected.");
                // Access the decrypted value via the client method or property depending on context
                // Assuming the client context is set to this key for GetConfigurationKeyAsync
                // Or accessing the Value property if already resolved by the library
                Console.WriteLine($"Value Length: {key.Value?.Length ?? 0}");
            }
            else
            {
                Console.WriteLine($"Value: {key.Value}");
            }
        }

        // Access a specific key value directly if the client supports scoped access
        // Note: Implementation depends on how the client internal state is managed
        var specificValue = await client.GetConfigurationKeyAsync();
        Console.WriteLine($"Retrieved Value: {specificValue}");
    }
}
```

## Notes

*   **Thread Safety**: The `BasicConfigurationClient` exposes mutable state via properties such as `Keys`, `LastModifiedAt`, and `GetKeyValue`. As the class relies on `async` methods that likely update these internal states upon completion, instances of this class should not be shared across threads without external synchronization. It is recommended to treat the client as transient or scoped per operation/request.
*   **Duplicate Signatures**: The public API surface includes duplicate member names (`Id`, `Description`). In practice, these likely correspond to different contexts (e.g., Client-level metadata vs. `ConfigurationKey`-level metadata). When accessing these via an instance of `BasicConfigurationClient`, ensure you are referencing the correct object context (the client itself vs. items within the `Keys` list).
*   **Encryption Handling**: The `IsEncrypted` property indicates the storage state of a value. Consumers must be aware that accessing the `Value` property or using `GetConfigurationKeyAsync` may involve runtime decryption operations. Failure to configure proper decryption keys in the host environment will result in exceptions during these calls.
*   **Empty Collections**: The `Keys` property returns a `List<ConfigurationKey>`. If no configuration is found for the specified `ApplicationId` and `Environment`, this list may be empty rather than null. Callers should verify `TotalCount` or check the list count before iterating.
*   **Health Check Semantics**: `IsHealthyAsync` returns a boolean rather than throwing on failure. This allows for graceful degradation strategies where the application can proceed with cached defaults if the configuration server is temporarily unreachable.
