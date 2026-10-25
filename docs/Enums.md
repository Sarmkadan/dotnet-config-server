# Enums

The enums in `Common/Enums.cs` (namespace `DotnetConfigServer.Common`) are the shared vocabulary for status, type, and algorithm values across models, services, repositories, and controllers. Each enum is declared with explicit integer values, so the numbers stay fixed even if members are added later. Models use these enums instead of strings, which lets the compiler check every `switch` and comparison.

## API

Each enum is listed below with its values and where it is used. Values marked as not referenced are defined but no production code uses them yet.

### ConfigurationValueType
```csharp
public enum ConfigurationValueType
```
The data type of a configuration key's value. `ConfigurationKey.GetTypedValue()` parses the stored string according to this type.

- `String` (`0`) - free-form text value.
- `Integer` (`1`) - whole number, parsed with `int.Parse`.
- `Boolean` (`2`) - true/false value, parsed with `bool.Parse`.
- `Json` (`3`) - JSON document, deserialized with `System.Text.Json`.
- `Decimal` (`4`) - fixed-precision decimal, parsed with `decimal.Parse`.
- `DateTime` (`5`) - date and time, parsed with round-trip date handling.

Used by:
- `ConfigurationKey.ValueType` (`Models/ConfigurationKey.cs`), defaults to `String`. `GetTypedValue()` switches on it.

### Environment
```csharp
public enum Environment
```
The deployment environment a configuration belongs to. The name collides with `System.Environment`, so files that use it add an alias such as `using Environment = DotnetConfigServer.Common.Environment;` or `using Env = DotnetConfigServer.Common.Environment;`.

- `Development` (`0`) - local development environment.
- `Staging` (`1`) - pre-production testing environment.
- `Production` (`2`) - live production environment.

Used by:
- `Configuration.Environment` (`Models/Configuration.cs`), defaults to `Development`. `Configuration.Update(...)` takes it as a parameter.
- `ConfigurationSnapshotService` (`Services/ConfigurationSnapshotService.cs`), which casts the stored value to this enum in `DeserializeConfiguration`.
- `ConfigWatchController` (`Controllers/ConfigWatchController.cs`), which uses the alias `Env` for the `Environment` property of `WatchRequestContext`.

### EncryptionAlgorithm
```csharp
public enum EncryptionAlgorithm
```
The algorithm used to protect a configuration value.

- `None` (`0`) - value is stored without encryption.
- `AES256` (`1`) - AES with a 256-bit key.

Used by:
- `EncryptionKey.Algorithm` (`Models/EncryptionKey.cs`), defaults to `AES256`.
- `Configuration.EncryptionAlgorithm` (`Models/Configuration.cs`), defaults to `None`. `Configuration` sets `IsEncrypted` to `algorithm != None`.
- `EncryptionService` (`Services/EncryptionService.cs`), which creates keys with `AES256`.

### ConfigurationVersionStatus
```csharp
public enum ConfigurationVersionStatus
```
The lifecycle state of a configuration version. Transitions are made by `ConfigurationVersion` methods.

- `Draft` (`0`) - version is being prepared and has not been published. Default for new versions.
- `Active` (`1`) - version is the currently published one. Set by `Publish(...)`.
- `Archived` (`2`) - version was replaced by a newer one and is kept for history. Set by `Archive(...)`.
- `Deprecated` (`3`) - version is still stored but should no longer be used. Set by `Deprecate()`, which requires the version to be `Active`.

Used by:
- `ConfigurationVersion.Status` (`Models/ConfigurationVersion.cs`), defaults to `Draft`.
- `VersioningService` (`Services/VersioningService.cs`), which creates new versions as `Draft` and archives non-active versions during cleanup.
- `ConfigurationVersionRepository` (`Repositories/RepositoryImplementations.cs`), which queries for `Active` and `Archived` versions.

### WebhookDeliveryStatus
```csharp
public enum WebhookDeliveryStatus
```
The state of a single webhook delivery attempt chain.

- `Pending` (`0`) - delivery is queued and has not been attempted yet. Default for new deliveries.
- `Success` (`1`) - delivery was accepted by the receiver. Set by `MarkSuccess(...)`.
- `Failed` (`2`) - delivery failed and will not be retried. Set by `MarkFailed(...)`.
- `Retry` (`3`) - delivery failed and is scheduled for another attempt. Set by `ScheduleRetry(...)`.

Used by:
- `WebhookDelivery.Status` (`Models/WebhookDelivery.cs`), defaults to `Pending`. `ShouldRetry(...)` returns true only for `Failed` deliveries under the retry limit.
- `WebhookService` (`Services/WebhookService.cs`), which creates deliveries as `Pending` and checks for `Success`.
- `WebhookDeliveryRepository` (`Repositories/RepositoryImplementations.cs`), which filters for `Failed` and `Pending` deliveries.
- `HealthChecks` (`Infrastructure/HealthChecks.cs`), which reports the last delivery as failed when its status is `Failed`.

### ChangeType
```csharp
public enum ChangeType
```
The kind of difference a single key has between two configuration sets.

- `Added` (`0`) - a key or value was added.
- `Modified` (`1`) - a key or value was modified.
- `Deleted` (`2`) - a key or value was deleted.

Used by:
- `ConfigurationDiff` (`Models/ConfigurationDiff.cs`), which counts changes by type and builds a description for each one. Validation checks that `OldValue` and `NewValue` are set for the matching type.
- `DiffEntry` views (`Models/DiffViewerModels.cs`), which expose filtered lists of added, modified, and deleted keys.
- `IConfigDiffer` (`Services/IConfigDiffer.cs`), which produces `Added`, `Modified`, and `Deleted` changes.
- `DiffService`, `DiffViewerService`, `ComparisonService`, and `RollbackService`, which create, filter, or count changes by type.

### WebhookStatus
```csharp
public enum WebhookStatus
```
The state of a webhook subscription. Value `1` is not defined.

- `Active` (`0`) - subscription receives events.
- `Failed` (`2`) - subscription deliveries are failing. Set by `Deactivate()`.

Used by:
- `WebhookSubscription.Status` (`Models/WebhookSubscription.cs`), defaults to `Active`. `Activate()` and `ResetRetryCount(...)` set it back to `Active`. `Deactivate()` sets it to `Failed`, and `IncrementRetryCount(...)` deactivates the subscription once the retry limit is reached.
- `WebhookSubscriptionRepository` (`Repositories/RepositoryImplementations.cs`), which returns only subscriptions with `Active` status.

### AuditActionType
```csharp
public enum AuditActionType
```
The kind of action recorded in an audit log entry. Value `4` is not defined.

- `ConfigurationCreated` (`0`) - a configuration was created.
- `ConfigurationUpdated` (`1`) - a configuration was updated.
- `ConfigurationDeleted` (`2`) - a configuration was deleted.
- `VersionPublished` (`3`) - a configuration version was published. Not referenced by any code outside this file.
- `EncryptionKeyRotated` (`5`) - an encryption key was rotated. Not referenced by any code outside this file.
- `WebhookDelivered` (`6`) - a webhook notification was delivered. Not referenced by any code outside this file.

Used by:
- `AuditLog.ActionType` (`Models/AuditLog.cs`). The `CreateEntry`, `UpdateEntry`, and `DeleteEntry` factory methods set the matching value.
- `RollbackService` (`Services/RollbackService.cs`), which records rollbacks as `ConfigurationUpdated`.
- `AuditLogsController` (`Controllers/AuditLogsController.cs`), which counts recent creates, updates, and deletes.

### ChangeRequestStatus
```csharp
public enum ChangeRequestStatus
```
The state of a change request in the approval workflow.

- `Pending` (`0`) - request is waiting for a decision. Default for new requests.
- `Approved` (`1`) - request was approved.
- `Rejected` (`2`) - request was rejected.
- `Applied` (`3`) - approved request has been applied to the configuration.
- `Cancelled` (`4`) - request was cancelled by its author.

Used by:
- `ChangeRequest.Status` (`Models/ChangeRequest.cs`), defaults to `Pending`. `Approve()`, `Reject()`, `MarkApplied()`, and `Cancel()` set the other values.
- `ChangeRequestService` (`Services/ChangeRequestService.cs`), which checks for `Pending` before approving, rejecting, or cancelling, and sets `Pending` when a request is created.
- `IRepository.GetByConfigurationAsync` (`Repositories/IRepository.cs`) and `ChangeRequestRepository` (`Repositories/ChangeRequestRepository.cs`), which filter by status.

### ChangeRequestOperation
```csharp
public enum ChangeRequestOperation
```
The mutation a change request performs when it is applied.

- `CreateKey` (`0`) - create a new configuration key.
- `UpdateKey` (`1`) - update an existing configuration key.
- `DeleteKey` (`2`) - delete a configuration key.
- `UpdateConfiguration` (`3`) - update the configuration as a whole. Not handled by `ChangeRequestService`; applying such a request throws `ConfigurationException`.

Used by:
- `ChangeRequest.Operation` (`Models/ChangeRequest.cs`), which determines how `Payload` is read.
- `ChangeRequestService.ApplyAsync` (`Services/ChangeRequestService.cs`), which switches on the operation to call the matching key method.

## Usage

Compare enum values directly instead of converting them to strings:

```csharp
using DotnetConfigServer.Common;

if (request.Status != ChangeRequestStatus.Pending)
    throw new InvalidOperationException($"Request is already {request.Status}");
```

When a `switch` covers an enum, add a `default` branch that throws, as `ChangeRequestService` does. This keeps unsupported values such as `UpdateConfiguration` from being silently ignored.
