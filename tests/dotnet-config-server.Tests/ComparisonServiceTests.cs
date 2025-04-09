#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

sealed public class ComparisonServiceTests
{
    private readonly ComparisonService _sut;

    public ComparisonServiceTests()
    {
        var loggerMock = new Mock<ILogger<ComparisonService>>();
        _sut = new ComparisonService(loggerMock.Object);
    }

    private sealed class SampleRecord
    {
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool Enabled { get; set; }
    }

    // ── Compare ──────────────────────────────────────────────────────────────

    [Fact]
    public void Compare_IdenticalObjects_ReturnsNoChanges()
    {
        var a = new SampleRecord { Name = "db", Port = 5432, Enabled = true };
        var b = new SampleRecord { Name = "db", Port = 5432, Enabled = true };

        var result = _sut.Compare(a, b);

        result.Changes.Should().BeEmpty();
        result.ItemType.Should().Be(nameof(SampleRecord));
    }

    [Fact]
    public void Compare_SingleFieldChanged_ReturnsSingleChange()
    {
        var original = new SampleRecord { Name = "db-host", Port = 5432, Enabled = true };
        var modified = new SampleRecord { Name = "prod-db", Port = 5432, Enabled = true };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(1);
        var change = result.Changes[0];
        change.PropertyName.Should().Be(nameof(SampleRecord.Name));
        change.OriginalValue.Should().Be("db-host");
        change.ModifiedValue.Should().Be("prod-db");
    }

    [Fact]
    public void Compare_MultipleFieldsChanged_ReturnsAllChanges()
    {
        var original = new SampleRecord { Name = "old", Port = 80, Enabled = false };
        var modified = new SampleRecord { Name = "new", Port = 443, Enabled = true };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(3);
        result.Changes.Select(c => c.PropertyName)
            .Should().BeEquivalentTo(
                new[] { nameof(SampleRecord.Name), nameof(SampleRecord.Port), nameof(SampleRecord.Enabled) });
    }

    [Fact]
    public void Compare_PropertyChange_IncludesPropertyType()
    {
        var a = new SampleRecord { Port = 80 };
        var b = new SampleRecord { Port = 443 };

        var result = _sut.Compare(a, b);

        var portChange = result.Changes.Single(c => c.PropertyName == nameof(SampleRecord.Port));
        portChange.PropertyType.Should().Be(nameof(Int32));
    }

    // ── HasDifferences ───────────────────────────────────────────────────────

    [Fact]
    public void HasDifferences_IdenticalObjects_ReturnsFalse()
    {
        var a = new SampleRecord { Name = "same", Port = 1234, Enabled = true };
        var b = new SampleRecord { Name = "same", Port = 1234, Enabled = true };

        _sut.HasDifferences(a, b).Should().BeFalse();
    }

    [Fact]
    public void HasDifferences_DifferentObjects_ReturnsTrue()
    {
        var a = new SampleRecord { Name = "before" };
        var b = new SampleRecord { Name = "after" };

        _sut.HasDifferences(a, b).Should().BeTrue();
    }

    // ── GetSummary ───────────────────────────────────────────────────────────

    [Fact]
    public void GetSummary_NoChanges_ReturnsTotalChangesZeroAndEmptyFields()
    {
        var a = new SampleRecord { Name = "x", Port = 1, Enabled = true };
        var b = new SampleRecord { Name = "x", Port = 1, Enabled = true };

        var summary = _sut.GetSummary(a, b);

        summary.TotalChanges.Should().Be(0);
        summary.ChangedFields.Should().BeEmpty();
        summary.ChangePercentage.Should().Be(0);
    }

    [Fact]
    public void GetSummary_OneOfThreeFieldsChanged_Returns33PercentChangePercentage()
    {
        var a = new SampleRecord { Name = "old", Port = 80, Enabled = false };
        var b = new SampleRecord { Name = "new", Port = 80, Enabled = false };

        var summary = _sut.GetSummary(a, b);

        summary.TotalChanges.Should().Be(1);
        summary.ChangedFields.Should().ContainSingle().Which.Should().Be(nameof(SampleRecord.Name));
        summary.ChangePercentage.Should().BeApproximately(33.33, 0.5);
    }

    [Fact]
    public void GetSummary_AllFieldsChanged_Returns100PercentChangePercentage()
    {
        var a = new SampleRecord { Name = "a", Port = 1, Enabled = false };
        var b = new SampleRecord { Name = "b", Port = 2, Enabled = true };

        var summary = _sut.GetSummary(a, b);

        summary.TotalChanges.Should().Be(3);
        summary.ChangePercentage.Should().BeApproximately(100.0, 0.1);
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public void Compare_NullToString_PropertyValue_ReturnsNullLiteral()
    {
        var original = new NullableRecord { Label = null };
        var modified = new NullableRecord { Label = "set" };

        var result = _sut.Compare(original, modified);

        result.Changes.Should().HaveCount(1);
        result.Changes[0].OriginalValue.Should().Be("null");
        result.Changes[0].ModifiedValue.Should().Be("set");
    }

    private sealed class NullableRecord
    {
        public string? Label { get; set; }
    }
}
