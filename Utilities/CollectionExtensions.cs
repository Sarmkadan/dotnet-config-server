#nullable enable
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
    /// <param name="source">The source collection to batch.</param>
    /// <param name="batchSize">The size of each batch. Must be greater than zero.</param>
    /// <returns>An enumerable of batches, each containing up to <paramref name="batchSize"/> elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="batchSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        ArgumentNullException.ThrowIfNull(source);

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
    /// <param name="source">The source collection.</param>
    /// <param name="action">The action to execute for each element.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Executes an action on each element with its index.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <param name="action">The action to execute for each element, receiving the element and its index.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="action"/> is null.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);

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
    /// <param name="source">The collection to check.</param>
    /// <returns>True if the collection is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>
    /// Checks if a collection has exactly one element.
    /// </summary>
    /// <param name="source">The collection to check.</param>
    /// <returns>True if the collection has exactly one element; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static bool IsSingle<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() && !enumerator.MoveNext();
    }

    /// <summary>
    /// Checks if a collection has multiple elements.
    /// </summary>
    /// <param name="source">The collection to check.</param>
    /// <returns>True if the collection has two or more elements; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static bool HasMultiple<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() && enumerator.MoveNext();
    }

    /// <summary>
    /// Gets all elements except the last one.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <returns>An enumerable containing all elements except the last one.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
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
    /// <param name="source">The source collection.</param>
    /// <param name="keySelector">A function to extract the key for determining uniqueness.</param>
    /// <returns>An enumerable of distinct elements based on the key selector.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

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
    /// <param name="source">The source collection.</param>
    /// <param name="keySelector">A function to extract the grouping key from each element.</param>
    /// <returns>An enumerable of groups containing consecutive elements with the same key.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="keySelector"/> is null.</exception>
    public static IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            yield break;

        var currentKey = keySelector(enumerator.Current);
        var group = new List<T> { enumerator.Current };

        while (enumerator.MoveNext())
        {
            var key = keySelector(enumerator.Current);

            if (!EqualityComparer<TKey>.Default.Equals(key, currentKey))
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
    /// <param name="source">The source collection to shuffle.</param>
    /// <param name="random">Optional random number generator. If null, a new instance will be created.</param>
    /// <returns>A new list containing the shuffled elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(source);

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
    /// <param name="first">The first collection.</param>
    /// <param name="second">The second collection.</param>
    /// <returns>An enumerable of tuples pairing elements from both collections.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="first"/> or <paramref name="second"/> is null.</exception>
    public static IEnumerable<(T1, T2)> ZipWith<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Zip(second, (a, b) => (a, b));
    }

    /// <summary>
    /// Converts the collection to a dictionary, handling duplicate keys.
    /// </summary>
    /// <param name="source">The source collection of key-value pairs.</param>
    /// <param name="comparer">Optional equality comparer for keys.</param>
    /// <returns>A dictionary containing all key-value pairs from the source.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return new Dictionary<TKey, TValue>(source, comparer);
    }

    /// <summary>
    /// Gets the first element or returns a default value without throwing.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <param name="defaultValue">The default value to return if the collection is empty.</param>
    /// <returns>The first element if the collection is not empty; otherwise, <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
    public static T? FirstOrDefault<T>(this IEnumerable<T> source, T? defaultValue)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
            return item;

        return defaultValue;
    }
}

/// <summary>
/// Helper class for consecutive grouping.
/// </summary>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="T">The type of elements in the group.</typeparam>
public sealed class ConsecutiveGroup<TKey, T> : IGrouping<TKey, T>
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