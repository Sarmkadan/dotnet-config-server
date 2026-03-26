#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class DictionaryExtensionsTests
{
    // ── GetValueOrDefault ────────────────────────────────────────────────────

    [Fact]
    public void GetValueOrDefault_ExistingKey_ReturnsValue()
    {
        var dict = new Dictionary<string, int> { ["timeout"] = 30 };

        dict.GetValueOrDefault("timeout", -1).Should().Be(30);
    }

    [Fact]
    public void GetValueOrDefault_MissingKey_ReturnsDefaultValue()
    {
        var dict = new Dictionary<string, int>();

        dict.GetValueOrDefault("missing", 42).Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_MissingKey_NoExplicitDefault_ReturnsTypeDefault()
    {
        var dict = new Dictionary<string, int>();

        dict.GetValueOrDefault("missing").Should().Be(0);
    }

    // ── AddIfNotExists ───────────────────────────────────────────────────────

    [Fact]
    public void AddIfNotExists_NewKey_AddsAndReturnsTrue()
    {
        var dict = new Dictionary<string, string>();

        var added = dict.AddIfNotExists("host", "localhost");

        added.Should().BeTrue();
        dict["host"].Should().Be("localhost");
    }

    [Fact]
    public void AddIfNotExists_ExistingKey_DoesNotOverwriteAndReturnsFalse()
    {
        var dict = new Dictionary<string, string> { ["host"] = "original" };

        var added = dict.AddIfNotExists("host", "new-value");

        added.Should().BeFalse();
        dict["host"].Should().Be("original");
    }

    // ── AddOrUpdate ──────────────────────────────────────────────────────────

    [Fact]
    public void AddOrUpdate_NewKey_AddsEntry()
    {
        var dict = new Dictionary<string, int>();

        dict.AddOrUpdate("port", 8080);

        dict["port"].Should().Be(8080);
    }

    [Fact]
    public void AddOrUpdate_ExistingKey_OverwritesValue()
    {
        var dict = new Dictionary<string, int> { ["port"] = 80 };

        dict.AddOrUpdate("port", 443);

        dict["port"].Should().Be(443);
    }

    // ── RemoveWhere ──────────────────────────────────────────────────────────

    [Fact]
    public void RemoveWhere_MatchingPredicate_RemovesMatchingEntries()
    {
        var dict = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
            ["d"] = 4
        };

        var removed = dict.RemoveWhere(kvp => kvp.Value % 2 == 0);

        removed.Should().Be(2);
        dict.Should().ContainKey("a").And.ContainKey("c");
        dict.Should().NotContainKey("b").And.NotContainKey("d");
    }

    [Fact]
    public void RemoveWhere_NothingMatches_RemovesZeroEntries()
    {
        var dict = new Dictionary<string, int> { ["x"] = 1 };

        var removed = dict.RemoveWhere(kvp => kvp.Value > 100);

        removed.Should().Be(0);
        dict.Should().HaveCount(1);
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    [Fact]
    public void Merge_OverwriteTrue_OverwritesExistingKeys()
    {
        var target = new Dictionary<string, string> { ["host"] = "old", ["port"] = "80" };
        var other = new Dictionary<string, string> { ["host"] = "new", ["path"] = "/" };

        target.Merge(other, overwrite: true);

        target["host"].Should().Be("new");
        target["port"].Should().Be("80");
        target["path"].Should().Be("/");
    }

    [Fact]
    public void Merge_OverwriteFalse_DoesNotOverwriteExistingKeys()
    {
        var target = new Dictionary<string, string> { ["host"] = "original" };
        var other = new Dictionary<string, string> { ["host"] = "override", ["extra"] = "added" };

        target.Merge(other, overwrite: false);

        target["host"].Should().Be("original");
        target["extra"].Should().Be("added");
    }

    // ── Invert ───────────────────────────────────────────────────────────────

    [Fact]
    public void Invert_UniqueValues_ProducesInvertedDictionary()
    {
        var dict = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["three"] = 3 };

        var inverted = dict.Invert();

        inverted[1].Should().Be("one");
        inverted[2].Should().Be("two");
        inverted[3].Should().Be("three");
    }

    // ── Where ────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_Predicate_ReturnsOnlyMatchingPairs()
    {
        var dict = new Dictionary<string, int>
        {
            ["small"] = 5,
            ["medium"] = 50,
            ["large"] = 500
        };

        var result = dict.Where(kvp => kvp.Value >= 50);

        result.Should().HaveCount(2);
        result.Should().ContainKey("medium");
        result.Should().ContainKey("large");
        result.Should().NotContainKey("small");
    }

    // ── Select ───────────────────────────────────────────────────────────────

    [Fact]
    public void Select_Selector_TransformsValues()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        var doubled = dict.Select(v => v * 2);

        doubled["a"].Should().Be(2);
        doubled["b"].Should().Be(4);
    }

    // ── Flatten ──────────────────────────────────────────────────────────────

    [Fact]
    public void Flatten_FlatDictionary_ReturnsSameKeys()
    {
        var dict = new Dictionary<string, object?> { ["host"] = "localhost", ["port"] = 5432 };

        var flat = dict.Flatten();

        flat.Should().ContainKey("host").WhoseValue.Should().Be("localhost");
        flat.Should().ContainKey("port").WhoseValue.Should().Be(5432);
    }

    [Fact]
    public void Flatten_WithPrefix_PrependsPrefixToKeys()
    {
        var dict = new Dictionary<string, object?> { ["host"] = "server" };

        var flat = dict.Flatten(prefix: "db");

        flat.Should().ContainKey("db.host");
    }

    // ── GetNestedValue ───────────────────────────────────────────────────────

    [Fact]
    public void GetNestedValue_ExistingDotPath_ReturnsValue()
    {
        var nested = new Dictionary<string, object?>
        {
            ["database"] = new Dictionary<string, object?> { ["host"] = "localhost" }
        };

        var value = nested.GetNestedValue("database.host");

        value.Should().Be("localhost");
    }

    [Fact]
    public void GetNestedValue_NonExistentPath_ReturnsNull()
    {
        var dict = new Dictionary<string, object?> { ["key"] = "value" };

        dict.GetNestedValue("missing.path").Should().BeNull();
    }

    // ── SetNestedValue ───────────────────────────────────────────────────────

    [Fact]
    public void SetNestedValue_NewPath_CreatesNestedStructure()
    {
        var dict = new Dictionary<string, object?>();

        dict.SetNestedValue("database.host", "prod-server");

        var retrieved = dict.GetNestedValue("database.host");
        retrieved.Should().Be("prod-server");
    }
}
