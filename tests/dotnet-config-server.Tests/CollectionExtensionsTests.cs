#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the CollectionExtensions class.
/// </summary>
public sealed class CollectionExtensionsTests
{
    /// <summary>
    /// Tests that the Batch method produces full batches when the collection is divisible by the batch size.
    /// </summary>
    [Fact]
    public void Batch_CollectionDivisibleByBatchSize_ProducesFullBatches()
    {
        var source = Enumerable.Range(1, 6);

        var batches = source.Batch(3).ToList();

        batches.Should().HaveCount(2);
        batches[0].Should().Equal(1, 2, 3);
        batches[1].Should().Equal(4, 5, 6);
    }

    /// <summary>
    /// Tests that the Batch method produces a smaller last batch when the collection is not divisible by the batch size.
    /// </summary>
    [Fact]
    public void Batch_CollectionNotDivisibleByBatchSize_LastBatchIsSmaller()
    {
        var source = Enumerable.Range(1, 7);

        var batches = source.Batch(3).ToList();

        batches.Should().HaveCount(3);
        batches[2].Should().HaveCount(1).And.Equal(7);
    }

    /// <summary>
    /// Tests that the Batch method returns no batches for an empty collection.
    /// </summary>
    [Fact]
    public void Batch_EmptyCollection_ReturnsNoBatches()
    {
        var batches = Enumerable.Empty<int>().Batch(3).ToList();

        batches.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the Batch method throws an ArgumentException for an invalid batch size.
    /// </summary>
    [Fact]
    public void Batch_InvalidBatchSize_ThrowsArgumentException()
    {
        var source = new[] { 1, 2, 3 };

        var act = () => source.Batch(0).ToList();

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that the ForEach method executes the action for each element in the collection.
    /// </summary>
    [Fact]
    public void ForEach_Action_ExecutesForEachElement()
    {
        var results = new List<int>();
        var source = new[] { 10, 20, 30 };

        source.ForEach(x => results.Add(x));

        results.Should().Equal(10, 20, 30);
    }

    /// <summary>
    /// Tests that the ForEach method passes the correct indices to the action.
    /// </summary>
    [Fact]
    public void ForEach_ActionWithIndex_PassesCorrectIndices()
    {
        var indices = new List<int>();
        var source = new[] { "a", "b", "c" };

        source.ForEach((_, i) => indices.Add(i));

        indices.Should().Equal(0, 1, 2);
    }

    /// <summary>
    /// Tests that the IsNullOrEmpty method returns true for a null collection.
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_NullCollection_ReturnsTrue()
    {
        IEnumerable<int>? source = null;

        source.IsNullOrEmpty().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the IsNullOrEmpty method returns true for an empty collection.
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_EmptyCollection_ReturnsTrue()
    {
        new List<int>().IsNullOrEmpty().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the IsNullOrEmpty method returns false for a non-empty collection.
    /// </summary>
    [Fact]
    public void IsNullOrEmpty_NonEmptyCollection_ReturnsFalse()
    {
        new[] { 1, 2 }.IsNullOrEmpty().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the IsSingle method returns true for a collection with one element.
    /// </summary>
    [Fact]
    public void IsSingle_CollectionWithOneElement_ReturnsTrue()
    {
        new[] { 42 }.IsSingle().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the IsSingle method returns false for a collection with multiple elements.
    /// </summary>
    [Fact]
    public void IsSingle_CollectionWithMultipleElements_ReturnsFalse()
    {
        new[] { 1, 2 }.IsSingle().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the IsSingle method returns false for an empty collection.
    /// </summary>
    [Fact]
    public void IsSingle_EmptyCollection_ReturnsFalse()
    {
        new int[0].IsSingle().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the HasMultiple method returns true for a collection with multiple elements.
    /// </summary>
    [Fact]
    public void HasMultiple_CollectionWithMultipleElements_ReturnsTrue()
    {
        new[] { 1, 2, 3 }.HasMultiple().Should().BeTrue();
    }

    /// <summary>
    /// Tests that the HasMultiple method returns false for a collection with one element.
    /// </summary>
    [Fact]
    public void HasMultiple_CollectionWithOneElement_ReturnsFalse()
    {
        new[] { 1 }.HasMultiple().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the HasMultiple method returns false for an empty collection.
    /// </summary>
    [Fact]
    public void HasMultiple_EmptyCollection_ReturnsFalse()
    {
        new int[0].HasMultiple().Should().BeFalse();
    }

    /// <summary>
    /// Tests that the SkipLast method omits the last element for a collection with multiple elements.
    /// </summary>
    [Fact]
    public void SkipLast_MultipleElements_OmitsLastElement()
    {
        var result = new[] { 1, 2, 3, 4 }.SkipLast().ToList();

        result.Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Tests that the SkipLast method returns an empty collection for a collection with one element.
    /// </summary>
    [Fact]
    public void SkipLast_SingleElement_ReturnsEmpty()
    {
        var result = new[] { 99 }.SkipLast().ToList();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the SkipLast method returns an empty collection for an empty collection.
    /// </summary>
    [Fact]
    public void SkipLast_EmptyCollection_ReturnsEmpty()
    {
        var result = new int[0].SkipLast().ToList();

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the DistinctBy method returns the first occurrence of each key.
    /// </summary>
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

    /// <summary>
    /// Tests that the DistinctBy method returns all elements when all keys are unique.
    /// </summary>
    [Fact]
    public void DistinctBy_AllUniqueKeys_ReturnsAllElements()
    {
        var source = new[] { 1, 2, 3, 4 };

        var result = DotnetConfigServer.Utilities.CollectionExtensions.DistinctBy(source, x => x).ToList();

        result.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that the ZipWith method creates correct pairs from two collections.
    /// </summary>
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

    /// <summary>
    /// Tests that the FirstOrDefault method returns the default value when the collection is empty.
    /// </summary>
    [Fact]
    public void FirstOrDefault_WithDefaultValue_ReturnsDefaultWhenEmpty()
    {
        var empty = new List<string>();

        var result = DotnetConfigServer.Utilities.CollectionExtensions.FirstOrDefault(empty, "fallback");

        result.Should().Be("fallback");
    }

    /// <summary>
    /// Tests that the FirstOrDefault method returns the first element when the collection is not empty.
    /// </summary>
    [Fact]
    public void FirstOrDefault_NonEmptyCollection_ReturnsFirstElement()
    {
        var source = new[] { "alpha", "beta" };

        var result = DotnetConfigServer.Utilities.CollectionExtensions.FirstOrDefault(source, "fallback");

        result.Should().Be("alpha");
    }

    /// <summary>
    /// Tests that the Shuffle method produces all original elements in a random order.
    /// </summary>
    [Fact]
    public void Shuffle_ProducesAllOriginalElements()
    {
        var source = Enumerable.Range(1, 10).ToList();

        var shuffled = source.Shuffle(new Random(42)).ToList();

        shuffled.Should().HaveCount(10);
        shuffled.Should().BeEquivalentTo(source);
    }

    /// <summary>
    /// Tests that the GroupConsecutive method groups consecutive elements with the same value.
    /// </summary>
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
