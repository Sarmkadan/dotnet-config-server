# ChangeRequest

Represents an atomic change operation within the configuration management lifecycle. Each instance tracks a single modification (create, update, or delete) against a configuration or its key, carrying the payload through a review workflow from initial request to approval, rejection, application, or cancellation. It maintains a full audit trail of who requested, reviewed, and applied the change, along with timestamps for each state transition.

## API

### Properties

#### `public Guid Id`
Globally unique identifier for this change request. Immutable after creation.

#### `public Guid ConfigurationId`
Identifier of the configuration this change targets. Must reference an existing configuration. Immutable after creation.

#### `public Guid? ConfigurationKeyId`
Identifier of the specific configuration key this change targets, if the operation scopes to a single key rather than the entire configuration. Null when the operation applies to the configuration as a whole.

#### `public ChangeRequestOperation Operation`
The type of mutation this request will perform when applied. Determines how the `Payload` is interpreted during application.

#### `public ChangeRequestStatus Status`
Current position in the change request lifecycle. Transitions occur only through the `Approve`, `Reject`, `MarkApplied`, and `Cancel` methods.

#### `public string Payload`
The serialized data to be applied. For create and update operations, contains the new value. For delete operations, may be empty or contain a tombstone marker. Must not be null, but may be an empty string.

#### `public string? Summary`
Optional human-readable description of the change for review purposes. Null when no summary was provided.

#### `public string RequestedBy`
Identity of the principal who submitted the change request. Immutable after creation.

#### `public DateTime RequestedAt`
UTC timestamp of when the request was originally submitted. Immutable after creation.

#### `public string? ReviewedBy`
Identity of the principal who performed the review (approval or rejection). Null until a review action occurs.

#### `public DateTime? ReviewedAt`
UTC timestamp of when the review action occurred. Null until a review action occurs.

#### `public string? ReviewComment`
Optional comment provided by the reviewer during approval or rejection. Null when no comment was supplied.

#### `public DateTime? AppliedAt`
UTC timestamp of when the change was successfully applied to the target configuration. Null until `MarkApplied` is called.

#### `public string? AppliedBy`
Identity of the principal or system component that applied the change. Null until `MarkApplied` is called.

### Methods

#### `public void Approve()`
Transitions the request from `PendingReview` to `Approved`. Sets `ReviewedBy`, `ReviewedAt`, and optionally `ReviewComment` based on the current reviewer context.

**Throws:** `InvalidOperationException` when `Status` is not `PendingReview`.

#### `public void Reject()`
Transitions the request from `PendingReview` to `Rejected`. Sets `ReviewedBy`, `ReviewedAt`, and optionally `ReviewComment` based on the current reviewer context.

**Throws:** `InvalidOperationException` when `Status` is not `PendingReview`.

#### `public void MarkApplied()`
Transitions the request from `Approved` to `Applied`. Sets `AppliedAt` and `AppliedBy` based on the current application context.

**Throws:** `InvalidOperationException` when `Status` is not `Approved`.

#### `public void Cancel()`
Transitions the request to `Cancelled`. Allowed from `PendingReview` or `Approved` states. When cancelled from `Approved`, the request is abandoned before application.

**Throws:** `InvalidOperationException` when `Status` is `Applied`, `Rejected`, or already `Cancelled`.

## Usage

### Example 1: Standard approval-to-application workflow

```csharp
// A change request is created externally and loaded
ChangeRequest request = changeRequestRepository.GetById(requestId);

// Reviewer examines the payload and summary, then approves
if (request.Status == ChangeRequestStatus.PendingReview)
{
    request.Approve();
    // request.Status is now Approved
    // request.ReviewedBy and request.ReviewedAt are populated
}

// Later, an automated job applies all approved changes
if (request.Status == ChangeRequestStatus.Approved)
{
    try
    {
        configurationService.ApplyChange(request);
        request.MarkApplied();
        // request.Status is now Applied
        // request.AppliedAt and request.AppliedBy are populated
    }
    catch (ApplicationException)
    {
        // Application failed; request remains Approved for retry
    }
}
```

### Example 2: Rejection with comment and cancellation of stale requests

```csharp
// Reviewer rejects a change with feedback
ChangeRequest request = changeRequestRepository.GetById(requestId);

if (request.Status == ChangeRequestStatus.PendingReview)
{
    // ReviewComment is set externally before rejection
    request.Reject();
    // request.Status is now Rejected
    // The requester can read ReviewComment for feedback
}

// Cleanup: cancel stale approved requests that are no longer relevant
ChangeRequest staleRequest = changeRequestRepository.GetById(staleRequestId);

if (staleRequest.Status == ChangeRequestStatus.Approved)
{
    staleRequest.Cancel();
    // request.Status is now Cancelled
    // No application will occur; audit trail preserved
}
```

## Notes

- **State machine enforcement:** Each transition method throws `InvalidOperationException` if called when the request is not in a valid source state. Callers must check `Status` before invoking transition methods or handle the exception explicitly.
- **Immutable identity fields:** `Id`, `ConfigurationId`, `RequestedBy`, and `RequestedAt` are set at creation time and must not be modified afterward. The type does not expose setters for these properties outside of construction.
- **Nullability of reviewer and applier fields:** `ReviewedBy`, `ReviewedAt`, `ReviewComment`, `AppliedAt`, and `AppliedBy` remain null until their corresponding lifecycle methods are called. Code consuming these properties must null-check before dereferencing.
- **Payload immutability during review:** The `Payload` property must not change after the request enters `PendingReview`. Validation of payload integrity against the original submission is the caller's responsibility.
- **Thread safety:** Instance methods are not thread-safe. Concurrent calls to `Approve`, `Reject`, `Cancel`, or `MarkApplied` on the same instance from multiple threads will produce unpredictable state transitions. Synchronization must be enforced externally when sharing instances across threads.
- **Re-entrancy:** Once a terminal state (`Applied`, `Rejected`, `Cancelled`) is reached, no further transitions are permitted. Any subsequent call to a transition method will throw.
- **ConfigurationKeyId semantics:** When `ConfigurationKeyId` is null, the operation targets the entire configuration. Application logic must branch on this nullability to determine whether to replace the whole configuration or a single key.
