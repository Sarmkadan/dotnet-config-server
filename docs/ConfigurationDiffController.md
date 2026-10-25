# ConfigurationDiffController

The `ConfigurationDiffController` provides HTTP endpoints for comparing two versions of a configuration. It returns either the full list of added, modified and deleted keys (`ConfigurationDiff`) or an aggregated count-only view (`ConfigurationDiffSummary`). Both endpoints verify that the requested versions belong to the configuration in the route before any comparison runs.

## Route

All endpoints are nested under the configuration they belong to:

```
api/v1/configurations/{configurationId}/ConfigurationDiff
```

The controller is annotated with `[ApiController]` and `[Produces("application/json")]`, so responses are always JSON.

## API

### `ConfigurationDiffController`
The controller class exposing the diff endpoints. It is `sealed` and derives from `ControllerBase`.

**Dependencies (constructor-injected)**
- `IDiffService`: generates diffs and summaries.
- `IVersioningService`: loads versions to check their ownership.
- `ILogger<ConfigurationDiffController>`: structured logging.

Constructor arguments must not be null; otherwise `ArgumentNullException` is thrown.

---

### `public async Task<IActionResult> GetConfigurationDiff([FromRoute] Guid configurationId, [FromRoute] Guid fromVersionId, [FromRoute] Guid toVersionId)`
Returns the detailed diff between two versions of a configuration, including every changed key.

**Route**
- `GET /api/v1/configurations/{configurationId}/ConfigurationDiff/compare/{fromVersionId}/{toVersionId}`

**Parameters**
- `configurationId`: The configuration that both versions must belong to.
- `fromVersionId`: The source (older) version. It is the "left side" of the diff.
- `toVersionId`: The target (newer) version. It is the "right side" of the diff.

**Behavior**
- Loads both versions through `IVersioningService.GetVersionAsync`.
- Returns `404 NotFound` if either version does not exist.
- Returns `404 NotFound` if either version belongs to a different configuration than `configurationId`.
- Calls `IDiffService.GenerateDiffAsync` with the caller's identity (`User.Identity.Name`, or `"system"` when the caller is anonymous). Comparison is exact and case-sensitive; whitespace is not normalized.

**Return value**
- `200 OK` with a `ConfigurationDiff` body on success.
- `404 NotFound` if a version is missing, does not belong to the configuration, or the service throws `ConfigurationNotFoundException`.
- `500 Internal Server Error` for any other unhandled exception.

**Response body (200)**

```json
{
  "id": "3f2a8c1e-9b7d-4e51-a2c3-0d1e2f3a4b5c",
  "configurationId": "7a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d",
  "fromVersionId": "11111111-2222-3333-4444-555555555555",
  "toVersionId": "66666666-7777-8888-9999-000000000000",
  "createdAt": "2026-10-20T12:34:56Z",
  "createdBy": "alice",
  "totalChanges": 2,
  "addedCount": 1,
  "modifiedCount": 1,
  "deletedCount": 0,
  "changes": [
    {
      "id": "d4e5f6a7-b8c9-4d0e-8f1a-2b3c4d5e6f70",
      "diffId": "3f2a8c1e-9b7d-4e51-a2c3-0d1e2f3a4b5c",
      "key": "ApiEndpoint",
      "changeType": 1,
      "oldValue": "https://api.example.com",
      "newValue": "https://new-api.example.com",
      "createdAt": "2026-10-20T12:34:56Z",
      "origin": 0
    },
    {
      "id": "e5f6a7b8-c9d0-4e1f-9a2b-3c4d5e6f7081",
      "diffId": "3f2a8c1e-9b7d-4e51-a2c3-0d1e2f3a4b5c",
      "key": "LogLevel",
      "changeType": 0,
      "oldValue": null,
      "newValue": "Debug",
      "createdAt": "2026-10-20T12:34:56Z",
      "origin": 0
    }
  ]
}
```

**Error body (404 / 500)**

```json
{ "error": "One or both versions not found" }
```

Possible `error` messages:
- `"One or both versions not found"`
- `"Versions do not belong to the specified configuration"`
- `"Configuration or version not found"` (from `ConfigurationNotFoundException`)
- `"Internal server error"` (500 only)

---

### `public async Task<IActionResult> GetConfigurationDiffSummary([FromRoute] Guid configurationId, [FromRoute] Guid version1Id, [FromRoute] Guid version2Id)`
Returns only the counts of changes between two versions. It does not list individual keys, which makes it suitable for dashboards and quick checks.

**Route**
- `GET /api/v1/configurations/{configurationId}/ConfigurationDiff/summary/{version1Id}/{version2Id}`

**Parameters**
- `configurationId`: The configuration that both versions must belong to.
- `version1Id`: First version identifier.
- `version2Id`: Second version identifier. Unlike `GetConfigurationDiff`, the route does not encode a direction, so the order is the order passed to `IDiffService.ComparVersionsAsync`.

**Behavior**
- Performs the same existence and ownership checks as `GetConfigurationDiff`.
- Calls `IDiffService.ComparVersionsAsync` with exact (non-whitespace-normalizing) comparison.
- If a diff for the same version pair already exists, the service returns its stored summary instead of recomputing it.

**Return value**
- `200 OK` with a `ConfigurationDiffSummary` body on success.
- `404 NotFound` for missing versions, ownership mismatch, or `ConfigurationNotFoundException`.
- `500 Internal Server Error` for any other unhandled exception.

**Response body (200)**

```json
{
  "id": "9c8b7a6d-5e4f-4321-8765-4321fedcba09",
  "totalChanges": 2,
  "addedCount": 1,
  "modifiedCount": 1,
  "deletedCount": 0,
  "createdAt": "2026-10-20T12:34:56Z",
  "createdBy": "system"
}
```

---

### `public override string ToString()`
Returns a short description of the controller instance in the form `ConfigurationDiffController { FromVersionId = ..., ToVersionId = ... }`. It is intended for diagnostics and is not part of the HTTP API.

## Models

### `ConfigurationDiff` (response of `compare`)
| Property | Type | Description |
|---|---|---|
| `id` | `Guid` | Identifier of the diff. |
| `configurationId` | `Guid` | Configuration the diff describes. |
| `fromVersionId` | `Guid` | Source (older) version. |
| `toVersionId` | `Guid` | Target (newer) version. |
| `createdAt` | `DateTime` (UTC) | When the diff was generated. |
| `createdBy` | `string` | Caller identity recorded when the diff was generated. |
| `totalChanges` | `int` | Sum of added, modified and deleted entries. |
| `addedCount` / `modifiedCount` / `deletedCount` | `int` | Counts per change type. |
| `changes` | `DiffEntry[]` | One entry per changed key. |

Each `DiffEntry` has `id`, `diffId`, `key`, `changeType`, `oldValue`, `newValue` (nullable), `createdAt`, and `origin`.

- `changeType` is serialized as the numeric `ChangeType` value: `0` = `Added`, `1` = `Modified`, `2` = `Deleted`.
- `origin` is the numeric `DiffEntryOrigin` value: `0` = `Direct`, `1` = `Inherited`, `2` = `Overridden`.

### `ConfigurationDiffSummary` (response of `summary`)
| Property | Type | Description |
|---|---|---|
| `id` | `Guid` | Identifier of the summarized diff. |
| `totalChanges` | `int` | Sum of added, modified and deleted counts. |
| `addedCount` / `modifiedCount` / `deletedCount` | `int` | Counts per change type. |
| `createdAt` | `DateTime` (UTC) | When the summary was produced. |
| `createdBy` | `string` | Who created the summary. |

## Usage

```csharp
// Example 1: Fetch the full diff between two versions
var client = factory.CreateClient();
var configurationId = Guid.Parse("7a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

var response = await client.GetAsync(
    $"/api/v1/configurations/{configurationId}/ConfigurationDiff/compare/{fromVersionId}/{toVersionId}");
response.EnsureSuccessStatusCode();

var diff = await response.Content.ReadFromJsonAsync<ConfigurationDiff>();
foreach (var change in diff!.Changes)
{
    Console.WriteLine($"{change.Key}: {change.ChangeType}");
}
```

```csharp
// Example 2: Check only how many keys changed, without loading each entry
var summaryResponse = await client.GetAsync(
    $"/api/v1/configurations/{configurationId}/ConfigurationDiff/summary/{version1Id}/{version2Id}");

if (summaryResponse.StatusCode == HttpStatusCode.NotFound)
{
    // Versions missing or not part of this configuration
    return;
}

summaryResponse.EnsureSuccessStatusCode();
var summary = await summaryResponse.Content.ReadFromJsonAsync<ConfigurationDiffSummary>();
if (summary!.TotalChanges > 0)
{
    Console.WriteLine($"{summary.TotalChanges} changes detected");
}
```

## Notes

- Both endpoints check version ownership before calling the diff service. A version that exists under another configuration returns `404`, not its diff.
- `GetConfigurationDiff` always uses exact comparison. Whitespace-insensitive comparison is available on `IDiffService` but is not exposed through this controller.
- `GetConfigurationDiffSummary` may return a previously stored summary for the same version pair, so its `createdAt` and `createdBy` reflect the original computation.
- The `FromVersionId` and `ToVersionId` properties on the controller always return `Guid.Empty`. They are not bound from the request and do not affect either endpoint; the versions come from the route parameters only.
- The `GetConfigurationDiffRequest` class declared in the same file is not used by any action. Both endpoints take their input from the route, not from a request body.
- Unhandled exceptions are logged with the configuration and version identifiers and are returned as `500` with a generic message, so internal details are not exposed to clients.
