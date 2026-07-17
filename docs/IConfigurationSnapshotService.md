# IConfigurationSnapshotService

Provides an abstraction for creating, retrieving, and restoring snapshots of the application’s configuration state. The service enables point‑in-time capture of configuration values, allowing rollback or audit scenarios without affecting the live configuration store.

## API

### ConfigurationSnapshotService()
**Purpose**  
Initializes a new instance of the service.

**Parameters**  
None.

**Return value**  
A ready‑to‑use `IConfigurationSnapshotService` instance.

**Throws**  
- `ObjectDisposedException` if the instance has been disposed prior to use.  
- `InvalidOperationException` if required internal dependencies are missing.

### Task<ConfigurationSnapshot> CreateSnapshotAsync()
**Purpose**  
Captures a snapshot of the current configuration and returns it as a `ConfigurationSnapshot` object.

**Parameters**  
None.

**Return value**  
A `Task` that completes with a `ConfigurationSnapshot` representing the configuration at the moment of the call.

**Throws**  
- `ObjectDisposedException` if the service has been disposed.  
- `InvalidOperationException` if the configuration source is unavailable or unreadable.  
- `OperationCanceledException` if the associated cancellation token is triggered (if one is supplied by the caller).

### Task<ConfigurationSnapshot?> GetSnapshotAsync()
**Purpose**  
Retrieves the most recently stored configuration snapshot, if any exists.

**Parameters**  
None.

**Return value**  
A `Task` that completes with a `ConfigurationSnapshot` instance, or `null` when no snapshot has been persisted.

**Throws**  
- `ObjectDisposedException` if the service has been disposed.  
- `InvalidOperationException` if the underlying snapshot store cannot be accessed.  
- `OperationCanceledException` if a cancellation token is triggered.

### Task<List<ConfigurationSnapshot>> GetConfigurationSnapshotsAsync()
**Purpose**  
Enumerates all persisted configuration snapshots.

**Parameters**  
None.

**Return value**  
A `Task` that completes with a list containing every snapshot known to the service, ordered from oldest to newest. Returns an empty list when no snapshots are present.

**Throws**  
- `ObjectDisposedException` if the service has been disposed.  
- `InvalidOperationException` if the snapshot store is inaccessible.  
- `OperationCanceledException` if a cancellation token is triggered.

### Task RestoreFromSnapshotAsync()
**Purpose**  
Restores the configuration to the state captured by the most recent snapshot.

**Parameters**  
None.

**Return value**  
A `Task` that completes when the restore operation has finished.

**Throws**  
- `ObjectDisposedException` if the service has been disposed.  
- `InvalidOperationException` if no snapshot is available to restore from or if the restore process fails.  
- `OperationCanceledException` if a cancellation token is triggered.

### string Key
**Purpose**  
Gets the configuration key associated with the service instance (if applicable).

**Parameters**  
None.

**Return value**  
The key string; may be `null` or empty when the service is not bound to a specific key.

**Throws**  
None.

### string Value
**Purpose**  
Gets the configuration value associated with the service instance (if applicable).

**Parameters**  
None.

**Return value**  
The value string; may be `null` or empty.

**Throws**  
None.

### bool IsEncrypted
**Purpose**  
Indicates whether the configuration value represented by the service is encrypted.

**Parameters**  
None.

**Return value**  
`true` if the value is encrypted; otherwise `false`.

**Throws**  
None.

### bool IsActive
**Purpose**  
Indicates whether the service instance is currently active and able to process snapshot operations.

**Parameters**  
None.

**Return value**  
`true` when the service is operational; `false` if it has been disposed or otherwise deactivated.

**Throws**  
None.

## Usage

```csharp
using System.Threading.Tasks;
using DotNetConfigServer.Services;

// Assume DI or manual instantiation
var snapshotService = new ConfigurationSnapshotService();

// Create a snapshot of the current configuration
ConfigurationSnapshot snapshot = await snapshotService.CreateSnapshotAsync();
// snapshot can be stored, logged, or used for comparison later

// Retrieve the most recent snapshot (if any)
ConfigurationSnapshot? latest = await snapshotService.GetSnapshotAsync();
if (latest != null)
{
    // Do something with the snapshot, e.g., display its contents
}

// Restore configuration to the state of the latest snapshot
await snapshotService.RestoreFromSnapshotAsync();
```

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetConfigServer.Services;

var service = new ConfigurationSnapshotService();

// List all snapshots that have been persisted
IReadOnlyList<ConfigurationSnapshot> allSnapshots = await service.GetConfigurationSnapshotsAsync();

foreach (var snap in allSnapshots)
{
    // Process each snapshot (e.g., export to backup storage)
    Console.WriteLine($"Snapshot taken at: { snap.Timestamp }");
}

// Example of checking service state before operating
if (service.IsActive && !service.IsEncrypted)
{
    // Proceed with snapshot creation knowing the value is plain text
    await service.CreateSnapshotAsync();
}
```

## Notes

- The service is **not thread‑safe** by default; concurrent calls to any of its members from multiple threads may result in undefined behavior. External synchronization is required when sharing an instance across threads.  
- All asynchronous methods accept an optional `System.Threading.CancellationToken` via the caller’s context; if a token is triggered, the methods will throw `OperationCanceledException`.  
- After disposing the service (via `IDisposable.Dispose` if implemented), any further invocation of its members will throw `ObjectDisposedException`.  
- The `Key`, `Value`, `IsEncrypted`, and `IsActive` properties reflect the state of the underlying configuration entry that the service wraps; they do not change after a snapshot is created or restored unless the underlying configuration source is modified externally.  
- `RestoreFromSnapshotAsync` does not require a snapshot identifier because it operates on the most recent snapshot known to the service; to restore a specific snapshot, the caller must first retrieve it via `GetConfigurationSnapshotsAsync` and then invoke a separate restore overload (not part of this interface).  
- If the underlying configuration store is read‑only, `CreateSnapshotAsync` and `RestoreFromSnapshotAsync` will throw `InvalidOperationException`.  
- Implementations should ensure that snapshots are immutable; mutating a returned `ConfigurationSnapshot` instance after retrieval is not supported and may lead to inconsistent state.
