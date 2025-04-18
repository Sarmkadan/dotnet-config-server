#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Caching;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

public sealed class CacheKeyGeneratorTests
{
    [Fact]
    public void GetConfigurationKey_ReturnsKeyWithConfigurationId()
    {
        var id = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationKey(id);

        key.Should().Contain(id.ToString());
        key.Should().StartWith("config:");
    }

    [Fact]
    public void GetApplicationConfigurationsKey_ContainsAppIdAndConfigsSuffix()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetApplicationConfigurationsKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().EndWith("configs");
    }

    [Fact]
    public void GetConfigurationKeysKey_ReturnsDistinctFromConfigurationKey()
    {
        var id = Guid.NewGuid();

        var configKey = CacheKeyGenerator.GetConfigurationKey(id);
        var keysKey = CacheKeyGenerator.GetConfigurationKeysKey(id);

        keysKey.Should().NotBe(configKey);
        keysKey.Should().Contain("keys");
    }

    [Fact]
    public void GetConfigurationKeyKey_ContainsKeyId()
    {
        var keyId = Guid.NewGuid();

        var cacheKey = CacheKeyGenerator.GetConfigurationKeyKey(keyId);

        cacheKey.Should().Contain(keyId.ToString());
        cacheKey.Should().StartWith("key:");
    }

    [Fact]
    public void GetConfigurationVersionsKey_ContainsVersionsSuffix()
    {
        var configId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationVersionsKey(configId);

        key.Should().Contain(configId.ToString());
        key.Should().EndWith("versions");
    }

    [Fact]
    public void GetConfigurationVersionKey_ContainsVersionId()
    {
        var versionId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationVersionKey(versionId);

        key.Should().Contain(versionId.ToString());
        key.Should().StartWith("version:");
    }

    [Fact]
    public void GetConfigurationDiffKey_ContainsBothVersionIds()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationDiffKey(fromId, toId);

        key.Should().Contain(fromId.ToString());
        key.Should().Contain(toId.ToString());
        key.Should().StartWith("diff:");
    }

    [Fact]
    public void GetConfigurationDiffKey_DifferentOrderProducesDifferentKey()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var key1 = CacheKeyGenerator.GetConfigurationDiffKey(id1, id2);
        var key2 = CacheKeyGenerator.GetConfigurationDiffKey(id2, id1);

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void GetApplicationKey_ContainsApplicationId()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetApplicationKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().StartWith("app:");
    }

    [Fact]
    public void GetWebhookSubscriptionsKey_ContainsApplicationId()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetWebhookSubscriptionsKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().StartWith("webhooks:");
    }

    [Fact]
    public void GetWebhookSubscriptionKey_ContainsSubscriptionId()
    {
        var subscriptionId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetWebhookSubscriptionKey(subscriptionId);

        key.Should().Contain(subscriptionId.ToString());
        key.Should().StartWith("webhook:");
    }

    [Fact]
    public void GetSearchKey_WithQueryOnly_ContainsEncodedQuery()
    {
        var key = CacheKeyGenerator.GetSearchKey("database host");

        key.Should().StartWith("search:");
        key.Should().Contain(Uri.EscapeDataString("database host"));
    }

    [Fact]
    public void GetSearchKey_WithQueryAndApplicationId_ContainsBoth()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetSearchKey("timeout", appId);

        key.Should().Contain(appId.ToString());
        key.Should().Contain("timeout");
    }

    [Fact]
    public void GetSearchKey_WithAndWithoutAppId_ProducesDifferentKeys()
    {
        var appId = Guid.NewGuid();
        var query = "config";

        var keyWithApp = CacheKeyGenerator.GetSearchKey(query, appId);
        var keyWithout = CacheKeyGenerator.GetSearchKey(query);

        keyWithApp.Should().NotBe(keyWithout);
    }

    [Fact]
    public void GetInvalidationPatternsForConfiguration_YieldsExpectedPatterns()
    {
        var configId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        var patterns = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(configId, appId).ToList();

        patterns.Should().Contain(CacheKeyGenerator.GetConfigurationKey(configId));
        patterns.Should().Contain(CacheKeyGenerator.GetConfigurationKeysKey(configId));
        patterns.Should().Contain(CacheKeyGenerator.GetConfigurationVersionsKey(configId));
        patterns.Should().Contain(CacheKeyGenerator.GetApplicationConfigurationsKey(appId));
        patterns.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void GetInvalidationPatternsForApplication_YieldsExpectedPatterns()
    {
        var appId = Guid.NewGuid();

        var patterns = CacheKeyGenerator.GetInvalidationPatternsForApplication(appId).ToList();

        patterns.Should().Contain(CacheKeyGenerator.GetApplicationKey(appId));
        patterns.Should().Contain(CacheKeyGenerator.GetApplicationConfigurationsKey(appId));
        patterns.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void DifferentIds_ProduceDifferentKeys()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        CacheKeyGenerator.GetConfigurationKey(id1).Should().NotBe(CacheKeyGenerator.GetConfigurationKey(id2));
        CacheKeyGenerator.GetApplicationKey(id1).Should().NotBe(CacheKeyGenerator.GetApplicationKey(id2));
    }
}
