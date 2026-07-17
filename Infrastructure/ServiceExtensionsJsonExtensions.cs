#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Represents the configuration of service extensions for serialization purposes
/// </summary>
public sealed class ServiceExtensionsConfiguration
{
    /// <summary>
    /// List of registered data services
    /// </summary>
    public string[]? DataServices { get; set; }

    /// <summary>
    /// List of registered business services
    /// </summary>
    public string[]? BusinessServices { get; set; }

    /// <summary>
    /// List of registered webhook clients
    /// </summary>
    public string[]? WebhookClient { get; set; }

    /// <summary>
    /// List of registered Swagger configurations
    /// </summary>
    public string[]? SwaggerConfiguration { get; set; }

    /// <summary>
    /// List of registered database initialization methods
    /// </summary>
    public string[]? DatabaseInitialization { get; set; }
}

/// <summary>
/// Provides System.Text.Json serialization helpers for ServiceExtensions
/// </summary>
public static class ServiceExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the ServiceExtensions configuration to a JSON string
    /// </summary>
    /// <param name="value">The ServiceExtensions configuration to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the ServiceExtensions configuration</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this ServiceExtensionsConfiguration value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a ServiceExtensionsConfiguration instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized ServiceExtensionsConfiguration instance, or null if JSON is empty or invalid</returns>
    public static ServiceExtensionsConfiguration? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ServiceExtensionsConfiguration>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a ServiceExtensionsConfiguration instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">The deserialized ServiceExtensionsConfiguration instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    public static bool TryFromJson(string json, out ServiceExtensionsConfiguration? value)
    {
        if (string.IsNullOrEmpty(json))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ServiceExtensionsConfiguration>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}