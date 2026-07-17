# ConfigurationException

The `ConfigurationException` class and its derived exceptions provide a consistent way to signal configuration-related errors in the `dotnet-config-server` project. These exceptions are thrown when expected configuration resources (configurations, keys, snapshots, versions, webhooks, or encryption operations) are missing, invalid, or otherwise unavailable. They derive from standard .NET exception types and include contextual information in their messages to aid debugging.

## API

### `ConfigurationException(string message)`
Constructs a new `ConfigurationException` with the specified error message.
- **Parameters**
  - `message` (string): The error message describing the configuration failure.
- **Notes**
  - Inherits from `System.Exception`; does not include additional context in the message by default.

### `ConfigurationException(string message, Exception innerException)`
Constructs a new `ConfigurationException` with the specified error message and inner exception.
- **Parameters**
  - `message` (string): The error message describing the configuration failure.
  - `innerException` (Exception): The exception that is the cause of the current exception.
- **Notes**
  - Inherits from `System.Exception`; propagates the inner exception for chaining.

### `ConfigurationNotFoundException(string configId)`
Constructs a new `ConfigurationNotFoundException` indicating that a configuration with the specified identifier was not found.
- **Parameters**
  - `configId` (string): The identifier of the missing configuration.
- **Message Format**
  - `"Configuration '{configId}' not found."`

### `ConfigurationNotFoundException(Guid configId)`
Constructs a new `ConfigurationNotFoundException` indicating that a configuration with the specified GUID was not found.
- **Parameters**
  - `configId` (Guid): The GUID of the missing configuration.
- **Message Format**
  - `"Configuration '{configId}' not found."`

### `ConfigurationKeyNotFoundException(string key)`
Constructs a new `ConfigurationKeyNotFoundException` indicating that a configuration key with the specified name was not found.
- **Parameters**
  - `key` (string): The name of the missing configuration key.
- **Message Format**
  - `"Configuration key '{key}' not found."`

### `ConfigurationKeyNotFoundException(Guid keyId)`
Constructs a new `ConfigurationKeyNotFoundException` indicating that a configuration key with the specified GUID was not found.
- **Parameters**
  - `keyId` (Guid): The GUID of the missing configuration key.
- **Message Format**
  - `"Configuration key '{keyId}' not found."`

### `EncryptionException(string message)`
Constructs a new `EncryptionException` indicating a failure during encryption or decryption operations.
- **Parameters**
  - `message` (string): The error message describing the encryption failure.
- **Notes**
  - Inherits from `System.Exception`; used for cryptographic operation errors.

### `EncryptionException(string message, Exception innerException)`
Constructs a new `EncryptionException` indicating a failure during encryption or decryption operations with an inner exception.
- **Parameters**
  - `message` (string): The error message describing the encryption failure.
  - `innerException` (Exception): The exception that is the cause of the current exception.
- **Notes**
  - Inherits from `System.Exception`; propagates the inner exception for chaining.

### `ConfigurationSnapshotNotFoundException(string snapshotId)`
Constructs a new `ConfigurationSnapshotNotFoundException` indicating that a configuration snapshot with the specified identifier was not found.
- **Parameters**
  - `snapshotId` (string): The identifier of the missing configuration snapshot.
- **Message Format**
  - `"Configuration snapshot '{snapshotId}' not found."`

### `ConfigurationSnapshotNotFoundException(Guid snapshotId)`
Constructs a new `ConfigurationSnapshotNotFoundException` indicating that a configuration snapshot with the specified GUID was not found.
- **Parameters**
  - `snapshotId` (Guid): The GUID of the missing configuration snapshot.
- **Message Format**
  - `"Configuration snapshot '{snapshotId}' not found."`

### `ConfigurationVersionNotFoundException(string versionId)`
Constructs a new `ConfigurationVersionNotFoundException` indicating that a configuration version with the specified identifier was not found.
- **Parameters**
  - `versionId` (string): The identifier of the missing configuration version.
- **Message Format**
  - `"Configuration version '{versionId}' not found."`

### `ConfigurationVersionNotFoundException(Guid versionId)`
Constructs a new `ConfigurationVersionNotFoundException` indicating that a configuration version with the specified GUID was not found.
- **Parameters**
  - `versionId` (Guid): The GUID of the missing configuration version.
- **Message Format**
  - `"Configuration version '{versionId}' not found."`

### `WebhookException(string message)`
Constructs a new `WebhookException` indicating a failure related to webhook operations.
- **Parameters**
  - `message` (string): The error message describing the webhook failure.
- **Notes**
  - Inherits from `System.Exception`; used for webhook delivery or subscription errors.

### `WebhookException(string webhookId, string message)`
Constructs a new `WebhookException` indicating a failure related to a specific webhook with the given identifier.
- **Parameters**
  - `webhookId` (string): The identifier of the webhook associated with the failure.
  - `message` (string): The error message describing the webhook failure.
- **Notes**
  - Inherits from `System.Exception`; includes the webhook identifier in the diagnostic context.

## Usage

### Retrieving a Missing Configuration
