#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Provides unit tests for the <see cref="DictionaryExtensions"/> class extension methods.
/// Tests various dictionary operations including value retrieval, addition, updates, filtering, merging, and nested value manipulation.
/// </summary>
public sealed class DictionaryExtensionsTests
{
    // ── GetValueOrDefault ────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.GetValueOrDefault{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> returns the value when the key exists in the dictionary.
    /// </summary>
    [Fact]
    public void GetValueOrDefault_ExistingKey_ReturnsValue()
    {
        var dict = new Dictionary<string, int> { ["timeout"] = 30 };

        dict.GetValueOrDefault("timeout", -1).Should().Be(30);
    }

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.GetValueOrDefault{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> returns the default value when the key does not exist in the dictionary.
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKey_ReturnsDefaultValue()
    {
        var dict = new Dictionary<string, int>();

        dict.GetValueOrDefault("missing", 42).Should().Be(42);
    }

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.GetValueOrDefault{TKey,TValue}(IDictionary{TKey,TValue}, TKey)"/> returns the type's default value when the key does not exist and no explicit default is provided.
    /// </summary>
    [Fact]
    public void GetValueOrDefault_MissingKey_NoExplicitDefault_ReturnsTypeDefault()
    {
        var dict = new Dictionary<string, int>();

        dict.GetValueOrDefault("missing").Should().Be(0);
    }

    // ── AddIfNotExists ───────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.AddIfNotExists{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> adds a new key-value pair and returns true when the key does not exist.
    /// </summary>
    [Fact]
    public void AddIfNotExists_NewKey_AddsAndReturnsTrue()
    {
        var dict = new Dictionary<string, string>();

        var added = dict.AddIfNotExists("host", "localhost");

        added.Should().BeTrue();
        dict["host"].Should().Be("localhost");
    }

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.AddIfNotExists{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> does not overwrite an existing key-value pair and returns false when the key already exists.
    /// </summary>
    [Fact]
    public void AddIfNotExists_ExistingKey_DoesNotOverwriteAndReturnsFalse()
    {
        var dict = new Dictionary<string, string> { ["host"] = "original" };

        var added = dict.AddIfNotExists("host", "new-value");

        added.Should().BeFalse();
        dict["host"].Should().Be("original");
    }

    // ── AddOrUpdate ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.AddOrUpdate{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> adds a new entry when the key does not exist.
    /// </summary>
    [Fact]
    public void AddOrUpdate_NewKey_AddsEntry()
    {
        var dict = new Dictionary<string, int>();

        dict.AddOrUpdate("port", 8080);

        dict["port"].Should().Be(8080);
    }

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.AddOrUpdate{TKey,TValue}(IDictionary{TKey,TValue}, TKey, TValue)"/> overwrites the existing value when the key already exists.
    /// </summary>
    [Fact]
    public void AddOrUpdate_ExistingKey_OverwritesValue()
    {
        var dict = new Dictionary<string, int> { ["port"] = 80 };

        dict.AddOrUpdate("port", 443);

        dict["port"].Should().Be(443);
    }

    // ── RemoveWhere ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.RemoveWhere{TKey,TValue}(IDictionary{TKey,TValue}, Func{KeyValuePair{TKey,TValue}, bool}")/> removes all entries matching the predicate and returns the count of removed entries.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.RemoveWhere{TKey,TValue}(IDictionary{TKey,TValue}, Func{KeyValuePair{TKey,TValue}, bool}")/> returns zero and makes no changes when no entries match the predicate.
    /// </summary>
    [Fact]
    public void RemoveWhere_NothingMatches_RemovesZeroEntries()
    {
        var dict = new Dictionary<string, int> { ["x"] = 1 };

        var removed = dict.RemoveWhere(kvp => kvp.Value > 100);

        removed.Should().Be(0);
        dict.Should().HaveCount(1);
    }

    // ── Merge ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Merge{TKey,TValue}(IDictionary{TKey,TValue}, IDictionary{TKey,TValue}, bool)"/> overwrites existing keys in the target dictionary when the overwrite parameter is true.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Merge{TKey,TValue}(IDictionary{TKey,TValue}, IDictionary{TKey,TValue}, bool)"/> does not overwrite existing keys in the target dictionary when the overwrite parameter is false.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Invert{TKey,TValue}(IDictionary{TKey,TValue})"/> produces an inverted dictionary where keys become values and values become keys.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Where{TKey,TValue}(IDictionary{TKey,TValue}, Func{KeyValuePair{TKey,TValue}, bool}")/> returns a new dictionary containing only the key-value pairs that match the predicate.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Select{TKey,TValue,TResult}(IDictionary{TKey,TValue}, Func{TValue, TResult}")/> transforms the values of the dictionary using the selector function.
    /// </summary>
    [Fact]
    public void Select_Selector_TransformsValues()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        var doubled = dict.Select(v => v * 2);

        doubled["a"].Should().Be(2);
        doubled["b"].Should().Be(4);
    }

    // ── Flatten ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Flatten{TKey,TValue}(IDictionary{TKey,TValue})"/> returns a new dictionary with the same keys when the dictionary is already flat.
    /// </summary>
    [Fact]
    public void Flatten_FlatDictionary_ReturnsSameKeys()
    {
        var dict = new Dictionary<string, object?> { ["host"] = "localhost", ["port"] = 5432 };

        var flat = dict.Flatten();

        flat.Should().ContainKey("host").WhoseValue.Should().Be("localhost");
        flat.Should().ContainKey("port").WhoseValue.Should().Be(5432);
    }

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.Flatten{TKey,TValue}(IDictionary{TKey,TValue}, string)"/> prepends the specified prefix to all keys in the resulting dictionary.
    /// </summary>
    [Fact]
    public void Flatten_WithPrefix_PrependsPrefixToKeys()
    {
        var dict = new Dictionary<string, object?> { ["host"] = "server" };

        var flat = dict.Flatten(prefix: "db");

        flat.Should().ContainKey("db.host");
    }

    // ── GetNestedValue ───────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.GetNestedValue(IDictionary{string, object}, string)"/> returns the value at the specified dot-separated path in a nested dictionary structure.
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.GetNestedValue(IDictionary{string, object}, string)"/> returns null when the specified path does not exist in the nested dictionary structure.
    /// </summary>
    [Fact]
    public void GetNestedValue_NonExistentPath_ReturnsNull()
    {
        var dict = new Dictionary<string, object?> { ["key"] = "value" };

        dict.GetNestedValue("missing.path").Should().BeNull();
    }

    // ── SetNestedValue ───────────────────────────────────────────────────────

    /// <summary>
    /// Tests that <see cref="DictionaryExtensions.SetNestedValue(IDictionary{string, object}, string, object)"/> creates the nested structure and sets the value at the specified dot-separated path.
    /// </summary>
    [Fact]
    public void SetNestedValue_NewPath_CreatesNestedStructure()
    {
        var dict = new Dictionary<string, object?>();

        dict.SetNestedValue("database.host", "prod-server");

        var retrieved = dict.GetNestedValue("database.host");
        retrieved.Should().Be("prod-server");
    }
}