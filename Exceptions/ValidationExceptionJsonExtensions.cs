#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetConfigServer.Exceptions;

public static class ValidationExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes a <see cref="ValidationException"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The exception to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this ValidationException value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="ValidationException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="ValidationException"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static ValidationException? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ValidationException>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="ValidationException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The resulting <see cref="ValidationException"/> instance if deserialization succeeds; otherwise, null.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    public static bool TryFromJson(string json, out ValidationException? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}