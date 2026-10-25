#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Small LINQ-style helpers for <see cref="IEnumerable{T}"/> that complement <see cref="CollectionExtensions"/>.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Filters out null references from a sequence.
    /// </summary>
    /// <param name="source">The source sequence that may contain null elements.</param>
    /// <returns>A sequence containing only the non-null elements of <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (item is not null)
                yield return item;
        }
    }

    /// <summary>
    /// Returns the sequence itself, or an empty sequence when the source is null.
    /// </summary>
    /// <param name="source">The source sequence, which may be null.</param>
    /// <returns><paramref name="source"/> if it is not null; otherwise, an empty sequence.</returns>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    /// <summary>
    /// Checks whether every element of <paramref name="values"/> is present in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The collection to search.</param>
    /// <param name="values">The elements that must all be present.</param>
    /// <returns>True if every element of <paramref name="values"/> is contained in <paramref name="source"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="values"/> is null.</exception>
    public static bool ContainsAll<T>(this IEnumerable<T> source, IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(values);

        var lookup = new HashSet<T>(source);
        return values.All(lookup.Contains);
    }
}
