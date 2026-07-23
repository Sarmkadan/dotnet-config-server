#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for comparing configurations and detecting changes.
/// Generates detailed reports of what has changed between versions.
/// </summary>
public interface IComparisonService
{
    /// <summary>
    /// Compares two configurations and returns detailed differences.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    ComparisonResult Compare<T>(T original, T modified) where T : notnull;

    /// <summary>
    /// Checks if two objects have differences.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    bool HasDifferences<T>(T original, T modified) where T : notnull;

    /// <summary>
    /// Gets a summary of changes.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    SummaryOfChanges GetSummary<T>(T original, T modified) where T : notnull;
}

/// <summary>
/// Reflection-based implementation of <see cref="IComparisonService"/>. Flattens the public
/// properties of the compared objects into a key/value map and delegates the actual
/// added/removed/changed decision to the shared <see cref="IConfigDiffer"/>, so object-level
/// comparison and configuration-version diffing (<see cref="DiffService"/>) can never disagree
/// on what counts as a change for the same pair of values.
/// </summary>
public sealed class ComparisonService : IComparisonService
{
    private readonly IConfigDiffer _differ;
    private readonly ILogger<ComparisonService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ComparisonService"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="differ"/> or <paramref name="logger"/> is <c>null</c>.</exception>
    public ComparisonService(IConfigDiffer differ, ILogger<ComparisonService> logger)
    {
        ArgumentNullException.ThrowIfNull(differ);
        ArgumentNullException.ThrowIfNull(logger);

        _differ = differ;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    public ComparisonResult Compare<T>(T original, T modified) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(modified);

        var result = new ComparisonResult { ItemType = typeof(T).Name };
        var properties = typeof(T).GetProperties();

        var originalMap = new Dictionary<string, string?>();
        var modifiedMap = new Dictionary<string, string?>();
        var propertyTypeByName = new Dictionary<string, string>();

        foreach (var property in properties)
        {
            originalMap[property.Name] = property.GetValue(original)?.ToString();
            modifiedMap[property.Name] = property.GetValue(modified)?.ToString();
            propertyTypeByName[property.Name] = property.PropertyType.Name;
        }

        // Every property key is present on both sides (same type T), so the canonical
        // differ can only ever report ChangeType.Modified here - never Added/Deleted.
        var changes = _differ.Diff(originalMap, modifiedMap, ConfigDiffOptions.Default);

        foreach (var change in changes.Where(c => c.ChangeType == ChangeType.Modified))
        {
            result.Changes.Add(new PropertyChange
            {
                PropertyName = change.Key,
                OriginalValue = change.OldValue ?? "null",
                ModifiedValue = change.NewValue ?? "null",
                PropertyType = propertyTypeByName[change.Key]
            });
        }

        return result;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    public bool HasDifferences<T>(T original, T modified) where T : notnull =>
        Compare(original, modified).Changes.Count > 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="modified"/> is <c>null</c>.</exception>
    public SummaryOfChanges GetSummary<T>(T original, T modified) where T : notnull
    {
        var comparison = Compare(original, modified);

        return new SummaryOfChanges
        {
            TotalChanges = comparison.Changes.Count,
            ChangedFields = comparison.Changes.Select(c => c.PropertyName).ToList(),
            ChangePercentage = CalculateChangePercentage<T>(comparison)
        };
    }

    /// <summary>
    /// Computes the percentage of public properties on <typeparamref name="T"/> that changed.
    /// </summary>
    private static double CalculateChangePercentage<T>(ComparisonResult result) where T : notnull
    {
        var totalProperties = typeof(T).GetProperties().Length;
        return totalProperties == 0 ? 0 : (double)result.Changes.Count / totalProperties * 100;
    }
}

public sealed class ComparisonResult
{
    public string ItemType { get; set; } = string.Empty;
    public List<PropertyChange> Changes { get; set; } = new();
}

public sealed class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string ModifiedValue { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
}

public sealed class SummaryOfChanges
{
    public int TotalChanges { get; set; }
    public List<string> ChangedFields { get; set; } = new();
    public double ChangePercentage { get; set; }
}
