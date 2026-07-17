# ConfigurationDiff

`ConfigurationDiff` represents the difference between two versions of a configuration, tracking changes such as additions, modifications, and deletions. It provides detailed insights into what changed between two configuration states, including counts of each change type and a summary of the differences.

## API

### Properties

#### `public Guid Id`
A unique identifier for the `ConfigurationDiff` instance. This value is set when the diff is created and remains constant throughout its lifecycle.

#### `public Guid ConfigurationId`
The identifier of the configuration being compared. This links the diff to the specific configuration it describes.

#### `public Guid FromVersionId`
The identifier of the older configuration version being compared. This represents the "left side" of the diff.

#### `public Guid ToVersionId`
The identifier of the newer configuration version being compared. This represents the "right side" of the diff.

#### `public DateTime CreatedAt`
The timestamp when the diff was generated. This indicates when the comparison between the two versions was performed.

#### `public string CreatedBy`
The identifier or name of the user or system that created the diff. This provides accountability for the change tracking operation.

#### `public int TotalChanges`
The total number of changes detected between the two configuration versions. This is the sum of all additions, modifications, and deletions.

#### `public int AddedCount`
The number of new configuration entries added between the two versions. These are keys that exist in the newer version but not in the older version.

#### `public int ModifiedCount`
The number of configuration entries that were modified between the two versions. These are keys that exist in both versions but have different values.

#### `public int DeletedCount`
The number of configuration entries removed between the two versions. These are keys that exist in the older version but not in the newer version.

#### `public List<DiffEntry> Changes`
A list of individual changes between the two configuration versions. Each entry describes a specific change, including the key, type of change, and old/new values where applicable.

### Methods

#### `public void AddChange(DiffEntry entry)`
Adds a single change to the diff.

- **Parameters**:
  - `entry`: The `DiffEntry` to add. Must not be `null`.
- **Throws**:
  - `ArgumentNullException`: If `entry` is `null`.

#### `public List<DiffEntry> GetChangesByType(ChangeType changeType)`
Retrieves all changes of a specific type from the diff.

- **Parameters**:
  - `changeType`: The type of change to filter by (e.g., `Added`, `Modified`, `Deleted`).
- **Returns**:
  - A list of `DiffEntry` objects matching the specified change type. Returns an empty list if no changes of that type exist.
- **Throws**:
  - `ArgumentOutOfRangeException`: If `changeType` is not a valid `ChangeType` value.

#### `public ConfigurationDiffSummary GetSummary()`
Generates a summary of the diff, including counts of each change type and totals.

- **Returns**:
  - A `ConfigurationDiffSummary` object containing counts of added, modified, deleted, and total changes.

#### `public string GetChangesSummary()`
Generates a human-readable summary of the changes in the diff.

- **Returns**:
  - A string summarizing the changes, including counts of each type and a brief description of the changes. Returns an empty string if no changes exist.

## Usage

### Example 1: Generating and Inspecting a Configuration Diff
```csharp
// Assume we have two configuration versions with IDs 'fromVersion' and 'toVersion'
var diff = new ConfigurationDiff
{
    ConfigurationId = configId,
    FromVersionId = fromVersion,
    ToVersionId = toVersion,
    CreatedAt = DateTime.UtcNow,
    CreatedBy = "system"
};

// Add individual changes detected during comparison
diff.AddChange(new DiffEntry
{
    Key = "ApiEndpoint",
    ChangeType = ChangeType.Added,
    NewValue = "https://api.example.com/v2"
});

diff.AddChange(new DiffEntry
{
    Key = "Timeout",
    ChangeType = ChangeType.Modified,
    OldValue = "30",
    NewValue = "60"
});

// Retrieve summary of changes
var summary = diff.GetSummary();
Console.WriteLine($"Total changes: {summary.TotalChanges}");
Console.WriteLine($"Added: {summary.AddedCount}, Modified: {summary.ModifiedCount}, Deleted: {summary.DeletedCount}");

// Get all added changes
var addedChanges = diff.GetChangesByType(ChangeType.Added);
foreach (var change in addedChanges)
{
    Console.WriteLine($"Added key: {change.Key}");
}
```

### Example 2: Comparing Configuration Versions Programmatically
```csharp
// Assume we have a service to fetch configuration versions
var fromConfig = await configService.GetConfigurationAsync(fromVersionId);
var toConfig = await configService.GetConfigurationAsync(toVersionId);

// Generate a diff between the two versions
var diff = new ConfigurationDiff
{
    ConfigurationId = configId,
    FromVersionId = fromVersionId,
    ToVersionId = toVersionId,
    CreatedAt = DateTime.UtcNow,
    CreatedBy = "automated-comparison"
};

// Compare each key in the configurations
foreach (var key in toConfig.Keys)
{
    if (!fromConfig.ContainsKey(key))
    {
        diff.AddChange(new DiffEntry
        {
            Key = key,
            ChangeType = ChangeType.Added,
            NewValue = toConfig[key]
        });
    }
    else if (fromConfig[key] != toConfig[key])
    {
        diff.AddChange(new DiffEntry
        {
            Key = key,
            ChangeType = ChangeType.Modified,
            OldValue = fromConfig[key],
            NewValue = toConfig[key]
        });
    }
}

// Output a human-readable summary
Console.WriteLine(diff.GetChangesSummary());
```

## Notes

- **Thread Safety**: This class is not thread-safe. Concurrent modifications to the `Changes` list or calls to methods like `AddChange` from multiple threads may result in race conditions or data corruption. External synchronization is required if used in a multi-threaded context.
- **Empty Diffs**: If no changes exist between the two versions, `TotalChanges` will be 0, and `Changes` will be an empty list. Methods like `GetChangesByType` will return empty lists, and `GetChangesSummary` will return an empty string.
- **Null Handling**: The `AddChange` method throws an `ArgumentNullException` if the provided `DiffEntry` is `null`. Ensure all entries added to the diff are valid.
- **Change Type Validation**: The `GetChangesByType` method throws an `ArgumentOutOfRangeException` if an invalid `ChangeType` value is provided. Always use valid enum values.
- **Immutable Properties**: Properties like `Id`, `ConfigurationId`, `FromVersionId`, `ToVersionId`, `CreatedAt`, and `CreatedBy` are typically set once during initialization and should not be modified afterward.
