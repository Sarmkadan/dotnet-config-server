# MultiEnvironmentManager

`MultiEnvironmentManager` is a sample client for managing one application's configurations across deployment environments (for example Development, Staging, Production). It talks to the Configuration Server's REST API over HTTP and provides operations to find or create an environment's configuration, list configurations, promote keys between environments, compare two environments, write a key into every environment, and print the recommended promotion workflow.

- **Namespace:** `DotnetConfigServer.Examples`
- **Source:** `examples/MultiEnvironmentManager.cs`

The type is a standalone example. It is not part of the server's `Services/` layer and does not use EF Core or the server's service classes; it only issues HTTP requests.

## API

### Constructor

#### `public MultiEnvironmentManager(string baseUrl, string applicationId)`
Creates a manager bound to one application on one server.

- **`baseUrl`:** Absolute base URL of the server, e.g. `https://localhost:5001`. Used as the `HttpClient.BaseAddress`.
- **`applicationId`:** Identifier of the application whose configurations are managed.
- **Exceptions:** `ArgumentException` when either argument is null or empty. `UriFormatException` when `baseUrl` is not a valid absolute URI.

The manager creates and owns a private `HttpClient`. The class is `sealed` and does not implement `IDisposable`, so that client is never disposed.

### Overrides

#### `public override string ToString()`
Returns `MultiEnvironmentManager { ApplicationId = ..., BaseAddress = ... }`.

### Methods

#### `public async Task<string> GetOrCreateEnvironmentConfigAsync(string environment, string description = null)`
Returns the identifier of the configuration for `environment`, creating it if necessary.

- **Parameters:**
  - `environment` – environment name, matched exactly (case-sensitive) against `ConfigurationDto.Environment`.
  - `description` – optional description used on creation. Defaults to `Configuration for {environment} environment`.
- **Behavior:** Calls `ListEnvironmentConfigurationsAsync()` and returns the `Id` of the first match. If there is no match, sends `POST /api/v1/configurations` with `applicationId`, `environment` and `description`, and returns the `Id` of the response.
- **Returns:** Identifier of the existing or newly created configuration.
- **Exceptions:** `HttpRequestException` on a non-success status code from either request.

#### `public async Task<List<ConfigurationDto>> ListEnvironmentConfigurationsAsync()`
Lists the configurations of this application.

- **Behavior:** Sends `GET /api/v1/configurations?applicationId={applicationId}&pageSize=100` and returns `PagedResult<ConfigurationDto>.Items`. Only one page is requested.
- **Returns:** Configurations for the application (at most 100).
- **Exceptions:** `HttpRequestException` on a non-success status code.

#### `public async Task<int> PromoteAsync(string sourceEnvironment, string targetEnvironment, bool overwrite = true, List<string> keyFilter = null)`
Copies keys from the source environment to the target environment.

- **Parameters:**
  - `sourceEnvironment`, `targetEnvironment` – environment names.
  - `overwrite` – when `false`, keys that already exist in the target are skipped. Default `true`.
  - `keyFilter` – when non-null and non-empty, only keys whose names appear in this list are promoted. `null` or an empty list means all keys.
- **Behavior:**
  1. Writes a progress line to the console.
  2. Gets or creates the source configuration, then fetches its details (`GET /api/v1/configurations/{id}`).
  3. Gets or creates the target configuration, then fetches its details.
  4. For each candidate key, sends `POST /api/v1/configurations/{targetId}/keys` with `key`, `value`, `isEncrypted` and `description` copied from the source.
  5. Prints one line per key (promoted, skipped, or failed) and a summary line.
- **Returns:** The number of keys the server accepted (2xx responses).
- **Exceptions:** `HttpRequestException` if fetching either configuration fails. Per-key failures (non-success status or exception) are printed and do not throw; those keys are not counted.

#### `public async Task DisplayEnvironmentComparisonAsync(string env1, string env2)`
Prints a table comparing the keys of two environments.

- **Behavior:** Gets or creates both configurations, fetches their details, and prints every key name present in either environment (sorted) with both values. A key missing from one side is shown as `[NOT SET]`. Rows whose values differ are marked with `→`.
- **Exceptions:** `HttpRequestException` if fetching either configuration fails.

#### `public async Task SynchronizeKeyAsync(string keyName, string value, bool isEncrypted = false)`
Writes the same key into every existing configuration of the application.

- **Parameters:**
  - `keyName`, `value`, `isEncrypted` – the key to write. The description is set to `Synchronized key`.
- **Behavior:** Lists the configurations (first page only) and sends `POST /api/v1/configurations/{id}/keys` to each. Prints one line per environment with the outcome. Does not create missing environments.
- **Exceptions:** `HttpRequestException` only if the list request fails. Per-environment failures are printed and do not throw.

#### `public async Task DisplayPromotionWorkflowAsync()`
Prints the existing configurations in promotion order, followed by the recommended workflow.

- **Behavior:** Lists configurations and prints each one's environment, ID, key count and creation time, ordered by environment rank (see below). Then prints the workflow: develop in Development, promote to Staging, run integration tests in Staging, promote to Production. Makes no changes on the server.
- **Ordering:** Case-insensitive match on the environment name: `development`/`dev` → 0, `staging`/`stage` → 1, `production`/`prod` → 2. Any other name → 999 (sorted last). Ties keep the server's order.
- **Exceptions:** `HttpRequestException` if the list request fails.

### Data Transfer Objects

All are public, `sealed`, and have public settable properties. They are deserialized with case-insensitive property names.

#### `ConfigurationDto`
- `string Id` – configuration identifier.
- `string Environment` – environment name.
- `string Description` – description.
- `int? KeyCount` – number of keys; `null` when not computed.
- `DateTime CreatedAt` – creation timestamp.

#### `ConfigurationDetailsDto`
- `string Id` – configuration identifier.
- `string Environment` – environment name.
- `List<ConfigurationKeyDto> Keys` – the keys of this configuration.

#### `ConfigurationKeyDto`
- `string Key` – key name.
- `string Value` – value as returned by the server.
- `bool IsEncrypted` – whether the value is stored encrypted.
- `string Description` – description of the key.

#### `PagedResult<T>`
- `List<T> Items` – page of results.
- `int TotalCount` – total count reported by the server. The manager does not read it.

## Usage

### Example 1: Get or create a configuration and list its keys

```csharp
var manager = new MultiEnvironmentManager("https://localhost:5001", appId);
string stagingId = await manager.GetOrCreateEnvironmentConfigAsync("Staging");

var configurations = await manager.ListEnvironmentConfigurationsAsync();
foreach (var config in configurations)
{
    Console.WriteLine($"Environment: {config.Environment}, Keys: {config.KeyCount}");
}
```

The manager has no method that returns a single configuration's keys. To read keys, use the `ConfigurationDetailsDto` returned for a configuration ID through the server's `GET /api/v1/configurations/{id}` endpoint, or call `DisplayEnvironmentComparisonAsync` for a printed view.

### Example 2: Promote keys and display the workflow

```csharp
var manager = new MultiEnvironmentManager("https://localhost:5001", appId);

await manager.DisplayPromotionWorkflowAsync();

int promoted = await manager.PromoteAsync("Staging", "Production", overwrite: false);
Console.WriteLine($"Promoted {promoted} keys to Production.");

await manager.DisplayEnvironmentComparisonAsync("Staging", "Production");
```

### Example 3: Promote only selected keys

```csharp
await manager.PromoteAsync(
    "Staging",
    "Production",
    overwrite: false,
    keyFilter: new List<string> { "Database:Host", "Features:EnableNewCheckout" });
```

### Example 4: Write one key into every environment

```csharp
await manager.SynchronizeKeyAsync("Features:MaintenanceMode", "false");
```

## Edge Cases

- **Environment names are exact matches.** Lookup is case-sensitive ordinal comparison. `"staging"` and `"Staging"` are different environments, so requesting `"staging"` creates a second configuration.
- **Only the first 100 configurations are seen.** The list request uses `pageSize=100` and never fetches further pages. With more than 100 configurations, `GetOrCreateEnvironmentConfigAsync` can create a duplicate for an environment that exists on a later page, and `SynchronizeKeyAsync` skips the configurations past the first page.
- **Reads can create configurations.** `PromoteAsync` and `DisplayEnvironmentComparisonAsync` call `GetOrCreateEnvironmentConfigAsync` for both environments. A misspelled environment name therefore creates an empty configuration on the server rather than failing.
- **Promoting an environment to itself** re-sends every candidate key to the same configuration. With `overwrite: true` the returned count equals the number of keys, and no values change.
- **`overwrite: false` uses a snapshot.** The set of existing target keys is read once at the start. If another writer adds a key during the run, it can be overwritten.
- **Per-key failures are silent to the caller.** `PromoteAsync` and `SynchronizeKeyAsync` report failures only on the console and in the return count. Check the return value of `PromoteAsync` against the expected number of keys.
- **Duplicate key names in the target** make `PromoteAsync` throw `ArgumentException` when it builds its lookup dictionary. `DisplayEnvironmentComparisonAsync` does not fail in this case; it shows the first matching key.
- **Null collections are not guarded.** If the server returns `null` for `Items`, `Keys`, or the body of a response, the manager throws `NullReferenceException`. `GetOrCreateEnvironmentConfigAsync` dereferences the deserialized object without a null check.
- **Encrypted values are not protected by the manager.** `PromoteAsync` copies `Value` exactly as returned by the server and does not decrypt or re-encrypt it. `DisplayEnvironmentComparisonAsync` prints `Value` without masking, so encrypted values appear in plain text in the console. `DisplayPromotionWorkflowAsync` and Example 1 do not print values.
- **`SynchronizeKeyAsync` writes to every environment, including Production.** A single call changes all of them, with no confirmation step and no per-environment selection.
- **`KeyCount` null is shown as `0`.** `DisplayPromotionWorkflowAsync` prints `0` for a configuration whose key count was not computed, which can be mistaken for an empty configuration.
- **`applicationId` is not URL-escaped.** It is concatenated into the list query string as-is. Use identifiers that are safe in a query string (GUIDs are).
- **No credentials are sent.** The manager sets no authentication headers. If the server requires authentication, every request fails with `HttpRequestException`.
- **No cancellation and default timeout.** No method accepts a `CancellationToken`. The `HttpClient` uses its default timeout of 100 seconds.
- **Concurrency.** After construction the manager holds no mutable state beyond its `HttpClient`, so concurrent calls do not corrupt it. Console output from concurrent calls interleaves, and concurrent promotions to the same target can race as described above.
- **Console-only output.** Every progress and error message goes to `Console`. There is no logger or return value for the display methods, and the output format is not stable across versions.
