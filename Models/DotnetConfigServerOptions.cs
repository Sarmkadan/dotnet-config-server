using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Models;

/// <summary>
/// Main configuration options for Dotnet Config Server
/// </summary>
public class DotnetConfigServerOptions
{
    /// <summary>
    /// Application-specific settings
    /// </summary>
    [Required]
    public ApplicationSettingsOptions ApplicationSettings { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Encryption settings for sensitive configuration values
    /// </summary>
    [Required]
    public EncryptionOptions Encryption { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Webhook delivery configuration
    /// </summary>
    [Required]
    public WebhookOptions Webhook { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Rate limiting configuration to prevent abuse
    /// </summary>
    [Required]
    public RateLimitOptions RateLimit { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Caching configuration for performance optimization
    /// </summary>
    [Required]
    public CacheOptions Cache { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Database settings for connection management
    /// </summary>
    public DatabaseOptions Database { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Performance monitoring settings
    /// </summary>
    public PerformanceOptions Performance { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();

    /// <summary>
    /// Security settings for the application
    /// </summary>
    public SecurityOptions Security { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = new();
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class CacheOptions
{
    /// <summary>
    /// Cache duration in seconds (1-3600 seconds / 1 hour)
    /// </summary>
    [Range(1, 3600, ErrorMessage = "DefaultDurationSeconds must be between 1 and 3600 seconds")]
    public int DefaultDurationSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 300;

    /// <summary>
    /// Enable distributed caching (Redis, etc.)
    /// </summary>
    public bool EnableDistributedCache { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = false;

    /// <summary>
    /// Distributed cache duration in seconds (used when EnableDistributedCache is true)
    /// </summary>
    [Range(1, 86400, ErrorMessage = "DistributedCacheDurationSeconds must be between 1 and 86400 seconds (24 hours)")]
    public int DistributedCacheDurationSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 3600;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class ApplicationSettingsOptions
{
    /// <summary>
    /// API version identifier
    /// </summary>
    [Required(ErrorMessage = "ApiVersion is required")]
    [StringLength(10, MinimumLength = 1, ErrorMessage = "ApiVersion must be 1-10 characters")]
    public string ApiVersion { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = "v1";

    /// <summary>
    /// Maximum number of version history entries to keep (1-1000)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "MaxVersionHistory must be between 1 and 1000")]
    public int MaxVersionHistory { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 100;

    /// <summary>
    /// Enable CORS for cross-origin requests
    /// </summary>
    public bool EnableCors { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable Swagger/OpenAPI documentation
    /// </summary>
    public bool EnableSwagger { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable detailed error responses in development environment
    /// </summary>
    public bool EnableDetailedErrors { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = false;

    /// <summary>
    /// Enable request logging middleware
    /// </summary>
    public bool EnableRequestLogging { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable performance monitoring middleware
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class EncryptionOptions
{
    /// <summary>
    /// Encryption key size in bits (128, 192, or 256)
    /// </summary>
    [Range(128, 256, ErrorMessage = "KeySize must be 128, 192, or 256")]
    public int KeySize { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 256;

    /// <summary>
    /// Salt size in bytes (8-64)
    /// </summary>
    [Range(8, 64, ErrorMessage = "SaltSize must be between 8 and 64 bytes")]
    public int SaltSize { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 16;

    /// <summary>
    /// Number of PBKDF2 iterations (1000-1000000)
    /// </summary>
    [Range(1000, 1000000, ErrorMessage = "Iterations must be between 1000 and 1000000")]
    public int Iterations { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 10000;

    /// <summary>
    /// Encryption algorithm to use
    /// </summary>
    [Required(ErrorMessage = "Algorithm is required")]
    [RegularExpression(
        "^(AES256|AES192|AES128)$",
        ErrorMessage = "Algorithm must be AES256, AES192, or AES128")]
    public string Algorithm { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = "AES256";
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class WebhookOptions
{
    /// <summary>
    /// Maximum number of retry attempts for failed webhook deliveries (0-10)
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 5;

    /// <summary>
    /// Webhook request timeout in seconds (1-300)
    /// </summary>
    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 30;

    /// <summary>
    /// Batch size for webhook delivery processing (1-1000)
    /// </summary>
    [Range(1, 1000, ErrorMessage = "BatchSize must be between 1 and 1000")]
    public int BatchSize { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 100;

    /// <summary>
    /// Enable HMAC signature verification for webhooks
    /// </summary>
    public bool EnableSignatureVerification { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable automatic webhook retries
    /// </summary>
    public bool EnableAutoRetry { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class RateLimitOptions
{
    /// <summary>
    /// Maximum requests per minute per client (1-10000)
    /// </summary>
    [Range(1, 10000, ErrorMessage = "RequestsPerMinute must be between 1 and 10000")]
    public int RequestsPerMinute { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 100;

    /// <summary>
    /// Retry-After header value in seconds when rate limit is exceeded (1-3600)
    /// </summary>
    [Range(1, 3600, ErrorMessage = "RetryAfterSeconds must be between 1 and 3600")]
    public int RetryAfterSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 60;

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Exempt certain paths from rate limiting
    /// </summary>
    public string[] RateLimitExemptPaths { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = Array.Empty<string>();

    /// <summary>
    /// When true, rate-limit counters are stored in the shared <c>IDistributedCache</c>
    /// backend so every instance behind a load balancer enforces one combined limit.
    /// When false (default), each instance tracks its own in-memory counters.
    /// </summary>
    public bool UseDistributedStore { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = false;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class DatabaseOptions
{
    /// <summary>
    /// Enable database connection pooling
    /// </summary>
    public bool EnableConnectionPooling { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "ConnectionTimeoutSeconds must be between 1 and 300")]
    public int ConnectionTimeoutSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 30;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    [Range(1, 600, ErrorMessage = "CommandTimeoutSeconds must be between 1 and 600")]
    public int CommandTimeoutSeconds { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 30;

    /// <summary>
    /// Enable automatic database migration on startup
    /// </summary>
    public bool EnableAutomaticMigration { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class PerformanceOptions
{
    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnableMetrics { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Sample rate for performance monitoring (0.0-1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "MetricsSampleRate must be between 0.0 and 1.0")]
    public double MetricsSampleRate { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 0.1;

    /// <summary>
    /// Enable request tracing
    /// </summary>
    public bool EnableRequestTracing { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = false;

    /// <summary>
    /// Maximum request body size in kilobytes
    /// </summary>
    [Range(1, 10240, ErrorMessage = "MaxRequestBodySizeKb must be between 1 and 10240 (10MB)")]
    public int MaxRequestBodySizeKb { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = 1024;
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }

public class SecurityOptions
{
    /// <summary>
    /// Enable HTTPS redirection
    /// </summary>
    public bool EnableHttpsRedirection { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable request validation
    /// </summary>
    public bool EnableRequestValidation { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Enable CORS policy
    /// </summary>
    public bool EnableCorsPolicy { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = true;

    /// <summary>
    /// Trusted origins for CORS
    /// </summary>
    public string[] TrustedOrigins { get; set;         public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    } = Array.Empty<string>();
        public override string ToString() => $"DotnetConfigServerOptions {{ ApplicationSettings = {ApplicationSettings}, Encryption = {Encryption}, Webhook = {Webhook}, RateLimit = {RateLimit}, Cache = {Cache}, Database = {Database} }}";
    }
