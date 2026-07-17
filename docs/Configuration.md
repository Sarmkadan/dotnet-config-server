# Configuration

The `Configuration` type represents a versioned configuration item within the `dotnet-config-server` domain. It stores metadata such as identification, ownership, lifecycle timestamps, encryption details, and versioning relationships, and provides methods to validate the instance and generate a new version.

## API

### Id  
**Type:** `Guid`  
**Purpose:** Unique identifier for the configuration record. Should never be `Guid.Empty`.  
**Throws:** Property setters may throw `ArgumentException` if an empty `Guid` is assigned.

### Name  
**Type:** `string`  
**Purpose:** Human‑readable name of the configuration. Required; must not be null or whitespace.  
**Throws:** Setting to null, empty, or whitespace throws `ArgumentException`.

### Description  
**Type:** `string?`  
**Purpose:** Optional detailed description of the configuration’s purpose or usage. May be null.

### Environment  
**Type:** `Environment`  
**Purpose:** Indicates the deployment environment (e.g., Development, Staging, Production) to which the configuration belongs.  
**Throws:** Setting to null throws `ArgumentNullException`.

### ApplicationId  
**Type:** `Guid`  
**Purpose:** Identifier of the application that owns this configuration. Must not be `Guid.Empty`.  
**Throws:** Property setters may throw `ArgumentException` for an empty `Guid`.

### CreatedAt  
**Type:** `DateTime`  
**Purpose:** Timestamp when the configuration was first persisted. Set automatically on creation.  
**Throws:** Setting to a value later than `UpdatedAt` may throw `InvalidOperationException` (if enforced by validation).

### UpdatedAt  
**Type:** `DateTime`  
**Purpose:** Timestamp of the most recent mutation of the configuration. Updated on each change.  
**Throws:** Setting to a value earlier than `CreatedAt` may throw `InvalidOperationException`.

### DeletedAt  
**Type:** `DateTime?`  
**Purpose:** Soft‑delete timestamp; when non‑null the configuration is considered deleted. Null indicates an active record.

### CreatedBy  
**Type:** `string`  
**Purpose:** Identifier of the user or system that created the configuration. Required; must not be null or whitespace.  
**Throws:** Setting to null, empty, or whitespace throws `ArgumentException`.

### UpdatedBy  
**Type:** `string?`  
**Purpose:** Identifier of the user or system that last updated the configuration. May be null if never updated.

### DeletedBy  
**Type:** `string?`  
**Purpose:** Identifier of the user or system that performed the soft delete. May be null.

### IsActive  
**Type:** `bool`  
**Purpose:** Logical flag indicating whether the configuration is currently active. Typically false when `DeletedAt` has a value.

### IsEncrypted  
**Type:** `bool`  
**Purpose:** Indicates whether the configuration’s value is stored encrypted. When true, `EncryptionAlgorithm` and `EncryptionKeyId` must be provided.

### EncryptionAlgorithm  
**Type:** `EncryptionAlgorithm`  
**Purpose:** Specifies the algorithm used for encryption when `IsEncrypted` is true. Required if `IsEncrypted` is true.  
**Throws:** Setting to default (`None`) while `IsEncrypted` is true throws `InvalidOperationException`.

### EncryptionKeyId  
**Type:** `string?`  
**Purpose:** Identifier of the encryption key used; required when `IsEncrypted` is true. May be null otherwise.

### VersionNumber  
**Type:** `int`  
**Purpose:** Sequential version number for this configuration line. Starts at 1 and increments with each new version.  
**Throws:** Setting to a value less than 1 throws `ArgumentOutOfRangeException`.

### CurrentVersionId  
**Type:** `Guid?`  
**Purpose:** Identifier of the latest version in the version chain; null if this instance is the current version.

### ParentConfigurationId  
**Type:** `Guid?`  
**Purpose:** Identifier of the immediate predecessor version; null for the first version in a chain.

### Validate  
**Signature:** `public void Validate()`  
**Purpose:** Performs consistency checks on the instance (e.g., required fields, version ordering, encryption flags).  
**Parameters:** None.  
**Return:** None.  
**Throws:**  
- `InvalidOperationException` if required fields are missing or inconsistent (e.g., `IsEncrypted` true but `EncryptionAlgorithm` is `None`).  
- `ValidationException` (or a derived type) containing a collection of validation errors if multiple rules fail.

### CreateNewVersion  
**Signature:** `public Configuration CreateNewVersion()`  
**Purpose:** Produces a new `Configuration` instance representing the next version of this configuration. The new instance copies mutable properties, increments `VersionNumber`, sets `ParentConfigurationId` to this instance’s `Id`, and resets version‑linking fields (`CurrentVersionId` set to null on the new instance).  
**Parameters:** None.  
**Return:** A freshly instantiated `Configuration` object ready for persistence.  
**Throws:**  
- `InvalidOperationException` if the instance is not active (`IsActive` false) or has been soft‑deleted (`DeletedAt` has a value).  
- `ObjectDisposedException` if the instance has been disposed (if applicable).

## Usage

### Creating and validating a new configuration
```csharp
using DotNetConfigServer.Models; // Adjust namespace as needed

var config = new Configuration
{
    Id = Guid.NewGuid(),
    Name = "FeatureToggle.NewUI",
    Description = "Enables the new user interface for beta users.",
    Environment = Environment.Staging,
    ApplicationId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    CreatedBy = "ci-system",
    IsActive = true,
    IsEncrypted = false,
    VersionNumber = 1
};

config.Validate(); // Throws if any validation rule fails
// Persist `config` via repository or service layer
```

### Generating a subsequent version
```csharp
// Assume `current` is a fetched, active Configuration instance
var next = current.CreateNewVersion();

// Modify values for the new version as needed
next.Name = "FeatureToggle.NewUI";
next.Description = "Updated description after QA feedback.";
next.UpdatedAt = DateTime.UtcNow;
next.UpdatedBy = "developer-alice";
next.VersionNumber = current.VersionNumber + 1;

// Validate before saving
next.Validate();
// Persist `next` as the new version; optionally mark `current` as non‑current
```

## Notes

- **Nullability:** String‑valued properties that are nullable (`Description`, `UpdatedBy`, `DeletedBy`, `EncryptionKeyId`) may be left null; however, business rules may require them to be non‑null under certain conditions (e.g., `EncryptionKeyId` when `IsEncrypted` is true). Validation enforces these constraints.
- **Soft delete:** Setting `DeletedAt` to a non‑null value does not automatically update `IsActive`; callers should coordinate both fields to reflect the intended state.
- **Versioning:** `ParentConfigurationId` points to the immediate predecessor, while `CurrentVersionId` (when non‑null) indicates that another version is considered the head of the chain. The `CreateNewVersion` method does not modify the source instance; it is the caller’s responsibility to update any version‑linking fields on the original if needed.
- **Thread safety:** The class contains only mutable fields and no internal locking. Concurrent access to the same instance from multiple threads without external synchronization can lead to race conditions, particularly on properties like `UpdatedAt`, `VersionNumber`, and the version‑linking Guids. Consumers should either treat instances as immutable after publication or synchronize access using appropriate concurrency primitives (e.g., `lock`, `ReaderWriterLockSlim`, or immutable data structures).
- **Encryption semantics:** When `IsEncrypted` is true, both `EncryptionAlgorithm` and `EncryptionKeyId` must be valid; otherwise `Validate` will fail. Changing encryption state requires creating a new version with the appropriate flags set.
- **DateTime ordering:** Logical consistency expects `CreatedAt ≤ UpdatedAt` and, if present, `DeletedAt ≥ UpdatedAt`. Validation will reject instances that violate these temporal constraints.
