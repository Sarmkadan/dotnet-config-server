// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for importing configurations from various formats.
/// Supports JSON, CSV, YAML, and environment variable formats.
/// </summary>
public interface IConfigurationImportService
{
    /// <summary>
    /// Imports configurations from JSON format.
    /// </summary>
    Task<List<ConfigurationKey>> ImportFromJsonAsync(string json, Guid configurationId);

    /// <summary>
    /// Imports configurations from CSV format.
    /// </summary>
    Task<List<ConfigurationKey>> ImportFromCsvAsync(string csv, Guid configurationId);

    /// <summary>
    /// Imports configurations from environment variable format.
    /// </summary>
    Task<List<ConfigurationKey>> ImportFromEnvAsync(string envContent, Guid configurationId);

    /// <summary>
    /// Validates import data.
    /// </summary>
    Task<ImportValidationResult> ValidateAsync(string data, string format);
}

public class ConfigurationImportService : IConfigurationImportService
{
    private readonly ILogger<ConfigurationImportService> _logger;

    public ConfigurationImportService(ILogger<ConfigurationImportService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ConfigurationKey>> ImportFromJsonAsync(string json, Guid configurationId)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var keys = new List<ConfigurationKey>();

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    keys.Add(new ConfigurationKey
                    {
                        Id = Guid.NewGuid(),
                        ConfigurationId = configurationId,
                        Key = property.Name,
                        Value = property.Value.GetString() ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            _logger.LogInformation("Imported {Count} keys from JSON", keys.Count);
            return await Task.FromResult(keys);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing JSON for import");
            throw new InvalidOperationException("Invalid JSON format", ex);
        }
    }

    public async Task<List<ConfigurationKey>> ImportFromCsvAsync(string csv, Guid configurationId)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<ConfigurationKey>();

        if (lines.Length < 2)
            return keys;

        var headers = lines[0].Split(',');
        var keyIndex = Array.IndexOf(headers, "Key");
        var valueIndex = Array.IndexOf(headers, "Value");

        if (keyIndex == -1 || valueIndex == -1)
            throw new InvalidOperationException("CSV must contain 'Key' and 'Value' columns");

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length > Math.Max(keyIndex, valueIndex))
            {
                keys.Add(new ConfigurationKey
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configurationId,
                    Key = parts[keyIndex].Trim(),
                    Value = parts[valueIndex].Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
        }

        _logger.LogInformation("Imported {Count} keys from CSV", keys.Count);
        return await Task.FromResult(keys);
    }

    public async Task<List<ConfigurationKey>> ImportFromEnvAsync(string envContent, Guid configurationId)
    {
        var lines = envContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<ConfigurationKey>();

        foreach (var line in lines)
        {
            if (line.StartsWith("#") || !line.Contains("="))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                keys.Add(new ConfigurationKey
                {
                    Id = Guid.NewGuid(),
                    ConfigurationId = configurationId,
                    Key = parts[0].Trim(),
                    Value = parts[1].Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
        }

        _logger.LogInformation("Imported {Count} keys from environment format", keys.Count);
        return await Task.FromResult(keys);
    }

    public async Task<ImportValidationResult> ValidateAsync(string data, string format)
    {
        var result = new ImportValidationResult { IsValid = true };

        try
        {
            switch (format.ToLowerInvariant())
            {
                case "json":
                    JsonDocument.Parse(data);
                    break;
                case "csv":
                    ValidateCsv(data);
                    break;
                case "env":
                    ValidateEnv(data);
                    break;
                default:
                    result.IsValid = false;
                    result.Errors.Add("Unknown format");
                    break;
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
        }

        return await Task.FromResult(result);
    }

    private void ValidateCsv(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            throw new InvalidOperationException("CSV must contain header and at least one row");

        var headers = lines[0].Split(',');
        if (!headers.Contains("Key") || !headers.Contains("Value"))
            throw new InvalidOperationException("CSV must contain 'Key' and 'Value' columns");
    }

    private void ValidateEnv(string env)
    {
        var lines = env.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.Contains("="))
                throw new InvalidOperationException($"Invalid environment variable format: {line}");
        }
    }
}

public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int EstimatedRowCount { get; set; }
}
