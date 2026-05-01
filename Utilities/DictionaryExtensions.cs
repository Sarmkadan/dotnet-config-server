#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections;

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
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue? defaultValue = default)
        where TKey : notnull
    {
        return source.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Adds a key-value pair only if the key doesn't already exist.
    /// </summary>
    public static bool AddIfNotExists<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue value)
        where TKey : notnull
    {
        if (source.ContainsKey(key))
            return false;

        source.Add(key, value);
        return true;
    }

    /// <summary>
    /// Updates a value in the dictionary, or adds it if it doesn't exist.
    /// </summary>
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key, TValue value)
        where TKey : notnull
    {
        source[key] = value;
    }

    /// <summary>
    /// Removes a key-value pair if the predicate is true.
    /// </summary>
    public static int RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        where TKey : notnull
    {
        var keysToRemove = source.Where(predicate).Select(kvp => kvp.Key).ToList();
        return keysToRemove.Count(key => source.Remove(key));
    }

    /// <summary>
    /// Merges another dictionary into the current one.
    /// </summary>
    public static void Merge<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> other, bool overwrite = true)
        where TKey : notnull
    {
        foreach (var kvp in other)
        {
            if (overwrite || !source.ContainsKey(kvp.Key))
                source[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Creates a new dictionary with inverted keys and values.
    /// </summary>
    public static Dictionary<TValue, TKey> Invert<TKey, TValue>(this Dictionary<TKey, TValue> source)
        where TKey : notnull
        where TValue : notnull
    {
        return source.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }

    /// <summary>
    /// Filters the dictionary based on a predicate.
    /// </summary>
    public static Dictionary<TKey, TValue> Where<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        where TKey : notnull
    {
        return source.Where(predicate).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Transforms the dictionary values using a selector.
    /// </summary>
    public static Dictionary<TKey, TResult> Select<TKey, TValue, TResult>(this Dictionary<TKey, TValue> source, Func<TValue, TResult> selector)
        where TKey : notnull
    {
        return source.ToDictionary(kvp => kvp.Key, kvp => selector(kvp.Value));
    }

    /// <summary>
    /// Converts a dictionary to a flattened structure for nested keys.
    /// </summary>
    public static Dictionary<string, object?> Flatten<TKey, TValue>(
        this Dictionary<TKey, TValue> source,
        string prefix = "",
        string separator = ".")
        where TKey : notnull
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in source)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key.ToString() ?? "" : $"{prefix}{separator}{kvp.Key}";

            if (kvp.Value is Dictionary<string, object?> nestedDict)
            {
                var flattened = nestedDict.Flatten(key, separator);
                foreach (var item in flattened)
                    result[item.Key] = item.Value;
            }
            else if (kvp.Value is IDictionary dict)
            {
                var nestedDict2 = dict.Cast<DictionaryEntry>()
                    .ToDictionary(de => de.Key.ToString() ?? "", de => de.Value);
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
    public static object? GetNestedValue<TKey>(this Dictionary<TKey, object?> source, string path, string separator = ".")
        where TKey : notnull
    {
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
    public static void SetNestedValue<TKey>(this Dictionary<TKey, object?> source, string path, object? value, string separator = ".")
        where TKey : notnull
    {
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
