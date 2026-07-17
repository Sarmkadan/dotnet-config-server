#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Caching;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Provides unit tests for the <see cref="CacheKeyGenerator"/> class.
/// Tests verify that cache key generation methods produce correct and distinct keys
/// for different cache operations in the DotnetConfigServer application.
/// </summary>
public sealed class CacheKeyGeneratorTests
{
    /// <summary>
    /// Tests that the GetConfigurationKey method generates a key containing the provided configuration ID
    /// and prefixed with "config:".
    /// </summary>
    [Fact]
    public void GetConfigurationKey_ReturnsKeyWithConfigurationId()
    {
        var id = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationKey(id);

        key.Should().Contain(id.ToString());
        key.Should().StartWith("config:");
    }

    /// <summary>
    /// Tests that the GetApplicationConfigurationsKey method generates a key containing the provided application ID
    /// and ending with "configs" suffix.
    /// </summary>
    [Fact]
    public void GetApplicationConfigurationsKey_ContainsAppIdAndConfigsSuffix()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetApplicationConfigurationsKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().EndWith("configs");
    }

    /// <summary>
    /// Tests that the GetConfigurationKeysKey method generates a key that is distinct from the configuration key
    /// and contains "keys" in the key name.
    /// </summary>
    [Fact]
    public void GetConfigurationKeysKey_ReturnsDistinctFromConfigurationKey()
    {
        var id = Guid.NewGuid();

        var configKey = CacheKeyGenerator.GetConfigurationKey(id);
        var keysKey = CacheKeyGenerator.GetConfigurationKeysKey(id);

        keysKey.Should().NotBe(configKey);
        keysKey.Should().Contain("keys");
    }

    /// <summary>
    /// Tests that the GetConfigurationKeyKey method generates a key containing the provided key ID
    /// and prefixed with "key:".
    /// </summary>
    [Fact]
    public void GetConfigurationKeyKey_ContainsKeyId()
    {
        var keyId = Guid.NewGuid();

        var cacheKey = CacheKeyGenerator.GetConfigurationKeyKey(keyId);

        cacheKey.Should().Contain(keyId.ToString());
        cacheKey.Should().StartWith("key:");
    }

    /// <summary>
    /// Tests that the GetConfigurationVersionsKey method generates a key containing the provided configuration ID
    /// and ending with "versions" suffix.
    /// </summary>
    [Fact]
    public void GetConfigurationVersionsKey_ContainsVersionsSuffix()
    {
        var configId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationVersionsKey(configId);

        key.Should().Contain(configId.ToString());
        key.Should().EndWith("versions");
    }

    /// <summary>
    /// Tests that the GetConfigurationVersionKey method generates a key containing the provided version ID
    /// and prefixed with "version:".
    /// </summary>
    [Fact]
    public void GetConfigurationVersionKey_ContainsVersionId()
    {
        var versionId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetConfigurationVersionKey(versionId);

        key.Should().Contain(versionId.ToString());
        key.Should().StartWith("version:");
    }

    /// <summary>
    /// Tests that the GetConfigurationDiffKey method generates a key containing both version IDs
    /// and prefixed with "diff:".
    /// </summary>
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

    /// <summary>
    /// Tests that the GetConfigurationDiffKey method generates a different key when the version IDs are provided in a different order.
    /// </summary>
    [Fact]
    public void GetConfigurationDiffKey_DifferentOrderProducesDifferentKey()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var key1 = CacheKeyGenerator.GetConfigurationDiffKey(id1, id2);
        var key2 = CacheKeyGenerator.GetConfigurationDiffKey(id2, id1);

        key1.Should().NotBe(key2);
    }

    /// <summary>
    /// Tests that the GetApplicationKey method generates a key containing the provided application ID
    /// and prefixed with "app:".
    /// </summary>
    [Fact]
    public void GetApplicationKey_ContainsApplicationId()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetApplicationKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().StartWith("app:");
    }

    /// <summary>
    /// Tests that the GetWebhookSubscriptionsKey method generates a key containing the provided application ID
    /// and prefixed with "webhooks:".
    /// </summary>
    [Fact]
    public void GetWebhookSubscriptionsKey_ContainsApplicationId()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetWebhookSubscriptionsKey(appId);

        key.Should().Contain(appId.ToString());
        key.Should().StartWith("webhooks:");
    }

    /// <summary>
    /// Tests that the GetWebhookSubscriptionKey method generates a key containing the provided subscription ID
    /// and prefixed with "webhook:".
    /// </summary>
    [Fact]
    public void GetWebhookSubscriptionKey_ContainsSubscriptionId()
    {
        var subscriptionId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetWebhookSubscriptionKey(subscriptionId);

        key.Should().Contain(subscriptionId.ToString());
        key.Should().StartWith("webhook:");
    }

    /// <summary>
    /// Tests that the GetSearchKey method with query only generates a key prefixed with "search:"
    /// and containing the URL-encoded query string.
    /// </summary>
    [Fact]
    public void GetSearchKey_WithQueryOnly_ContainsEncodedQuery()
    {
        var key = CacheKeyGenerator.GetSearchKey("database host");

        key.Should().StartWith("search:");
        key.Should().Contain(Uri.EscapeDataString("database host"));
    }

    /// <summary>
    /// Tests that the GetSearchKey method with query and application ID generates a key containing both
    /// the application ID and the query string.
    /// </summary>
    [Fact]
    public void GetSearchKey_WithQueryAndApplicationId_ContainsBoth()
    {
        var appId = Guid.NewGuid();

        var key = CacheKeyGenerator.GetSearchKey("timeout", appId);

        key.Should().Contain(appId.ToString());
        key.Should().Contain("timeout");
    }

    /// <summary>
    /// Tests that the GetSearchKey method produces different keys when called with and without an application ID.
    /// </summary>
    [Fact]
    public void GetSearchKey_WithAndWithoutAppId_ProducesDifferentKeys()
    {
        var appId = Guid.NewGuid();
        var query = "config";

        var keyWithApp = CacheKeyGenerator.GetSearchKey(query, appId);
        var keyWithout = CacheKeyGenerator.GetSearchKey(query);

        keyWithApp.Should().NotBe(keyWithout);
    }

    /// <summary>
    /// Tests that the GetInvalidationPatternsForConfiguration method generates a collection of cache keys
    /// that includes keys for the configuration, its keys list, its versions, and the application configurations.
    /// </summary>
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

    /// <summary>
    /// Tests that the GetInvalidationPatternsForApplication method generates a collection of cache keys
    /// that includes keys for the application and its configurations.
    /// </summary>
    [Fact]
    public void GetInvalidationPatternsForApplication_YieldsExpectedPatterns()
    {
        var appId = Guid.NewGuid();

        var patterns = CacheKeyGenerator.GetInvalidationPatternsForApplication(appId).ToList();

        patterns.Should().Contain(CacheKeyGenerator.GetApplicationKey(appId));
        patterns.Should().Contain(CacheKeyGenerator.GetApplicationConfigurationsKey(appId));
        patterns.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    /// <summary>
    /// Tests that different GUIDs produce different cache keys for both configuration and application keys.
    /// </summary>
    [Fact]
    public void DifferentIds_ProduceDifferentKeys()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        CacheKeyGenerator.GetConfigurationKey(id1).Should().NotBe(CacheKeyGenerator.GetConfigurationKey(id2));
        CacheKeyGenerator.GetApplicationKey(id1).Should().NotBe(CacheKeyGenerator.GetApplicationKey(id2));
    }
}
