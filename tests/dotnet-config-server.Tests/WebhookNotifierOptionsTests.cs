#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Tests for <see cref="WebhookNotifierOptions"/> validation and the <c>AddConfigServer().AddWebhook()</c> registration chain.
/// </summary>
public sealed class WebhookNotifierOptionsTests
{
    private const string ValidUrl = "https://hooks.example.com/config";

    #region Validator: URL rule

    [Theory]
    [InlineData("https://hooks.example.com/config")]
    [InlineData("http://localhost:8080/hook")]
    [InlineData("HTTPS://Hooks.Example.com")]
    public void Validator_AbsoluteHttpOrHttpsUrl_Succeeds(string url)
    {
        var options = new WebhookNotifierOptions { Url = url };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/relative/path")]
    [InlineData("hooks.example.com/config")]
    [InlineData("ftp://files.example.com/hook")]
    [InlineData("mailto:ops@example.com")]
    [InlineData("https://")]
    [InlineData("https:///no-host")]
    [InlineData(" https://hooks.example.com/config")]
    [InlineData("https://hooks.example.com/config ")]
    public void Validator_InvalidUrl_FailsWithUrlMessage(string url)
    {
        var options = new WebhookNotifierOptions { Url = url };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("WebhookNotifierOptions.Url");
    }

    [Fact]
    public void Validator_UrlWithWhitespace_ExplainsTheProblem()
    {
        var options = new WebhookNotifierOptions { Url = " https://hooks.example.com/config" };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("leading or trailing whitespace");
    }

    #endregion

    #region Validator: timeout rule

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(60)]
    public void Validator_TimeoutWithinRange_Succeeds(int timeout)
    {
        var options = new WebhookNotifierOptions { Url = ValidUrl, TimeoutSeconds = timeout };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(61)]
    [InlineData(300)]
    public void Validator_TimeoutOutsideRange_FailsWithValueAndRange(int timeout)
    {
        var options = new WebhookNotifierOptions { Url = ValidUrl, TimeoutSeconds = timeout };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("TimeoutSeconds").And.Contain("between 1 and 60").And.Contain($"'{timeout}'");
    }

    #endregion

    #region Validator: retry rule

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validator_RetriesWithinRange_Succeeds(int retries)
    {
        var options = new WebhookNotifierOptions { Url = ValidUrl, MaxRetries = retries };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Validator_RetriesOutsideRange_FailsWithValueAndRange(int retries)
    {
        var options = new WebhookNotifierOptions { Url = ValidUrl, MaxRetries = retries };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("MaxRetries").And.Contain("between 0 and 10").And.Contain($"'{retries}'");
    }

    #endregion

    #region Validator: defaults

    [Fact]
    public void Options_Defaults_AreSafeAndValid()
    {
        var options = new WebhookNotifierOptions { Url = ValidUrl };

        options.TimeoutSeconds.Should().Be(10);
        options.MaxRetries.Should().Be(3);
        new WebhookNotifierOptionsValidator().Validate(null, options).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validator_ReportsEveryProblemAtOnce()
    {
        var options = new WebhookNotifierOptions { Url = "not-a-url", TimeoutSeconds = 0, MaxRetries = 11 };

        var result = new WebhookNotifierOptionsValidator().Validate(null, options);

        result.Failures.Should().HaveCount(3);
    }

    #endregion

    #region Builder: registration-time validation

    [Fact]
    public void AddWebhook_ValidUrl_RegistersOptionsAndAppliesConfigure()
    {
        var services = new ServiceCollection();

        services.AddConfigServer().AddWebhook(ValidUrl, o =>
        {
            o.TimeoutSeconds = 5;
            o.MaxRetries = 0;
        });

        using var provider = services.BuildServiceProvider();
        var registered = provider.GetRequiredService<IEnumerable<WebhookNotifierOptions>>().Single();
        registered.Url.Should().Be(ValidUrl);
        registered.TimeoutSeconds.Should().Be(5);
        registered.MaxRetries.Should().Be(0);
    }

    [Fact]
    public void AddWebhook_WithoutConfigure_UsesSafeDefaults()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection()).AddWebhook(ValidUrl);

        builder.Webhooks.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new WebhookNotifierOptions { Url = ValidUrl });
    }

    [Fact]
    public void AddWebhook_Chained_KeepsRegistrationOrder()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook("https://a.example.com/hook")
            .AddWebhook("https://b.example.com/hook");

        builder.Webhooks.Select(w => w.Url).Should()
            .Equal("https://a.example.com/hook", "https://b.example.com/hook");
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("ftp://files.example.com/hook")]
    public void AddWebhook_InvalidUrl_ThrowsOptionsValidationException(string url)
    {
        var services = new ServiceCollection();

        var act = () => services.AddConfigServer().AddWebhook(url);

        act.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle().Which.Should().Contain("WebhookNotifierOptions.Url");
        services.Should().NotContain(d => d.ServiceType == typeof(WebhookNotifierOptions));
    }

    [Theory]
    [InlineData(0, "TimeoutSeconds")]
    [InlineData(61, "TimeoutSeconds")]
    public void AddWebhook_TimeoutOutOfRange_ThrowsWithActionableMessage(int timeout, string field)
    {
        var act = () => new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook(ValidUrl, o => o.TimeoutSeconds = timeout);

        act.Should().Throw<OptionsValidationException>()
            .WithMessage($"*{field}*between 1 and 60*'{timeout}'*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void AddWebhook_RetriesOutOfRange_ThrowsWithActionableMessage(int retries)
    {
        var act = () => new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook(ValidUrl, o => o.MaxRetries = retries);

        act.Should().Throw<OptionsValidationException>()
            .WithMessage($"*MaxRetries*between 0 and 10*'{retries}'*");
    }

    [Fact]
    public void AddWebhook_MultipleProblems_ReportsAllOfThemInOneException()
    {
        var act = () => new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook("not-a-url", o =>
            {
                o.TimeoutSeconds = 0;
                o.MaxRetries = 11;
            });

        var exception = act.Should().Throw<OptionsValidationException>().Which;
        exception.Failures.Should().HaveCount(3);
    }

    [Fact]
    public void AddWebhook_InvalidCall_DoesNotRegisterAnything()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection());

        var act = () => builder.AddWebhook(ValidUrl, o => o.TimeoutSeconds = 99);

        act.Should().Throw<OptionsValidationException>();
        builder.Webhooks.Should().BeEmpty();
        builder.Services.Should().NotContain(d => d.ServiceType == typeof(WebhookNotifierOptions));
    }

    [Fact]
    public void AddWebhook_NullUrl_ThrowsArgumentNullException()
    {
        var act = () => new ConfigServerBuilder(new ServiceCollection()).AddWebhook(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("url");
    }

    #endregion

    #region Builder: duplicate URLs

    [Fact]
    public void AddWebhook_ExactDuplicateUrl_ThrowsOptionsValidationException()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection()).AddWebhook(ValidUrl);

        var act = () => builder.AddWebhook(ValidUrl);

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*already registered*Each webhook URL may be added only once*");
        builder.Webhooks.Should().ContainSingle();
    }

    [Theory]
    [InlineData("HTTPS://HOOKS.EXAMPLE.COM/config")]
    [InlineData("https://hooks.example.com:443/config")]
    public void AddWebhook_EquivalentSpellingOfSameUrl_IsRejectedAsDuplicate(string equivalentUrl)
    {
        var builder = new ConfigServerBuilder(new ServiceCollection()).AddWebhook(ValidUrl);

        var act = () => builder.AddWebhook(equivalentUrl);

        act.Should().Throw<OptionsValidationException>().WithMessage("*already registered*");
    }

    [Fact]
    public void AddWebhook_SameHostDifferentPath_IsAllowed()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook("https://hooks.example.com/a")
            .AddWebhook("https://hooks.example.com/b");

        builder.Webhooks.Should().HaveCount(2);
    }

    [Fact]
    public void AddWebhook_SameUrlDifferentScheme_IsAllowed()
    {
        var builder = new ConfigServerBuilder(new ServiceCollection())
            .AddWebhook("http://hooks.example.com/config")
            .AddWebhook("https://hooks.example.com/config");

        builder.Webhooks.Should().HaveCount(2);
    }

    [Fact]
    public void AddWebhook_DuplicateMessage_RedactsQueryString()
    {
        const string withToken = "https://hooks.example.com/config?token=super-secret";
        var builder = new ConfigServerBuilder(new ServiceCollection()).AddWebhook(withToken);

        var act = () => builder.AddWebhook(withToken);

        act.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().Contain("https://hooks.example.com/config?<redacted>")
            .And.NotContain("super-secret");
    }

    #endregion

    #region Entry point

    [Fact]
    public void AddConfigServer_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var act = () => services!.AddConfigServer();

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    #endregion
}
