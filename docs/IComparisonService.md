# IComparisonService

The `IComparisonService` interface defines a contract for comparing two objects of the same type and exposing the results through a set of properties. Implementations perform a deep or shallow comparison, track individual property changes, and provide summary statistics such as total changes and change percentage. The interface is designed to be used in configuration management scenarios where differences between configuration snapshots need to be detected, reported, and optionally acted upon.

## API

### Methods

#### `ComparisonResult Compare<T>()`

Performs a comparison between two objects of type `T`. The objects to compare are typically provided to the service implementation before calling this method (e.g., via constructor or separate setter). After execution, the result properties (`Changes`, `TotalChanges`, `ChangedFields`, `ChangePercentage`, etc.) are populated.

- **Type parameters**: `T` – The type of the objects being compared. Must be a reference type.
- **Returns**: A `ComparisonResult` object that encapsulates the outcome of the comparison. The exact structure of `ComparisonResult` is implementation-specific but generally includes a success flag and optional error information.
- **Throws**: `ArgumentNullException` if the objects to compare have not been set or are null. `InvalidOperationException` if the comparison cannot be performed due to incompatible types or missing comparer configuration.

#### `bool HasDifferences<T>()`

Returns a value indicating whether any differences were found between the two objects of type `T`. This method may trigger a comparison if one has not already been performed, or it may rely on a cached result.

- **Type parameters**: `T` – The type of the objects being compared.
- **Returns**: `true` if at least one property differs; otherwise `false`.
- **Throws**: Same as `Compare<T>` if the comparison has not been performed and the method attempts to do so.

#### `SummaryOfChanges GetSummary<T>()`

Returns a summary object that aggregates the changes found during the comparison. The summary typically includes the total number of changes, a list of changed field names, and the overall change percentage.

- **Type parameters**: `T` – The type of the objects being compared.
- **Returns**: A `SummaryOfChanges` instance containing aggregated change data.
- **Throws**: `InvalidOperationException` if no comparison has been performed yet.

### Properties

#### `string ItemType`

Gets the name of the type being compared (e.g., `"ConfigurationSection"`). This property is populated after a comparison is executed.

#### `List<PropertyChange> Changes`

Gets a list of `PropertyChange` objects, each representing a single property that differs between the two objects. Each `PropertyChange` contains `PropertyName`, `OriginalValue`, `ModifiedValue`, and `PropertyType`.

#### `string PropertyName`

Gets the name of the property that is currently being examined or that was last examined. This property is typically used during iteration over changes.

#### `string OriginalValue`

Gets the original (baseline) value of the property identified by `PropertyName`.

#### `string ModifiedValue`

Gets the modified (new) value of the property identified by `PropertyName`.

#### `string PropertyType`

Gets the type name (e.g., `"System.String"`, `"System.Int32"`) of the property identified by `PropertyName`.

#### `int TotalChanges`

Gets the total number of properties that differ between the two compared objects.

#### `List<string> ChangedFields`

Gets a list of property names that have changed. This is a convenience property that aggregates the `PropertyName` values from the `Changes` list.

#### `double ChangePercentage`

Gets the percentage of properties that changed relative to the total number of properties in the type. The value ranges from 0.0 (no changes) to 100.0 (all properties changed).

## Usage

### Example 1: Basic comparison and reporting

```csharp
public class AppSettings
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
    public bool EnableLogging { get; set; }
}

// Assume service is configured with original and modified objects
IComparisonService comparisonService = new DefaultComparisonService(original, modified);

ComparisonResult result = comparisonService.Compare<AppSettings>();

if (comparisonService.HasDifferences<AppSettings>())
{
    Console.WriteLine($"Total changes: {comparisonService.TotalChanges}");
    Console.WriteLine($"Change percentage: {comparisonService.ChangePercentage}%");

    foreach (var change in comparisonService.Changes)
    {
        Console.WriteLine($"Property '{change.PropertyName}' changed from '{change.OriginalValue}' to '{change.ModifiedValue}'");
    }
}
```

### Example 2: Using summary and checking for specific changes

```csharp
public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string DatabaseName { get; set; }
}

IComparisonService comparisonService = new DefaultComparisonService(currentConfig, newConfig);

SummaryOfChanges summary = comparisonService.GetSummary<DatabaseConfig>();

if (summary.TotalChanges > 0)
{
    Console.WriteLine($"Changed fields: {string.Join(", ", summary.ChangedFields)}");
    Console.WriteLine($"Change percentage: {summary.ChangePercentage}%");

    // Check if a critical property changed
    if (summary.ChangedFields.Contains("Host"))
    {
        // Trigger reconnection logic
    }
}
```

## Notes

- **Stateful nature**: The interface is designed to be stateful. After calling `Compare<T>`, the properties (`Changes`, `TotalChanges`, etc.) reflect the result of that comparison. Calling `Compare<T>` again with different objects (if the implementation supports it) will overwrite the previous results.
- **Thread safety**: Implementations of `IComparisonService` are not guaranteed to be thread-safe. Concurrent calls to `Compare<T>`, `HasDifferences<T>`, or `GetSummary<T>` from multiple threads may lead to inconsistent state. External synchronization (e.g., a lock) is recommended when the same instance is used across threads.
- **Null handling**: If either of the objects being compared is `null`, the behavior depends on the implementation. Most implementations will throw `ArgumentNullException` from `Compare<T>`. The `HasDifferences<T>` method may also throw if it triggers a comparison.
- **Type constraints**: The generic methods require `T` to be a reference type. Value types are not supported. Attempting to use a value type will result in a compile-time error if the implementation constrains `T` to `class`, or a runtime exception otherwise.
- **Property change details**: The `PropertyName`, `OriginalValue`, `ModifiedValue`, and `PropertyType` properties are intended to be used in conjunction with iteration over the `Changes` list. Their values are transient and may be overwritten by subsequent calls to `Compare<T>` or by internal iteration logic. For persistent access, use the `Changes` list directly.
- **Change percentage calculation**: The `ChangePercentage` is calculated as `(TotalChanges / TotalProperties) * 100`. If the type has no properties, the percentage is defined as 0.0.
