# ConfigurationService
The `ConfigurationService` class provides a set of methods for managing configurations and configuration keys in the `dotnet-config-server` project. It allows for creating, reading, updating, and deleting configurations and keys, as well as searching and retrieving configuration counts. This service is designed to handle configuration-related operations in a centralized manner.

## API
The `ConfigurationService` class has the following public members:
* `public ConfigurationService`: The constructor for the `ConfigurationService` class.
* `public async Task<Configuration> CreateAsync`: Creates a new configuration. Returns the created configuration. Throws if the creation fails.
* `public async Task<Configuration?> GetByIdAsync`: Retrieves a configuration by its ID. Returns the configuration if found, or null if not found. Throws if the retrieval fails.
* `public async Task<List<Configuration>> GetByApplicationAsync`: Retrieves a list of configurations for a given application. Returns the list of configurations. Throws if the retrieval fails.
* `public async Task<Configuration> UpdateAsync`: Updates an existing configuration. Returns the updated configuration. Throws if the update fails.
* `public async Task DeleteAsync`: Deletes a configuration. Throws if the deletion fails.
* `public async Task<ConfigurationKey> AddKeyAsync`: Adds a new configuration key. Returns the added key. Throws if the addition fails.
* `public async Task<ConfigurationKey> UpdateKeyAsync`: Updates an existing configuration key. Returns the updated key. Throws if the update fails.
* `public async Task<List<ConfigurationKey>> GetKeysAsync`: Retrieves a list of configuration keys. Returns the list of keys. Throws if the retrieval fails.
* `public async Task<ConfigurationKey?> GetKeyAsync`: Retrieves a configuration key by its ID. Returns the key if found, or null if not found. Throws if the retrieval fails.
* `public async Task DeleteKeyAsync`: Deletes a configuration key. Throws if the deletion fails.
* `public async Task<List<Configuration>> SearchAsync`: Searches for configurations based on a search query. Returns the list of matching configurations. Throws if the search fails.
* `public async Task<List<ConfigurationKey>> SearchKeysAsync`: Searches for configuration keys based on a search query. Returns the list of matching keys. Throws if the search fails.
* `public async Task<int> GetConfigurationCountAsync`: Retrieves the total count of configurations. Returns the count. Throws if the retrieval fails.

## Usage
Here are two examples of using the `ConfigurationService` class:
```csharp
// Example 1: Creating and retrieving a configuration
var configurationService = new ConfigurationService();
var newConfiguration = await configurationService.CreateAsync(new Configuration { Name = "MyConfig" });
var retrievedConfiguration = await configurationService.GetByIdAsync(newConfiguration.Id);
Console.WriteLine(retrievedConfiguration.Name); // Output: MyConfig

// Example 2: Adding and updating a configuration key
var configurationKey = await configurationService.AddKeyAsync(new ConfigurationKey { Name = "MyKey" });
configurationKey.Value = "New Value";
var updatedKey = await configurationService.UpdateKeyAsync(configurationKey);
Console.WriteLine(updatedKey.Value); // Output: New Value
```

## Notes
When using the `ConfigurationService` class, note the following:
* All methods are asynchronous, so they should be awaited to ensure proper execution.
* Methods that retrieve data may return null if the data is not found, so null checks should be performed as needed.
* Methods that modify data may throw exceptions if the modification fails, so error handling should be implemented as needed.
* The `ConfigurationService` class is designed to be thread-safe, but concurrent modifications to the same configuration or key may still result in unexpected behavior. It is recommended to use locking or other synchronization mechanisms to ensure data consistency in multi-threaded environments.
