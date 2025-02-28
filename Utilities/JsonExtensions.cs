// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Nodes;

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Extension methods for JSON operations and conversions.
/// Provides utilities for JSON serialization, deserialization, and merging.
/// </summary>
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
    public static string ToJson<T>(this T obj, bool pretty = false)
    {
        var options = pretty ? PrettyOptions : DefaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserializes a JSON string to an object.
    /// </summary>
    public static T? FromJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Safely deserializes a JSON string, returning null if parsing fails.
    /// </summary>
    public static T? TryFromJson<T>(this string json)
    {
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
    public static JsonObject Merge(this JsonObject target, JsonObject source)
    {
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
    public static JsonObject ToJsonObject(this Dictionary<string, object?> dict)
    {
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
    public static JsonNode? GetValueByPath(this JsonObject json, string path)
    {
        var parts = path.Split('.');
        JsonNode? current = json;

        foreach (var part in parts)
        {
            if (current is JsonObject obj && obj.ContainsKey(part))
                current = obj[part];
            else
                return null;
        }

        return current;
    }

    /// <summary>
    /// Checks if a JSON string is valid.
    /// </summary>
    public static bool IsValidJson(this string json)
    {
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
    public static string PrettyPrint(this string json)
    {
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
    public static string Minify(this string json)
    {
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
    public static Dictionary<string, object?>? ToJsonDictionary(this string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, DefaultOptions);
    }

    /// <summary>
    /// Extracts specified paths from a JSON object.
    /// </summary>
    public static Dictionary<string, object?> Extract(this JsonObject json, params string[] paths)
    {
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
    public static JsonObject RemoveNulls(this JsonObject json)
    {
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
            json.Remove(key);

        return json;
    }
}
