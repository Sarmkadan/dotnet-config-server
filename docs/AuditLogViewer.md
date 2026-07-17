# AuditLogViewer

The `AuditLogViewer` class provides a centralized interface for querying, displaying, exporting, and analyzing audit logs from a configuration server. It exposes properties that represent a single audit log entry (such as `Id`, `Timestamp`, `Action`, `User`, `IpAddress`, `Details`, `Changes`, `OldValue`, `NewValue`) as well as collection-level properties (`Items`, `TotalCount`) that reflect the results of the most recent query. The class is designed for interactive tooling and reporting scenarios where logs need to be retrieved, filtered, and presented in a human-readable format.

## API

### `public AuditLogViewer()`

Initializes a new instance of the `AuditLogViewer` class.

- **Parameters:** None.
- **Returns:** Nothing.
- **Throws:** None.

### `public async Task<List<AuditLog>> GetAuditLogsAsync()`

Retrieves all audit logs from the underlying data source.

- **Parameters:** None.
- **Returns:** A `Task<List<AuditLog>>` representing the asynchronous operation. The list contains all available audit log entries.
- **Throws:** `InvalidOperationException` if the data source is not accessible.

### `public async Task DisplayAuditLogsAsync()`

Displays the current set of audit logs (as stored in the `Items` property) in a formatted, human-readable output (e.g., console or UI).

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous display operation.
- **Throws:** `InvalidOperationException` if no logs have been loaded.

### `public async Task<List<AuditLog>> GetUserChangesAsync(string userId)`

Retrieves all audit log entries that were performed by the specified user.

- **Parameters:** `userId` – The identifier of the user whose changes are to be retrieved.
- **Returns:** A `Task<List<AuditLog>>` containing the filtered audit logs.
- **Throws:** `ArgumentException` if `userId` is `null` or empty.

### `public async Task<List<AuditLog>> GetChangesInDateRangeAsync(DateTime start, DateTime end)`

Retrieves audit log entries that occurred within the specified date range.

- **Parameters:**
  - `start` – The inclusive start date of the range.
  - `end` – The inclusive end date of the range.
- **Returns:** A `Task<List<AuditLog>>` containing the filtered audit logs.
- **Throws:** `ArgumentException` if `start` is later than `end`.

### `public async Task DisplayKeyChangeHistoryAsync(string key)`

Displays the change history for a specific configuration key.

- **Parameters:** `key` – The configuration key whose history is to be shown.
- **Returns:** A `Task` representing the asynchronous operation.
- **Throws:** `ArgumentException` if `key` is `null` or empty.

### `public async Task DisplayAuditReportAsync()`

Generates and displays a summary report of all audit logs, including counts of actions, users, and anomalies.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous report generation.
- **Throws:** `InvalidOperationException` if no logs are available.

### `public async Task ExportAuditLogsAsync(string filePath)`

Exports the current set of audit logs to a file at the specified path (e.g., CSV or JSON format).

- **Parameters:** `filePath` – The full path where the exported file will be written.
- **Returns:** A `Task` representing the asynchronous export operation.
- **Throws:** `ArgumentException` if `filePath` is `null` or empty; `IOException` if the file cannot be written.

### `public async Task DetectAnomaliesAsync()`

Analyzes the current set of audit logs for suspicious patterns (e.g., repeated failures, unusual IP addresses, or unexpected changes) and displays the results.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous anomaly detection.
- **Throws:** `InvalidOperationException` if no logs are loaded.

### `public string Id { get; set; }`

Gets or sets the unique identifier of the current audit log entry.

### `public DateTime Timestamp { get; set; }`

Gets or sets the date and time when the audit log entry was recorded.

### `public string Action { get; set; }`

Gets or sets the action that was performed (e.g., "Create", "Update", "Delete").

### `public string User { get; set; }`

Gets or sets the user who performed the action.

### `public string IpAddress { get; set; }`

Gets or sets the IP address from which the action originated.

### `public string Details { get; set; }`

Gets or sets additional human-readable details about the action.

### `public Dictionary<string, AuditChange> Changes { get; set; }`

Gets or sets a dictionary of changed configuration keys and their corresponding `AuditChange` objects (containing old and new values).

### `public string OldValue { get; set; }`

Gets or sets the previous value of the configuration key that was changed (for single‑key changes).

### `public string NewValue { get; set; }`

Gets or sets the new value of the configuration key that was changed (for single‑key changes).

### `public List<T> Items { get; set; }`

Gets or sets the list of audit log entries currently loaded in the viewer. The generic type `T` is typically `AuditLog`.

### `public int TotalCount { get; set; }`

Gets or sets the total number of audit log entries that match the most recent query (useful for pagination or summary display).

## Usage

### Example 1: Retrieve and display logs for a specific user

```csharp
using System;
using System.Threading.Tasks;

public class AuditExample
{
    public async Task ShowUserChangesAsync()
    {
        var viewer = new AuditLogViewer();
        var userLogs = await viewer.GetUserChangesAsync("jdoe");
        
        // The viewer's Items and TotalCount are now populated
        Console.WriteLine($"Found {viewer.TotalCount} changes by jdoe.");
        
        // Display the logs in a formatted way
        await viewer.DisplayAuditLogsAsync();
    }
}
```

### Example 2: Export logs within a date range and detect anomalies

```csharp
using System;
using System.Threading.Tasks;

public class ExportAndAnalyzeExample
{
    public async Task RunAsync()
    {
        var viewer = new AuditLogViewer();
        var logs = await viewer.GetChangesInDateRangeAsync(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 31));
        
        // Export to a CSV file
        await viewer.ExportAuditLogsAsync(@"C:\audit\january_logs.csv");
        
        // Run anomaly detection on the loaded logs
        await viewer.DetectAnomaliesAsync();
    }
}
```

## Notes

- **Thread safety:** The `AuditLogViewer` class is not thread‑safe. Concurrent calls to methods or property setters from multiple threads may result in inconsistent state. Use external synchronization (e.g., a lock) if the instance is shared across threads.
- **Property state:** The properties `Id`, `Timestamp`, `Action`, `User`, `IpAddress`, `Details`, `Changes`, `OldValue`, `NewValue`, `Items`, and `TotalCount` are mutable. Their values are typically set automatically after a query method (e.g., `GetAuditLogsAsync`) completes, but they can also be assigned manually for custom scenarios.
- **Empty results:** If a query returns no logs, `Items` will be an empty list and `TotalCount` will be `0`. Methods like `DisplayAuditLogsAsync` and `DetectAnomaliesAsync` will throw `InvalidOperationException` when `Items` is empty.
- **Data source dependency:** The underlying data source (e.g., a database or file) must be accessible at the time of query. Network or permission errors will propagate as exceptions.
- **Generic `Items`:** The `Items` property is declared as `List<T>`. In practice, `T` is expected to be `AuditLog`, but the type parameter allows flexibility for derived or wrapper types. Ensure the assigned list is compatible with the methods that consume it.
