#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Fluent registration entry point returned by <c>AddConfigServer</c>. Each <c>AddWebhook</c> call
/// validates its settings immediately and throws <see cref="OptionsValidationException"/> on the
/// first call that is invalid, so misconfiguration surfaces at the line that caused it rather than at delivery time.
/// </summary>
public sealed class ConfigServerBuilder
{
    private readonly List<WebhookNotifierOptions> _webhooks = [];
    private readonly WebhookNotifierOptionsValidator _validator = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigServerBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection that webhook registrations are added to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public ConfigServerBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Gets the webhook endpoints registered so far, in registration order.
    /// </summary>
    public IReadOnlyList<WebhookNotifierOptions> Webhooks => _webhooks.AsReadOnly();

    /// <summary>
    /// Registers a webhook endpoint for the <c>WebhookNotifier</c>.
    /// </summary>
    /// <param name="url">Absolute http or https URL that receives notifications. Must not already be registered.</param>
    /// <param name="configure">Optional callback to set <see cref="WebhookNotifierOptions.TimeoutSeconds"/> and <see cref="WebhookNotifierOptions.MaxRetries"/>.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="OptionsValidationException">
    /// The URL is not an absolute http or https URL, the timeout is outside 1-60 seconds, the retry count is outside 0-10,
    /// or the URL duplicates an already registered webhook. The message lists every problem found.
    /// </exception>
    public ConfigServerBuilder AddWebhook(string url, Action<WebhookNotifierOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(url);

        var options = new WebhookNotifierOptions { Url = url };
        configure?.Invoke(options);

        var failures = new List<string>();
        var result = _validator.Validate(null, options);
        if (result.Failed)
        {
            failures.AddRange(result.Failures);
        }

        var key = TryNormalizeUrl(options.Url);
        if (key is not null && _webhooks.Any(existing => TryNormalizeUrl(existing.Url) == key))
        {
            failures.Add(
                $"WebhookNotifierOptions.Url '{RedactQuery(options.Url)}' is already registered. Each webhook URL may be added only once; remove the duplicate AddWebhook call or use a different endpoint.");
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(WebhookNotifierOptions), failures);
        }

        _webhooks.Add(options);
        Services.AddSingleton(options);
        return this;
    }

    /// <summary>
    /// Returns the normalized form of a usable http or https URL for duplicate comparison, or <see langword="null"/>
    /// if the URL is not usable. <see cref="Uri"/> already lowercases the scheme and host and drops the default
    /// port, so equivalent spellings compare equal.
    /// </summary>
    private static string? TryNormalizeUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrEmpty(uri.Host)
            ? uri.AbsoluteUri
            : null;

    /// <summary>
    /// Removes the query and fragment before a URL is echoed in an error, since they often carry tokens.
    /// </summary>
    private static string RedactQuery(string url)
    {
        var uri = new Uri(url, UriKind.Absolute);
        return uri.GetLeftPart(UriPartial.Path) + (uri.Query.Length > 0 ? "?<redacted>" : string.Empty);
    }
}

/// <summary>
/// Extension methods that start the fluent configuration chain.
/// </summary>
public static class ConfigServerServiceCollectionExtensions
{
    /// <summary>
    /// Starts fluent registration of config server features, for example
    /// <c>services.AddConfigServer().AddWebhook(url, o => o.TimeoutSeconds = 5)</c>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>A <see cref="ConfigServerBuilder"/> bound to <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static ConfigServerBuilder AddConfigServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new ConfigServerBuilder(services);
    }
}
