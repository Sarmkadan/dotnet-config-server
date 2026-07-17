using System;
using System.Text.Json;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="EncryptionBenchmarks"/>.
/// </summary>
public static class EncryptionBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an <see cref="EncryptionBenchmarks"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The <see cref="EncryptionBenchmarks"/> instance to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation; otherwise it will be compact.</param>
    /// <returns>A JSON string representing the provided <see cref="EncryptionBenchmarks"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this EncryptionBenchmarks value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return indented switch
        {
            true => JsonSerializer.Serialize(value, _jsonSerializerOptions),
            false => JsonSerializer.Serialize(value)
        };
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="EncryptionBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>
    /// An <see cref="EncryptionBenchmarks"/> instance if deserialization succeeds; otherwise <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    public static EncryptionBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonSerializer.Deserialize<EncryptionBenchmarks>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into an <see cref="EncryptionBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="EncryptionBenchmarks"/> instance if the conversion succeeded, or <c>null</c> if it failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the JSON string was successfully deserialized; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    public static bool TryFromJson(string json, out EncryptionBenchmarks? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<EncryptionBenchmarks>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}