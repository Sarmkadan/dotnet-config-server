# ConfigurationBenchmarks

Benchmark suite for evaluating the performance of configuration operations in the dotnet-config-server. This class measures throughput and latency for common configuration management tasks including CRUD operations, key management, and encrypted configuration handling.

## API

### `GlobalSetup`
Initializes the benchmark environment before any tests run. Sets up in-memory test infrastructure, test databases, and any required services. Must be called before any other members.

**Parameters:** None
**Return value:** `Task` (completed when setup is finished)
**Throws:** `InvalidOperationException` if called after `GlobalCleanup` or if setup fails.

---

### `GlobalCleanup`
Releases resources allocated during `GlobalSetup`. Cleans up test databases, services, and in-memory state. Must be called after all benchmark tests complete.

**Parameters:** None
**Return value:** `Task` (completed when cleanup is finished)
**Throws:** `InvalidOperationException` if called before `GlobalSetup` or if cleanup fails.

---

### `CreateConfiguration`
Creates a new configuration entry in the server. Used to benchmark configuration creation throughput.

**Parameters:**
- `config`: The configuration object to create (must not be null)
**Return value:** `Task` returning the created configuration's unique identifier
**Throws:**
- `ArgumentNullException` if `config` is null
- `InvalidOperationException` if the configuration already exists or if the server is unavailable

---

### `GetConfigurationById`
Retrieves a configuration by its unique identifier. Used to benchmark read latency for single configurations.

**Parameters:**
- `id`: The unique identifier of the configuration to retrieve
**Return value:** `Task` returning the configuration object if found, otherwise null
**Throws:** `ArgumentException` if `id` is empty or invalid

---
### `GetConfigurationsByApplication`
Retrieves all configurations associated with a given application name. Used to benchmark batch read operations.

**Parameters:**
- `applicationName`: The name of the application whose configurations are being retrieved
**Return value:** `Task` returning a collection of configuration objects
**Throws:** `ArgumentException` if `applicationName` is null or whitespace

---
### `UpdateConfiguration`
Updates an existing configuration entry. Used to benchmark configuration update throughput.

**Parameters:**
- `id`: The unique identifier of the configuration to update
- `config`: The updated configuration object (must not be null)
**Return value:** `Task` returning true if the update succeeded, false otherwise
**Throws:**
- `ArgumentNullException` if `config` is null
- `ArgumentException` if `id` is empty or invalid

---
### `SearchConfigurations`
Searches configurations using a query string. Used to benchmark search performance.

**Parameters:**
- `query`: The search query string
**Return value:** `Task` returning a collection of matching configuration objects
**Throws:** `ArgumentException` if `query` is null or whitespace

---
### `GetKeys`
Retrieves all configuration keys currently stored in the server. Used to benchmark key enumeration.

**Parameters:** None
**Return value:** `Task` returning a collection of key names
**Throws:** None

---
### `SearchKeys`
Searches configuration keys using a query string. Used to benchmark key search performance.

**Parameters:**
- `query`: The search query string
**Return value:** `Task` returning a collection of matching key names
**Throws:** `ArgumentException` if `query` is null or whitespace

---
### `GetConfigurationCount`
Retrieves the total number of configurations stored in the server. Used to benchmark count operations.

**Parameters:** None
**Return value:** `Task` returning the total count of configurations
**Throws:** None

---
### `AddKey`
Adds a new key to the configuration server. Used to benchmark key creation throughput.

**Parameters:**
- `key`: The name of the key to add (must not be null or whitespace)
**Return value:** `Task` returning true if the key was added, false if it already exists
**Throws:** `ArgumentException` if `key` is null or whitespace

---
### `UpdateKey`
Updates an existing key in the configuration server. Used to benchmark key update throughput.

**Parameters:**
- `key`: The name of the key to update (must not be null or whitespace)
- `value`: The new value for the key
**Return value:** `Task` returning true if the update succeeded, false otherwise
**Throws:** `ArgumentException` if `key` is null or whitespace

---
### `DeleteKey`
Removes a key from the configuration server. Used to benchmark key deletion throughput.

**Parameters:**
- `key`: The name of the key to delete
**Return value:** `Task` returning true if the key was deleted, false if it did not exist
**Throws:** `ArgumentException` if `key` is null or whitespace

---
### `CreateConfigurationWithEncryption`
Creates a new encrypted configuration entry in the server. Used to benchmark encrypted configuration creation throughput.

**Parameters:**
- `config`: The encrypted configuration object to create (must not be null)
**Return value:** `Task` returning the created configuration's unique identifier
**Throws:**
- `ArgumentNullException` if `config` is null
- `InvalidOperationException` if encryption is not supported or if the configuration already exists

---
### `GetConfigurationWithEncryption`
Retrieves an encrypted configuration by its unique identifier. Used to benchmark read latency for encrypted configurations.

**Parameters:**
- `id`: The unique identifier of the encrypted configuration to retrieve
**Return value:** `Task` returning the decrypted configuration object if found, otherwise null
**Throws:** `ArgumentException` if `id` is empty or invalid

## Usage

### Example 1: Basic Configuration CRUD Benchmark
