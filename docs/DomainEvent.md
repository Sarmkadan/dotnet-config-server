# DomainEvent

`DomainEvent` is a base class for domain events within the `dotnet-config-server` project. It captures the common metadata of any configuration- or key-related occurrence (e.g., creation, update, deletion) and provides optional fields for event-specific payloads. The class is designed to be immutable after construction, ensuring a consistent audit trail for configuration and key changes.

## API

- **`public Guid Id`**  
  A unique identifier for the event instance. Generated at creation time.

- **`public DateTime OccurredAt`**  
  The UTC timestamp when the event occurred.

- **`public string? Source`**  
  An optional identifier for the system or component that raised the event (e.g., service name, HTTP endpoint).

- **`public string? UserId`**  
  An optional identifier of the user who triggered the event. May be `null` for system-generated events.

- **`public Guid ConfigurationId`**  
  The identifier of the configuration that this event relates to. Always present.

- **`public Guid ApplicationId`**  
  The identifier of the application that owns the configuration. Always present.

- **`public string ConfigurationName`**  
  The name of the configuration entry. Always present.

- **`public string Environment`**  
  The environment (e.g., "development", "production") in which the configuration is defined. Always present.

- **`public Dictionary<string, object?> Changes`**  
  A dictionary of changed fields and their new values, used when the event represents a configuration update. May be empty or `null` for non-update events (e.g., creation, deletion, key events). The dictionary is not thread-safe for concurrent writes.

- **`public Guid KeyId`**  
  The identifier of the key within the configuration, relevant only for key-related events (e.g., key creation, rotation). For configuration-level events this value is `Guid.Empty`.

- **`public string Key`**  
  The name of the key, relevant only for key-related events. May be `null` or empty for configuration-level events.

- **`public string? OldValue`**  
  The previous value of the key, present only for key update events. `null` for new keys or configuration-level events.

- **`public string? NewValue`**  
  The new value of the key, present only for key update events. `null` for key deletion or configuration-level events.

- **`public bool IsEncrypted`**  
  Indicates whether the key value is stored encrypted. Relevant only for key-related events; for configuration-level events this is `false`.

None of the members throw exceptions under normal usage. The dictionary `Changes` may be accessed safely after construction, but concurrent modification of the dictionary instance is not supported.

## Usage

### Example 1: Creating and inspecting a configuration update event

```csharp
var configEvent = new DomainEvent
{
    Id = Guid.NewGuid(),
    OccurredAt = DateTime.UtcNow,
    Source = "ConfigService",
    UserId = "user-123",
    ConfigurationId = Guid.Parse("a1b2c3d4-..."),
    ApplicationId = Guid.Parse("e5f6g7h8-..."),
    ConfigurationName = "appsettings.json",
    Environment = "production",
    Changes = new Dictionary<string, object?>
    {
        ["ConnectionString"] = "Server=...;Database=...",
        ["MaxRetries"] = 5
    }
};

Console.WriteLine($"Configuration '{configEvent.ConfigurationName}' updated in {configEvent.Environment}");
foreach (var change in configEvent.Changes)
{
    Console.WriteLine($"  {change.Key} = {change.Value}");
}
```

### Example 2: Handling a key rotation event

```csharp
var keyEvent = new DomainEvent
{
    Id = Guid.NewGuid(),
    OccurredAt = DateTime.UtcNow,
    Source = "KeyRotationService",
    UserId = null, // system event
    ConfigurationId = Guid.Parse("a1b2c3d4-..."),
    ApplicationId = Guid.Parse("e5f6g7h8-..."),
    ConfigurationName = "secrets.json",
    Environment = "production",
    KeyId = Guid.Parse("k1l2m3n4-..."),
    Key = "ApiKey",
    OldValue = "old-secret",
    NewValue = "new-secret",
    IsEncrypted = true
};

if (keyEvent.IsEncrypted)
{
    Console.WriteLine($"Encrypted key '{keyEvent.Key}' rotated for configuration '{keyEvent.ConfigurationName}'");
}
```

## Notes

- **Edge cases**  
  - For configuration-level events (creation, deletion, update without key changes), `KeyId` is `Guid.Empty`, `Key` is `null` or empty, `OldValue` and `NewValue` are `null`, and `IsEncrypted` is `false`.  
  - For key-level events, `Changes` is typically `null` or empty because the key-specific fields carry the relevant data.  
  - `Source` and `UserId` may both be `null` for fully automated system events.  
  - The `Changes` dictionary can be `null`; always check for `null` before iterating.

- **Thread safety**  
  The `DomainEvent` class is intended to be immutable after construction. All properties are read-only (no public setters are shown; in practice they are set via constructor or object initializer). Once an instance is created, its state does not change. The `Changes` dictionary, if provided, should not be modified after the event is constructed. Concurrent reads of the same event instance are safe. If the dictionary is shared across threads, external synchronization is required for any write access.
