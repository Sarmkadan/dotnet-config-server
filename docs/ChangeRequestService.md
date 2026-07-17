# ChangeRequestService
The `ChangeRequestService` class is designed to manage change requests for configurations in the dotnet-config-server project. It provides methods for submitting new change requests, retrieving existing requests, and performing actions on them such as approval, rejection, and cancellation. This service is a crucial component in the configuration management process, allowing for controlled and auditable changes to configurations.

## API
* `public ChangeRequestService`: The constructor for the `ChangeRequestService` class.
* `public async Task<ChangeRequest> SubmitAsync`: Submits a new change request. This method returns a `ChangeRequest` object representing the newly submitted request. It throws if the submission fails due to validation errors or other internal errors.
* `public async Task<List<ChangeRequest>> GetByConfigurationAsync`: Retrieves a list of change requests associated with a specific configuration. The method returns a list of `ChangeRequest` objects. It throws if the retrieval fails due to internal errors.
* `public async Task<List<ChangeRequest>> GetPendingAsync`: Retrieves a list of pending change requests. The method returns a list of `ChangeRequest` objects. It throws if the retrieval fails due to internal errors.
* `public async Task<ChangeRequest?> GetByIdAsync`: Retrieves a change request by its ID. The method returns a `ChangeRequest` object if found, or `null` if not found. It throws if the retrieval fails due to internal errors.
* `public async Task<ChangeRequest> ApproveAsync`: Approves a change request. The method returns the approved `ChangeRequest` object. It throws if the approval fails due to validation errors or other internal errors.
* `public async Task<ChangeRequest> RejectAsync`: Rejects a change request. The method returns the rejected `ChangeRequest` object. It throws if the rejection fails due to validation errors or other internal errors.
* `public async Task<ChangeRequest> CancelAsync`: Cancels a change request. The method returns the cancelled `ChangeRequest` object. It throws if the cancellation fails due to validation errors or other internal errors.

## Usage
The following examples demonstrate how to use the `ChangeRequestService` class:
```csharp
// Example 1: Submitting a new change request
var service = new ChangeRequestService();
var newRequest = await service.SubmitAsync(new ChangeRequest { /* request details */ });
Console.WriteLine($"New request submitted: {newRequest.Id}");

// Example 2: Approving a pending change request
var pendingRequests = await service.GetPendingAsync();
var requestToApprove = pendingRequests.FirstOrDefault(r => r.Id == "request-id");
if (requestToApprove != null)
{
    var approvedRequest = await service.ApproveAsync(requestToApprove);
    Console.WriteLine($"Request {approvedRequest.Id} approved");
}
```

## Notes
* The `ChangeRequestService` class is designed to be thread-safe, allowing for concurrent access and modification of change requests.
* When retrieving change requests, the service may return cached results to improve performance. However, this caching mechanism does not affect the thread-safety of the class.
* The `ApproveAsync`, `RejectAsync`, and `CancelAsync` methods may throw exceptions if the corresponding action fails due to validation errors or other internal errors. It is recommended to handle these exceptions accordingly to ensure robust error handling.
* The `GetByIdAsync` method returns `null` if the requested change request is not found. This allows for distinguishing between successful retrieval of a non-existent request and internal errors.
