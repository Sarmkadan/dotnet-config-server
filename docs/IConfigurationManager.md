# IConfigurationManager

Central interface for retrieving, tracking, and synchronizing configuration values across services. Provides change detection, auditing, and type-safe retrieval with optional caching. Designed for distributed systems where configuration may be updated externally or via background services.

## API

### `public string Key`
Unique identifier for a configuration entry. Used as the primary lookup key when retrieving or tracking configuration values. Immutable after construction.

### `public string OldValue`
Previous value of the configuration entry before the most recent change. `null` if the entry has never been modified or if the change originated locally.

### `public string NewValue`
Current value of the configuration entry. Reflects the latest known state, whether updated via external source, user action, or internal process.

### `public DateTime Timestamp`
Moment when the configuration change was recorded. Represents the time of the last detected or applied change. Useful for ordering, auditing, and determining staleness.

### `public CachedConfigurationManager`
Reference to the underlying cache manager responsible for storing and serving configuration values. Provides access to cached snapshots and change history.

### `public async Task<string> GetConfigurationValueAsync()`
Retrieves the current value of the configuration entry as a string.

- **Returns**: The current value of the configuration entry.
- **Throws**: `InvalidOperationException` if the configuration entry is not found or the backing store is unavailable.

### `public async Task<T> GetConfigurationAsync<T>()`
Retrieves the current value of the configuration entry and deserializes it to the specified type `T`.

- **Type Parameter**: `T` – The type to deserialize the configuration value into. Must be deserializable from JSON.
- **Returns**: The deserialized configuration value of type `T`.
- **Throws**: `InvalidOperationException` if the configuration entry is not found, the value cannot be deserialized, or the backing store is unavailable.

### `public Guid Id`
Globally unique identifier for this configuration entry instance. Used for tracking, logging, and correlation across services.

### `public List<ConfigurationKeyDto> Keys`
Collection of all known configuration keys managed by this instance. Used for bulk inspection, validation, or synchronization.

### `public string Key`
Alias for the primary configuration key. Identical in purpose and behavior to the `Key` property above.

### `public string Value`
Current value of the configuration entry. Equivalent to the result of `GetConfigurationValueAsync()` but provided synchronously for convenience.

### `public OrderService`
Service responsible for processing configuration-driven orders or actions. Used to trigger business logic when configuration changes occur.

### `public async Task ProcessOrderAsync()`
Triggers processing of any pending orders or actions derived from the current configuration state. Typically invoked after configuration changes are detected.

- **Throws**: `InvalidOperationException` if the `OrderService` is not initialized or the processing fails.

### `public ConfigurationSyncBackgroundService`
Background service responsible for periodically synchronizing configuration with external sources (e.g., config server, database, or file system).

### `public static IServiceCollection AddConfigurationManager(this IServiceCollection services)`
Extension method to register the configuration manager and its dependencies in the dependency injection container.

- **Parameters**: `services` – The `IServiceCollection` to configure.
- **Returns**: The configured `IServiceCollection` for method chaining.

### `public static async Task Main()`
Entry point for applications using this manager. Bootstraps configuration, starts synchronization, and begins processing.

- **Returns**: A `Task` representing the application lifecycle.

### `public Guid Id`
Unique identifier for this manager instance. Used for logging, correlation, and distinguishing between multiple manager instances.

### `public decimal Total`
Aggregated metric representing the total number of configuration entries managed or processed. Used for monitoring and scaling decisions.

## Usage

### Example 1: Basic Retrieval and Monitoring
