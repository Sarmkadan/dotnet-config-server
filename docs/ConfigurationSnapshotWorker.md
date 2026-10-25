# ConfigurationSnapshotWorker

The `ConfigurationSnapshotWorker` type is a hosted background service (`BackgroundService`) that periodically creates a snapshot of every configuration in the system. It is registered in `Program.cs` with `AddHostedService<ConfigurationSnapshotWorker>()` and runs for the lifetime of the application. Its settings come from the `DotnetConfigServer:Snapshot` section, bound to `ConfigurationSnapshotOptions`.

## Purpose

A `ConfigurationSnapshot` is a point-in-time record of a configuration's state, used for auditing, rollback, and diffing. Snapshots are normally created by hand through the API. `ConfigurationSnapshotWorker` makes sure a recent snapshot exists for each configuration without manual action, so there is always a recent restore point even when nobody has requested one.

The worker does not compare snapshots or restore anything. It only decides which configurations need a new snapshot and calls `IConfigurationSnapshotService.CreateSnapshotAsync` for them.

## Scheduling behavior

The worker's `ExecuteAsync` loop runs as follows:

1. **Log startup.** It logs the configured `IntervalMinutes` at information level.
2. **Initial delay.** It waits a fixed 30 seconds to let the application finish starting.
3. **Loop.** On each pass the worker:
   - Computes the next run time (`UtcNow + IntervalMinutes`) and logs it.
   - Waits `IntervalMinutes` with `Task.Delay`.
   - Stops if shutdown has been requested. Otherwise it runs `CreateScheduledSnapshotsAsync`.

Consequences of this loop:

- **First run.** The first batch runs about 30 seconds plus `IntervalMinutes` after startup. With the default of 60 minutes, that is roughly 60.5 minutes after the application starts. No snapshots are created at startup.
- **Spacing between batches.** The wait starts only after the previous batch returns. The gap between the start of one batch and the start of the next is therefore `IntervalMinutes` plus the duration of the batch. A long batch pushes the following run back.
- **Error recovery.** If the loop itself throws (for example, the configuration list cannot be loaded), the worker logs the error, waits 1 minute, and continues. Errors for an individual configuration do not reach this path (see below).
- **Shutdown.** When the host stops, the cancellation raised during the wait ends the loop. The worker logs that it has stopped. A batch that is already running checks for cancellation between configurations and stops early.

### Batch behavior

Each batch (`CreateScheduledSnapshotsAsync`) runs these steps:

1. Loads **all** configurations with `IConfigurationRepository.GetAllAsync()`. There is no paging, so every configuration is loaded on each run.
2. Processes the configurations one after another. For each one:
   - If `SkipIfUnchanged` is `true` and a latest snapshot exists (`IConfigurationSnapshotRepository.GetLatestSnapshotAsync`), the worker checks whether the configuration changed after that snapshot was created. If nothing changed, it logs a debug message and skips the configuration.
   - Otherwise it calls `IConfigurationSnapshotService.CreateSnapshotAsync(config.Id, DefaultUserId, DefaultReason)`.
   - A configuration that has no snapshot yet is always snapshotted, regardless of `SkipIfUnchanged`.
3. Catches and logs any exception for a single configuration, then moves on to the next one. One failing configuration does not stop the batch.
4. Logs a summary with the number of snapshots created and the number skipped.

### Change detection

`HasConfigurationChangedSinceAsync` decides whether a configuration has changed since its latest snapshot:

- If the configuration no longer exists, it returns `false` and the configuration is skipped.
- If `config.UpdatedAt` is later than the snapshot's `CreatedAt`, it returns `true`.
- It then tries to check whether any `ConfigurationKey` was updated after the snapshot. This check is currently ineffective; see [Notes](#notes).

## Configuration

Settings are read from the `DotnetConfigServer:Snapshot` section of `appsettings.json` (or environment variables / user secrets that map to the same keys).

| Setting | Type | Default | Constraints | Meaning |
|---------|------|---------|-------------|---------|
| `Enabled` | `bool` | `false` | – | Intended to turn automatic snapshots on or off. **Not read by this worker**; see [Notes](#notes). |
| `IntervalMinutes` | `int` | `60` | 5 to 1440 | Time to wait between batches |
| `DefaultUserId` | `string` | `"system"` | Required, max 256 characters, must not be empty or whitespace | Value stored as the author of each automatic snapshot |
| `DefaultReason` | `string` | `"Automatic scheduled snapshot"` | Max 1024 characters | Reason stored on each automatic snapshot |
| `SkipIfUnchanged` | `bool` | `true` | – | When `true`, configurations with no changes since their latest snapshot are skipped |

Example:

```json
{
  "DotnetConfigServer": {
    "Snapshot": {
      "Enabled": true,
      "IntervalMinutes": 60,
      "DefaultUserId": "system",
      "DefaultReason": "Automatic scheduled snapshot",
      "SkipIfUnchanged": true
    }
  }
}
```

### Validation

The options are validated when the host starts (`ValidateDataAnnotations` and `ValidateOnStart`):

- Data annotations enforce the `IntervalMinutes` range and the `DefaultUserId` and `DefaultReason` length limits.
- `ConfigurationSnapshotOptionsValidator` rejects a `DefaultUserId` that is empty or whitespace.

An invalid value stops the application from starting, and the error message names the property and the offending value.

### Reloading

The worker reads the options once, in its constructor (`options.Value`), and keeps the values for its lifetime. Changing the configuration at runtime has no effect; the application must be restarted.

## Public API

### ConfigurationSnapshotWorker (constructor)

**Purpose**  
Creates the worker. The DI container calls it when the hosted service starts.

**Parameters**  
- `logger`: The `ILogger<ConfigurationSnapshotWorker>` used for structured logging.
- `snapshotService`: The `IConfigurationSnapshotService` that creates snapshots.
- `configurationRepository`: The `IConfigurationRepository` used to list configurations and load them by ID.
- `snapshotRepository`: The `IConfigurationSnapshotRepository` used to find the latest snapshot.
- `options`: The `IOptions<ConfigurationSnapshotOptions>`. Its `Value` is read once.

**Return value**  
A new `ConfigurationSnapshotWorker` object.

**Exceptions**  
- `ArgumentNullException` if any parameter is `null`.

### ExecuteAsync

**Purpose**  
The main loop of the hosted service. It logs startup, waits 30 seconds, then repeatedly waits `IntervalMinutes` and runs a batch until the host requests shutdown. Exceptions from the loop are logged and the loop continues after a 1-minute pause.

**Return value**  
A `Task` that completes when the host stops the service.

**Exceptions**  
- `OperationCanceledException` is caught and ends the loop. This is the normal shutdown path.

### CreateScheduledSnapshotsAsync (private)

**Purpose**  
Runs one batch: loads all configurations and creates a snapshot for each one that needs it.

**Return value**  
A `Task` that completes when the batch has finished or cancellation has stopped it.

**Exceptions**  
- An exception from loading the configuration list propagates to `ExecuteAsync`, which logs it and waits 1 minute.
- Exceptions for an individual configuration are caught and logged, and the batch continues.

### HasConfigurationChangedSinceAsync (private)

**Purpose**  
Decides whether a configuration has changed after a given cutoff time (the latest snapshot's `CreatedAt`).

**Parameters**  
- `configurationId`: The ID of the configuration to check.
- `cutoffDate`: The time to compare against.

**Return value**  
`true` if the configuration itself was updated after `cutoffDate`. `false` if it was not, or if the configuration does not exist.

## Usage

### Registering the worker

The worker is registered in `Program.cs` alongside the other hosted services. Its options are bound and validated in the same file:

```csharp
builder.Services.AddOptions<ConfigurationSnapshotOptions>()
    .Bind(builder.Configuration.GetSection("DotnetConfigServer:Snapshot"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ConfigurationSnapshotOptions>, ConfigurationSnapshotOptionsValidator>();

// ...
builder.Services.AddHostedService<ConfigurationSnapshotWorker>();
```

The worker is added without a check on `Enabled`. Removing the registration is currently the only way to stop it.

### Running a snapshot pass on demand

The worker has no public method to trigger a batch. To snapshot a single configuration immediately, call the snapshot service directly:

```csharp
var snapshot = await snapshotService.CreateSnapshotAsync(configurationId, userId, reason);
```

## Notes

- **`Enabled` is not honored.** The worker starts and creates snapshots regardless of `DotnetConfigServer:Snapshot:Enabled`. By contrast, `EncryptionKeyRotationWorker` checks its `Enabled` flag. Until this is fixed, the flag has no effect on this worker.
- **The key-level change check does nothing.** `HasConfigurationChangedSinceAsync` looks for a public `Context` property on the configuration repository using reflection. `ConfigurationRepository` does not expose one (its `DbContext` is a protected field in `BaseRepository`), so the lookup returns `null` and the key-level query never runs. Only `Configuration.UpdatedAt` is compared. A change that touches only keys, without updating the configuration row, is not detected.
- **Scoped services are injected into a singleton.** A hosted service is registered as a singleton, but the worker's constructor takes `IConfigurationSnapshotService`, `IConfigurationRepository`, and `IConfigurationSnapshotRepository`, which are all registered as scoped. Unlike `WebhookRetryWorker`, this worker does not create a scope per batch. With `ValidateScopes` enabled (the default in Development), the host should refuse to start with a "scoped service from a singleton" error. In other environments, these services, and the `ApplicationDbContext` behind them, are resolved from the root provider and live for the whole application. Changing the worker to create a scope per batch, as `WebhookRetryWorker` does, would resolve this. I have not run the application to confirm the Development behavior.
- **Configurations are processed sequentially and all are loaded at once.** The batch loads every configuration into memory and snapshots them one at a time. Large numbers of configurations make a batch slow, and that delays the next run.
- **Failures are logged but not counted.** A configuration whose snapshot fails is included in neither the `Created` nor the `Skipped` count in the summary log. Look for the "Failed to create snapshot" error entries to find these.
- **Restart to apply changes.** Options are read once at construction; see [Reloading](#reloading).
