#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Formatters;

/// <summary>
/// Exports configurations in multiple formats: JSON, CSV, XML, YAML.
/// Handles serialization with proper escaping and structure preservation.
/// </summary>
public sealed class ConfigurationExporter
{
    /// <summary>
    /// Streams configurations as a JSON array directly onto the destination stream using a
    /// <see cref="Utf8JsonWriter"/>, without ever materializing the full payload in memory.
    /// Intended for large exports (thousands of configurations) where buffering the whole
    /// response as a string would spike Large Object Heap allocations.
    /// </summary>
    /// <param name="destination">Stream to write the JSON array to, typically the HTTP response body.</param>
    /// <param name="configurations">Asynchronous source of configurations to serialize, pulled one at a time.</param>
    /// <param name="pretty">Whether to indent the output.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and writing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> or <paramref name="configurations"/> is null.</exception>
    public static async Task WriteAsJsonAsync(
        Stream destination,
        IAsyncEnumerable<Configuration> configurations,
        bool pretty = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(configurations);

        await using var writer = new Utf8JsonWriter(destination, new JsonWriterOptions { Indented = pretty });

        writer.WriteStartArray();

        await foreach (var config in configurations.WithCancellation(cancellationToken))
        {
            writer.WriteStartObject();
            writer.WriteString("id", config.Id);
            writer.WriteString("applicationId", config.ApplicationId);
            writer.WriteString("name", config.Name);
            writer.WriteString("description", config.Description);
            writer.WriteString("environment", config.Environment.ToString());
            writer.WriteBoolean("isActive", config.IsActive);
            writer.WriteBoolean("isEncrypted", config.IsEncrypted);
            writer.WriteString("createdAt", config.CreatedAt);
            writer.WriteString("updatedAt", config.UpdatedAt);
            writer.WriteString("createdBy", config.CreatedBy);
            writer.WriteEndObject();

            // Flush periodically so the writer's internal buffer never grows unbounded
            // and downstream middleware (e.g. response compression) can start sending bytes early.
            await writer.FlushAsync(cancellationToken);
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Streams configuration keys as a JSON array directly onto the destination stream using a
    /// <see cref="Utf8JsonWriter"/>, without buffering the whole payload in memory.
    /// </summary>
    /// <param name="destination">Stream to write the JSON array to, typically the HTTP response body.</param>
    /// <param name="keys">Asynchronous source of configuration keys to serialize, pulled one at a time.</param>
    /// <param name="pretty">Whether to indent the output.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration and writing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> or <paramref name="keys"/> is null.</exception>
    public static async Task WriteKeysAsJsonAsync(
        Stream destination,
        IAsyncEnumerable<ConfigurationKey> keys,
        bool pretty = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(keys);

        await using var writer = new Utf8JsonWriter(destination, new JsonWriterOptions { Indented = pretty });

        writer.WriteStartArray();

        await foreach (var key in keys.WithCancellation(cancellationToken))
        {
            writer.WriteStartObject();
            writer.WriteString("id", key.Id);
            writer.WriteString("key", key.Key);
            writer.WriteString("value", key.Value);
            writer.WriteString("description", key.Description);
            writer.WriteBoolean("isEncrypted", key.IsEncrypted);
            writer.WriteBoolean("isActive", key.IsActive);
            writer.WriteString("createdAt", key.CreatedAt);
            writer.WriteString("updatedAt", key.UpdatedAt);
            writer.WriteEndObject();

            await writer.FlushAsync(cancellationToken);
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Adapts a synchronous, potentially deferred-execution <see cref="IEnumerable{T}"/> source
    /// (e.g. an EF Core <c>IQueryable</c> materialized lazily) into an <see cref="IAsyncEnumerable{T}"/>
    /// suitable for the streaming export methods, yielding control back to the caller between items.
    /// </summary>
    /// <typeparam name="T">Element type of the sequence.</typeparam>
    /// <param name="source">Sequence to adapt.</param>
    /// <param name="cancellationToken">Token used to stop enumeration early.</param>
    /// <returns>An asynchronous stream that yields the same elements as <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }

    /// <summary>
    /// Exports configurations to JSON format.
    /// </summary>
    public static string ExportAsJson(IEnumerable<Configuration> configurations, bool pretty = true)
    {
        var data = configurations.Select(c => new
        {
            c.Id,
            c.ApplicationId,
            c.Name,
            c.Description,
            c.Environment,
            c.IsActive,
            c.IsEncrypted,
            c.CreatedAt,
            c.UpdatedAt,
            c.CreatedBy
        }).ToList();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = pretty
        };

        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Exports configuration keys to JSON format with values.
    /// </summary>
    public static string ExportKeysAsJson(IEnumerable<ConfigurationKey> keys, bool pretty = true)
    {
        var data = keys.Select(k => new
        {
            k.Id,
            k.Key,
            k.Value,
            k.Description,
            k.IsEncrypted,
            k.IsActive,
            k.CreatedAt,
            k.UpdatedAt
        }).ToList();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = pretty
        };

        return JsonSerializer.Serialize(data, options);
    }

    /// <summary>
    /// Exports configurations to CSV format.
    /// </summary>
    public static string ExportAsCsv(IEnumerable<Configuration> configurations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy");

        foreach (var config in configurations)
        {
            var line = new[]
            {
                EscapeCsvValue(config.Id.ToString()),
                EscapeCsvValue(config.ApplicationId.ToString()),
                EscapeCsvValue(config.Name),
                EscapeCsvValue(config.Description ?? string.Empty),
                EscapeCsvValue(config.Environment.ToString()),
                config.IsActive.ToString(),
                config.IsEncrypted.ToString(),
                config.CreatedAt.ToString("O"),
                EscapeCsvValue(config.CreatedBy)
            };

            sb.AppendLine(string.Join(",", line));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports configuration keys to CSV format.
    /// </summary>
    public static string ExportKeysAsCsv(IEnumerable<ConfigurationKey> keys)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ConfigurationId,Key,Value,Description,IsEncrypted,IsActive,CreatedAt");

        foreach (var key in keys)
        {
            var line = new[]
            {
                EscapeCsvValue(key.Id.ToString()),
                EscapeCsvValue(key.ConfigurationId.ToString()),
                EscapeCsvValue(key.Key),
                EscapeCsvValue(key.Value ?? string.Empty),
                EscapeCsvValue(key.Description ?? string.Empty),
                key.IsEncrypted.ToString(),
                key.IsActive.ToString(),
                key.CreatedAt.ToString("O")
            };

            sb.AppendLine(string.Join(",", line));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports configurations to XML format.
    /// </summary>
    public static string ExportAsXml(IEnumerable<Configuration> configurations)
    {
        var root = new XElement("Configurations");

        foreach (var config in configurations)
        {
            var element = new XElement("Configuration",
                new XElement("Id", config.Id),
                new XElement("ApplicationId", config.ApplicationId),
                new XElement("Name", config.Name),
                new XElement("Description", config.Description),
                new XElement("Environment", config.Environment),
                new XElement("IsActive", config.IsActive),
                new XElement("IsEncrypted", config.IsEncrypted),
                new XElement("CreatedAt", config.CreatedAt),
                new XElement("UpdatedAt", config.UpdatedAt),
                new XElement("CreatedBy", config.CreatedBy)
            );

            root.Add(element);
        }

        return root.ToString();
    }

    /// <summary>
    /// Exports configuration keys to XML format.
    /// </summary>
    public static string ExportKeysAsXml(IEnumerable<ConfigurationKey> keys)
    {
        var root = new XElement("ConfigurationKeys");

        foreach (var key in keys)
        {
            var element = new XElement("Key",
                new XElement("Id", key.Id),
                new XElement("ConfigurationId", key.ConfigurationId),
                new XElement("KeyName", key.Key),
                new XElement("Value", key.Value),
                new XElement("Description", key.Description),
                new XElement("IsEncrypted", key.IsEncrypted),
                new XElement("IsActive", key.IsActive),
                new XElement("CreatedAt", key.CreatedAt)
            );

            root.Add(element);
        }

        return root.ToString();
    }

    /// <summary>
    /// Exports configurations as key-value pairs suitable for environment variables.
    /// </summary>
    public static string ExportAsEnvFormat(IEnumerable<ConfigurationKey> keys)
    {
        var sb = new StringBuilder();

        foreach (var key in keys)
        {
            var escapedValue = EscapeEnvValue(key.Value);
            sb.AppendLine($"{key.Key}={escapedValue}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports configurations to YAML format.
    /// </summary>
    public static string ExportAsYaml(IEnumerable<Configuration> configurations)
    {
        var sb = new StringBuilder();

        foreach (var config in configurations)
        {
            sb.AppendLine("- Id: " + EscapeYamlValue(config.Id.ToString()));
            sb.AppendLine("  ApplicationId: " + EscapeYamlValue(config.ApplicationId.ToString()));
            sb.AppendLine("  Name: " + EscapeYamlValue(config.Name));
            sb.AppendLine("  Description: " + EscapeYamlValue(config.Description));
            sb.AppendLine("  Environment: " + EscapeYamlValue(config.Environment.ToString()));
            sb.AppendLine("  IsActive: " + config.IsActive.ToString().ToLower());
            sb.AppendLine("  IsEncrypted: " + config.IsEncrypted.ToString().ToLower());
            sb.AppendLine("  CreatedAt: " + EscapeYamlValue(config.CreatedAt.ToString("O")));
            // UpdatedAt is a non‑nullable DateTime, so we call ToString directly.
            sb.AppendLine("  UpdatedAt: " + EscapeYamlValue(config.UpdatedAt.ToString("O")));
            sb.AppendLine("  CreatedBy: " + EscapeYamlValue(config.CreatedBy));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports configuration keys to YAML format.
    /// </summary>
    public static string ExportKeysAsYaml(IEnumerable<ConfigurationKey> keys)
    {
        var sb = new StringBuilder();

        foreach (var key in keys)
        {
            sb.AppendLine("- Id: " + EscapeYamlValue(key.Id.ToString()));
            sb.AppendLine("  ConfigurationId: " + EscapeYamlValue(key.ConfigurationId.ToString()));
            sb.AppendLine("  Key: " + EscapeYamlValue(key.Key));
            sb.AppendLine("  Value: " + EscapeYamlValue(key.Value));
            sb.AppendLine("  Description: " + EscapeYamlValue(key.Description));
            sb.AppendLine("  IsEncrypted: " + key.IsEncrypted.ToString().ToLower());
            sb.AppendLine("  IsActive: " + key.IsActive.ToString().ToLower());
            sb.AppendLine("  CreatedAt: " + EscapeYamlValue(key.CreatedAt.ToString("O")));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes special characters in CSV values.
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    /// <summary>
    /// Escapes special characters in environment variable values.
    /// </summary>
    private static string EscapeEnvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(" ") || value.Contains("\"") || value.Contains("$"))
        {
            return "\"" + value.Replace("\"", "\\\"").Replace("$", "\\$") + "\"";
        }

        return value;
    }

    /// <summary>
    /// Escapes special characters in YAML values.
    /// </summary>
    private static string EscapeYamlValue(string? value)
    {
        if (value == null)
            return "null";

        // Empty string should be quoted to differentiate from null
        if (value.Length == 0)
            return "\"\"";

        // Characters that require quoting in YAML
        bool needsQuotes = value.Contains(':') ||
                           value.Contains('-') && (value.StartsWith("-") || value.Contains("\n-")) ||
                           value.Contains('#') ||
                           value.Contains('{') ||
                           value.Contains('}') ||
                           value.Contains('[') ||
                           value.Contains(']') ||
                           value.Contains(',') ||
                           value.Contains('&') ||
                           value.Contains('*') ||
                           value.Contains('?') ||
                           value.Contains('|') ||
                           value.Contains('>') ||
                           value.Contains('\'') ||
                           value.Contains('\"') ||
                           value.Contains('\n') ||
                           value.Contains('\r') ||
                           value.StartsWith(' ') ||
                           value.EndsWith(' ') ||
                           value.StartsWith("\"") ||
                           value.StartsWith("'");

        if (needsQuotes)
        {
            // Escape double quotes
            var escaped = value.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
