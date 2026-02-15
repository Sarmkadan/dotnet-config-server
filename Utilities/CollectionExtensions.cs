// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for collections and enumerable operations.
/// Provides batch processing, partitioning, and collection utilities.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Batches a collection into smaller chunks of specified size.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));

        var batch = new List<T>(batchSize);

        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch.ToList();
                batch.Clear();
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Executes an action on each element in the collection.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Executes an action on each element with its index.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        var index = 0;
        foreach (var item in source)
        {
            action(item, index);
            index++;
        }
    }

    /// <summary>
    /// Checks if a collection is null or empty.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// Checks if a collection has exactly one element.
    /// </summary>
    public static bool IsSingle<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() && !enumerator.MoveNext();
    }

    /// <summary>
    /// Checks if a collection has multiple elements.
    /// </summary>
    public static bool HasMultiple<T>(this IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() && enumerator.MoveNext();
    }

    /// <summary>
    /// Gets all elements except the last one.
    /// </summary>
    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
    {
        var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        var previous = enumerator.Current;

        while (enumerator.MoveNext())
        {
            yield return previous;
            previous = enumerator.Current;
        }
    }

    /// <summary>
    /// Gets all distinct elements based on a selector.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var seen = new HashSet<TKey?>();

        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seen.Add(key))
                yield return item;
        }
    }

    /// <summary>
    /// Groups consecutive elements where the selector returns the same value.
    /// </summary>
    public static IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        var currentKey = keySelector(enumerator.Current);
        var group = new List<T> { enumerator.Current };

        while (enumerator.MoveNext())
        {
            var key = keySelector(enumerator.Current);

            if (!key?.Equals(currentKey) ?? currentKey != null)
            {
                yield return new ConsecutiveGroup<TKey, T>(currentKey, group);
                group = new List<T>();
                currentKey = key;
            }

            group.Add(enumerator.Current);
        }

        yield return new ConsecutiveGroup<TKey, T>(currentKey, group);
    }

    /// <summary>
    /// Shuffles the collection randomly.
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random? random = null)
    {
        random ??= new Random();
        var list = source.ToList();

        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    /// <summary>
    /// Zips two collections together, creating pairs of elements.
    /// </summary>
    public static IEnumerable<(T1, T2)> ZipWith<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second)
    {
        return first.Zip(second, (a, b) => (a, b));
    }

    /// <summary>
    /// Converts the collection to a dictionary, handling duplicate keys.
    /// </summary>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        return new Dictionary<TKey, TValue>(source, comparer);
    }

    /// <summary>
    /// Gets the first element or returns a default value without throwing.
    /// </summary>
    public static T? FirstOrDefault<T>(this IEnumerable<T> source, T? defaultValue)
    {
        foreach (var item in source)
            return item;

        return defaultValue;
    }
}

/// <summary>
/// Helper class for consecutive grouping.
/// </summary>
public class ConsecutiveGroup<TKey, T> : IGrouping<TKey, T>
{
    private readonly List<T> _items;

    public ConsecutiveGroup(TKey? key, List<T> items)
    {
        Key = key;
        _items = items;
    }

    public TKey? Key { get; }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
