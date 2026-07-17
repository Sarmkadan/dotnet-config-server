# AuditLog

Represents an immutable audit entry for configuration changes, captures who did what, when, and to which entity, including before/after states and contextual request data.

## API

### `public Guid Id`
Unique identifier for the audit entry. Automatically assigned when the entry is created.

### `public AuditActionType ActionType`
Gets the type of audit action performed (e.g., Create, Update, Delete). Determines the semantic meaning of the entry.

### `public string EntityType`
Fully qualified type name of the entity being audited (e.g., `DotnetConfigServer.Models.ConfigurationSnapshot`). Used to categorize audit entries by domain object.

### `public string EntityId`
Primary identifier of the audited entity (e.g., a GUID or string key). Uniquely identifies the entity within its `EntityType`.

### `public string? EntityName`
Optional human-readable name of the entity at the time of the action. May be null if not provided or unavailable.

### `public DateTime Timestamp`
UTC timestamp when the audit entry was created. Reflects the moment the action occurred or was recorded.

### `public string UserId`
Identifier of the user who performed the action (e.g., a GUID or username). Required for accountability.

### `public string? UserEmail`
Optional email address of the user who performed the action. May be null if not collected.

### `public string Status`
Current processing status of the audit entry (e.g., "Pending", "Completed", "Failed"). Used to track lifecycle and filtering.

### `public string? Details`
Optional additional context about the action. May include reasons, comments, or system-generated notes.

### `public string? OldValues`
JSON-serialized representation of the entity state before the action, if applicable. Used for diffing and rollback scenarios.

### `public string? NewValues`
JSON-serialized representation of the entity state after the action, if applicable. Enables reconstruction of changes.

### `public string? IpAddress`
Optional IP address of the client that initiated the action. Used for security and access logging.

### `public string? UserAgent`
Optional user agent string of the client that initiated the action. Used for device and client identification.

### `public string? ConfigurationId`
Optional identifier of the configuration entity involved in the action. Used to correlate audit entries with specific configurations.

### `public static AuditLog CreateEntry(AuditActionType actionType, string entityType, string entityId, string? entityName, string userId, string? userEmail, string? details, string? ipAddress, string? userAgent, Guid configurationId)`
Creates a new audit entry for a creation action.

- **Parameters**:
  - `actionType`: Must be `AuditActionType.Create`.
  - `entityType`: Non-null type name of the entity being created.
  - `entityId`: Non-null identifier of the new entity.
  - `entityName`: Optional display name of the entity.
  - `userId`: Non-null identifier of the creating user.
  - `userEmail`: Optional email of the user.
  - `details`: Optional context about the creation.
  - `ipAddress`: Optional client IP address.
  - `userAgent`: Optional client user agent.
  - `configurationId`: Identifier of the configuration being created.
- **Returns**: A new `AuditLog` instance with `ActionType = Create`, `Timestamp = UtcNow`, and `Status = "Pending"`.
- **Throws**: `ArgumentNullException` if `actionType`, `entityType`, `entityId`, or `userId` is null.

### `public static AuditLog UpdateEntry(AuditActionType actionType, string entityType, string entityId, string? entityName, string userId, string? userEmail, string oldValues, string newValues, string? details, string? ipAddress, string? userAgent, Guid configurationId)`
Creates a new audit entry for an update action.

- **Parameters**:
  - `actionType`: Must be `AuditActionType.Update`.
  - `entityType`: Non-null type name of the entity being updated.
  - `entityId`: Non-null identifier of the entity.
  - `entityName`: Optional display name of the entity.
  - `userId`: Non-null identifier of the updating user.
  - `userEmail`: Optional email of the user.
  - `oldValues`: Non-null JSON-serialized state before the update.
  - `newValues`: Non-null JSON-serialized state after the update.
  - `details`: Optional context about the update.
  - `ipAddress`: Optional client IP address.
  - `userAgent`: Optional client user agent.
  - `configurationId`: Identifier of the configuration being updated.
- **Returns**: A new `AuditLog` instance with `ActionType = Update`, `Timestamp = UtcNow`, and `Status = "Pending"`.
- **Throws**: `ArgumentNullException` if any required parameter is null.

### `public static AuditLog DeleteEntry(AuditActionType actionType, string entityType, string entityId, string? entityName, string userId, string? userEmail, string? details, string? ipAddress, string? userAgent, Guid configurationId)`
Creates a new audit entry for a deletion action.

- **Parameters**:
  - `actionType`: Must be `AuditActionType.Delete`.
  - `entityType`: Non-null type name of the entity being deleted.
  - `entityId`: Non-null identifier of the entity.
  - `entityName`: Optional display name of the entity.
  - `userId`: Non-null identifier of the deleting user.
  - `userEmail`: Optional email of the user.
  - `details`: Optional context about the deletion.
  - `ipAddress`: Optional client IP address.
  - `userAgent`: Optional client user agent.
  - `configurationId`: Identifier of the configuration being deleted.
- **Returns**: A new `AuditLog` instance with `ActionType = Delete`, `Timestamp = UtcNow`, and `Status = "Pending"`.
- **Throws**: `ArgumentNullException` if `actionType`, `entityType`, `entityId`, or `userId` is null.

### `public void SetRequestContext(string? ipAddress, string? userAgent)`
Populates the audit entry with request context data.

- **Parameters**:
  - `ipAddress`: Optional IP address of the client.
  - `userAgent`: Optional user agent string of the client.
- **Throws**: None.

### `public void MarkAsFailed(string failureReason)`
Updates the audit entry status to indicate failure and records the reason.

- **Parameters**:
  - `failureReason`: Non-null description of why the action failed.
- **Throws**: `ArgumentNullException` if `failureReason` is null.
- **Notes**: Only valid to call if `Status` is `"Pending"`. Sets `Status = "Failed"` and stores the reason in `Details`.

## Usage

### Creating and persisting an audit entry for a configuration update
