#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Middleware;
using DotnetConfigServer.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Unit tests for the <c>IValidateOptions&lt;T&gt;</c> implementations used with <c>ValidateOnStart</c>.
/// </summary>
public sealed class OptionsValidatorTests
{
    #region DotnetConfigServerOptionsValidator

    [Fact]
    public void DotnetConfigServerOptionsValidator_DefaultOptions_Succeeds()
    {
        var validator = new DotnetConfigServerOptionsValidator();

        var result = validator.Validate(null, new DotnetConfigServerOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void DotnetConfigServerOptionsValidator_KeySizeDoesNotMatchAlgorithm_Fails()
    {
        var validator = new DotnetConfigServerOptionsValidator();
        var options = new DotnetConfigServerOptions();
        options.Encryption.Algorithm = "AES128";
        options.Encryption.KeySize = 256;

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("EncryptionOptions.KeySize").And.Contain("'256'").And.Contain("AES128");
    }

    [Fact]
    public void DotnetConfigServerOptionsValidator_MalformedTrustedOrigin_Fails()
    {
        var validator = new DotnetConfigServerOptionsValidator();
        var options = new DotnetConfigServerOptions();
        options.Security.TrustedOrigins = ["https://ok.example.com", "not-a-url"];

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("SecurityOptions.TrustedOrigins[1]").And.Contain("'not-a-url'");
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("ws://example.com")]
    public void DotnetConfigServerOptionsValidator_NonHttpScheme_Fails(string origin)
    {
        var validator = new DotnetConfigServerOptionsValidator();
        var options = new DotnetConfigServerOptions();
        options.Security.TrustedOrigins = [origin];

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("SecurityOptions.TrustedOrigins[0]").And.Contain(origin);
    }

    [Fact]
    public void DotnetConfigServerOptionsValidator_ValidEncryptionAndHttpsOrigins_Succeeds()
    {
        var validator = new DotnetConfigServerOptionsValidator();
        var options = new DotnetConfigServerOptions();
        options.Encryption.Algorithm = "AES192";
        options.Encryption.KeySize = 192;
        options.Security.TrustedOrigins = ["https://config.example.com", "http://localhost:5000"];

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region ConfigurationSnapshotOptionsValidator

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConfigurationSnapshotOptionsValidator_BlankDefaultUserId_Fails(string userId)
    {
        var validator = new ConfigurationSnapshotOptionsValidator();
        var options = new ConfigurationSnapshotOptions { DefaultUserId = userId };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("ConfigurationSnapshotOptions.DefaultUserId");
    }

    [Fact]
    public void ConfigurationSnapshotOptionsValidator_DefaultOptions_Succeeds()
    {
        var validator = new ConfigurationSnapshotOptionsValidator();

        var result = validator.Validate(null, new ConfigurationSnapshotOptions());

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region EncryptionKeyRotationOptionsValidator

    [Fact]
    public void EncryptionKeyRotationOptionsValidator_WhitespaceDefaultUserId_Fails()
    {
        var validator = new EncryptionKeyRotationOptionsValidator();
        var options = new EncryptionKeyRotationOptions { DefaultUserId = "  " };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("EncryptionKeyRotationOptions.DefaultUserId");
    }

    [Fact]
    public void EncryptionKeyRotationOptionsValidator_DefaultOptions_Succeeds()
    {
        var validator = new EncryptionKeyRotationOptionsValidator();

        var result = validator.Validate(null, new EncryptionKeyRotationOptions());

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region PerformanceMonitoringOptionsValidator

    [Fact]
    public void PerformanceMonitoringOptionsValidator_NegativeSlowRequestThreshold_Fails()
    {
        var validator = new PerformanceMonitoringOptionsValidator();
        var options = new PerformanceMonitoringOptions { SlowRequestThresholdMs = -1 };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("PerformanceMonitoringOptions.SlowRequestThresholdMs").And.Contain("'-1'");
    }

    [Fact]
    public void PerformanceMonitoringOptionsValidator_NonPositiveMaxMetrics_Fails()
    {
        var validator = new PerformanceMonitoringOptionsValidator();
        var options = new PerformanceMonitoringOptions { MaxMetrics = 0 };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("PerformanceMonitoringOptions.MaxMetrics").And.Contain("'0'");
    }

    [Fact]
    public void PerformanceMonitoringOptionsValidator_NonPositiveRecentMetricsCount_Fails()
    {
        var validator = new PerformanceMonitoringOptionsValidator();
        var options = new PerformanceMonitoringOptions { RecentMetricsCount = -5 };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("PerformanceMonitoringOptions.RecentMetricsCount").And.Contain("'-5'");
    }

    [Fact]
    public void PerformanceMonitoringOptionsValidator_DefaultOptions_Succeeds()
    {
        var validator = new PerformanceMonitoringOptionsValidator();

        var result = validator.Validate(null, new PerformanceMonitoringOptions());

        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Host startup

    [Fact]
    public async Task ValidateOnStart_InvalidOptions_AbortsHostStartupWithPropertyAndValue()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOptions<DotnetConfigServerOptions>()
                    .Configure(o => o.Security.TrustedOrigins = ["not-a-url"])
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddSingleton<IValidateOptions<DotnetConfigServerOptions>, DotnetConfigServerOptionsValidator>();
            })
            .Build();

        var act = () => host.StartAsync();

        var exception = await act.Should().ThrowAsync<OptionsValidationException>();
        exception.Which.Message.Should().Contain("SecurityOptions.TrustedOrigins[0]").And.Contain("'not-a-url'");
    }

    [Fact]
    public async Task ValidateOnStart_ValidOptions_StartsHost()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOptions<DotnetConfigServerOptions>()
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                services.AddSingleton<IValidateOptions<DotnetConfigServerOptions>, DotnetConfigServerOptionsValidator>();
            })
            .Build();

        var act = () => host.StartAsync();

        await act.Should().NotThrowAsync();
        await host.StopAsync();
    }

    #endregion
}
