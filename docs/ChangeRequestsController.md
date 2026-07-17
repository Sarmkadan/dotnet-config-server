# ChangeRequestsController

Manages the lifecycle of configuration change requests in the dotnet-config-server. This controller provides endpoints to submit, review, approve, reject, cancel, and query change requests, enabling a gated workflow where configuration modifications require explicit approval before being applied.

## API

### `public ChangeRequestsController`

Default constructor. Instantiates the controller with its required dependencies injected by the framework.

### `public async Task<IActionResult> Submit`

Submits a new change request for a configuration. The request enters a pending state and awaits review.

**Parameters:** The change request payload is bound from the request body. Expected fields include the target configuration identifier, the proposed changes, and an optional description.

**Returns:** `201 Created` with the created change request resource on success. `400 Bad Request` if the payload fails validation or the target configuration does not exist.

**Throws:** No documented exceptions beyond standard framework-level dispatch failures.

### `public async Task<IActionResult> GetById`

Retrieves a single change request by its unique identifier.

**Parameters:** The change request ID is taken from the route (e.g., `{id}`).

**Returns:** `200 OK` with the change request object. `404 Not Found` if no change request matches the given ID.

**Throws:** No documented exceptions.

### `public async Task<IActionResult> GetPending`

Lists all change requests currently in a pending state, awaiting review.

**Parameters:** None.

**Returns:** `200 OK` with a collection of pending change requests. Returns an empty collection if none are pending.

### `public async Task<IActionResult> GetByConfiguration`

Retrieves all change requests associated with a specific configuration, regardless of their state.

**Parameters:** The configuration identifier is taken from the route (e.g., `{configurationId}`).

**Returns:** `200 OK` with a collection of change requests for that configuration. `404 Not Found` if the configuration itself does not exist.

### `public async Task<IActionResult> Approve`

Approves a pending change request, transitioning it to an approved state and typically triggering the application of the proposed changes.

**Parameters:** The change request ID from the route, and a `ReviewDecisionRequest` body containing an optional reviewer comment.

**Returns:** `200 OK` with the updated change request. `404 Not Found` if the change request does not exist. `409 Conflict` if the change request is not in a state that allows approval (e.g., already approved, rejected, or cancelled).

### `public async Task<IActionResult> Reject`

Rejects a pending change request, preventing the proposed changes from being applied.

**Parameters:** The change request ID from the route, and a `ReviewDecisionRequest` body containing an optional reason for rejection.

**Returns:** `200 OK` with the updated change request. `404 Not Found` if the change request does not exist. `409 Conflict` if the change request is not in a pending state.

### `public async Task<IActionResult> Cancel`

Cancels a pending change request, typically by its original submitter. This permanently closes the request without applying changes.

**Parameters:** The change request ID from the route.

**Returns:** `200 OK` with the cancelled change request. `404 Not Found` if the change request does not exist. `409 Conflict` if the change request is not in a cancellable state.

### `public sealed record ReviewDecisionRequest`

A data transfer record representing the payload for approval and rejection actions.

**Properties:**
- `Comment` (string, optional): A reviewer comment explaining the decision.

## Usage

### Submitting and Approving a Change Request

```csharp
// Assume _client is an HttpClient pointed at the config server
var submitPayload = new
{
    ConfigurationId = "app-settings-prod",
    ProposedChanges = new { LogLevel = "Debug", MaxRetries = 5 },
    Description = "Adjust logging and retry policy for diagnostics"
};

// Submit
var submitResponse = await _client.PostAsJsonAsync("/api/changerequests", submitPayload);
submitResponse.EnsureSuccessStatusCode();

var changeRequest = await submitResponse.Content.ReadFromJsonAsync<ChangeRequest>();
var requestId = changeRequest.Id;

// Approve
var decision = new ReviewDecisionRequest { Comment = "Approved for production rollout." };
var approveResponse = await _client.PostAsJsonAsync(
    $"/api/changerequests/{requestId}/approve", decision);
approveResponse.EnsureSuccessStatusCode();
```

- **Querying Pending Requests and Rejecting One**

```csharp
// Fetch all pending change requests
var pendingResponse = await _client.GetAsync("/api/changerequests/pending");
pendingResponse.EnsureSuccessStatusCode();

var pendingRequests = await pendingResponse.Content
    .ReadFromJsonAsync<List<ChangeRequest>>();

// Reject the first one that touches a specific configuration
var target = pendingRequests?.FirstOrDefault(r => r.ConfigurationId == "legacy-service");
if (target != null)
{
    var rejection = new ReviewDecisionRequest { Comment = "Legacy service is frozen." };
    var rejectResponse = await _client.PostAsJsonAsync(
        $"/api/changerequests/{target.Id}/reject", rejection);
    rejectResponse.EnsureSuccessStatusCode();
}
```

## Notes

- **State machine enforcement:** Approve, Reject, and Cancel validate the current state of the change request. Attempting an action on a request that is not in the expected state (typically `Pending`) results in a `409 Conflict`. Clients should inspect the current state before invoking these endpoints.
- **Idempotency:** `GetById`, `GetPending`, and `GetByConfiguration` are safe, idempotent reads. `Approve`, `Reject`, and `Cancel` are not idempotent; repeating a successful call will likely yield a `409 Conflict` since the state will have already transitioned.
- **Thread safety:** The controller itself does not manage concurrency. If multiple reviewers attempt to approve or reject the same request simultaneously, the underlying service layer must handle optimistic concurrency or locking to prevent race conditions. The `409 Conflict` response serves as the primary safeguard against conflicting state transitions.
- **`ReviewDecisionRequest` immutability:** The record is sealed and immutable by design, ensuring that decision payloads are not modified after construction.
