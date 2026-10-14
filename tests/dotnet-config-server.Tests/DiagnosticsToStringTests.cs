#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Reflection;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for the diagnostic output of configuration entries, versions and
/// webhook subscriptions. Verifies that secret values are redacted from <c>ToString()</c>
/// and that <see cref="DebuggerDisplayAttribute"/> is present.
/// </summary>
public sealed class DiagnosticsToStringTests
{
    private const string PlainSecret = "super-secret-value-123";

    [Fact]
    public void ConfigurationKey_ToString_RedactsValue_WhenEncrypted()
    {
        var key = new ConfigurationKey { Key = "Db.Password", Value = PlainSecret, IsEncrypted = true };

        var text = key.ToString();

        text.Should().NotContain(PlainSecret);
        text.Should().Contain("<redacted>");
        text.Should().Contain("Db.Password");
    }

    [Fact]
    public void ConfigurationKey_ToString_RedactsValueAndDefault_WhenSensitive()
    {
        var key = new ConfigurationKey
        {
            Key = "Api.Token",
            Value = PlainSecret,
            DefaultValue = "default-" + PlainSecret,
            IsSensitive = true
        };

        key.ToString().Should().NotContain(PlainSecret);
    }

    [Fact]
    public void ConfigurationKey_ToString_ShowsValue_WhenNotSecret()
    {
        var key = new ConfigurationKey { Key = "Feature.Flag", Value = "on" };

        key.ToString().Should().Contain("Value = on");
    }

    [Fact]
    public void ConfigurationVersion_ToString_OmitsKeyValues()
    {
        var version = new ConfigurationVersion
        {
            VersionNumber = "1.2.0",
            KeyCount = 1,
            HasEncryptedKeys = true,
            Keys = [new ConfigurationKey { Key = "Db.Password", Value = PlainSecret, IsEncrypted = true }]
        };

        var text = version.ToString();

        text.Should().NotContain(PlainSecret);
        text.Should().Contain("KeyCount = 1");
        text.Should().Contain("HasEncryptedKeys = True");
    }

    [Fact]
    public void WebhookSubscription_ToString_RedactsSecretHeadersAndUrlPath()
    {
        var subscription = new WebhookSubscription
        {
            Name = "orders",
            Url = "https://hooks.example.com/services/T000/" + PlainSecret,
            Secret = PlainSecret,
            CustomHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer " + PlainSecret }
        };

        var text = subscription.ToString();

        text.Should().NotContain(PlainSecret);
        text.Should().Contain("https://hooks.example.com/<redacted>");
        text.Should().Contain("Secret = <redacted>");
        text.Should().Contain("Authorization");
    }

    [Fact]
    public void WebhookSubscription_ToString_ReportsNoSecret_WhenUnset()
    {
        var subscription = new WebhookSubscription { Name = "orders", Url = "https://hooks.example.com/x" };

        subscription.ToString().Should().Contain("Secret = <none>");
    }

    [Theory]
    [InlineData(typeof(ConfigurationKey))]
    [InlineData(typeof(ConfigurationVersion))]
    [InlineData(typeof(WebhookSubscription))]
    public void DiagnosticTypes_HaveDebuggerDisplay(Type type)
    {
        var attribute = type.GetCustomAttribute<DebuggerDisplayAttribute>();

        attribute.Should().NotBeNull();
    }
}
