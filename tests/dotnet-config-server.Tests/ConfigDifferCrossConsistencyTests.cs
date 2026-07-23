#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Services;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Cross-consistency tests asserting that <see cref="DiffService"/> (via <see cref="IConfigDiffer"/>)
/// and <see cref="ComparisonService"/> agree on the canonical diff semantics for the same inputs,
/// since both public entry points delegate their change-detection to <see cref="KeyValueConfigDiffer"/>.
/// </summary>
public sealed class ConfigDifferCrossConsistencyTests
{
    private readonly KeyValueConfigDiffer _sut = new();

    private sealed class ConfigLikeObject
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// The same key/value pairs, expressed once as a raw dictionary diff and once as an object
    /// property comparison, must report the same set of modified keys.
    /// </summary>
    [Fact]
    public void Diff_And_ComparisonService_AgreeOnModifiedKeys()
    {
        var original = new ConfigLikeObject { Name = "db.host", Value = "localhost" };
        var modified = new ConfigLikeObject { Name = "db.host", Value = "prod-db" };

        var baseline = new Dictionary<string, string?> { ["Name"] = original.Name, ["Value"] = original.Value };
        var updated = new Dictionary<string, string?> { ["Name"] = modified.Name, ["Value"] = modified.Value };

        var directDiff = _sut.Diff(baseline, updated);
        var comparisonService = new ComparisonService(_sut, new Moq.Mock<Microsoft.Extensions.Logging.ILogger<ComparisonService>>().Object);
        var comparisonResult = comparisonService.Compare(original, modified);

        var directModifiedKeys = directDiff.Where(c => c.ChangeType == ChangeType.Modified).Select(c => c.Key).OrderBy(k => k).ToList();
        var comparisonModifiedKeys = comparisonResult.Changes.Select(c => c.PropertyName).OrderBy(k => k).ToList();

        comparisonModifiedKeys.Should().BeEquivalentTo(directModifiedKeys);
    }

    /// <summary>
    /// A missing key (Added/Deleted) must never be conflated with a present key whose value is
    /// <c>null</c> - the latter normalizes to an empty string and is compared like any other value.
    /// </summary>
    [Fact]
    public void Diff_MissingKey_IsDistinctFrom_PresentNullValue()
    {
        var baseline = new Dictionary<string, string?> { ["a"] = null };
        var updated = new Dictionary<string, string?> { ["a"] = null, ["b"] = "x" };

        var changes = _sut.Diff(baseline, updated);

        changes.Should().ContainSingle(c => c.Key == "b" && c.ChangeType == ChangeType.Added);
        changes.Should().NotContain(c => c.Key == "a");
    }

    /// <summary>
    /// Key comparison respects <see cref="ConfigDiffOptions.CaseSensitiveKeys"/> explicitly instead
    /// of an implicit default, avoiding disagreements between callers on whether "Key" and "key"
    /// are the same configuration entry.
    /// </summary>
    [Fact]
    public void Diff_CaseSensitivity_IsExplicitAndConsistent()
    {
        var baseline = new Dictionary<string, string?> { ["Key"] = "1" };
        var updated = new Dictionary<string, string?> { ["key"] = "1" };

        var caseSensitive = _sut.Diff(baseline, updated, new ConfigDiffOptions(CaseSensitiveKeys: true));
        var caseInsensitive = _sut.Diff(baseline, updated, new ConfigDiffOptions(CaseSensitiveKeys: false));

        caseSensitive.Should().Contain(c => c.Key == "Key" && c.ChangeType == ChangeType.Deleted);
        caseSensitive.Should().Contain(c => c.Key == "key" && c.ChangeType == ChangeType.Added);
        caseInsensitive.Should().BeEmpty();
    }
}
