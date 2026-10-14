#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Extension methods for registering the configuration server health checks.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ConfigServerHealthCheck"/> under the name <c>config-server</c>
    /// with the <c>ready</c> tag, and ensures the <see cref="ConfigWatcherStatus"/> singleton it reads is registered.
    /// </summary>
    /// <param name="builder">The health checks builder to add the check to.</param>
    /// <returns>The configured <see cref="IHealthChecksBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHealthChecksBuilder AddConfigServerHealthCheck(this IHealthChecksBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<ConfigWatcherStatus>();

        return builder.AddCheck<ConfigServerHealthCheck>("config-server", tags: ["ready"]);
    }
}
