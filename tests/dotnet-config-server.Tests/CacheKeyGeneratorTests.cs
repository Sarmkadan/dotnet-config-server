// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Caching;
using FluentAssertions;

namespace DotnetConfigServer.Tests;

public class CacheKeyGeneratorTests
{
    private static readonly Guid TestId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void GetConfigurationKey_ReturnsCorrectFormat()
    {
        CacheKeyGenerator.GetConfigurationKey(TestId).Should().Be($"config:{TestId}");
    }

    [Fact]
    public void GetApplicationConfigurationsKey_ReturnsCorrectFormat()
    {
        CacheKeyGenerator.GetApplicationConfigurationsKey(TestId).Should().Be($"app:{TestId}:configs");
    }

    [Fact]
    public void GetConfigurationKeysKey_ReturnsCorrectFormat()
    {
        CacheKeyGenerator.GetConfigurationKeysKey(TestId).Should().Be($"config:{TestId}:keys");
    }

    [Fact]
    public void GetConfigurationKeyKey_ReturnsCorrectFormat()
    {
        CacheKeyGenerator.GetConfigurationKeyKey(TestId).Should().Be($"key:{TestId}");
    }

    [Fact]
    public void GetConfigurationVersionsKey_ReturnsCorrectFormat()
    {
        CacheKeyGenerator.GetConfigurationVersionsKey(TestId).Should().Be($"config:{TestId}:versions");
    }

    [Fact]
    public void DifferentGuids_ProduceDifferentKeys()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        CacheKeyGenerator.GetConfigurationKey(id1).Should().NotBe(CacheKeyGenerator.GetConfigurationKey(id2));
    }

    [Fact]
    public void SameGuid_DifferentMethods_ProduceDifferentKeys()
    {
        var configKey = CacheKeyGenerator.GetConfigurationKey(TestId);
        var keyKey = CacheKeyGenerator.GetConfigurationKeyKey(TestId);
        configKey.Should().NotBe(keyKey);
    }

    [Fact]
    public void EmptyGuid_StillProducesValidKey()
    {
        var key = CacheKeyGenerator.GetConfigurationKey(Guid.Empty);
        key.Should().NotBeNullOrWhiteSpace();
        key.Should().Contain(Guid.Empty.ToString());
    }
}
