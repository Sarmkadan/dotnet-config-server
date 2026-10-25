# BatchConfigurationImporter

Sample HTTP client (namespace `DotnetConfigServer.Examples`, source in `examples/BatchConfigurationImporter.cs`) that imports, exports, clones and merges configuration keys through the server's REST API under `/api/v1/configurations/`. It wraps a single `HttpClient` and returns a `BatchImportResult` with success and failure counts and error messages.

The `examples/` folder is excluded from the main build (`<Compile Remove="examples/**/*.cs" />` in `dotnet-config-server.csproj`), so this class is a reference client, not part of the server assembly.

## API

### Constructor

- **`public BatchConfigurationImporter(string baseUrl)`**  
  Creates the importer and an internal `HttpClient` whose `BaseAddress` is `new Uri(baseUrl)`. The `HttpClient` is not disposed by this class (see Notes).  
  **Throws:** `ArgumentNullException` if `baseUrl` is null, `UriFormatException` if it is not an absolute URI.

### Methods

- **`public async Task<BatchImportResult> ImportFromJsonAsync(string jsonFilePath)`**  
  Reads the file as text, deserializes it into a `BatchImportRequest` with default `System.Text.Json` options, and delegates to `ImportAsync`.  
  **Returns:** The `BatchImportResult` from `ImportAsync`.  
  **Throws:** `ArgumentNullException` for a null path, `FileNotFoundException` if the file is missing, `JsonException` if the JSON is malformed. Property names must match the PascalCase names of `BatchImportRequest` and `ConfigurationKeyImport` (see Notes).

- **`public async Task<BatchImportResult> ImportAsync(BatchImportRequest request)`**  
  Sends one `POST /api/v1/configurations/{ConfigurationId}/keys` per entry in `request.Keys`, sequentially. The body is `{ key, value, isEncrypted, description }`. Progress is written to the console (`✓ Imported key: ...` / `✗ ...`).  
  **Returns:** A new `BatchImportResult`. Per-key failures are counted in `FailureCount` and described in `Errors`; the method does not throw for them.  
  **Throws:** `NullReferenceException` if `request` is null.

- **`public async Task ExportToJsonAsync(string configurationId, string outputPath)`**  
  Sends `GET /api/v1/configurations/{configurationId}`, checks the status code, and writes the response body unchanged to `outputPath`. Prints `Configuration exported to <outputPath>` on success.  
  **Returns:** A `Task` that completes when the file is written.  
  **Throws:** `HttpRequestException` for a non-success status (no file is written), `IOException` or `UnauthorizedAccessException` if `outputPath` cannot be written.

- **`public async Task<BatchImportResult> CloneConfigurationAsync(string sourceConfigId, string targetConfigId)`**  
  Fetches the source configuration (`GET /api/v1/configurations/{sourceConfigId}`), deserializes it as a `ConfigurationDto` with case-insensitive property matching, copies each key's `Key`, `Value`, `IsEncrypted` and `Description` (server `Id`s are not copied), and runs `ImportAsync` against `targetConfigId`.  
  **Returns:** The `BatchImportResult` from `ImportAsync` for the target.  
  **Throws:** `HttpRequestException` if the source fetch is not successful, `JsonException` if its body cannot be parsed, `NullReferenceException` if the source has no `Keys` list.

- **`public async Task<BatchImportResult> MergeConfigurationsAsync(string sourceConfigId, string targetConfigId, bool overwrite = true)`**  
  Fetches both configurations, then posts each source key to `POST /api/v1/configurations/{targetConfigId}/keys`. If `overwrite` is `false`, source keys whose name already exists in the target are skipped. Keys that exist only in the target are left untouched; nothing is deleted. Unlike `ImportAsync`, this method does not write to the console and does not record messages in `Errors`.  
  **Returns:** A new `BatchImportResult`. Skipped keys are counted in neither `SuccessCount` nor `FailureCount`.  
  **Throws:** `HttpRequestException` if either fetch is not successful (the source response is checked first), `ArgumentException` if the target contains duplicate key names.

- **`public override string ToString()`** (on `BatchImportResult`)  
  Returns `Success: {SuccessCount}, Failed: {FailureCount}, Errors: {Errors.Count}`.

### Types

- **`BatchImportRequest`**  
  - `string ConfigurationId` - target configuration identifier used in the URL.  
  - `List<ConfigurationKeyImport> Keys` - keys to write; defaults to an empty list.

- **`ConfigurationKeyImport`**  
  - `string Key`, `string Value`, `bool IsEncrypted`, `string Description`.

- **`BatchImportResult`**  
  - `int SuccessCount` - keys written successfully by the call.  
  - `int FailureCount` - keys that failed in the call.  
  - `List<string> Errors` - failure messages; defaults to an empty list. Only `ImportAsync` (and therefore `ImportFromJsonAsync` and `CloneConfigurationAsync`) populates it.

- **`ConfigurationDto`** (response model for `GET` in clone and merge)  
  - `Guid Id`, `string Environment`, `List<ConfigurationKeyDto> Keys`.

- **`ConfigurationKeyDto`**  
  - `Guid Id`, `string Key`, `string Value`, `bool IsEncrypted`, `string Description`.

## Usage

### Example 1: Importing Configuration from a JSON File

The file must use the PascalCase property names expected by the deserializer:

```json
{
  "ConfigurationId": "app-settings-v2",
  "Keys": [
    { "Key": "Database:Host", "Value": "prod-db.example.com", "IsEncrypted": false, "Description": "Database server hostname" },
    { "Key": "ApiKey:ThirdParty", "Value": "<encrypted-value>", "IsEncrypted": true, "Description": "Third-party API key" }
  ]
}
```

```csharp
var importer = new BatchConfigurationImporter("https://localhost:5001");

BatchImportResult result = await importer.ImportFromJsonAsync("configurations.json");
Console.WriteLine(result);

if (result.FailureCount > 0)
{
    foreach (string error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

### Example 2: Building a Batch in Code

```csharp
var importer = new BatchConfigurationImporter("https://localhost:5001");

var request = new BatchImportRequest
{
    ConfigurationId = "app-settings-v2",
    Keys = new List<ConfigurationKeyImport>
    {
        new ConfigurationKeyImport
        {
            Key = "NewFeature:Enabled",
            Value = "true",
            IsEncrypted = false,
            Description = "Enables the new feature flag"
        }
    }
};

BatchImportResult importResult = await importer.ImportAsync(request);
Console.WriteLine($"Import result: {importResult.SuccessCount} keys written.");
```

### Example 3: Cloning and Merging Between Configurations

```csharp
var importer = new BatchConfigurationImporter("https://localhost:5001");

// Copy every key from the dev configuration into prod
BatchImportResult cloneResult = await importer.CloneConfigurationAsync("dev-config-id", "prod-config-id");
Console.WriteLine($"Clone result: {cloneResult}");

// Copy keys from staging into prod, but keep prod values where the key already exists
BatchImportResult mergeResult = await importer.MergeConfigurationsAsync(
    "staging-config-id",
    "prod-config-id",
    overwrite: false);
Console.WriteLine($"Merge result: {mergeResult.SuccessCount} written, {mergeResult.FailureCount} failed.");
```

### Example 4: Exporting a Backup

```csharp
await importer.ExportToJsonAsync("550e8400-e29b-41d4-a716-446655440001", "backup.json");
```

The exported file is the raw `GET` response (the configuration DTO shape), not the `BatchImportRequest` shape. Reshape it before passing it to `ImportFromJsonAsync`.

## Error Handling

Failures fall into three groups: exceptions that propagate to the caller, per-key failures that are counted and recorded, and silent no-ops. Check `FailureCount` after `ImportAsync`, `ImportFromJsonAsync`, or `CloneConfigurationAsync`; the absence of an exception does not mean every key was written.

| Situation | Method | Result |
|---|---|---|
| Null or non-absolute `baseUrl` | constructor | `ArgumentNullException` / `UriFormatException` |
| Missing JSON file | `ImportFromJsonAsync` | `FileNotFoundException` |
| Malformed JSON | `ImportFromJsonAsync` | `JsonException` |
| JSON with camelCase or other non-matching property names | `ImportFromJsonAsync` | No exception; `ConfigurationId` is null and `Keys` is empty, so the result is `Success: 0, Failed: 0`. |
| JSON body `null` | `ImportFromJsonAsync` | `NullReferenceException` from `ImportAsync` |
| Non-success HTTP status for one key | `ImportAsync` | `FailureCount++`; `Errors` gets `Failed to import {Key}: {StatusCode}`; next key continues |
| Transport error or timeout for one key | `ImportAsync` | `FailureCount++`; `Errors` gets `Error importing {Key}: {message}`; next key continues |
| Null `request` | `ImportAsync` | `NullReferenceException` |
| Non-success status on export | `ExportToJsonAsync` | `HttpRequestException`; file is not written |
| Unwritable output path | `ExportToJsonAsync` | `IOException` / `UnauthorizedAccessException` after the GET succeeded |
| Non-success status on source fetch | `CloneConfigurationAsync`, `MergeConfigurationsAsync` | `HttpRequestException`; no keys are written |
| Source or target response cannot be parsed | `CloneConfigurationAsync`, `MergeConfigurationsAsync` | `JsonException` |
| Source has no `Keys` list (null) | `CloneConfigurationAsync` | `NullReferenceException` |
| Target contains duplicate key names | `MergeConfigurationsAsync` | `ArgumentException` from `ToDictionary` |
| Non-success or exception for one key | `MergeConfigurationsAsync` | `FailureCount++` only; no entry in `Errors`, so the reason is lost |
| Key exists in target and `overwrite` is `false` | `MergeConfigurationsAsync` | Skipped silently; counted in neither counter |

Practical guidance:

- Wrap the calls in `try`/`catch` for `HttpRequestException`, `JsonException`, and `IOException`. Per-key failures never throw from `ImportAsync`, so inspect `FailureCount` and `Errors` instead.
- `MergeConfigurationsAsync` gives no reason for a failed key. If you need the cause, check the server logs or run the operation through `ImportAsync` with the keys you care about.
- The client has no retry logic. A failed key stays failed; rerun the failed keys only.
- Validate the JSON shape before import. The importer cannot distinguish "no keys in the file" from "wrong property names in the file".

## Notes

- **Thread Safety:** After construction the importer holds only its `HttpClient` and base URL, and each call returns a new `BatchImportResult`, so concurrent calls do not share counters. `ImportAsync` and `ImportFromJsonAsync` write to `Console`, which is synchronized but interleaves output.
- **Disposal:** `BatchConfigurationImporter` does not implement `IDisposable`, so the `HttpClient` it creates is never disposed. Create one instance per server and reuse it rather than creating one per batch.
- **Sequential Processing:** Keys are posted one at a time with `await`. A large batch is slow but cannot overwhelm the server with concurrent requests.
- **Encryption Handling:** The importer does not encrypt or decrypt anything. It forwards `Value` and `IsEncrypted` unchanged, so an encrypted value must already be in the form the server expects.
- **Empty Keys:** Importing an empty `Keys` list returns a result with zero successes and zero failures, not an exception.
- **Duplicate Keys:** `ImportAsync` and `MergeConfigurationsAsync` post every occurrence of a duplicate source key. The final value is whatever the server stores last; the client does not deduplicate.
- **Overwrite Semantics:** `overwrite: true` posts every source key, relying on the server to create or update it. There is no timestamp comparison despite what the source comment suggests; the flag is the only decision.
- **JSON Property Names:** `ImportFromJsonAsync` uses default `System.Text.Json` options, which are case-sensitive. camelCase keys such as `configurationId` and `key` do not bind and produce an empty result without an error. `CloneConfigurationAsync` uses case-insensitive matching for the server response, so the two paths differ.
