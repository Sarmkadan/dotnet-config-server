#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Formatters;

/// <summary>
/// Exports configurations in multiple formats: JSON, CSV, XML, YAML.
/// Handles serialization with proper escaping and structure preservation.
/// </summary>
public sealed class ConfigurationExporter
{
    // ------------------------------------------------------------------------
    // JSON – streaming version (already existed)
    // ------------------------------------------------------------------------
    /// <summary>
    /// Streams configurations as a JSON array directly onto the destination stream using a
    /// <see cref="Utf8JsonWriter"/>, without ever materializing the full payload in memory.
    /// Intended for large exports (thousands of configurations) where buffering the whole
    /// response as a string would spike Large Object Heap allocations.
    /// </summary>
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
            await writer.FlushAsync(cancellationToken);
        }

        writer.WriteEndArray();
        await writer.FlushAsync(cancellationToken);
    }

    // ------------------------------------------------------------------------
    // JSON – keys (streaming)
    // ------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------
    // Helper – adapt IEnumerable to IAsyncEnumerable
    // ------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------
    // CSV – streaming (TextWriter) and string wrapper
    // ------------------------------------------------------------------------
    public static void WriteAsCsv(TextWriter writer, IEnumerable<Configuration> configurations)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(configurations);

        writer.WriteLine("Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy");

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

            writer.WriteLine(string.Join(",", line));
        }
    }

    public static string ExportAsCsv(IEnumerable<Configuration> configurations)
    {
        using var sw = new StringWriter();
        WriteAsCsv(sw, configurations);
        return sw.ToString();
    }

    public static void WriteKeysAsCsv(TextWriter writer, IEnumerable<ConfigurationKey> keys)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(keys);

        writer.WriteLine("Id,ConfigurationId,Key,Value,Description,IsEncrypted,IsActive,CreatedAt");

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

            writer.WriteLine(string.Join(",", line));
        }
    }

    public static string ExportKeysAsCsv(IEnumerable<ConfigurationKey> keys)
    {
        using var sw = new StringWriter();
        WriteKeysAsCsv(sw, keys);
        return sw.ToString();
    }

    // ------------------------------------------------------------------------
    // XML – streaming (TextWriter) and string wrapper
    // ------------------------------------------------------------------------
    public static void WriteAsXml(TextWriter writer, IEnumerable<Configuration> configurations)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(configurations);

        writer.WriteLine("<Configurations>");

        foreach (var config in configurations)
        {
            writer.WriteLine("  <Configuration>");
            writer.WriteLine($"    <Id>{config.Id}</Id>");
            writer.WriteLine($"    <ApplicationId>{config.ApplicationId}</ApplicationId>");
            writer.WriteLine($"    <Name>{EscapeXmlValue(config.Name)}</Name>");
            writer.WriteLine($"    <Description>{EscapeXmlValue(config.Description)}</Description>");
            writer.WriteLine($"    <Environment>{EscapeXmlValue(config.Environment.ToString())}</Environment>");
            writer.WriteLine($"    <IsActive>{config.IsActive}</IsActive>");
            writer.WriteLine($"    <IsEncrypted>{config.IsEncrypted}</IsEncrypted>");
            writer.WriteLine($"    <CreatedAt>{config.CreatedAt}</CreatedAt>");
            writer.WriteLine($"    <UpdatedAt>{config.UpdatedAt}</UpdatedAt>");
            writer.WriteLine($"    <CreatedBy>{EscapeXmlValue(config.CreatedBy)}</CreatedBy>");
            writer.WriteLine("  </Configuration>");
        }

        writer.WriteLine("</Configurations>");
    }

    public static string ExportAsXml(IEnumerable<Configuration> configurations)
    {
        using var sw = new StringWriter();
        WriteAsXml(sw, configurations);
        return sw.ToString();
    }

    public static void WriteKeysAsXml(TextWriter writer, IEnumerable<ConfigurationKey> keys)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(keys);

        writer.WriteLine("<ConfigurationKeys>");

        foreach (var key in keys)
        {
            writer.WriteLine("  <Key>");
            writer.WriteLine($"    <Id>{key.Id}</Id>");
            writer.WriteLine($"    <ConfigurationId>{key.ConfigurationId}</ConfigurationId>");
            writer.WriteLine($"    <KeyName>{EscapeXmlValue(key.Key)}</KeyName>");
            writer.WriteLine($"    <Value>{EscapeXmlValue(key.Value)}</Value>");
            writer.WriteLine($"    <Description>{EscapeXmlValue(key.Description)}</Description>");
            writer.WriteLine($"    <IsEncrypted>{key.IsEncrypted}</IsEncrypted>");
            writer.WriteLine($"    <IsActive>{key.IsActive}</IsActive>");
            writer.WriteLine($"    <CreatedAt>{key.CreatedAt}</CreatedAt>");
            writer.WriteLine("  </Key>");
        }

        writer.WriteLine("</ConfigurationKeys>");
    }

    public static string ExportKeysAsXml(IEnumerable<ConfigurationKey> keys)
    {
        using var sw = new StringWriter();
        WriteKeysAsXml(sw, keys);
        return sw.ToString();
    }

    // ------------------------------------------------------------------------
    // ENV – streaming (TextWriter) and string wrapper
    // ------------------------------------------------------------------------
    public static void WriteAsEnvFormat(TextWriter writer, IEnumerable<ConfigurationKey> keys)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            var escapedValue = EscapeEnvValue(key.Value);
            writer.WriteLine($"{key.Key}={escapedValue}");
        }
    }

    public static string ExportAsEnvFormat(IEnumerable<ConfigurationKey> keys)
    {
        using var sw = new StringWriter();
        WriteAsEnvFormat(sw, keys);
        return sw.ToString();
    }

    // ------------------------------------------------------------------------
    // YAML – streaming (TextWriter) and string wrapper
    // ------------------------------------------------------------------------
    public static void WriteAsYaml(TextWriter writer, IEnumerable<Configuration> configurations)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(configurations);

        foreach (var config in configurations)
        {
            writer.WriteLine("- Id: " + EscapeYamlValue(config.Id.ToString()));
            writer.WriteLine("  ApplicationId: " + EscapeYamlValue(config.ApplicationId.ToString()));
            writer.WriteLine("  Name: " + EscapeYamlValue(config.Name));
            writer.WriteLine("  Description: " + EscapeYamlValue(config.Description));
            writer.WriteLine("  Environment: " + EscapeYamlValue(config.Environment.ToString()));
            writer.WriteLine("  IsActive: " + config.IsActive.ToString().ToLower());
            writer.WriteLine("  IsEncrypted: " + config.IsEncrypted.ToString().ToLower());
            writer.WriteLine("  CreatedAt: " + EscapeYamlValue(config.CreatedAt.ToString("O")));
            writer.WriteLine("  UpdatedAt: " + EscapeYamlValue(config.UpdatedAt.ToString("O")));
            writer.WriteLine("  CreatedBy: " + EscapeYamlValue(config.CreatedBy));
        }
    }

    public static string ExportAsYaml(IEnumerable<Configuration> configurations)
    {
        using var sw = new StringWriter();
        WriteAsYaml(sw, configurations);
        return sw.ToString();
    }

    public static void WriteKeysAsYaml(TextWriter writer, IEnumerable<ConfigurationKey> keys)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            writer.WriteLine("- Id: " + EscapeYamlValue(key.Id.ToString()));
            writer.WriteLine("  ConfigurationId: " + EscapeYamlValue(key.ConfigurationId.ToString()));
            writer.WriteLine("  Key: " + EscapeYamlValue(key.Key));
            writer.WriteLine("  Value: " + EscapeYamlValue(key.Value));
            writer.WriteLine("  Description: " + EscapeYamlValue(key.Description));
            writer.WriteLine("  IsEncrypted: " + key.IsEncrypted.ToString().ToLower());
            writer.WriteLine("  IsActive: " + key.IsActive.ToString().ToLower());
            writer.WriteLine("  CreatedAt: " + EscapeYamlValue(key.CreatedAt.ToString("O")));
        }
    }

    public static string ExportKeysAsYaml(IEnumerable<ConfigurationKey> keys)
    {
        using var sw = new StringWriter();
        WriteKeysAsYaml(sw, keys);
        return sw.ToString();
    }

    // ------------------------------------------------------------------------
    // JSON – string wrappers (kept for compatibility)
    // ------------------------------------------------------------------------
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

    // ------------------------------------------------------------------------
    // Helper methods – escaping
    // ------------------------------------------------------------------------
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

    private static string EscapeYamlValue(string? value)
    {
        if (value == null)
            return "null";

        if (value.Length == 0)
            return "\"\"";

        bool needsQuotes = value.Contains(':') ||
                           (value.Contains('-') && (value.StartsWith("-") || value.Contains("\n-"))) ||
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
            var escaped = value.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return value;
    }

    private static string EscapeXmlValue(string? value)
    {
        if (value == null)
            return string.Empty;

        // Simple XML escaping – sufficient for our use‑case
        return System.Security.SecurityElement.Escape(value);
    }
}
