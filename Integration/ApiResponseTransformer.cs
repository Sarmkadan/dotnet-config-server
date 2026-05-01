#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetConfigServer.Integration;

/// <summary>
/// Transforms API responses from various formats into a standard format.
/// Handles response mapping, field transformation, and error handling.
/// </summary>
public interface IApiResponseTransformer
{
    /// <summary>
    /// Transforms a JSON response into a typed object.
    /// </summary>
    T Transform<T>(string json) where T : notnull;

    /// <summary>
    /// Transforms a JSON response and maps fields according to a mapping.
    /// </summary>
    T TransformWithMapping<T>(string json, Dictionary<string, string> fieldMapping) where T : notnull, new();

    /// <summary>
    /// Extracts specific fields from a JSON response.
    /// </summary>
    Dictionary<string, object?> ExtractFields(string json, params string[] fields);

    /// <summary>
    /// Flattens nested JSON structure.
    /// </summary>
    Dictionary<string, object?> Flatten(string json, string separator = ".");
}

sealed public class ApiResponseTransformer : IApiResponseTransformer
{
    private readonly ILogger<ApiResponseTransformer> _logger;
    private readonly JsonSerializerOptions _options;

    public ApiResponseTransformer(ILogger<ApiResponseTransformer> logger)
    {
        _logger = logger;
        _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public T Transform<T>(string json) where T : notnull
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, _options);
            return result ?? throw new InvalidOperationException("Deserialization resulted in null");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error transforming JSON response");
            throw;
        }
    }

    public T TransformWithMapping<T>(string json, Dictionary<string, string> fieldMapping) where T : notnull, new()
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var result = new T();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                if (fieldMapping.TryGetValue(property.Name, out var jsonPath))
                {
                    var value = ExtractValue(doc.RootElement, jsonPath);
                    if (value is not null)
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(result, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not convert value for property {Property}", property.Name);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming JSON with field mapping");
            throw;
        }
    }

    public Dictionary<string, object?> ExtractFields(string json, params string[] fields)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, object?>();

            foreach (var field in fields)
            {
                var value = ExtractValue(doc.RootElement, field);
                result[field] = value;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting fields from JSON");
            throw;
        }
    }

    public Dictionary<string, object?> Flatten(string json, string separator = ".")
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, object?>();
            FlattenElement(doc.RootElement, string.Empty, separator, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flattening JSON");
            throw;
        }
    }

    private object? ExtractValue(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
            {
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetDecimal(),
            JsonValueKind.True or JsonValueKind.False => current.GetBoolean(),
            JsonValueKind.Null => null,
            _ => current.GetRawText()
        };
    }

    private void FlattenElement(JsonElement element, string prefix, string separator, Dictionary<string, object?> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}{separator}{property.Name}";
                FlattenElement(property.Value, key, separator, result);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var key = $"{prefix}[{index}]";
                FlattenElement(item, key, separator, result);
                index++;
            }
        }
        else
        {
            result[prefix] = element.ValueKind == JsonValueKind.Null ? null : element.GetRawText();
        }
    }
}
