# AuditLogsController

The `AuditLogsController` serves as the primary HTTP interface for retrieving audit trail data within the `dotnet-config-server` application. It exposes endpoints to query configuration changes filtered by specific entities or users, retrieve comprehensive logs, and access high-level statistical summaries of system activity. This controller facilitates compliance monitoring and debugging by providing structured access to historical modification records.

## API

### Constructors

#### `public AuditLogsController()`
Initializes a new instance of the `AuditLogsController` class. This constructor sets up the necessary internal dependencies required to query the audit log storage.

### Action Methods

#### `public async Task<IActionResult> GetByEntity(string entityName, int pageNumber = 1, int pageSize = 50)`
Retrieves a paginated list of audit log entries associated with a specific configuration entity.
*   **Parameters**:
    *   `entityName`: The name of the entity to filter logs by. Cannot be null or empty.
    *   `pageNumber`: The page index to retrieve (1-based). Defaults to 1.
    *   `pageSize`: The number of records per page. Defaults to 50.
*   **Return Value**: Returns an `IActionResult` containing an `OkObjectResult` with the list of logs if found, or a `NotFoundResult` if no records exist for the entity.
*   **Exceptions**: Throws an `ArgumentException` if `entityName` is null or whitespace.

#### `public async Task<IActionResult> GetByUser(string userId, int pageNumber = 1, int pageSize = 50)`
Retrieves a paginated list of audit log entries performed by a specific user.
*   **Parameters**:
    *   `userId`: The unique identifier of the user whose actions are being queried.
    *   `pageNumber`: The page index to retrieve (1-based). Defaults to 1.
    *   `pageSize`: The number of records per page. Defaults to 50.
*   **Return Value**: Returns an `IActionResult` containing an `OkObjectResult` with the filtered log list, or a `NotFoundResult` if no actions are found for the user.
*   **Exceptions**: Throws an `ArgumentException` if `userId` is null or whitespace.

#### `public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 50)`
Retrieves a paginated list of all audit log entries in the system without filtering.
*   **Parameters**:
    *   `pageNumber`: The page index to retrieve (1-based). Defaults to 1.
    *   `pageSize`: The number of records per page. Defaults to 50.
*   **Return Value**: Returns an `IActionResult` containing an `OkObjectResult` with the complete log set (paginated).
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if `pageNumber` or `pageSize` is less than 1.

#### `public async Task<IActionResult> GetById(long id)`
Retrieves a single audit log entry by its unique identifier.
*   **Parameters**:
    *   `id`: The unique primary key of the audit log entry.
*   **Return Value**: Returns an `IActionResult` containing an `OkObjectResult` with the specific log entry, or a `NotFoundResult` if the ID does not exist.
*   **Exceptions**: None specific to logic; standard runtime exceptions may occur if the underlying store is unavailable.

#### `public async Task<IActionResult> GetSummary()`
Generates and returns a statistical summary of audit activities.
*   **Parameters**: None.
*   **Return Value**: Returns an `IActionResult` containing an `OkObjectResult` with a summary object detailing total changes, operation counts, unique user counts, and the timestamp of the last change.
*   **Exceptions**: None.

### Properties

The following properties reflect the data contained within the most recent summary calculation or the current context state:

*   `public int TotalChanges`: Gets the total number of recorded changes across all entities.
*   `public int CreateCount`: Gets the count of "Create" operations recorded.
*   `public int UpdateCount`: Gets the count of "Update" operations recorded.
*   `public int DeleteCount`: Gets the count of "Delete" operations recorded.
*   `public int UniqueUsers`: Gets the number of distinct users who have performed actions.
*   `public DateTime? LastChange`: Gets the timestamp of the most recent audit event. Returns `null` if no changes have been recorded.

## Usage

### Example 1: Retrieving Paginated Logs for a Specific Entity
This example demonstrates how to call the controller to fetch the second page of changes made to the "DatabaseConnection" entity.

```csharp
var controller = new AuditLogsController();
string targetEntity = "DatabaseConnection";
int page = 2;
int size = 20;

// Execute the request
var result = await controller.GetByEntity(targetEntity, page, size);

if (result is OkObjectResult okResult)
{
    var logs = okResult.Value as IEnumerable<AuditLogEntry>;
    foreach (var log in logs)
    {
        Console.WriteLine($"User {log.UserId} modified {log.EntityName} at {log.Timestamp}");
    }
}
else if (result is NotFoundResult)
{
    Console.WriteLine("No audit logs found for the specified entity.");
}
```

### Example 2: Fetching System-Wide Activity Summary
This example illustrates retrieving high-level metrics to display on an administrative dashboard.

```csharp
var controller = new AuditLogsController();

// Request the summary
var summaryResult = await controller.GetSummary();

if (summaryResult is OkObjectResult okResult)
{
    // Assuming the controller populates its public properties upon summary generation
    // or the result object contains these fields directly depending on implementation.
    // Based on the signature, we inspect the result content.
    
    dynamic summary = okResult.Value; 
    
    Console.WriteLine($"Total Changes: {summary.TotalChanges}");
    Console.WriteLine($"Creates: {summary.CreateCount}, Updates: {summary.UpdateCount}, Deletes: {summary.DeleteCount}");
    Console.WriteLine($"Active Users: {summary.UniqueUsers}");
    
    if (summary.LastChange.HasValue)
    {
        Console.WriteLine($"Last Activity: {summary.LastChange.Value:yyyy-MM-dd HH:mm:ss}");
    }
    else
    {
        Console.WriteLine("No activity recorded yet.");
    }
}
```

## Notes

*   **Null Handling**: The `LastChange` property is nullable (`DateTime?`). Consumers must check `HasValue` before accessing the underlying `DateTime` struct to avoid runtime exceptions.
*   **Pagination Limits**: While default values are provided for `pageNumber` and `pageSize`, passing values less than 1 to any pagination-enabled method will result in an `ArgumentOutOfRangeException`.
*   **Input Validation**: Methods requiring string identifiers (`GetByEntity`, `GetByUser`) strictly validate input. Passing null, empty, or whitespace-only strings will trigger an `ArgumentException` immediately, before any database query occurs.
*   **Thread Safety**: As an ASP.NET Core controller, `AuditLogsController` is designed to be instantiated per request. The instance members (properties like `TotalChanges`) represent state relevant only to the specific execution context or the last hydrated summary result for that instance. Do not share controller instances across threads or requests.
*   **Asynchronous Execution**: All data retrieval methods are asynchronous (`async Task`). Calling code must await these tasks to prevent blocking the request thread and to properly handle potential exceptions arising from I/O operations.
