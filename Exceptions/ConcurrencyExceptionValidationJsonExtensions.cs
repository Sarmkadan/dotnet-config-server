#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="ConcurrencyException"/>.
/// </summary>
public static class ConcurrencyExceptionValidationJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Serializes a <see cref="ConcurrencyException"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The exception instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the exception instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static string ToJson(this ConcurrencyException? value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true,
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ConcurrencyException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="ConcurrencyException"/> instance, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static ConcurrencyException? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<ConcurrencyException>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ConcurrencyException"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or consists only of whitespace.</exception>
    public static bool TryFromJson(string json, out ConcurrencyException? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<ConcurrencyException>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}