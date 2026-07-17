# DotnetConfigServerOptions

Configuration options for the .NET Configuration Server, providing centralized management of application settings, security, performance, caching, and other runtime behaviors.

## API

### `public ApplicationSettingsOptions ApplicationSettings`
Gets or sets the application-specific settings configuration. This includes settings like feature flags, logging preferences, and other application-level configurations. The options are applied when the server initializes.

### `public EncryptionOptions Encryption`
Gets or sets the encryption configuration for securing sensitive data. This includes settings like key size, salt size, and encryption algorithms. Throws `ArgumentNullException` if set to `null`.

### `public WebhookOptions Webhook`
Gets or sets the webhook configuration for external integrations. This includes settings like endpoint URLs, retry policies, and authentication. Throws `ArgumentNullException` if set to `null`.

### `public RateLimitOptions RateLimit`
Gets or sets the rate limiting configuration to control API request throughput. This includes settings like burst limits, sustained limits, and response headers. Throws `ArgumentNullException` if set to `null`.

### `public CacheOptions Cache`
Gets or sets the caching configuration for in-memory and distributed caching. This includes settings like cache duration, eviction policies, and storage providers. Throws `ArgumentNullException` if set to `null`.

### `public DatabaseOptions Database`
Gets or sets the database configuration for persistent storage. This includes connection strings, timeouts, and retry policies. Throws `ArgumentNullException` if set to `null`.

### `public PerformanceOptions Performance`
Gets or sets the performance tuning options for the server. This includes settings like thread pool adjustments, garbage collection tuning, and buffer sizes. Throws `ArgumentNullException` if set to `null`.

### `public SecurityOptions Security`
Gets or sets the security hardening options for the server. This includes settings like CORS policies, CSRF protection, and input validation rules. Throws `ArgumentNullException` if set to `null`.

### `public int DefaultDurationSeconds`
Gets or sets the default duration in seconds for temporary resources (e.g., cache entries, tokens). Must be a positive integer. Throws `ArgumentOutOfRangeException` if set to a value ≤ 0.

### `public bool EnableDistributedCache`
Gets or sets a value indicating whether distributed caching is enabled. When `true`, the server uses a distributed cache backend (e.g., Redis) for shared state across instances. Defaults to `false`.

### `public int DistributedCacheDurationSeconds`
Gets or sets the duration in seconds for items stored in the distributed cache. Must be a positive integer. Ignored if `EnableDistributedCache` is `false`. Throws `ArgumentOutOfRangeException` if set to a value ≤ 0.

### `public string ApiVersion`
Gets or sets the API version string exposed by the server. Used for versioning endpoints and responses. Must be a non-empty string. Throws `ArgumentException` if set to `null` or whitespace.

### `public int MaxVersionHistory`
Gets or sets the maximum number of historical API versions to retain. Must be a non-negative integer. Used for version cleanup and rollback scenarios. Throws `ArgumentOutOfRangeException` if set to a value < 0.

### `public bool EnableCors`
Gets or sets a value indicating whether Cross-Origin Resource Sharing (CORS) is enabled. When `true`, the server permits cross-origin requests based on configured policies.

### `public bool EnableSwagger`
Gets or sets a value indicating whether Swagger/OpenAPI documentation is enabled. When `true`, the server exposes interactive API documentation at `/swagger`.

### `public bool EnableDetailedErrors`
Gets or sets a value indicating whether detailed error responses are enabled. When `true`, the server includes stack traces and diagnostic information in error responses. Disable in production for security.

### `public bool EnableRequestLogging`
Gets or sets a value indicating whether HTTP request logging is enabled. When `true`, the server logs incoming requests for diagnostics and auditing.

### `public bool EnablePerformanceMonitoring`
Gets or sets a value indicating whether performance monitoring is enabled. When `true`, the server tracks metrics like request latency, throughput, and error rates.

### `public int KeySize`
Gets or sets the size in bits for cryptographic keys used in encryption and signing. Must be a positive multiple of 8. Throws `ArgumentOutOfRangeException` if set to a value ≤ 0 or not divisible by 8.

### `public int SaltSize`
Gets or sets the size in bytes for salt values used in encryption. Must be a positive integer. Throws `ArgumentOutOfRangeException` if set to a value ≤ 0.

## Usage

### Basic Configuration
