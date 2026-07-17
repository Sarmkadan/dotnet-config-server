# VersioningAndRollback

Provides version management capabilities for configuration data in the dotnet-config-server system. It enables creation, enumeration, publishing, comparison, rollback, archiving, and deployment of configuration versions, as well as utilities for displaying history and differences.

## API

### CreateVersionAsync
```csharp
public async Task<ConfigurationVersion> CreateVersionAsync()
```
Creates a new configuration version from the current configuration source.  
**Return value:** A `ConfigurationVersion` instance representing the newly created version.  
**Throws:**  
- `InvalidOperationException` if the configuration source cannot be read.  
- `UnauthorizedAccessException` if the caller lacks permission to create a version.

### ListVersionsAsync
```csharp
public async Task<List<ConfigurationVersion>> ListVersionsAsync()
```
Retrieves all known configuration versions, ordered by version number descending.  
**Return value:** A list of `ConfigurationVersion` objects.  
**Throws:**  
- `IOException` if the underlying storage cannot be accessed.

### GetActiveVersionAsync
```csharp
public async Task<ConfigurationVersion> GetActiveVersionAsync()
```
Gets the version that is currently marked as active (published).  
**Return value:** The active `ConfigurationVersion`, or `null` if no version has been published.  
**Throws:**  
- `InvalidOperationException` if the internal state is inconsistent (e.g., multiple versions marked active).

### PublishVersionAsync
```csharp
public async Task<ConfigurationVersion> PublishVersionAsync()
```
Publishes the version represented by this instance as the active configuration.  
**Return value:** The published `ConfigurationVersion`.  
**Throws:**  
- `InvalidOperationException` if the version is already published or does not exist in the store.  
- `UnauthorizedAccessException` if publishing is not permitted for the caller.

### CompareVersionsAsync
```csharp
public async Task<List<ConfigurationDiff>> CompareVersionsAsync()
```
Compares the active version with the version represented by this instance and returns the differences.  
**Return value:** A list of `ConfigurationDiff` objects describing the changes.  
**Throws:**  
- `ArgumentException` if either version cannot be found.  
- `InvalidOperationException` if the comparison cannot be performed (e.g., missing data).

### RollbackVersionAsync
```csharp
public async Task RollbackVersionAsync()
```
Rolls back the active configuration to the version represented by this instance.  
**Return value:** None.  
**Throws:**  
- `InvalidOperationException` if the target version is not eligible for rollback (e.g., archived) or if no active version exists.  
- `UnauthorizedAccessException` if rollback is disallowed for the caller.

### ArchiveVersionAsync
```csharp
public async Task ArchiveVersionAsync()
```
Marks the version represented by this instance as archived, removing it from active rotation.  
**Return value:** None.  
**Throws:**  
- `IOException` if the archival operation fails.  
- `InvalidOperationException` if the version is already archived.

### DisplayVersionHistoryAsync
```csharp
public async Task DisplayVersionHistoryAsync()
```
Outputs a formatted history of all configuration versions to the standard output (or configured logger).  
**Return value:** None.  
**Throws:** No documented exceptions; failures in logging are swallowed per logger configuration.

### DisplayDifferencesAsync
```csharp
public async Task DisplayDifferencesAsync()
```
Displays the differences between the active version and the version represented by this instance in a human‑readable format.  
**Return value:** None.  
**Throws:** No documented exceptions; errors in formatting are logged internally.

### BlueGreenDeploymentAsync
```csharp
public async Task<string> BlueGreenDeploymentAsync()
```
Executes a blue‑green deployment strategy using the current and staged versions.  
**Return value:** A string identifier for the deployment (e.g., deployment ID or endpoint).  
**Throws:**  
- `DeploymentException` if the deployment fails (e.g., health checks do not pass).  
- `InvalidOperationException` if a staged version is not available.

### CanaryDeploymentAsync
```csharp
public async Task CanaryDeploymentAsync()
```
Performs a canary deployment, gradually shifting traffic to the new version.  
**Return value:** None.  
**Throws:**  
- `DeploymentException` if the canary rollout encounters errors.  
- `InvalidOperationException` if no candidate version exists for canary.

### Id
```csharp
public string Id { get; }
```
Unique identifier for the configuration version (typically a GUID or similar).

### Version
```csharp
public int Version { get; }
```
Sequential numeric version number.

### Status
```csharp
public string Status { get; }
```
Current lifecycle status of the version (e.g., `"Draft"`, `"Published"`, `"Archived"`).

### KeyCount
```csharp
public int KeyCount { get; }
```
Number of distinct configuration keys contained in this version.

### Description
```csharp
public string Description { get; }
```
User‑provided description of the version’s purpose or changes.

### CreatedAt
```csharp
public DateTime CreatedAt { get; }
```
Timestamp indicating when the version was initially created.

### PublishedAt
```csharp
public DateTime PublishedAt { get; }
```
Timestamp indicating when the version was published; if the version has never been published, the value is `DateTime.MinValue`.

### Key
```csharp
public string Key { get; }
```
The primary configuration key associated with this version (relevant for single‑key versions; otherwise may be empty or null).

## Usage

### Example 1: Creating and publishing a new version
```csharp
using System;
using System.Threading.Tasks;
using DotNetConfigServer;

class Program
{
    static async Task Main()
    {
        var manager = new VersioningAndRollback(); // Assume appropriate constructor/di

        // Create a new version from current configuration
        var newVersion = await manager.CreateVersionAsync();
        Console.WriteLine($"Created version {newVersion.Version} (Id: {newVersion.Id})");

        // Publish the newly created version
        var published = await manager.PublishVersionAsync();
        Console.WriteLine($"Published version {published.Version} at {published.PublishedAt}");
    }
}
```

### Example 2: Listing versions, comparing, and rolling back
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetConfigServer;

class Program
{
    static async Task Main()
    {
        var manager = new VersioningAndRollback();

        // List all known versions
        IReadOnlyList<ConfigurationVersion> versions = await manager.ListVersionsAsync();
        Console.WriteLine($"Found {versions.Count} versions:");
        foreach (var v in versions)
        {
            Console.WriteLine($"  v{v.Version} ({v.Status}) - {v.CreatedAt}");
        }

        // Get the active version
        var active = await manager.GetActiveVersionAsync();
        if (active != null)
        {
            Console.WriteLine($"Active version: v{active.Version}");
        }

        // Compare active version with the latest draft (assuming the first list item is latest)
        var latest = versions[0];
        if (latest.Status != "Published")
        {
            var diffs = await manager.CompareVersionsAsync(); // Compares active vs latest
            Console.WriteLine($"Differences: {diffs.Count} changes");
        }

        // Rollback to the previous version if needed
        if (active?.Version > 1)
        {
            var target = versions.Find(v => v.Version == active.Version - 1);
            if (target != null)
            {
                // Temporarily assign target's Id/Version to manager instance for rollback
                // (In a real implementation, rollback would accept a version identifier)
                await manager.RollbackVersionAsync();
                Console.WriteLine($"Rolled back to version {target.Version}");
            }
        }
    }
}
```

## Notes
- **Edge Cases:**  
  - Calling `PublishVersionAsync` on a version already marked as published throws `InvalidOperationException`.  
  - `RollbackVersionAsync` will throw if the target version is archived or if no active version exists.  
  - `CompareVersionsAsync` requires both the active and the instance version to be present in the store; missing data results in `ArgumentException`.  
  - Display methods (`DisplayVersionHistoryAsync`, `DisplayDifferencesAsync`) are intended for console or logging output; they suppress I/O errors and rely on the configured logger’s error handling.  
- **Thread‑Safety:**  
  - The `VersioningAndRollback` instance does not synchronize internal state; concurrent calls on the same instance may lead to race conditions.  
  - It is safe to invoke methods on different instances concurrently, provided they operate on distinct version identifiers.  
  - Shared resources such as the configuration store are accessed via underlying services; consumers should ensure those services are thread‑safe or provide external synchronization when needed.
