#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;

namespace DotnetConfigServer.Services;

/// <summary>
/// Canonical, single-source-of-truth key/value diffing engine. Both configuration-version
/// diffing (<see cref="IDiffService"/>) and object-level comparison (<see cref="IComparisonService"/>)
/// delegate their actual change-detection logic here, so the two public entry points can never
/// disagree on what "added", "removed" or "changed" means.
/// </summary>
/// <remarks>
/// Canonical semantics:
/// <list type="bullet">
/// <item>A key present only in <c>updated</c> is <see cref="ChangeType.Added"/>.</item>
/// <item>A key present only in <c>baseline</c> is <see cref="ChangeType.Deleted"/>.</item>
/// <item>A key present in both is <see cref="ChangeType.Modified"/> when its normalized value
/// differs; keys whose normalized values are equal are omitted from the result entirely.</item>
/// <item>A <b>missing</b> key and a key that is present with a <c>null</c> value are different
/// concepts: a missing key always yields Added/Deleted, while a present key with a <c>null</c>
/// value is compared like any other value (a <c>null</c> value normalizes to an empty string,
/// so a present-but-null value is equal to a present-but-empty-string value).</item>
/// <item>Key comparison is ordinal; case sensitivity is controlled explicitly by
/// <see cref="ConfigDiffOptions.CaseSensitiveKeys"/> (default <c>true</c>) instead of being an
/// implicit, implementation-specific choice.</item>
/// <item>Values are compared as opaque strings. No structural/array-aware comparison is
/// performed here - a caller that needs order-insensitive array comparison must flatten arrays
/// into individually keyed entries (e.g. <c>"Servers[0]"</c>, <c>"Servers[1]"</c>) before calling
/// <see cref="Diff"/>.</item>
/// </list>
/// </remarks>
public interface IConfigDiffer
{
    /// <summary>
    /// Computes the set of key-level changes between <paramref name="baseline"/> and <paramref name="updated"/>.
    /// </summary>
    /// <param name="baseline">The "before" set of key/value pairs.</param>
    /// <param name="updated">The "after" set of key/value pairs.</param>
    /// <param name="options">Comparison options; when <c>null</c>, <see cref="ConfigDiffOptions.Default"/> is used.</param>
    /// <returns>
    /// A list of <see cref="ConfigKeyChange"/> entries ordered as: additions (in <paramref name="updated"/>
    /// enumeration order), then modifications (in <paramref name="baseline"/> enumeration order), then
    /// deletions (in <paramref name="baseline"/> enumeration order).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseline"/> or <paramref name="updated"/> is <c>null</c>.</exception>
    IReadOnlyList<ConfigKeyChange> Diff(
        IReadOnlyDictionary<string, string?> baseline,
        IReadOnlyDictionary<string, string?> updated,
        ConfigDiffOptions? options = null);
}

/// <summary>
/// Options controlling <see cref="IConfigDiffer.Diff"/> semantics.
/// </summary>
/// <param name="IgnoreWhitespaceAndBlankLines">
/// When <c>true</c>, values are trimmed and a value consisting solely of whitespace (or <c>null</c>)
/// is treated as an empty value for comparison purposes.
/// </param>
/// <param name="CaseSensitiveKeys">
/// When <c>true</c> (default), key lookup uses ordinal, case-sensitive comparison. When <c>false</c>,
/// keys are matched case-insensitively (ordinal, ignoring case).
/// </param>
public sealed record ConfigDiffOptions(bool IgnoreWhitespaceAndBlankLines = false, bool CaseSensitiveKeys = true)
{
    /// <summary>
    /// The canonical default: exact, case-sensitive comparison with no whitespace normalization.
    /// </summary>
    public static ConfigDiffOptions Default { get; } = new();
}

/// <summary>
/// A single key-level difference produced by <see cref="IConfigDiffer"/>.
/// </summary>
/// <param name="Key">The configuration key, exactly as it appeared in whichever side introduced or retained it.</param>
/// <param name="ChangeType">The kind of change.</param>
/// <param name="OldValue">The value in the baseline set, or <c>null</c> when <see cref="ChangeType"/> is <see cref="ChangeType.Added"/>.</param>
/// <param name="NewValue">The value in the updated set, or <c>null</c> when <see cref="ChangeType"/> is <see cref="ChangeType.Deleted"/>.</param>
public sealed record ConfigKeyChange(string Key, ChangeType ChangeType, string? OldValue, string? NewValue);

/// <summary>
/// Default implementation of <see cref="IConfigDiffer"/>. Stateless and thread-safe.
/// </summary>
public sealed class KeyValueConfigDiffer : IConfigDiffer
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseline"/> or <paramref name="updated"/> is <c>null</c>.</exception>
    public IReadOnlyList<ConfigKeyChange> Diff(
        IReadOnlyDictionary<string, string?> baseline,
        IReadOnlyDictionary<string, string?> updated,
        ConfigDiffOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(updated);

        var effectiveOptions = options ?? ConfigDiffOptions.Default;
        var comparer = effectiveOptions.CaseSensitiveKeys ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        var baselineLookup = new Dictionary<string, string?>(baseline, comparer);
        var updatedLookup = new Dictionary<string, string?>(updated, comparer);

        var changes = new List<ConfigKeyChange>();

        foreach (var (key, newValue) in updated)
        {
            if (!baselineLookup.TryGetValue(key, out var oldValue))
            {
                changes.Add(new ConfigKeyChange(key, ChangeType.Added, null, newValue));
            }
        }

        foreach (var (key, oldValue) in baseline)
        {
            if (updatedLookup.TryGetValue(key, out var newValue) && ValuesDiffer(oldValue, newValue, effectiveOptions.IgnoreWhitespaceAndBlankLines))
            {
                changes.Add(new ConfigKeyChange(key, ChangeType.Modified, oldValue, newValue));
            }
        }

        foreach (var (key, oldValue) in baseline)
        {
            if (!updatedLookup.ContainsKey(key))
            {
                changes.Add(new ConfigKeyChange(key, ChangeType.Deleted, oldValue, null));
            }
        }

        return changes;
    }

    /// <summary>
    /// Determines whether two configuration values differ, taking the optional
    /// whitespace-ignoring behaviour into account.
    /// </summary>
    private static bool ValuesDiffer(string? a, string? b, bool ignoreWhitespaceAndBlankLines) =>
        ignoreWhitespaceAndBlankLines ? NormalizeValue(a) != NormalizeValue(b) : a != b;

    /// <summary>
    /// Normalises a configuration value for whitespace-ignoring comparison. Converts a value
    /// that is <c>null</c>, empty, or consists only of whitespace to an empty string, and trims
    /// leading/trailing whitespace otherwise.
    /// </summary>
    private static string NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
