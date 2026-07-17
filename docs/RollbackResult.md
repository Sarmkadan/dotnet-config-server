# RollbackResult

Represents the outcome of a rollback operation in the configuration server, capturing both the new version created as a result of the rollback and the historical version that was restored. This record provides a complete audit trail of the rollback action, including who performed it, when it occurred, and how many configuration keys were affected.

## API

### Properties

#### `Guid Id`
Gets the unique identifier for this rollback result record.

#### `Guid ConfigurationId`
Gets the unique identifier of the configuration that was rolled back.

#### `ConfigurationVersionSummary NewVersion`
Gets the newly created version that represents the state after the rollback. This version contains the same configuration values as the restored version but with a new version identifier and timestamp.

#### `ConfigurationVersionSummary RestoredFromVersion`
Gets the historical version that was restored during the rollback. This is the source version whose configuration values were copied into the new version.

#### `Guid NewVersionId`
Gets the unique identifier of the newly created version resulting from the rollback. This corresponds to `NewVersion.Id`.

#### `Guid RestoredFromVersionId`
Gets the unique identifier of the historical version that was restored. This corresponds to `RestoredFromVersion.Id`.

#### `string Reason`
Gets the reason provided for performing the rollback. This is a human-readable explanation of why the configuration was reverted to a previous state.

#### `string PerformedBy`
Gets the identity of the user or system that initiated the rollback operation.

#### `DateTime PerformedAt`
Gets the UTC timestamp indicating when the rollback operation was completed.

#### `int KeysRestored`
Gets the number of configuration keys that were restored to their previous values as part of the rollback operation.

## Usage

### Performing a Rollback and Inspecting the Result

```csharp
// Assume configurationService is an instance of IConfigurationService
var configurationId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
var targetVersionId = Guid.Parse("f9e8d7c6-b5a4-3210-fedc-ba0987654321");

RollbackResult result = await configurationService.RollbackAsync(
    configurationId,
    targetVersionId,
    reason: "Deployment failed in production - reverting to stable version",
    performedBy: "ops-team@example.com"
);

Console.WriteLine($"Rollback completed successfully.");
Console.WriteLine($"New version created: {result.NewVersion.VersionNumber}");
Console.WriteLine($"Restored from version: {result.RestoredFromVersion.VersionNumber}");
Console.WriteLine($"Keys restored: {result.KeysRestored}");
Console.WriteLine($"Performed by: {result.PerformedBy} at {result.PerformedAt:O}");
```

### Auditing Rollback Operations

```csharp
// Retrieve and log rollback history for compliance auditing
var configurationId = 456;
var rollbackHistory = await auditService.GetRollbackResultsAsync(configurationId);

foreach (var rollback in rollbackHistory.OrderByDescending(r => r.PerformedAt))
{
    var auditEntry = new
    {
        RollbackId = rollback.Id,
        ConfigurationId = rollback.ConfigurationId,
        RestoredFromVersion = rollback.RestoredFromVersion.VersionNumber,
        NewVersion = rollback.NewVersion.VersionNumber,
        KeysAffected = rollback.KeysRestored,
        Reason = rollback.Reason,
        Operator = rollback.PerformedBy,
        Timestamp = rollback.PerformedAt
    };

    await auditLogger.LogAsync(auditEntry);
}
```

## Notes

- The `NewVersion` and `RestoredFromVersion` properties reference distinct version objects, even though they contain identical configuration data. The `NewVersion` is a fresh version with its own identifier and creation timestamp, while `RestoredFromVersion` retains its original metadata from when it was first created.
- `KeysRestored` reflects the count of configuration keys that differed between the current version and the restored version at the time of rollback. If no keys had changed, this value may be zero.
- The `PerformedAt` timestamp is recorded in UTC and represents the moment the rollback transaction was committed, not when it was initiated.
- This type is a data transfer object and is not designed for concurrent modification. Instances are typically created by the server and returned to callers as immutable snapshots of completed operations.
- The `Reason` property may be empty or null if the rollback was initiated programmatically without a human-readable justification. Consumers should handle this case gracefully when displaying audit information.
