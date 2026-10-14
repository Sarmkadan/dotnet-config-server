#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Models;

/// <summary>
/// Settings for a single webhook endpoint notified by the <c>WebhookNotifier</c>.
/// Instances are created through <c>AddConfigServer().AddWebhook(url, configure)</c>, which
/// validates them at registration time. Defaults are conservative: a 10 second timeout and
/// 3 retries, matching <c>WebhookNotifier.RetryFailedAsync</c>.
/// </summary>
public sealed class WebhookNotifierOptions
{
    /// <summary>
    /// Minimum allowed value for <see cref="TimeoutSeconds"/>.
    /// </summary>
    public const int MinTimeoutSeconds = 1;

    /// <summary>
    /// Maximum allowed value for <see cref="TimeoutSeconds"/>.
    /// </summary>
    public const int MaxTimeoutSeconds = 60;

    /// <summary>
    /// Minimum allowed value for <see cref="MaxRetries"/>.
    /// </summary>
    public const int MinRetries = 0;

    /// <summary>
    /// Maximum allowed value for <see cref="MaxRetries"/>.
    /// </summary>
    public const int MaxRetriesLimit = 10;

    /// <summary>
    /// Absolute http or https URL that receives notifications. Must be unique across registered webhooks.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Per-request timeout in seconds (1-60).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Number of retry attempts after the first failed delivery (0-10).
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
