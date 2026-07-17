# MultiEnvironmentManager

`MultiEnvironmentManager` orchestrates configuration management across multiple deployment environments. It provides facilities to retrieve or create environment-specific configurations, list configurations for a given environment, promote configurations between environments, compare environments visually, synchronize individual keys, and display promotion workflow steps. The type models environments as containers of configuration keys, each with a value, encryption flag, and description, and exposes paginated lists of items through a generic `Items` property.

## API

### Properties

#### `string Id`
Unique identifier of the environment configuration record.

#### `string Environment`
Name of the deployment environment (e.g., "Development", "Staging", "Production").

#### `string Description`
Human-readable description of the environment or its configuration set.

#### `int? KeyCount`
Number of configuration keys associated with this environment. Nullable when the count has not been computed.

#### `DateTime CreatedAt`
Timestamp indicating when the environment configuration was initially created.

#### `List<ConfigurationKeyDto> Keys`
Collection of configuration key entries belonging to the environment. Each entry exposes:
- `string Key` – the configuration key name.
- `string Value` – the configuration value.
- `bool IsEncrypted` – whether the value is stored encrypted.
- `string Description` – a description of the key’s purpose.

#### `List<T> Items`
Generic paginated result container holding a list of items of type `T`. Used to wrap results returned from list-oriented methods.

### Constructors

#### `public MultiEnvironmentManager`
Default constructor. Initializes a new instance of the manager. Dependencies are typically injected or resolved internally.

### Methods

#### `public async Task<string> GetOrCreateEnvironmentConfigAsync`
Retrieves the configuration for a specified environment, creating it if it does not already exist.

- **Parameters:** (inferred from typical usage) an environment name and optional creation metadata.
- **Returns:** The identifier of the existing or newly created environment configuration.
- **Exceptions:** Throws when the underlying data store is unreachable or when the environment name is invalid.

#### `public async Task<List<ConfigurationDto>> ListEnvironmentConfigurationsAsync`
Lists all configurations for a given environment.

- **Parameters:** (inferred) an environment identifier.
- **Returns:** A list of `ConfigurationDto` objects representing the environment’s configurations.
- **Exceptions:** Throws when the environment does not exist or the data store cannot be queried.

#### `public async Task<int> PromoteAsync`
Promotes a configuration from a source environment to a target environment.

- **Parameters:** (inferred) source environment identifier, target environment identifier, and optional promotion options.
- **Returns:** The number of keys promoted.
- **Exceptions:** Throws when the source or target environment is missing, when a promotion conflict occurs, or when the operation is rejected by business rules.

#### `public async Task DisplayEnvironmentComparisonAsync`
Outputs a comparison between two environments, highlighting differences in keys and values.

- **Parameters:** (inferred) identifiers for the two environments to compare.
- **Returns:** A task representing the asynchronous display operation.
- **Exceptions:** Throws when either environment is not found or when the comparison engine encounters an error.

#### `public async Task SynchronizeKeyAsync`
Synchronizes a single configuration key from a source environment to a target environment.

- **Parameters:** (inferred) source environment identifier, target environment identifier, and the key to synchronize.
- **Returns:** A task that completes when the key has been synchronized.
- **Exceptions:** Throws when the key does not exist in the source, when the target environment is read-only, or when a conflict cannot be resolved.

#### `public async Task DisplayPromotionWorkflowAsync`
Displays the steps involved in promoting configurations between environments, including validation and approval stages.

- **Parameters:** (inferred) source and target environment identifiers.
- **Returns:** A task that completes when the workflow has been displayed.
- **Exceptions:** Throws when the workflow definition cannot be loaded or when the environments are not part of a valid promotion path.

## Usage

### Example 1: Retrieve or create a configuration and list its keys

```csharp
var manager = new MultiEnvironmentManager();
string envId = await manager.GetOrCreateEnvironmentConfigAsync("Staging");

var configurations = await manager.ListEnvironmentConfigurationsAsync(envId);
foreach (var config in configurations)
{
    Console.WriteLine($"Environment: {config.Environment}, Keys: {config.KeyCount}");
    foreach (var key in config.Keys)
    {
        Console.WriteLine($"  {key.Key} = {(key.IsEncrypted ? "***" : key.Value)}");
    }
}
```

### Example 2: Promote keys and display the workflow

```csharp
var manager = new MultiEnvironmentManager();
string sourceEnvId = await manager.GetOrCreateEnvironmentConfigAsync("Development");
string targetEnvId = await manager.GetOrCreateEnvironmentConfigAsync("Production");

await manager.DisplayPromotionWorkflowAsync(sourceEnvId, targetEnvId);

int promotedCount = await manager.PromoteAsync(sourceEnvId, targetEnvId);
Console.WriteLine($"Promoted {promotedCount} keys to Production.");

await manager.DisplayEnvironmentComparisonAsync(sourceEnvId, targetEnvId);
```

## Notes

- **Thread safety:** Instance methods are not guaranteed to be thread-safe. Callers should synchronize access when sharing a single `MultiEnvironmentManager` instance across multiple threads.
- **Null handling:** `KeyCount` is nullable and may be `null` when the count has not been computed. Callers should guard against null before performing arithmetic.
- **Encryption:** The `IsEncrypted` flag on keys indicates at-rest encryption; consumers must decrypt values separately if required.
- **Pagination:** Methods returning `List<T>` may return partial results when the underlying data set is large. Check for pagination metadata if available.
- **Promotion failures:** `PromoteAsync` returns the count of successfully promoted keys. A return value of zero may indicate that no changes were necessary or that the operation was blocked by validation rules.
- **Display methods:** `DisplayEnvironmentComparisonAsync` and `DisplayPromotionWorkflowAsync` are intended for interactive or logging scenarios. Their output format is not guaranteed to be stable across versions.
