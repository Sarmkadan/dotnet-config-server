#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class CollectionExtensionsTests
{
    [Fact]
    public void Batch_CollectionDivisibleByBatchSize_ProducesFullBatches()
    {
        var source = Enumerable.Range(1, 6);

        var batches = source.Batch(3).ToList();

        batches.Should().HaveCount(2);
        batches[0].Should().Equal(1, 2, 3);
        batches[1].Should().Equal(4, 5, 6);
    }

    [Fact]
    public void Batch_CollectionNotDivisibleByBatchSize_LastBatchIsSmaller()
    {
        var source = Enumerable.Range(1, 7);

        var batches = source.Batch(3).ToList();

        batches.Should().HaveCount(3);
        batches[2].Should().HaveCount(1).And.Equal(7);
    }

    [Fact]
    public void Batch_EmptyCollection_ReturnsNoBatches()
    {
        var batches = Enumerable.Empty<int>().Batch(3).ToList();

        batches.Should().BeEmpty();
    }

    [Fact]
    public void Batch_InvalidBatchSize_ThrowsArgumentException()
    {
        var source = new[] { 1, 2, 3 };

        var act = () => source.Batch(0).ToList();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForEach_Action_ExecutesForEachElement()
    {
        var results = new List<int>();
        var source = new[] { 10, 20, 30 };

        source.ForEach(x => results.Add(x));

        results.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void ForEach_ActionWithIndex_PassesCorrectIndices()
    {
        var indices = new List<int>();
        var source = new[] { "a", "b", "c" };

        source.ForEach((_, i) => indices.Add(i));

        indices.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void IsNullOrEmpty_NullCollection_ReturnsTrue()
    {
        IEnumerable<int>? source = null;

        source.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
    {
        new List<int>().IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyCollection_ReturnsFalse()
    {
        new[] { 1, 2 }.IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void IsSingle_CollectionWithOneElement_ReturnsTrue()
    {
        new[] { 42 }.IsSingle().Should().BeTrue();
    }

    [Fact]
    public void IsSingle_CollectionWithMultipleElements_ReturnsFalse()
    {
        new[] { 1, 2 }.IsSingle().Should().BeFalse();
    }

    [Fact]
    public void IsSingle_EmptyCollection_ReturnsFalse()
    {
        new int[0].IsSingle().Should().BeFalse();
    }

    [Fact]
    public void HasMultiple_CollectionWithMultipleElements_ReturnsTrue()
    {
        new[] { 1, 2, 3 }.HasMultiple().Should().BeTrue();
    }

    [Fact]
    public void HasMultiple_CollectionWithOneElement_ReturnsFalse()
    {
        new[] { 1 }.HasMultiple().Should().BeFalse();
    }

    [Fact]
    public void HasMultiple_EmptyCollection_ReturnsFalse()
    {
        new int[0].HasMultiple().Should().BeFalse();
    }

    [Fact]
    public void SkipLast_MultipleElements_OmitsLastElement()
    {
        var result = new[] { 1, 2, 3, 4 }.SkipLast().ToList();

        result.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void SkipLast_SingleElement_ReturnsEmpty()
    {
        var result = new[] { 99 }.SkipLast().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void SkipLast_EmptyCollection_ReturnsEmpty()
    {
        var result = new int[0].SkipLast().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void DistinctBy_DuplicateKeys_ReturnsFirstOccurrence()
    {
        var source = new[]
        {
            new { Id = 1, Name = "first" },
            new { Id = 1, Name = "duplicate" },
            new { Id = 2, Name = "unique" }
        };

        var result = DotnetConfigServer.Utilities.CollectionExtensions.DistinctBy(source, x => x.Id).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Name == "first");
        result.Should().NotContain(x => x.Name == "duplicate");
    }

    [Fact]
    public void DistinctBy_AllUniqueKeys_ReturnsAllElements()
    {
        var source = new[] { 1, 2, 3, 4 };

        var result = DotnetConfigServer.Utilities.CollectionExtensions.DistinctBy(source, x => x).ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void ZipWith_TwoCollections_CreatesCorrectPairs()
    {
        var numbers = new[] { 1, 2, 3 };
        var letters = new[] { "a", "b", "c" };

        var pairs = numbers.ZipWith(letters).ToList();

        pairs.Should().HaveCount(3);
        pairs[0].Should().Be((1, "a"));
        pairs[1].Should().Be((2, "b"));
        pairs[2].Should().Be((3, "c"));
    }

    [Fact]
    public void FirstOrDefault_WithDefaultValue_ReturnsDefaultWhenEmpty()
    {
        var empty = new List<string>();

        var result = DotnetConfigServer.Utilities.CollectionExtensions.FirstOrDefault(empty, "fallback");

        result.Should().Be("fallback");
    }

    [Fact]
    public void FirstOrDefault_NonEmptyCollection_ReturnsFirstElement()
    {
        var source = new[] { "alpha", "beta" };

        var result = DotnetConfigServer.Utilities.CollectionExtensions.FirstOrDefault(source, "fallback");

        result.Should().Be("alpha");
    }

    [Fact]
    public void Shuffle_ProducesAllOriginalElements()
    {
        var source = Enumerable.Range(1, 10).ToList();

        var shuffled = source.Shuffle(new Random(42)).ToList();

        shuffled.Should().HaveCount(10);
        shuffled.Should().BeEquivalentTo(source);
    }

    [Fact]
    public void GroupConsecutive_ConsecutiveSameValues_GroupsCorrectly()
    {
        var source = new[] { 1, 1, 2, 2, 2, 1 };

        var groups = source.GroupConsecutive(x => x).ToList();

        groups.Should().HaveCount(3);
        groups[0].Key.Should().Be(1);
        groups[0].Should().HaveCount(2);
        groups[1].Key.Should().Be(2);
        groups[1].Should().HaveCount(3);
        groups[2].Key.Should().Be(1);
        groups[2].Should().HaveCount(1);
    }
}
