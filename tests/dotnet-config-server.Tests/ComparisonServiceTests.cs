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

/// <summary>
/// Tests for the ComparisonService class.
/// </summary>
public sealed class ComparisonServiceTests
{
    private readonly ComparisonService _sut;

    /// <summary>
    /// Initializes a new instance of the ComparisonServiceTests class.
    /// </summary>
    public ComparisonServiceTests()
    {
        var loggerMock = new Mock<ILogger<ComparisonService>>();
        _sut = new ComparisonService(loggerMock.Object);
    }

    private sealed class SampleRecord
    {
        /// <summary>
        /// Gets or sets the name of the record.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the port of the record.
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the record is enabled.
        /// </summary>
        public bool Enabled { get; set; }
    }

    // ── Compare ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that comparing two identical objects returns no changes.
    /// </summary>
    [Fact]
    public void Compare_IdenticalObjects_ReturnsNoChanges()
    {
        var a = new SampleRecord { Name = "db", Port = 5432, Enabled = true };
        var b = new SampleRecord { Name = "db", Port = 5432, Enabled = true };

        var result = _sut.Compare(a, b);

        result.Changes.Should().BeEmpty();
        result.ItemType.Should().Be(nameof(SampleRecord));
    }

    /// <summary>
    /// Tests that comparing two objects with a single field changed returns a single change.
    /// </summary>
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

    /// <summary>
    /// Tests that comparing two objects with multiple fields changed returns all changes.
    /// </summary>
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

    /// <summary>
    /// Tests that comparing two objects with a property change includes the property type.
    /// </summary>
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

    /// <summary>
    /// Tests that checking for differences between two identical objects returns false.
    /// </summary>
    [Fact]
    public void HasDifferences_IdenticalObjects_ReturnsFalse()
    {
        var a = new SampleRecord { Name = "same", Port = 1234, Enabled = true };
        var b = new SampleRecord { Name = "same", Port = 1234, Enabled = true };

        _sut.HasDifferences(a, b).Should().BeFalse();
    }

    /// <summary>
    /// Tests that checking for differences between two different objects returns true.
    /// </summary>
    [Fact]
    public void HasDifferences_DifferentObjects_ReturnsTrue()
    {
        var a = new SampleRecord { Name = "before" };
        var b = new SampleRecord { Name = "after" };

        _sut.HasDifferences(a, b).Should().BeTrue();
    }

    // ── GetSummary ───────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that getting a summary of two identical objects returns zero total changes and an empty list of changed fields.
    /// </summary>
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

    /// <summary>
    /// Tests that getting a summary of two objects with one of three fields changed returns a 33% change percentage.
    /// </summary>
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

    /// <summary>
    /// Tests that getting a summary of two objects with all fields changed returns a 100% change percentage.
    /// </summary>
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

    /// <summary>
    /// Tests that comparing two objects with a null to string property value returns the null literal.
    /// </summary>
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
        /// <summary>
        /// Gets or sets the label of the record.
        /// </summary>
        public string? Label { get; set; }
    }
}
