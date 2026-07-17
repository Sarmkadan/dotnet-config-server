# BatchConfigurationImporter

Provides mechanisms for importing, exporting, cloning, and merging batch configuration data from JSON sources. It encapsulates the state of a batch operation, including the configuration identifier, environment, keys, and a detailed result summary with success and failure counts and error messages.

## API

### Constructors

- **`public BatchConfigurationImporter`**  
  Initializes a new instance of the `BatchConfigurationImporter` class. The default constructor prepares an empty importer with no pre-configured keys or results.

### Methods

- **`public async Task<BatchImportResult> ImportFromJsonAsync`**  
  Imports configuration data from a JSON source asynchronously. The exact source (stream, string, file path) is determined by the overload used.  
  **Returns:** A `BatchImportResult` containing the number of successfully imported keys, failures, and any error messages encountered during the operation.  
  **Throws:** May throw `ArgumentNullException` if the JSON source is null, `JsonException` if the JSON is malformed, or `InvalidOperationException` if the importer is not in a valid state.

- **`public async Task<BatchImportResult> ImportAsync`**  
  Performs an asynchronous import of configuration keys that have been set on the instance.  
  **Returns:** A `BatchImportResult` detailing the outcome of the import process.  
  **Throws:** May throw `InvalidOperationException` if required properties such as `ConfigurationId` are not set.

- **`public async Task ExportToJsonAsync`**  
  Exports the current configuration keys to a JSON destination asynchronously.  
  **Returns:** A `Task` representing the asynchronous export operation.  
  **Throws:** May throw `InvalidOperationException` if there is no configuration data to export, or `IOException` if the target destination is not writable.

- **`public async Task<BatchImportResult> CloneConfigurationAsync`**  
  Clones an existing configuration identified by `ConfigurationId` into a new configuration.  
  **Returns:** A `BatchImportResult` with the outcome of the clone operation.  
  **Throws:** May throw `KeyNotFoundException` if the source configuration does not exist, or `InvalidOperationException` if the clone operation fails due to validation errors.

- **`public async Task<BatchImportResult> MergeConfigurationsAsync`**  
  Merges the current set of keys into an existing configuration. The merge strategy (add, update, skip) is determined by the implementation.  
  **Returns:** A `BatchImportResult` summarizing the merge outcome.  
  **Throws:** May throw `InvalidOperationException` if the target configuration is locked or if a merge conflict cannot be resolved.

- **`public override string ToString`**  
  Returns a string representation of the current `BatchConfigurationImporter`, typically including the `ConfigurationId` and the number of keys loaded.  
  **Returns:** A human-readable string describing the importer state.

### Properties

- **`public string ConfigurationId`**  
  Gets or sets the identifier of the configuration being imported or manipulated.

- **`public List<ConfigurationKeyImport> Keys`**  
  Gets the list of `ConfigurationKeyImport` objects representing the keys to be imported or that have been imported. Each entry contains a `Key`, `Value`, `IsEncrypted`, and `Description`.

- **`public string Key`**  
  Gets or sets the key name for an individual configuration entry within a `ConfigurationKeyImport`.

- **`public string Value`**  
  Gets or sets the value for an individual configuration entry within a `ConfigurationKeyImport`.

- **`public bool IsEncrypted`**  
  Gets or sets a value indicating whether the configuration value is encrypted.

- **`public string Description`**  
  Gets or sets a human-readable description for a configuration key.

- **`public int SuccessCount`**  
  Gets the number of keys successfully imported or processed in the last operation.

- **`public int FailureCount`**  
  Gets the number of keys that failed to import or process in the last operation.

- **`public List<string> Errors`**  
  Gets the list of error messages accumulated during the last import or processing operation.

- **`public Guid Id`**  
  Gets or sets the unique identifier for the configuration entity (appears on both the importer and the DTO).

- **`public string Environment`**  
  Gets or sets the target environment name for the configuration.

- **`public List<ConfigurationKeyDto> Keys`**  
  Gets or sets the list of `ConfigurationKeyDto` objects representing the configuration keys in a data-transfer context.

## Usage

### Example 1: Importing Configuration from a JSON File

```csharp
var importer = new BatchConfigurationImporter();
importer.ConfigurationId = "app-settings-v2";

// Assume ImportFromJsonAsync reads from a file path
BatchImportResult result = await importer.ImportFromJsonAsync("config.json");

Console.WriteLine($"Imported: {result.SuccessCount} succeeded, {result.FailureCount} failed.");
if (result.FailureCount > 0)
{
    foreach (string error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Example 2: Cloning a Configuration and Merging Keys

```csharp
var importer = new BatchConfigurationImporter();
importer.ConfigurationId = "source-config-id";

// Clone the source configuration
BatchImportResult cloneResult = await importer.CloneConfigurationAsync();
Console.WriteLine($"Clone result: {cloneResult.SuccessCount} keys copied.");

// Prepare new keys for merging
importer.Keys = new List<ConfigurationKeyImport>
{
    new ConfigurationKeyImport
    {
        Key = "NewFeature:Enabled",
        Value = "true",
        IsEncrypted = false,
        Description = "Enables the new feature flag"
    }
};

// Merge into the cloned configuration
BatchImportResult mergeResult = await importer.MergeConfigurationsAsync();
Console.WriteLine($"Merge result: {mergeResult.SuccessCount} keys added.");
```

## Notes

- **Thread Safety:** Instance members are not guaranteed to be thread-safe. Do not share `BatchConfigurationImporter` instances across multiple threads without external synchronization, especially when modifying `Keys` or reading `Errors` concurrently with an async operation.
- **State Reuse:** The `SuccessCount`, `FailureCount`, and `Errors` properties reflect the outcome of the most recent operation. Calling a new import, clone, or merge method overwrites these values.
- **Encryption Handling:** When `IsEncrypted` is `true` on a key, the importer expects the `Value` to be pre-encrypted or handles encryption during the import pipeline. Providing plaintext values with `IsEncrypted` set to `true` may result in storage of improperly secured data.
- **Empty Keys:** Importing an empty `Keys` list typically results in a `BatchImportResult` with zero successes and zero failures, not an exception. Validate the list beforehand if a minimum key count is required.
- **Duplicate Keys:** Behavior on duplicate keys within the `Keys` list depends on the underlying merge or import strategy. The last occurrence may overwrite earlier ones, or an error may be recorded in `Errors`.
- **JSON Parsing:** `ImportFromJsonAsync` expects a well-formed JSON structure matching the configuration schema. Malformed JSON throws a `JsonException` rather than populating `Errors`.
