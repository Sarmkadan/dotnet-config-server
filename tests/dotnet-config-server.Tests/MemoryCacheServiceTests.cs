#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

public sealed class MemoryCacheServiceTests : IDisposable
{
    private readonly Mock<ILogger<MemoryCacheService>> _loggerMock;
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests()
    {
        _loggerMock = new Mock<ILogger<MemoryCacheService>>();
        _sut = new MemoryCacheService(_loggerMock.Object);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        await _sut.SetAsync("key1", "value1");

        var result = await _sut.GetAsync<string>("key1");

        result.Should().Be("value1");
    }

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsDefault()
    {
        var result = await _sut.GetAsync<string>("missing-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_AfterExpiration_ReturnsDefault()
    {
        await _sut.SetAsync("expiring-key", "value", TimeSpan.FromMilliseconds(50));

        await Task.Delay(100);

        var result = await _sut.GetAsync<string>("expiring-key");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_BeforeExpiration_ReturnsValue()
    {
        await _sut.SetAsync("long-lived-key", 42, TimeSpan.FromMinutes(10));

        var result = await _sut.GetAsync<int>("long-lived-key");

        result.Should().Be(42);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesEntry()
    {
        await _sut.SetAsync("remove-me", "here");

        await _sut.RemoveAsync("remove-me");

        var result = await _sut.GetAsync<string>("remove-me");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        var act = () => _sut.RemoveAsync("ghost-key");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_MultipleKeys_RemovesAllSpecified()
    {
        await _sut.SetAsync("k1", "v1");
        await _sut.SetAsync("k2", "v2");
        await _sut.SetAsync("k3", "v3");

        await _sut.RemoveAsync(new[] { "k1", "k2" });

        (await _sut.GetAsync<string>("k1")).Should().BeNull();
        (await _sut.GetAsync<string>("k2")).Should().BeNull();
        (await _sut.GetAsync<string>("k3")).Should().Be("v3");
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        await _sut.SetAsync("exists-key", "present");

        var result = await _sut.ExistsAsync("exists-key");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ReturnsFalse()
    {
        var result = await _sut.ExistsAsync("not-there");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExpiredKey_ReturnsFalse()
    {
        await _sut.SetAsync("soon-gone", "bye", TimeSpan.FromMilliseconds(50));
        await Task.Delay(100);

        var result = await _sut.ExistsAsync("soon-gone");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_KeyDoesNotExist_InvokesFactory()
    {
        var factoryInvoked = false;

        var result = await _sut.GetOrCreateAsync("new-key", () =>
        {
            factoryInvoked = true;
            return Task.FromResult("factory-value");
        });

        factoryInvoked.Should().BeTrue();
        result.Should().Be("factory-value");
    }

    [Fact]
    public async Task GetOrCreateAsync_KeyAlreadyExists_DoesNotInvokeFactory()
    {
        await _sut.SetAsync("cached-key", "existing");
        var factoryInvoked = false;

        var result = await _sut.GetOrCreateAsync("cached-key", () =>
        {
            factoryInvoked = true;
            return Task.FromResult("factory-value");
        });

        factoryInvoked.Should().BeFalse();
        result.Should().Be("existing");
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _sut.SetAsync("a", 1);
        await _sut.SetAsync("b", 2);
        await _sut.SetAsync("c", 3);

        await _sut.ClearAsync();

        (await _sut.ExistsAsync("a")).Should().BeFalse();
        (await _sut.ExistsAsync("b")).Should().BeFalse();
        (await _sut.ExistsAsync("c")).Should().BeFalse();
    }

    [Fact]
    public async Task GetKeysAsync_ReturnsMatchingKeys()
    {
        await _sut.SetAsync("config:app1", "v1");
        await _sut.SetAsync("config:app2", "v2");
        await _sut.SetAsync("version:1", "v3");

        var keys = (await _sut.GetKeysAsync("config")).ToList();

        keys.Should().HaveCount(2);
        keys.Should().Contain("config:app1");
        keys.Should().Contain("config:app2");
    }

    [Fact]
    public async Task GetStatsAsync_AfterOperations_ReflectsCorrectCounts()
    {
        await _sut.SetAsync("s1", "v1");
        await _sut.SetAsync("s2", "v2");
        await _sut.GetAsync<string>("s1");     // hit
        await _sut.GetAsync<string>("ghost");  // miss
        await _sut.RemoveAsync("s2");

        var stats = await _sut.GetStatsAsync();

        stats.Sets.Should().BeGreaterThanOrEqualTo(2);
        stats.Hits.Should().BeGreaterThanOrEqualTo(1);
        stats.Misses.Should().BeGreaterThanOrEqualTo(1);
        stats.Deletes.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingKey()
    {
        await _sut.SetAsync("overwrite-key", "original");
        await _sut.SetAsync("overwrite-key", "updated");

        var result = await _sut.GetAsync<string>("overwrite-key");
        result.Should().Be("updated");
    }

    [Fact]
    public async Task SetAsync_StoresComplexTypes()
    {
        var obj = new Dictionary<string, int> { ["alpha"] = 1, ["beta"] = 2 };

        await _sut.SetAsync("complex-key", obj);

        var result = await _sut.GetAsync<Dictionary<string, int>>("complex-key");
        result.Should().NotBeNull();
        result!["alpha"].Should().Be(1);
        result["beta"].Should().Be(2);
    }

    [Fact]
    public async Task SetAsync_WithNoExpiration_EntryDoesNotExpire()
    {
        await _sut.SetAsync("no-expiry", "persistent");
        await Task.Delay(50);

        var result = await _sut.GetAsync<string>("no-expiry");
        result.Should().Be("persistent");
    }
}
