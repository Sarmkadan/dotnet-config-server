#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Middleware;
using DotnetConfigServer.Models;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Startup validation for <see cref="ConfigurationSnapshotOptions"/>.
/// Rules that are not already covered by data annotations; failure messages include the offending value.
/// </summary>
public sealed class ConfigurationSnapshotOptionsValidator : IValidateOptions<ConfigurationSnapshotOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ConfigurationSnapshotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.DefaultUserId)
            ? ValidateOptionsResult.Fail($"ConfigurationSnapshotOptions.DefaultUserId must not be empty or whitespace, got '{options.DefaultUserId}'")
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Startup validation for <see cref="EncryptionKeyRotationOptions"/>.
/// Rules that are not already covered by data annotations; failure messages include the offending value.
/// </summary>
public sealed class EncryptionKeyRotationOptionsValidator : IValidateOptions<EncryptionKeyRotationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, EncryptionKeyRotationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.DefaultUserId)
            ? ValidateOptionsResult.Fail($"EncryptionKeyRotationOptions.DefaultUserId must not be empty or whitespace, got '{options.DefaultUserId}'")
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Startup validation for <see cref="PerformanceMonitoringOptions"/>.
/// Counts and thresholds must be positive; failure messages include the offending value.
/// </summary>
public sealed class PerformanceMonitoringOptionsValidator : IValidateOptions<PerformanceMonitoringOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PerformanceMonitoringOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.SlowRequestThresholdMs < 0)
        {
            failures.Add($"PerformanceMonitoringOptions.SlowRequestThresholdMs must not be negative, got '{options.SlowRequestThresholdMs}'");
        }

        if (options.MaxMetrics < 1)
        {
            failures.Add($"PerformanceMonitoringOptions.MaxMetrics must be at least 1, got '{options.MaxMetrics}'");
        }

        if (options.RecentMetricsCount < 1)
        {
            failures.Add($"PerformanceMonitoringOptions.RecentMetricsCount must be at least 1, got '{options.RecentMetricsCount}'");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
