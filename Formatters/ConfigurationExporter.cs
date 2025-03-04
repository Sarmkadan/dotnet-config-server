#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Formatters;

/// <summary>
/// Exports configurations in multiple formats: JSON, CSV, XML, YAML.
/// Handles serialization with proper escaping and structure preservation.
/// </summary>
sealed public class ConfigurationExporter
{
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
}
