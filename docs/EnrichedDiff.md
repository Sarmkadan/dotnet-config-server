# EnrichedDiff
The `EnrichedDiff` type represents a detailed comparison of two configuration versions, providing information about the changes made between them. It is used to analyze and understand the differences between configuration versions, which can be useful for auditing, debugging, and troubleshooting purposes.

## API
The `EnrichedDiff` type has the following public members:
* `DiffId`: A unique identifier for the diff.
* `ConfigurationId`: The identifier of the configuration being compared.
* `FromVersion` and `ToVersion`: The versions being compared, represented as `ConfigurationVersionSummary` objects.
* `Changes`: A list of `DiffEntry` objects, each representing a change made between the two versions.
* `AddedCount`, `ModifiedCount`, and `DeletedCount`: The number of additions, modifications, and deletions made between the two versions.
* `GeneratedAt`: The date and time when the diff was generated.
* `CurrentVersion` and `TargetVersion`: The current and target versions of the configuration, represented as `ConfigurationVersionSummary` objects.
* `IsRollbackSafe`: A boolean indicating whether the diff is safe to roll back.
* `WarningMessages`: A list of warning messages related to the diff.
* `Version`: The version of the configuration, represented as a `ConfigurationVersionSummary` object.
* `DiffFromPrevious`: A summary of the diff from the previous version, represented as a `ConfigurationDiffSummary` object.

## Usage
Here are two examples of using the `EnrichedDiff` type:
```csharp
// Example 1: Analyzing a diff
EnrichedDiff diff = GetEnrichedDiff(configurationId, fromVersion, toVersion);
Console.WriteLine($"Diff ID: {diff.DiffId}");
Console.WriteLine($"Added: {diff.AddedCount}, Modified: {diff.ModifiedCount}, Deleted: {diff.DeletedCount}");
foreach (var change in diff.Changes)
{
    Console.WriteLine($"Change: {change}");
}

// Example 2: Checking if a diff is safe to roll back
EnrichedDiff diff = GetEnrichedDiff(configurationId, fromVersion, toVersion);
if (diff.IsRollbackSafe)
{
    Console.WriteLine("Diff is safe to roll back");
}
else
{
    Console.WriteLine("Diff is not safe to roll back");
    foreach (var warning in diff.WarningMessages)
    {
        Console.WriteLine($"Warning: {warning}");
    }
}
```

## Notes
When working with `EnrichedDiff` objects, note that the `Changes` list may contain multiple entries for the same configuration item if it has been modified multiple times between the two versions. Additionally, the `IsRollbackSafe` property is based on the analysis of the changes made between the two versions and may not cover all possible edge cases. The `EnrichedDiff` type is designed to be thread-safe, but it is still important to ensure that the underlying configuration data is accessed in a thread-safe manner to avoid inconsistencies.
