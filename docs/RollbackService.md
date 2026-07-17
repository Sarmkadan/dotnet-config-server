# RollbackService

The `RollbackService` provides functionality to revert configuration versions to a previous state, including tracking rollback operations and their metadata. It is designed to restore configuration keys from a specified version while maintaining an auditable history of all rollback actions performed.

## API

### `RollbackService`

The primary service class responsible for executing and tracking configuration rollbacks. This class is instantiated with contextual information about the rollback operation such as the source version, reason, and performer details.

### `async Task<RollbackResult> ExecuteRollbackAsync()`

Executes a configuration rollback to the version identified by `RestoredFromVersionId`.

- **Parameters**: None.
- **Return value**: A `Task<RollbackResult>` representing the asynchronous operation. The `RollbackResult` contains details about the success or failure of the rollback, including the number of keys restored and any encountered errors.
- **Throws**:
  - `InvalidOperationException` if `RestoredFromVersionId` is not a valid version identifier.
  - `ArgumentNullException` if `Reason` or `PerformedBy` is null or empty.
  - `IOException` if there is an issue writing the restored configuration to storage.

### `async Task<List<RollbackRecord>> GetRollbackHistoryAsync()`

Retrieves the complete history of rollback operations performed by this service instance.

- **Parameters**: None.
- **Return value**: A `Task<List<RollbackRecord>>` containing a list of `RollbackRecord` objects, each representing a past rollback operation with metadata such as timestamp, performer, and restored version.
- **Throws**:
  - `IOException` if the rollback history cannot be read from storage.

### `Guid RestoredFromVersionId`

Gets or sets the unique identifier of the configuration version to which the current rollback operation will restore.

- **Type**: `Guid`
- **Access**: Public property with both get and set accessors.
- **Validation**: Must be set to a non-empty `Guid` before calling `ExecuteRollbackAsync()`.

### `string Reason`

Gets or sets the rationale provided for performing the rollback operation.

- **Type**: `string`
- **Access**: Public property with both get and set accessors.
- **Validation**: Must not be null or whitespace. Throws `ArgumentNullException` if set to null or empty.

### `string PerformedBy`

Gets or sets the identifier of the user or system entity that initiated the rollback.

- **Type**: `string`
- **Access**: Public property with both get and set accessors.
- **Validation**: Must not be null or whitespace. Throws `ArgumentNullException` if set to null or empty.

### `DateTime PerformedAt`

Gets or sets the timestamp when the rollback was performed.

- **Type**: `DateTime`
- **Access**: Public property with both get and set accessors.
- **Default**: Automatically set to `DateTime.UtcNow` when `ExecuteRollbackAsync()` is called.

### `int KeysRestored`

Gets the number of configuration keys successfully restored during the most recent rollback operation.

- **Type**: `int`
- **Access**: Public read-only property.
- **Initial value**: `0`
- **Update**: Set during `ExecuteRollbackAsync()` based on the number of keys restored from the source version.

## Usage

### Example 1: Performing a Rollback
