# VersionsController

The `VersionsController` is an ASP.NET Core controller responsible for managing configuration versions within the `dotnet-config-server` project. It provides endpoints for retrieving, creating, publishing, archiving, and comparing versions of configurations, enabling version control workflows such as rollback, diffing, and cleanup of outdated versions. The controller integrates with the underlying versioning system to track active, archived, and historical states of configurations.

## API

### `Task<IActionResult> GetVersions()`
**Purpose**: Retrieves a list of all available versions for a configuration.
**Parameters**: None.
**Return Value**: An `IActionResult` containing a collection of version metadata (e.g., version identifiers, timestamps, status).
**Throws**:
- `InvalidOperationException` if the configuration does not exist or cannot be accessed.

---

### `Task<IActionResult> GetActiveVersion()`
**Purpose**: Returns the currently active version of a configuration.
**Parameters**: None.
**Return Value**: An `IActionResult` containing the active version's metadata and content.
**Throws**:
- `InvalidOperationException` if no active version exists or the configuration is not found.

---

### `Task<IActionResult> GetVersion()`
**Purpose**: Retrieves a specific version of a configuration by its identifier.
**Parameters**: Implicitly accepts a version identifier (e.g., via route parameters).
**Return Value**: An `IActionResult` containing the requested version's metadata and content.
**Throws**:
- `KeyNotFoundException` if the specified version does not exist.
- `InvalidOperationException` if the configuration is not found.

---

### `Task<IActionResult> CreateVersion()`
**Purpose**: Creates a new version of a configuration with the provided content.
**Parameters**: Accepts version content (e.g., via request body) and optional metadata (e.g., release notes).
**Return Value**: An `IActionResult` confirming the creation of the new version, including its identifier.
**Throws**:
- `ArgumentNullException` if the request body is empty or invalid.
- `InvalidOperationException` if the configuration cannot be modified.

---

### `Task<IActionResult> PublishVersion()`
**Purpose**: Publishes a specified version, making it the active version.
**Parameters**: Implicitly accepts a version identifier (e.g., via route parameters).
**Return Value**: An `IActionResult` confirming the version was published.
**Throws**:
- `KeyNotFoundException` if the version does not exist.
- `InvalidOperationException` if the version is already active or cannot be published.

---

### `Task<IActionResult> ArchiveVersion()`
**Purpose**: Archives a specified version, removing it from the active version pool but retaining it for historical reference.
**Parameters**: Implicitly accepts a version identifier (e.g., via route parameters).
**Return Value**: An `IActionResult` confirming the version was archived.
**Throws**:
- `KeyNotFoundException` if the version does not exist.
- `InvalidOperationException` if the version is already archived or cannot be archived.

---

### `Task<IActionResult> Rollback()`
**Purpose**: Reverts the active version to a previously published version.
**Parameters**: Implicitly accepts a target version identifier (e.g., via route parameters).
**Return Value**: An `IActionResult` confirming the rollback operation.
**Throws**:
- `KeyNotFoundException` if the target version does not exist.
- `InvalidOperationException` if the target version cannot be rolled back to (e.g., it is archived or invalid).

---

### `Task<IActionResult> GetDiff()`
**Purpose**: Compares two versions of a configuration and returns the differences between them.
**Parameters**: Implicitly accepts two version identifiers (e.g., via route/query parameters).
**Return Value**: An `IActionResult` containing a structured diff (e.g., added, removed, or modified keys/values).
**Throws**:
- `KeyNotFoundException` if either version does not exist.
- `InvalidOperationException` if the versions cannot be compared (e.g., incompatible formats).

---

### `Task<IActionResult> Cleanup()`
**Purpose**: Permanently deletes archived versions that exceed the retention limit (`ArchivedCount`).
**Parameters**: None.
**Return Value**: An `IActionResult` confirming the number of versions deleted.
**Throws**:
- `InvalidOperationException` if cleanup fails (e.g., due to concurrent modifications).

---

### `string? ReleaseNotes`
**Purpose**: Stores or retrieves release notes associated with the most recent version operation (e.g., creation, publishing).
**Parameters**: None (property accessor).
**Return Value**: A nullable string containing the release notes, or `null` if none exist.
**Throws**: None.

---

### `int ArchivedCount`
**Purpose**: Specifies the maximum number of archived versions to retain before cleanup.
**Parameters**: None (property accessor).
**Return Value**: An integer representing the retention limit.
**Throws**: None.

## Usage

### Example 1: Creating and Publishing a New Version
