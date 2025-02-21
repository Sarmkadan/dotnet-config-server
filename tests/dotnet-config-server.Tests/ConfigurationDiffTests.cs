// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

public class ConfigurationDiffTests
{
    private static ConfigurationDiff CreateEmptyDiff() => new()
    {
        ConfigurationId = Guid.NewGuid(),
        FromVersionId = Guid.NewGuid(),
        ToVersionId = Guid.NewGuid(),
        CreatedBy = "deploy-bot"
    };

    [Fact]
    public void AddChange_SingleAddedEntry_IncrementsOnlyAddedCounter()
    {
        var diff = CreateEmptyDiff();

        diff.AddChange("database.host", ChangeType.Added, null, "localhost");

        diff.AddedCount.Should().Be(1);
        diff.ModifiedCount.Should().Be(0);
        diff.DeletedCount.Should().Be(0);
        diff.TotalChanges.Should().Be(1);
        diff.Changes.Should().HaveCount(1);
    }

    [Fact]
    public void AddChange_MixedChangeTypes_TotalReflectsAllCounters()
    {
        var diff = CreateEmptyDiff();

        diff.AddChange("key.new1", ChangeType.Added, null, "value-a");
        diff.AddChange("key.new2", ChangeType.Added, null, "value-b");
        diff.AddChange("key.changed", ChangeType.Modified, "old", "new");
        diff.AddChange("key.removed", ChangeType.Deleted, "gone");

        diff.AddedCount.Should().Be(2);
        diff.ModifiedCount.Should().Be(1);
        diff.DeletedCount.Should().Be(1);
        diff.TotalChanges.Should().Be(4);
    }

    [Fact]
    public void GetChangesByType_FiltersBySpecifiedType()
    {
        var diff = CreateEmptyDiff();
        diff.AddChange("key1", ChangeType.Added, null, "v1");
        diff.AddChange("key2", ChangeType.Modified, "old", "new");
        diff.AddChange("key3", ChangeType.Deleted, "removed");
        diff.AddChange("key4", ChangeType.Added, null, "v4");

        var added = diff.GetChangesByType(ChangeType.Added);
        var deleted = diff.GetChangesByType(ChangeType.Deleted);

        added.Should().HaveCount(2);
        added.Should().OnlyContain(e => e.ChangeType == ChangeType.Added);
        deleted.Should().HaveCount(1);
        deleted.Single().Key.Should().Be("key3");
    }

    [Fact]
    public void GetSummary_ReflectsAccumulatedCounts()
    {
        var diff = CreateEmptyDiff();
        diff.AddChange("a", ChangeType.Added, null, "1");
        diff.AddChange("b", ChangeType.Modified, "x", "y");
        diff.AddChange("c", ChangeType.Modified, "p", "q");

        var summary = diff.GetSummary();

        summary.Id.Should().Be(diff.Id);
        summary.TotalChanges.Should().Be(3);
        summary.AddedCount.Should().Be(1);
        summary.ModifiedCount.Should().Be(2);
        summary.DeletedCount.Should().Be(0);
        summary.CreatedBy.Should().Be("deploy-bot");
    }

    [Fact]
    public void DiffEntry_Validate_AddedEntryWithNullNewValue_ThrowsValidationException()
    {
        var entry = new DiffEntry
        {
            Key = "feature.flag",
            ChangeType = ChangeType.Added,
            NewValue = null
        };

        var act = () => entry.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("NewValue");
    }

    [Fact]
    public void DiffEntry_Validate_DeletedEntryWithNullOldValue_ThrowsValidationException()
    {
        var entry = new DiffEntry
        {
            Key = "legacy.endpoint",
            ChangeType = ChangeType.Deleted,
            OldValue = null
        };

        var act = () => entry.Validate();

        act.Should().Throw<ValidationException>()
           .Which.Errors.Should().ContainKey("OldValue");
    }

    [Fact]
    public void DiffEntry_Validate_ModifiedEntryWithBothValues_DoesNotThrow()
    {
        var entry = new DiffEntry
        {
            Key = "db.timeout",
            ChangeType = ChangeType.Modified,
            OldValue = "30",
            NewValue = "60"
        };

        var act = () => entry.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void DiffEntry_GetChangeDescription_ModifiedEntry_ContainsBothOldAndNewValues()
    {
        var entry = new DiffEntry
        {
            Key = "cache.ttl",
            ChangeType = ChangeType.Modified,
            OldValue = "300",
            NewValue = "600"
        };

        var description = entry.GetChangeDescription();

        description.Should().Contain("cache.ttl");
        description.Should().Contain("300");
        description.Should().Contain("600");
    }

    [Fact]
    public void DiffEntry_GetChangeDescription_AddedEntry_FormatsCorrectly()
    {
        var entry = new DiffEntry
        {
            Key = "metrics.enabled",
            ChangeType = ChangeType.Added,
            NewValue = "true"
        };

        var description = entry.GetChangeDescription();

        description.Should().Be("Added: metrics.enabled = true");
    }
}
