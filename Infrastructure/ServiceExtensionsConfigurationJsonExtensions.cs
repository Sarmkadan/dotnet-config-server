#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Provides System.Text.Json serialization helpers for ServiceExtensionsConfiguration
/// </summary>
public static class ServiceExtensionsConfigurationJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the ServiceExtensionsConfiguration to a JSON string
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the ServiceExtensionsConfiguration</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this ServiceExtensionsConfiguration value, bool indented = false) =>
        ToJson(value, indented, _jsonOptions);

    /// <summary>
    /// Deserializes a JSON string to a ServiceExtensionsConfiguration instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized ServiceExtensionsConfiguration instance, or null if JSON is empty or invalid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static ServiceExtensionsConfiguration? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<ServiceExtensionsConfiguration>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a ServiceExtensionsConfiguration instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">The deserialized ServiceExtensionsConfiguration instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static bool TryFromJson(string json, out ServiceExtensionsConfiguration? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ServiceExtensionsConfiguration>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ToJson(ServiceExtensionsConfiguration value, bool indented, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        var localOptions = new JsonSerializerOptions(options)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(value, localOptions);
    }
}