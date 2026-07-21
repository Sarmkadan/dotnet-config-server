#nullable enable

using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace DotnetConfigServer.Tests;

public sealed class ComparisonServiceTests
{
    private readonly Mock<ILogger<ComparisonService>> _loggerMock;
    private readonly ComparisonService _sut;

    public ComparisonServiceTests()
    {
        _loggerMock = new Mock<ILogger<ComparisonService>>();
        _sut = new ComparisonService(_loggerMock.Object);
    }

    [Fact]
    public void Compare_DifferentObjects_ReturnsChanges()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Modified" };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(1);
        result.Changes[0].PropertyName.Should().Be("Name");
        result.Changes[0].OriginalValue.Should().Be("Original");
        result.Changes[0].ModifiedValue.Should().Be("Modified");
    }

    [Fact]
    public void Compare_SameObjects_ReturnsNoChanges()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Original" };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public void Compare_MissingKeyInOriginal_ReturnsChange()
    {
        var original = new Dictionary<string, string> { { "key1", "value1" } };
        var modified = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(1);
        result.Changes[0].PropertyName.Should().Be("key2");
        result.Changes[0].OriginalValue.Should().Be("null");
        result.Changes[0].ModifiedValue.Should().Be("value2");
    }

    [Fact]
    public void Compare_MissingKeyInModified_ReturnsChange()
    {
        var original = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
        var modified = new Dictionary<string, string> { { "key1", "value1" } };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(1);
        result.Changes[0].PropertyName.Should().Be("key2");
        result.Changes[0].OriginalValue.Should().Be("value2");
        result.Changes[0].ModifiedValue.Should().Be("null");
    }

    [Fact]
    public void HasDifferences_DifferentObjects_ReturnsTrue()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Modified" };

        var result = _sut.HasDifferences(original, modified);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasDifferences_SameObjects_ReturnsFalse()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Original" };

        var result = _sut.HasDifferences(original, modified);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSummary_DifferentObjects_ReturnsSummary()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Modified" };

        var result = _sut.GetSummary(original, modified);

        result.TotalChanges.Should().Be(1);
        result.ChangedFields.Should().Contain("Name");
        result.ChangePercentage.Should().Be(50);
    }

    [Fact]
    public void GetSummary_SameObjects_ReturnsSummary()
    {
        var original = new TestObject { Id = 1, Name = "Original" };
        var modified = new TestObject { Id = 1, Name = "Original" };

        var result = _sut.GetSummary(original, modified);

        result.TotalChanges.Should().Be(0);
        result.ChangedFields.Should().BeEmpty();
        result.ChangePercentage.Should().Be(0);
    }

    private sealed class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
