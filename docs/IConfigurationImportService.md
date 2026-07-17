# IConfigurationImportService

The `IConfigurationImportService` interface defines the contract for importing configuration data from various file formats (JSON, CSV, environment‑file) into the configuration store. It also provides validation capabilities and exposes the state of the last import or validation operation. Implementations of this interface are typically used in the `dotnet-config-server` to ingest configuration from external sources, validate the data before committing, and report any errors or warnings.

## API

### `ImportFromJsonAsync`
```csharp
Task<List<ConfigurationKey>> ImportFromJsonAsync(Stream jsonStream);
```
- **Purpose**: Parses a JSON stream containing configuration key‑value pairs and imports them into the configuration store.
- **Parameters**:
  - `jsonStream` – A `Stream` containing valid JSON configuration data.
- **Returns**: A `Task<List<ConfigurationKey>>` representing the asynchronous operation. The result is a list of `ConfigurationKey` objects that were successfully imported.
- **Throws**:
  - `ArgumentNullException` if `jsonStream` is `null`.
  - `InvalidDataException` if the JSON is malformed or does not match the expected schema.
  - `InvalidOperationException` if the service is already processing an import or validation.

### `ImportFromCsvAsync`
```csharp
Task<List<ConfigurationKey>> ImportFromCsvAsync(Stream csvStream);
```
- **Purpose**: Parses a CSV stream (typically with columns for key, value, and optional metadata) and imports the configuration entries.
- **Parameters**:
  - `csvStream` – A `Stream` containing CSV‑formatted configuration data.
- **Returns**: A `Task<List<ConfigurationKey>>` representing the asynchronous operation. The result is a list of `ConfigurationKey` objects that were successfully imported.
- **Throws**:
  - `ArgumentNullException` if `csvStream` is `null`.
  - `InvalidDataException` if the CSV cannot be parsed or required columns are missing.
  - `InvalidOperationException` if the service is already busy.

### `ImportFromEnvAsync`
```csharp
Task<List<ConfigurationKey>> ImportFromEnvAsync(Stream envStream);
```
- **Purpose**: Parses a stream in the standard `.env` file format (e.g., `KEY=VALUE` lines) and imports the configuration entries.
- **Parameters**:
  - `envStream` – A `Stream` containing environment‑file‑style configuration data.
- **Returns**: A `Task<List<ConfigurationKey>>` representing the asynchronous operation. The result is a list of `ConfigurationKey` objects that were successfully imported.
- **Throws**:
  - `ArgumentNullException` if `envStream` is `null`.
  - `InvalidDataException` if the stream contains invalid syntax (e.g., malformed lines).
  - `InvalidOperationException` if the service is already processing.

### `ValidateAsync`
```csharp
Task<ImportValidationResult> ValidateAsync(Stream configurationStream);
```
- **Purpose**: Validates a configuration stream (in any supported format) without importing it. The validation checks for structural correctness, duplicate keys, and any business rules defined in the server.
- **Parameters**:
  - `configurationStream` – A `Stream` containing configuration data in JSON, CSV, or `.env` format.
- **Returns**: A `Task<ImportValidationResult>` representing the asynchronous operation. The result contains details about the validation outcome, including any errors or warnings.
- **Throws**:
  - `ArgumentNullException` if `configurationStream` is `null`.
  - `InvalidOperationException` if the service is already performing an import or validation.

### `IsValid`
```csharp
bool IsValid { get; }
```
- **Purpose**: Indicates whether the last import or validation operation completed without errors. This property is updated after each call to any import method or `ValidateAsync`.
- **Value**: `true` if the last operation succeeded with no errors; otherwise `false`.

### `Errors`
```csharp
List<string> Errors { get; }
```
- **Purpose**: Provides a list of error messages from the last import or validation operation. Each string describes a specific issue (e.g., malformed line, duplicate key, missing value).
- **Value**: A `List<string>` that may be empty if the last operation was successful. The list is replaced on each new operation.

### `EstimatedRowCount`
```csharp
int EstimatedRowCount { get; }
```
- **Purpose**: Returns an estimate of the number of configuration entries that were processed during the last import or validation. This can be useful for progress reporting or logging.
- **Value**: An integer representing the approximate row count. The value is `0` if no operation has been performed yet.

## Usage

### Example 1: Importing configuration from a JSON file and checking for errors

```csharp
public async Task ImportConfigAsync(IConfigurationImportService importService, string jsonFilePath)
{
    using var stream = File.OpenRead(jsonFilePath);
    var importedKeys = await importService.ImportFromJsonAsync(stream);

    if (!importService.IsValid)
    {
        Console.WriteLine("Import completed with errors:");
        foreach (var error in importService.Errors)
        {
            Console.WriteLine($"  - {error}");
        }
    }
    else
    {
        Console.WriteLine($"Successfully imported {importedKeys.Count} configuration keys.");
    }
}
```

### Example 2: Validating a CSV file before importing

```csharp
public async Task ValidateThenImportAsync(IConfigurationImportService importService, Stream csvStream)
{
    // First, validate the stream without importing
    var validationResult = await importService.ValidateAsync(csvStream);

    if (!importService.IsValid)
    {
        Console.WriteLine("Validation failed. Errors:");
        foreach (var error in importService.Errors)
        {
            Console.WriteLine($"  - {error}");
        }
        return;
    }

    Console.WriteLine($"Validation passed. Estimated rows: {importService.EstimatedRowCount}");

    // Reset stream position for actual import (if stream is seekable)
    csvStream.Position = 0;
    var importedKeys = await importService.ImportFromCsvAsync(csvStream);
    Console.WriteLine($"Imported {importedKeys.Count} keys.");
}
```

## Notes

- **Thread safety**: Instances of `IConfigurationImportService` are **not thread‑safe**. The properties `IsValid`, `Errors`, and `EstimatedRowCount` are mutable and reflect the state of the most recent operation. Concurrent calls to import or validate methods on the same instance will produce undefined behaviour. Use separate instances or external synchronization (e.g., a lock) when accessing the service from multiple threads.
- **Stream consumption**: All import and validation methods consume the provided `Stream`. If the stream is non‑seekable (e.g., a network stream), it cannot be reused after a call. For scenarios requiring both validation and import, ensure the stream supports seeking or buffer the data beforehand.
- **Empty input**: Importing an empty stream (zero bytes) will result in a successful operation with an empty list of `ConfigurationKey` and `IsValid` set to `true`. `EstimatedRowCount` will be `0`.
- **Duplicate keys**: Behaviour on duplicate keys depends on the implementation. Typically, the last occurrence wins, but the duplicate may be reported as a warning in the `Errors` list. Always check `IsValid` and `Errors` after an import to detect such issues.
- **Large files**: For very large configuration files, the import methods may take considerable time and memory. Consider using streaming parsers or splitting the file into smaller chunks if performance is a concern. The `EstimatedRowCount` property provides a rough indication of the file size after parsing begins.
