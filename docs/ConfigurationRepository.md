# ConfigurationRepository

The `ConfigurationRepository` class provides data access operations for managing `Configuration` entities within the `dotnet-config-server` project. It facilitates retrieval of configuration records based on various criteria such as application ID, name, search queries, or deletion status, enabling efficient configuration management in a multi-tenant or application-specific context.

## API

### `Task<List<Configuration>> GetByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default)`
Retrieves all active configurations associated with a specific application ID.

**Parameters:**
- `applicationId` (`int`): The unique identifier of the application for which configurations are requested.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Returns:**
- A `Task` resolving to a `List<Configuration>` containing all configurations linked to the specified `applicationId`. Returns an empty list if no configurations exist for the given ID.

**Throws:**
- `ArgumentOutOfRangeException`: If `applicationId` is less than or equal to zero.

---

### `Task<Configuration?> GetByNameAsync(string name, CancellationToken cancellationToken = default)`
Retrieves a single configuration by its exact name.

**Parameters:**
- `name` (`string`): The name of the configuration to retrieve. Case-sensitive.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Returns:**
- A `Task` resolving to the `Configuration` object if found, or `null` if no configuration with the specified name exists.

**Throws:**
- `ArgumentNullException`: If `name` is `null` or whitespace.

---

### `Task<List<Configuration>> SearchAsync(string? query = null, CancellationToken cancellationToken = default)`
Searches configurations based on an optional query string. If no query is provided, returns all active configurations.

**Parameters:**
- `query` (`string?`, optional): A search term to filter configurations by name or other searchable fields. Partial matches are supported.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Returns:**
- A `Task` resolving to a `List<Configuration>` containing all configurations matching the query. Returns all active configurations if `query` is `null` or empty.

**Throws:**
- None.

---

### `Task<int> GetCountByApplicationAsync(int applicationId, CancellationToken cancellationToken = default)`
Returns the count of active configurations for a given application ID.

**Parameters:**
- `applicationId` (`int`): The unique identifier of the application.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Returns:**
- A `Task` resolving to an `int` representing the number of active configurations for the specified `applicationId`.

**Throws:**
- `ArgumentOutOfRangeException`: If `applicationId` is less than or equal to zero.

---

### `Task<List<Configuration>> GetDeletedBeforeAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)`
Retrieves all configurations marked as deleted before a specified cutoff date. Useful for cleanup or auditing purposes.

**Parameters:**
- `cutoffDate` (`DateTime`): The threshold date. Configurations deleted before this date are included in the result.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Returns:**
- A `Task` resolving to a `List<Configuration>` containing all configurations deleted before `cutoffDate`.

**Throws:**
- `ArgumentOutOfRangeException`: If `cutoffDate` is in the future or represents an invalid date.

## Usage

### Example 1: Retrieve configurations for an application
