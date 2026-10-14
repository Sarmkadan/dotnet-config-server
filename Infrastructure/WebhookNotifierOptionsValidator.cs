#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Validates a single <see cref="WebhookNotifierOptions"/> entry: the URL must be an absolute
/// http or https URL with a host, the timeout must be 1-60 seconds and the retry count 0-10.
/// Uniqueness across webhooks is a collection-level rule and is enforced by the registration builder.
/// </summary>
public sealed class WebhookNotifierOptionsValidator : IValidateOptions<WebhookNotifierOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, WebhookNotifierOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateUrl(options.Url, failures);

        if (options.TimeoutSeconds < WebhookNotifierOptions.MinTimeoutSeconds ||
            options.TimeoutSeconds > WebhookNotifierOptions.MaxTimeoutSeconds)
        {
            failures.Add(
                $"WebhookNotifierOptions.TimeoutSeconds must be between {WebhookNotifierOptions.MinTimeoutSeconds} and {WebhookNotifierOptions.MaxTimeoutSeconds} seconds, got '{options.TimeoutSeconds}'. Choose a value in that range.");
        }

        if (options.MaxRetries < WebhookNotifierOptions.MinRetries ||
            options.MaxRetries > WebhookNotifierOptions.MaxRetriesLimit)
        {
            failures.Add(
                $"WebhookNotifierOptions.MaxRetries must be between {WebhookNotifierOptions.MinRetries} and {WebhookNotifierOptions.MaxRetriesLimit}, got '{options.MaxRetries}'. Choose a value in that range.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// The URL must be an absolute http or https URL with a host and no leading or trailing whitespace.
    /// </summary>
    private static void ValidateUrl(string? url, List<string> failures)
    {
        const string example = "for example 'https://hooks.example.com/config'";

        if (string.IsNullOrWhiteSpace(url))
        {
            failures.Add($"WebhookNotifierOptions.Url is required and must be an absolute http or https URL, {example}.");
            return;
        }

        if (url != url.Trim())
        {
            failures.Add($"WebhookNotifierOptions.Url must not contain leading or trailing whitespace, got '{url}'.");
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host))
        {
            failures.Add($"WebhookNotifierOptions.Url must be an absolute http or https URL with a host, {example}, got '{url}'.");
        }
    }
}
