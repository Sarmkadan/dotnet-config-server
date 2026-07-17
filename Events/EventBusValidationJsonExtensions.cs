#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;

namespace DotnetConfigServer.Events;

/// <summary>
/// JSON serialization helpers for <see cref="EventBus"/>.
/// </summary>
public static class EventBusValidationJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Converts an <see cref="EventBus"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="EventBus"/> instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the <see cref="EventBus"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this EventBus value, bool indented = false) =>
        JsonSerializer.Serialize(value, indented ? _options : new JsonSerializerOptions(_options) { WriteIndented = false });

    /// <summary>
    /// Deserializes a JSON string to an <see cref="EventBus"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="EventBus"/> instance; or null if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
    public static EventBus? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<EventBus>(json, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="EventBus"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized <see cref="EventBus"/> instance; or null if deserialization fails.</param>
    /// <returns>True if deserialization is successful; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
    public static bool TryFromJson(string json, out EventBus? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<EventBus>(json, _options);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}