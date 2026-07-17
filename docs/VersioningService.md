# VersioningService

The `VersioningService` class provides operations for managing configuration versions within the dotnet-config-server system. It supports the full lifecycle of a configuration version: creation, retrieval, activation, publishing, archiving, deprecation, rollback, history tracking, and cleanup of old versions. All public methods are asynchronous and return `Task` or `Task<T>`.

## API

### `VersioningService`
The constructor. Initializes a new instance of the `VersioningService`.  
*Parameters*: (none publicly documented)  
*Throws*: May throw if required dependencies are not resolved (e.g., missing repository or configuration).

### `CreateVersionAsync`
Creates a new configuration version.  
```csharp
public async Task<ConfigurationVersion> CreateVersionAsync(string configurationId, VersionData versionData)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration to which the version belongs.  
  - `versionData` – The data for the new version (e.g., content, metadata).  
- **Returns**: The newly created `ConfigurationVersion` object.  
- **Throws**: `ArgumentNullException` if either parameter is `null`; `InvalidOperationException` if the configuration does not exist or if a version with the same label already exists.

### `GetVersionAsync`
Retrieves a specific version by its identifier.  
```csharp
public async Task<ConfigurationVersion?> GetVersionAsync(string versionId)
```
- **Parameters**:  
  - `versionId` – The unique identifier of the version.  
- **Returns**: The `ConfigurationVersion` if found; otherwise `null`.  
- **Throws**: `ArgumentNullException` if `versionId` is `null`.

### `GetVersionsAsync`
Returns all versions for a given configuration.  
```csharp
public async Task<List<ConfigurationVersion>> GetVersionsAsync(string configurationId)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration.  
- **Returns**: A list of `ConfigurationVersion` objects (may be empty).  
- **Throws**: `ArgumentNullException` if `configurationId` is `null`.

### `GetActiveVersionAsync`
Gets the currently active (published) version for a configuration.  
```csharp
public async Task<ConfigurationVersion?> GetActiveVersionAsync(string configurationId)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration.  
- **Returns**: The active `ConfigurationVersion`, or `null` if no version is active.  
- **Throws**: `ArgumentNullException` if `configurationId` is `null`.

### `PublishVersionAsync`
Publishes a version, making it the active version for its configuration.  
```csharp
public async Task<ConfigurationVersion> PublishVersionAsync(string versionId)
```
- **Parameters**:  
  - `versionId` – The identifier of the version to publish.  
- **Returns**: The updated `ConfigurationVersion` with its status set to published.  
- **Throws**: `ArgumentNullException` if `versionId` is `null`; `InvalidOperationException` if the version is already published, archived, or deprecated.

### `ArchiveVersionAsync`
Archives a version, removing it from active use but preserving its history.  
```csharp
public async Task<ConfigurationVersion> ArchiveVersionAsync(string versionId)
```
- **Parameters**:  
  - `versionId` – The identifier of the version to archive.  
- **Returns**: The updated `ConfigurationVersion` with its status set to archived.  
- **Throws**: `ArgumentNullException` if `versionId` is `null`; `InvalidOperationException` if the version is already archived or deprecated.

### `DeprecateVersionAsync`
Marks a version as deprecated, indicating it should no longer be used.  
```csharp
public async Task<ConfigurationVersion> DeprecateVersionAsync(string versionId)
```
- **Parameters**:  
  - `versionId` – The identifier of the version to deprecate.  
- **Returns**: The updated `ConfigurationVersion` with its status set to deprecated.  
- **Throws**: `ArgumentNullException` if `versionId` is `null`; `InvalidOperationException` if the version is already deprecated or archived.

### `RollbackAsync`
Rolls back the active version of a configuration to a specified previous version.  
```csharp
public async Task<ConfigurationVersion> RollbackAsync(string configurationId, string targetVersionId)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration.  
  - `targetVersionId` – The identifier of the version to roll back to.  
- **Returns**: The `ConfigurationVersion` that becomes the new active version after rollback.  
- **Throws**: `ArgumentNullException` if any parameter is `null`; `InvalidOperationException` if the target version does not exist, is not in a valid state (e.g., archived), or if the configuration has no active version.

### `GetVersionHistoryAsync`
Retrieves a summary of all versions for a configuration, typically including metadata such as creation date, status, and author.  
```csharp
public async Task<List<ConfigurationVersionSummary>> GetVersionHistoryAsync(string configurationId)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration.  
- **Returns**: A list of `ConfigurationVersionSummary` objects (may be empty).  
- **Throws**: `ArgumentNullException` if `configurationId` is `null`.

### `CleanupOldVersionsAsync`
Removes old versions that exceed a specified retention count, typically archiving or deleting them.  
```csharp
public async Task<int> CleanupOldVersionsAsync(string configurationId, int retentionCount)
```
- **Parameters**:  
  - `configurationId` – The identifier of the configuration.  
  - `retentionCount` – The number of most recent versions to keep.  
- **Returns**: The number of versions that were cleaned up.  
- **Throws**: `ArgumentNullException` if `configurationId` is `null`; `ArgumentOutOfRangeException` if `retentionCount` is less than zero.

## Usage

### Example 1: Creating and publishing a new version

```csharp
public async Task CreateAndPublishAsync(VersioningService service, string configId)
{
    // Create a new version with some data
    var versionData = new VersionData
    {
        Content = "{\"key\": \"value\"}",
        Label = "v1.0"
    };

    ConfigurationVersion newVersion = await service.CreateVersionAsync(configId, versionData);
    Console.WriteLine($"Created version {newVersion.Id}");

    // Publish it to make it active
    ConfigurationVersion published = await service.PublishVersionAsync(newVersion.Id);
    Console.WriteLine($"Published version {published.Id}, status: {published.Status}");
}
```

### Example 2: Rolling back and cleaning up old versions

```csharp
public async Task RollbackAndCleanupAsync(VersioningService service, string configId)
{
    // Get the version history to find a target for rollback
    List<ConfigurationVersionSummary> history = await service.GetVersionHistoryAsync(configId);
    if (history.Count < 2)
    {
        Console.WriteLine("Not enough versions to rollback.");
        return;
    }

    // Rollback to the second most recent version
    string targetVersionId = history[^2].Id;
    ConfigurationVersion rolledBack = await service.RollbackAsync(configId, targetVersionId);
    Console.WriteLine($"Rolled back to version {rolledBack.Id}");

    // Clean up all but the 5 most recent versions
    int cleaned = await service.CleanupOldVersionsAsync(configId, 5);
    Console.WriteLine($"Cleaned up {cleaned} old version(s).");
}
```

## Notes

- **Thread safety**: This service is not guaranteed to be thread-safe. Concurrent calls to methods that modify the same configuration or version (e.g., `PublishVersionAsync`, `RollbackAsync`, `CleanupOldVersionsAsync`) may lead to race conditions or inconsistent state. External synchronization (e.g., a lock per configuration) is recommended when used in multi-threaded scenarios.
- **Null parameters**: All methods throw `ArgumentNullException` if a required string parameter is `null`. Always validate inputs before calling.
- **Version state transitions**: A version can only be published, archived, or deprecated once. Attempting to change the state of a version that is already in a terminal state (e.g., archived) will throw `InvalidOperationException`.
- **Rollback prerequisites**: `RollbackAsync` requires that the target version exists and is in a state that can become active (typically `draft` or `published`). Archived or deprecated versions cannot be rolled back to.
- **Cleanup behavior**: `CleanupOldVersionsAsync` retains the `retentionCount` most recent versions (by creation date) and removes older ones. The exact removal behavior (soft-delete vs. hard-delete) depends on the underlying storage implementation.
- **Empty results**: `GetVersionsAsync` and `GetVersionHistoryAsync` return an empty list if no versions exist for the given configuration; they do not throw.
- **Active version**: If no version has been published, `GetActiveVersionAsync` returns `null`.
