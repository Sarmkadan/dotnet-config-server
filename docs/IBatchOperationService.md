# IBatchOperationService

The `IBatchOperationService` interface defines the contract for managing asynchronous bulk operations within the configuration server, enabling the atomic update or deletion of multiple configuration keys in a single transactional context. It provides mechanisms to initiate long-running batch tasks, track their progress via status polling, and retrieve detailed results including success counts and specific error messages for failed items, ensuring robust handling of large-scale configuration changes without blocking the calling thread.

## API

### Methods

#### `UpdateKeysAsync`
Initiates an asynchronous batch operation to update multiple configuration keys to new values.
*   **Parameters**: Accepts a collection of key-value pairs where each item specifies a `KeyId` (`Guid`) and the `NewValue` (`string`) to apply.
*   **Return Value**: Returns a `Task<BatchOperationResult>` that completes when the operation is queued or finished, containing the `OperationId`, total counts, and immediate validation results.
*   **Exceptions**: Throws an exception if the input collection is null, empty, or contains duplicate keys within the same batch request.

#### `DeleteKeysAsync`
Initiates an asynchronous batch operation to remove multiple configuration keys from the store.
*   **Parameters**: Accepts a collection of `Guid` identifiers representing the keys to be deleted.
*   **Return Value**: Returns a `Task<BatchOperationResult>` providing the `OperationId` and summary statistics regarding the deletion attempt.
*   **Exceptions**: Throws an exception if the provided key list is null or if any key format is invalid.

#### `GetStatusAsync`
Retrieves the current execution state of a specific batch operation.
*   **Parameters**: Requires the `OperationId` (`Guid`) of the target batch operation.
*   **Return Value**: Returns a `Task<BatchOperationStatus>` containing the current `Status` string, `StartedAt` timestamp, optional `CompletedAt` timestamp, and any global `Error` message if the entire batch failed.
*   **Exceptions**: Throws a `KeyNotFoundException` if the provided `OperationId` does not exist in the system.

#### `CancelAsync`
Requests the cancellation of a running batch operation.
*   **Parameters**: Requires the `OperationId` (`Guid`) of the operation to cancel.
*   **Return Value**: Returns a `Task` that completes when the cancellation request has been processed. Note that items already processed may not be rolled back by this action.
*   **Exceptions**: Throws an exception if the operation is already in a terminal state (Completed or Failed).

### Properties

The following properties are typically exposed on result objects (`BatchOperationResult`, `BatchOperationStatus`) or the service context associated with an operation:

*   **`OperationId`** (`Guid`): The unique identifier assigned to the batch operation upon creation. Used for tracking status and cancellation.
*   **`Status`** (`string`): A textual representation of the current state (e.g., "Pending", "Running", "Completed", "Cancelled", "Failed").
*   **`TotalItems`** (`int`): The total number of items included in the original batch request.
*   **`StartedAt`** (`DateTime`): The UTC timestamp indicating when the processing of the batch began.
*   **`CompletedAt`** (`DateTime?`): The UTC timestamp indicating when the batch finished. Null if the operation is still in progress.
*   **`Error`** (`string?`): A global error message describing why the entire batch operation failed, if applicable. Null if the operation succeeded or is ongoing.
*   **`KeyId`** (`Guid`): Represents the specific configuration key identifier involved in a specific item result.
*   **`NewValue`** (`string`): The value that was attempted to be set for a specific key during an update operation.
*   **`Success`** (`bool`): Indicates whether a specific item within the batch was processed successfully.
*   **`SuccessCount`** (`int`): The aggregate number of items within the batch that were processed successfully.
*   **`ErrorCount`** (`int`): The aggregate number of items within the batch that failed to process.
*   **`Errors`** (`List<string>`): A collection of specific error messages detailing why individual items or the overall batch failed.

## Usage

### Example 1: Performing a Bulk Update and Polling for Completion
This example demonstrates initiating a batch update of multiple configuration keys and polling the status until the operation completes.

```csharp
public async Task ExecuteBulkUpdate(IBatchOperationService service, List<(Guid KeyId, string Value)> updates)
{
    // Initiate the batch update
    var result = await service.UpdateKeysAsync(updates.Select(u => new { u.KeyId, u.Value }).ToList());
    Guid opId = result.OperationId;

    Console.WriteLine($"Batch started with ID: {opId}. Total items: {result.TotalItems}");

    // Poll for status
    BatchOperationStatus status;
    do
    {
        await Task.Delay(500); // Wait before polling again
        status = await service.GetStatusAsync(opId);
        
        Console.WriteLine($"Status: {status.Status} | Processed: {status.StartedAt}");
        
        if (!string.IsNullOrEmpty(status.Error))
        {
            Console.WriteLine($"Global Error: {status.Error}");
            break;
        }
    } while (status.CompletedAt == null);

    if (status.CompletedAt.HasValue)
    {
        Console.WriteLine($"Operation finished. Success: {result.SuccessCount}, Errors: {result.ErrorCount}");
        if (result.Errors.Any())
        {
            foreach (var err in result.Errors)
            {
                Console.WriteLine($"- {err}");
            }
        }
    }
}
```

### Example 2: Bulk Deletion with Cancellation Support
This example shows how to initiate a bulk deletion and provides a mechanism to cancel the operation if it takes too long.

```csharp
public async Task DeleteKeysWithTimeout(IBatchOperationService service, List<Guid> keysToDelete, TimeSpan timeout)
{
    var cts = new CancellationTokenSource(timeout);
    
    try
    {
        // Start deletion
        var deleteResult = await service.DeleteKeysAsync(keysToDelete);
        Console.WriteLine($"Delete operation {deleteResult.OperationId} initiated.");

        // Monitor and potentially cancel
        while (!cts.Token.IsCancellationRequested)
        {
            var status = await service.GetStatusAsync(deleteResult.OperationId);
            
            if (status.CompletedAt.HasValue || status.Status == "Failed")
            {
                Console.WriteLine($"Operation ended with status: {status.Status}");
                return;
            }

            await Task.Delay(1000, cts.Token);
        }
        
        // Timeout reached, attempt cancellation
        Console.WriteLine("Timeout reached. Attempting to cancel operation...");
        await service.CancelAsync(deleteResult.OperationId);
        Console.WriteLine("Cancellation requested.");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operation monitoring cancelled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during operation: {ex.Message}");
    }
}
```

## Notes

*   **Eventual Consistency**: Batch operations are asynchronous. The `UpdateKeysAsync` and `DeleteKeysAsync` methods return immediately after validation and queuing. Consumers must use `GetStatusAsync` to determine final completion and item-level success rates.
*   **Partial Success**: A batch operation does not guarantee atomicity across all items. It is possible for `SuccessCount` to be greater than zero while `ErrorCount` is also greater than zero. The `Errors` list should always be inspected if `ErrorCount > 0`.
*   **Cancellation Limitations**: Calling `CancelAsync` stops the processing of remaining items in the queue but does not rollback items that have already been successfully committed to the configuration store prior to the cancellation request.
*   **Thread Safety**: The service implementation is designed to be thread-safe. Multiple callers can invoke `GetStatusAsync` or `CancelAsync` for the same `OperationId` concurrently without causing state corruption. However, initiating two batch operations with the exact same input set simultaneously will result in distinct `OperationId`s and independent execution paths.
*   **Resource Cleanup**: Completed or failed operation status records are retained for a limited retention period defined by the server configuration. Attempting to access an `OperationId` older than this period via `GetStatusAsync` will result in an exception.
