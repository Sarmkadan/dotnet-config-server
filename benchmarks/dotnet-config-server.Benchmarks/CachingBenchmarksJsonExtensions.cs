using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetConfigServer.Benchmarks;

public static class CachingBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson(this CachingBenchmarks value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return indented
            ? JsonSerializer.Serialize(value, _jsonSerializerOptions)
            : JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }

    public static CachingBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonSerializer.Deserialize<CachingBenchmarks>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryFromJson(string json, out CachingBenchmarks? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<CachingBenchmarks>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
