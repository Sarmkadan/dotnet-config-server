# DiffViewerController

The `DiffViewerController` exposes read-only HTTP endpoints for the diff viewer UI. It returns enriched diffs between two configuration versions, a per-configuration version timeline annotated with change statistics, and a non-destructive rollback preview. All endpoints are nested under a single configuration:

```
/api/v1/configurations/{configurationId}/diff-viewer
```

The controller performs no writes. Rollbacks are not executed here; use the rollback endpoints for that.

## API

### `DiffViewerController`
The controller class is `[ApiController]`, `sealed`, and produces `application/json`. It depends on `IDiffViewerService` for all business logic and on `ILogger<DiffViewerController>` for error logging.

### `public async Task<IActionResult> GetEnrichedDiffAsync([FromRoute] Guid configurationId, [FromRoute] Guid fromVersionId, [FromRoute] Guid toVersionId)`
Returns a diff between two versions enriched with version metadata and grouped change lists.

**Route**
- `GET /api/v1/configurations/{configurationId}/diff-viewer/{fromVersionId}/{toVersionId}`

**Parameters**
- `configurationId`: The configuration that both versions must belong to.
- `fromVersionId`: The identifier of the source (older) version.
- `toVersionId`: The identifier of the target (newer) version.

**Return value**
- `200 OK` with an `EnrichedDiff` body (see [Response shapes](#response-shapes)).
- `404 NotFound` with `{ "error": "One or both versions not found" }` if either version does not exist, or if the resulting diff belongs to a different configuration than `configurationId`.
- `500 InternalServerError` with `{ "error": "Internal server error" }` for any other exception. The exception is logged.

**Notes**
- The service uses a persisted diff record when one exists; otherwise it computes the diff from the current key state without persisting anything.
- A malformed GUID in the route is rejected by ASP.NET Core model binding with `400 BadRequest` before the action runs.

---

### `public async Task<IActionResult> GetVersionTimelineAsync([FromRoute] Guid configurationId)`
Returns every version of a configuration in chronological order, oldest first.

**Route**
- `GET /api/v1/configurations/{configurationId}/diff-viewer/timeline`

**Parameters**
- `configurationId`: The configuration whose version history is requested.

**Return value**
- `200 OK` with a list of `VersionTimelineEntry` objects, oldest to newest.
- `500 InternalServerError` with `{ "error": "Internal server error" }` for any exception. The exception is logged.

**Notes**
- The first entry has `IsFirst = true` and `DiffFromPrevious = null`.
- For every later entry, `DiffFromPrevious` is populated only when a persisted diff record exists between that version and its predecessor; otherwise it is `null`.

---

### `public async Task<IActionResult> GetRollbackPreviewAsync([FromRoute] Guid configurationId, [FromRoute] Guid targetVersionId)`
Previews what a rollback to `targetVersionId` would change relative to the currently active version. Nothing is persisted.

**Route**
- `GET /api/v1/configurations/{configurationId}/diff-viewer/rollback-preview/{targetVersionId}`

**Parameters**
- `configurationId`: The configuration the rollback would apply to.
- `targetVersionId`: The version that would be restored.

**Return value**
- `200 OK` with a `RollbackPreview` body.
- `404 NotFound` with `{ "error": "Version not found" }` if the service throws `ConfigurationNotFoundException` (for example, the target version or the active configuration cannot be found).
- `500 InternalServerError` with `{ "error": "Internal server error" }` for any other exception. The exception is logged.

**Notes**
- `IsRollbackSafe` is `false` when the rollback would remove a key marked as required in the active version. `WarningMessages` describes such issues.

---

## Response shapes

These are the serialized shapes of the models returned by the endpoints. Property names follow the default `System.Text.Json` camelCase convention. Enum values are serialized using the application's configured JSON options.

### `EnrichedDiff`
| Property | Type | Description |
|----------|------|-------------|
| `diffId` | `Guid` | Diff record identifier. A new GUID when the diff is computed on the fly. |
| `configurationId` | `Guid` | Owning configuration. |
| `fromVersion` | `ConfigurationVersionSummary` | Metadata of the source version. |
| `toVersion` | `ConfigurationVersionSummary` | Metadata of the target version. |
| `changes` | `DiffEntry[]` | All individual key changes. |
| `addedCount` | `int` | Keys added in the target version. |
| `modifiedCount` | `int` | Keys whose values changed. |
| `deletedCount` | `int` | Keys removed in the target version. |
| `generatedAt` | `DateTime` (UTC) | When the diff was computed or retrieved. |

`EnrichedDiff` also exposes computed members (`totalChanges`, `addedKeys`, `modifiedKeys`, `deletedKeys`) that are derived from the fields above.

### `RollbackPreview`
| Property | Type | Description |
|----------|------|-------------|
| `configurationId` | `Guid` | Configuration the rollback applies to. |
| `currentVersion` | `ConfigurationVersionSummary?` | Currently active version, or `null` if none is active. |
| `targetVersion` | `ConfigurationVersionSummary` | Version that would be restored. |
| `changes` | `DiffEntry[]` | Key changes the rollback would introduce. |
| `addedCount` | `int` | Keys that would be added. |
| `modifiedCount` | `int` | Keys whose values would change. |
| `deletedCount` | `int` | Keys that would be removed. |
| `isRollbackSafe` | `bool` | `false` if a required key in the active version would be removed. |
| `warningMessages` | `string[]` | Human-readable warnings about potentially breaking changes. |

`RollbackPreview` also exposes `totalChanges`, derived from the three counts.

### `VersionTimelineEntry`
| Property | Type | Description |
|----------|------|-------------|
| `version` | `ConfigurationVersionSummary` | Version at this point in the timeline. |
| `diffFromPrevious` | `ConfigurationDiffSummary?` | Change counts from the preceding version, or `null` when none is persisted or the entry is the first. |
| `isFirst` | `bool` | `true` for the oldest version. |

### `ConfigurationVersionSummary`
| Property | Type | Description |
|----------|------|-------------|
| `id` | `Guid` | Version identifier. |
| `versionNumber` | `string` | Human-readable version number. |
| `status` | `ConfigurationVersionStatus` | `Draft`, `Active`, `Archived`, or `Deprecated`. |
| `keyCount` | `int` | Number of keys in the version. |
| `createdAt` | `DateTime` | Creation timestamp. |
| `publishedAt` | `DateTime?` | Publication timestamp, if published. |
| `createdBy` | `string` | Author of the version. |

### `ConfigurationDiffSummary`
| Property | Type | Description |
|----------|------|-------------|
| `id` | `Guid` | Diff record identifier. |
| `totalChanges` | `int` | Sum of added, modified, and deleted changes. |
| `addedCount` | `int` | Keys added. |
| `modifiedCount` | `int` | Keys modified. |
| `deletedCount` | `int` | Keys deleted. |
| `createdAt` | `DateTime` | When the diff record was created. |
| `createdBy` | `string` | Author of the diff record. |

### `DiffEntry`
| Property | Type | Description |
|----------|------|-------------|
| `id` | `Guid` | Entry identifier. |
| `diffId` | `Guid` | Owning diff record. |
| `key` | `string` | Configuration key that changed. |
| `changeType` | `ChangeType` | `Added` (0), `Modified` (1), or `Deleted` (2). |
| `oldValue` | `string?` | Value before the change. |
| `newValue` | `string?` | Value after the change. |
| `origin` | `DiffEntryOrigin` | `Direct`, `Inherited`, or `Overridden`. |
| `createdAt` | `DateTime` (UTC) | Creation timestamp of the entry. |

---

## Usage

```csharp
// Example 1: Fetch an enriched diff and list added keys
var response = await client.GetAsync(
    $"/api/v1/configurations/{configurationId}/diff-viewer/{fromVersionId}/{toVersionId}");

if (response.StatusCode == HttpStatusCode.NotFound)
{
    // One or both versions do not exist for this configuration
    return;
}

response.EnsureSuccessStatusCode();
var diff = await response.Content.ReadFromJsonAsync<EnrichedDiff>();
var addedKeys = diff!.AddedKeys.Select(c => c.Key);
```

```csharp
// Example 2: Show the timeline and a rollback preview for an older version
var timeline = await client.GetFromJsonAsync<List<VersionTimelineEntry>>(
    $"/api/v1/configurations/{configurationId}/diff-viewer/timeline");

var target = timeline!.Last(e => !e.IsFirst);

var preview = await client.GetFromJsonAsync<RollbackPreview>(
    $"/api/v1/configurations/{configurationId}/diff-viewer/rollback-preview/{target.Version.Id}");

if (!preview!.IsRollbackSafe)
{
    foreach (var warning in preview.WarningMessages)
        Console.WriteLine(warning);
}
```

## Notes

- All endpoints are `GET` and read-only. Rollback previews do not change any state.
- Errors other than the documented `404` cases are returned as `500` with a generic message. Details are written to the server log only.
- The `404` check on `{fromVersionId}/{toVersionId}` also covers a diff that belongs to another configuration, so a caller cannot read versions from a configuration it does not name in the route.
- `GET .../timeline` has no `404` branch: an unknown or empty configuration is reported as `200` with whatever the service returns, or as `500` if the service throws.
- The controller does not pass `configurationId` to `GetEnrichedDiffAsync`; it validates ownership after the call by comparing `diff.ConfigurationId`.
