# ConfigurationSnapshot

A `ConfigurationSnapshot` represents an immutable record of a configuration’s state at a specific point in time. It captures the serialized configuration data, a hash of its keys, and audit information such as who created the snapshot and why. Snapshots are primarily used for auditing, rollback, and diffing configuration changes within the dotnet‑config‑server system.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `Id` | `Guid` | Unique identifier for this snapshot instance. | None | The snapshot’s identifier. | Property access does not throw exceptions. |
| `ConfigurationId` | `Guid` | Identifier of the `Configuration` to which this snapshot belongs. | None | The configuration identifier. | Property access does not throw exceptions. |
| `ConfigurationState` | `string` | Serialized representation of the full configuration payload (e.g., JSON or XML). | None | The configuration state string. | Property access does not throw exceptions. |
| `KeysState` | `string` | A deterministic hash or concatenation of all configuration keys at the time of the snapshot, used for quick change detection. | None | The keys state string. | Property access does not throw exceptions. |
| `CreatedAt` | `DateTime` | UTC timestamp indicating when the snapshot was created. | None | The creation date and time. | Property access does not throw exceptions. |
| `CreatedBy` | `string` | Name or identifier of the user, service, or process that created the snapshot. | None | The creator identifier. | Property access does not throw exceptions. |
| `Reason` | `string?` | Optional free‑form text describing why the snapshot was taken (e.g., “pre‑deploy backup”). May be `null`. | None | The reason string, or `null` if not provided. | Property access does not throw exceptions. |
| `Configuration` | `Configuration?` | Navigation property to the related `Configuration` entity. May be `null` if the entity is not loaded (e.g., in a projection). | None | The associated `Configuration` instance, or `null`. | Property access does not throw exceptions. |

## Usage

### Creating a snapshot manually

```csharp
using System;
using DotnetConfigServer.Domain;

var snapshot = new ConfigurationSnapshot
{
    Id = Guid.NewGuid(),
    ConfigurationId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    ConfigurationState = @"{ ""FeatureToggle"": true, ""MaxConnections"": 100 }",
    KeysState = "FeatureToggle|MaxConnections",
    CreatedAt = DateTime.UtcNow,
    CreatedBy = "admin@example.com",
    Reason = "Before deploying version 2.3",
    Configuration = null // will be set by the ORM when loaded
};

// Persist the snapshot via your repository or DbContext
await _snapshotRepository.AddAsync(snapshot);
```

### Reading snapshot data for an audit report

```csharp
using System;
using DotnetConfigServer.Domain;

public string GetAuditLine(ConfigurationSnapshot snap)
{
    var reason = snap.Reason ?? "<no reason>";
    return $"{snap.CreatedAt:u} | {snap.CreatedBy} | {reason} | Id:{snap.Id}";
}

// Example usage
var line = GetAuditLine(snapshot);
Console.WriteLine(line);
```

## Notes

- The class is intended to be treated as an immutable data transfer object; once instantiated, its properties should not be modified. Mutating properties after creation can lead to inconsistent audit trails and is not thread‑safe.
- All property getters and setters are simple field accesses; they do not throw exceptions under normal operation. Nullability is only expressed for `Reason` and the navigation property `Configuration`.
- When using an ORM such as Entity Framework Core, the `Configuration` navigation property may be `null` if the entity is not eagerly or explicitly loaded. Accessing it without loading will not throw but will return `null`.
- `CreatedAt` should always be stored in UTC to avoid timezone‑related ambiguities when comparing snapshots across different environments.
- The `KeysState` field is not validated by the type itself; callers must ensure it represents a deterministic snapshot of the configuration’s key set (e.g., a sorted concatenation or a cryptographic hash) to guarantee correct change‑detection behavior.
