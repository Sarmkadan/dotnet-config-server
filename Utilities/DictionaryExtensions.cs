#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for dictionary operations.
/// Provides utilities for safe dictionary access, merging, and transformation.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Gets a value from the dictionary, returning a default if the key doesn't exist.
    /// </summary>
    /// <param name="source">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <returns>The value associated with the specified key, or <paramref name="defaultValue"/> if the key is not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue? defaultValue = default)
    where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        return source.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Adds a key-value pair only if the key doesn't already exist.
    /// </summary>
    /// <param name="source">The dictionary to modify.</param>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <returns><see langword="true"/> if the key was added; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key already exists in the dictionary.</exception>
    public static bool AddIfNotExists<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue value)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        if (source.ContainsKey(key))
            return false;

        source.Add(key, value);
        return true;
    }

    /// <summary>
    /// Updates a value in the dictionary, or adds it if it doesn't exist.
    /// </summary>
    /// <param name="source">The dictionary to modify.</param>
    /// <param name="key">The key to update or add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue value)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        source[key] = value;
    }

    /// <summary>
    /// Removes key-value pairs that match the specified predicate.
    /// </summary>
    /// <param name="source">The dictionary to modify.</param>
    /// <param name="predicate">The predicate used to determine which items to remove.</param>
    /// <returns>The number of items removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static int RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var keysToRemove = System.Linq.Enumerable.Where(source, predicate).Select(kvp => kvp.Key).ToList();
        return keysToRemove.Count(key => source.Remove(key));
    }

    /// <summary>
    /// Merges another dictionary into the current one.
    /// </summary>
    /// <param name="source">The target dictionary to merge into.</param>
    /// <param name="other">The dictionary to merge from.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite existing values; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> other, bool overwrite = true)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(other);

        foreach (var kvp in other)
        {
            if (overwrite || !source.ContainsKey(kvp.Key))
                source[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Creates a new dictionary with inverted keys and values.
    /// </summary>
    /// <param name="source">The dictionary to invert.</param>
    /// <returns>A new dictionary with keys and values swapped.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A value in the dictionary is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A duplicate value exists in the dictionary.</exception>
    public static Dictionary<TValue, TKey> Invert<TKey, TValue>(this Dictionary<TKey, TValue> source)
        where TKey : notnull
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        try
        {
            return source.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Cannot invert dictionary: duplicate values found.", ex);
        }
    }

    /// <summary>
    /// Filters the dictionary based on a predicate.
    /// </summary>
    /// <param name="source">The dictionary to filter.</param>
    /// <param name="predicate">The predicate to apply.</param>
    /// <returns>A new dictionary containing only the items that match the predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static Dictionary<TKey, TValue> Where<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return System.Linq.Enumerable.Where(source, predicate).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Transforms the dictionary values using a selector.
    /// </summary>
    /// <param name="source">The dictionary to transform.</param>
    /// <param name="selector">The transformation function.</param>
    /// <returns>A new dictionary with transformed values.</returns>
    /// <typeparam name="TResult">The type of the transformed values.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Dictionary<TKey, TResult> Select<TKey, TValue, TResult>(this Dictionary<TKey, TValue> source, Func<TValue, TResult> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return source.ToDictionary(kvp => kvp.Key, kvp => selector(kvp.Value));
    }

    /// <summary>
    /// Converts a dictionary to a flattened structure for nested keys.
    /// </summary>
    /// <param name="source">The dictionary to flatten.</param>
    /// <param name="prefix">The prefix for flattened keys.</param>
    /// <param name="separator">The separator to use between key parts.</param>
    /// <returns>A flattened dictionary with dot-separated keys.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="separator"/> is <see langword="null"/>.</exception>
    public static Dictionary<string, object?> Flatten<TKey, TValue>(
        this Dictionary<TKey, TValue> source,
        string prefix = "",
        string separator = ".")
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(separator);

        var result = new Dictionary<string, object?>();

        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix)
                ? kvp.Key.ToString() ?? ""
                : $"{prefix}{separator}{kvp.Key}";

            if (kvp.Value is Dictionary<string, object?> nestedDict)
            {
                var flattened = nestedDict.Flatten(key, separator);
                foreach (var item in flattened)
                    result[item.Key] = item.Value;
            }
            else if (kvp.Value is IDictionary dict)
            {
                var nestedDict2 = dict.Cast<DictionaryEntry>()
                    .ToDictionary(de => de.Key?.ToString() ?? "", de => de.Value);
                var flattened = nestedDict2.Flatten(key, separator);
                foreach (var item in flattened)
                    result[item.Key] = item.Value;
            }
            else
            {
                result[key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Safely gets a nested value using dot notation (e.g., "user.profile.name").
    /// </summary>
    /// <param name="source">The dictionary to search.</param>
    /// <param name="path">The dot-separated path to the nested value.</param>
    /// <param name="separator">The separator to use between path segments.</param>
    /// <returns>The nested value, or <see langword="null"/> if the path is invalid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="separator"/> is <see langword="null"/>.</exception>
    public static object? GetNestedValue<TKey>(this Dictionary<TKey, object?> source, string path, string separator = ".")
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(separator);

        var parts = path.Split(new[] { separator }, StringSplitOptions.None);
        object? current = source;

        foreach (var part in parts)
        {
            if (current is Dictionary<TKey, object?> dict && dict.TryGetValue((TKey)(object)part, out var value))
            {
                current = value;
            }
            else if (current is IDictionary<string, object?> stringDict && stringDict.TryGetValue(part, out var value2))
            {
                current = value2;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Sets a nested value using dot notation.
    /// </summary>
    /// <param name="source">The dictionary to modify.</param>
    /// <param name="path">The dot-separated path to the nested value.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="separator">The separator to use between path segments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="separator"/> is <see langword="null"/>.</exception>
    public static void SetNestedValue<TKey>(this Dictionary<TKey, object?> source, string path, object? value, string separator = ".")
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(separator);

        var parts = path.Split(new[] { separator }, StringSplitOptions.None);

        if (parts.Length == 0)
            return;

        var current = source;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var key = (TKey)(object)parts[i];

            if (!current.ContainsKey(key))
                current[key] = new Dictionary<TKey, object?>();

            if (current[key] is Dictionary<TKey, object?> dict)
                current = dict;
            else
                return;
        }

        current[(TKey)(object)parts[parts.Length - 1]] = value;
    }
}