#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for JSON operations and conversions.
/// Provides utilities for JSON serialization, deserialization, and merging.
/// </summary>
/// <remarks>
/// All methods are thread-safe as they use immutable <see cref="JsonSerializerOptions"/> instances.
/// </remarks>
public static class JsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="pretty">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
    public static string ToJson<T>(this T obj, bool pretty = false)
    {
        ArgumentNullException.ThrowIfNull(obj);
        var options = pretty ? PrettyOptions : DefaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static T? FromJson<T>(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Safely deserializes a JSON string, returning null if parsing fails.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize into.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static T? TryFromJson<T>(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Merges two JSON objects. The second object overwrites the first.
    /// </summary>
    /// <param name="target">The target JSON object to merge into.</param>
    /// <param name="source">The source JSON object to merge from.</param>
    /// <returns>The merged target JSON object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="target"/> or <paramref name="source"/> is null.</exception>
    public static JsonObject Merge(this JsonObject target, JsonObject source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var property in source)
        {
            if (target.ContainsKey(property.Key))
            {
                if (property.Value is JsonObject sourceObj && target[property.Key] is JsonObject targetObj)
                {
                    targetObj.Merge(sourceObj);
                }
                else
                {
                    target[property.Key] = property.Value;
                }
            }
            else
            {
                target.Add(property.Key, property.Value);
            }
        }

        return target;
    }

    /// <summary>
    /// Converts a dictionary to a JSON object.
    /// </summary>
    /// <param name="dict">The dictionary to convert.</param>
    /// <returns>A new <see cref="JsonObject"/> containing the dictionary values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dict"/> is null.</exception>
    public static JsonObject ToJsonObject(this Dictionary<string, object?> dict)
    {
        ArgumentNullException.ThrowIfNull(dict);
        var json = new JsonObject();

        foreach (var kvp in dict)
        {
            json[kvp.Key] = kvp.Value switch
            {
                null => JsonValue.Create((string?)null),
                string s => JsonValue.Create(s),
                int i => JsonValue.Create(i),
                long l => JsonValue.Create(l),
                double d => JsonValue.Create(d),
                bool b => JsonValue.Create(b),
                DateTime dt => JsonValue.Create(dt),
                _ => JsonValue.Create(kvp.Value.ToString())
            };
        }

        return json;
    }

    /// <summary>
    /// Gets a value from a JSON object using dot notation.
    /// </summary>
    /// <param name="json">The JSON object to search.</param>
    /// <param name="path">The dot-separated path to the value (e.g., "parent.child.grandchild").</param>
    /// <returns>The found <see cref="JsonNode"/>, or null if the path doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="json"/> or <paramref name="path"/> is null.</exception>
    public static JsonNode? GetValueByPath(this JsonObject json, string path)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(path);

        var parts = path.Split('.');
        JsonNode? current = json;

        foreach (var part in parts)
        {
            if (current is JsonObject obj && obj.ContainsKey(part))
            {
                current = obj[part];
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Checks if a JSON string is valid.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>True if the JSON is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static bool IsValidJson(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            _ = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Pretty prints a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to format.</param>
    /// <returns>The pretty-printed JSON string, or the original string if formatting fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static string PrettyPrint(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var document = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(document.RootElement, options);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Minifies a JSON string by removing whitespace.
    /// </summary>
    /// <param name="json">The JSON string to minify.</param>
    /// <returns>The minified JSON string, or the original string if minification fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static string Minify(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var document = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions { WriteIndented = false };
            return JsonSerializer.Serialize(document.RootElement, options);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Converts a JSON string to a dictionary.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A dictionary containing the JSON data, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static Dictionary<string, object?>? ToJsonDictionary(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, DefaultOptions);
    }

    /// <summary>
    /// Extracts specified paths from a JSON object.
    /// </summary>
    /// <param name="json">The JSON object to extract values from.</param>
    /// <param name="paths">The dot-separated paths to extract.</param>
    /// <returns>A dictionary mapping paths to their values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="json"/> or <paramref name="paths"/> is null.</exception>
    public static Dictionary<string, object?> Extract(this JsonObject json, params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(paths);

        var result = new Dictionary<string, object?>();

        foreach (var path in paths)
        {
            var value = json.GetValueByPath(path);
            result[path] = value?.GetValue<object?>();
        }

        return result;
    }

    /// <summary>
    /// Removes null values from a JSON object recursively.
    /// </summary>
    /// <param name="json">The JSON object to clean.</param>
    /// <returns>The cleaned JSON object with null values removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static JsonObject RemoveNulls(this JsonObject json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var keysToRemove = new List<string>();

        foreach (var property in json)
        {
            if (property.Value is null)
            {
                keysToRemove.Add(property.Key);
            }
            else if (property.Value is JsonObject nestedObj)
            {
                nestedObj.RemoveNulls();
            }
        }

        foreach (var key in keysToRemove)
        {
            json.Remove(key);
        }

        return json;
    }
}