using System.ComponentModel.DataAnnotations;

namespace DotnetConfigServer.Models;

public class DotnetConfigServerOptions
{
    [Required]
    public ApplicationSettingsOptions ApplicationSettings { get; set; } = new();

    [Required]
    public EncryptionOptions Encryption { get; set; } = new();

    [Required]
    public WebhookOptions Webhook { get; set; } = new();

    [Required]
    public RateLimitOptions RateLimit { get; set; } = new();

    [Required]
    public CacheOptions Cache { get; set; } = new();
}

public class CacheOptions
{
    [Range(1, 3600)]
    public int DefaultDurationSeconds { get; set; } = 300;
}

public class ApplicationSettingsOptions
{
    [Required]
    public string ApiVersion { get; set; } = "v1";

    [Range(1, 1000)]
    public int MaxVersionHistory { get; set; } = 100;

    public bool EnableCors { get; set; } = true;

    public bool EnableSwagger { get; set; } = true;
}

public class EncryptionOptions
{
    [Range(128, 256)]
    public int KeySize { get; set; } = 256;

    [Range(8, 64)]
    public int SaltSize { get; set; } = 16;

    [Range(1000, 1000000)]
    public int Iterations { get; set; } = 10000;

    [Required]
    public string Algorithm { get; set; } = "AES256";
}

public class WebhookOptions
{
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 5;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;
}

public class RateLimitOptions
{
    [Range(1, 10000)]
    public int RequestsPerMinute { get; set; } = 100;

    [Range(1, 3600)]
    public int RetryAfterSeconds { get; set; } = 60;
}
