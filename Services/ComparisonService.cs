#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    ComparisonResult Compare<T>(T original, T modified) where T : notnull;

    /// <summary>
    /// Checks if two objects have differences.
    /// </summary>
    bool HasDifferences<T>(T original, T modified) where T : notnull;

    /// <summary>
    /// Gets a summary of changes.
    /// </summary>
    SummaryOfChanges GetSummary<T>(T original, T modified) where T : notnull;
}

sealed public class ComparisonService : IComparisonService
{
    private readonly ILogger<ComparisonService> _logger;

    public ComparisonService(ILogger<ComparisonService> logger)
    {
        _logger = logger;
    }

    public ComparisonResult Compare<T>(T original, T modified) where T : notnull
    {
        var result = new ComparisonResult { ItemType = typeof(T).Name };
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            var originalValue = property.GetValue(original);
            var modifiedValue = property.GetValue(modified);

            if (!Equals(originalValue, modifiedValue))
            {
                result.Changes.Add(new PropertyChange
                {
                    PropertyName = property.Name,
                    OriginalValue = originalValue?.ToString() ?? "null",
                    ModifiedValue = modifiedValue?.ToString() ?? "null",
                    PropertyType = property.PropertyType.Name
                });
            }
        }

        return result;
    }

    public bool HasDifferences<T>(T original, T modified) where T : notnull
    {
        return Compare(original, modified).Changes.Count > 0;
    }

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

    private double CalculateChangePercentage<T>(ComparisonResult result) where T : notnull
    {
        var totalProperties = typeof(T).GetProperties().Length;
        if (totalProperties == 0)
            return 0;

        return (double)result.Changes.Count / totalProperties * 100;
    }
}

sealed public class ComparisonResult
{
    public string ItemType { get; set; } = string.Empty;
    public List<PropertyChange> Changes { get; set; } = new();
}

sealed public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string ModifiedValue { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
}

sealed public class SummaryOfChanges
{
    public int TotalChanges { get; set; }
    public List<string> ChangedFields { get; set; } = new();
    public double ChangePercentage { get; set; }
}
