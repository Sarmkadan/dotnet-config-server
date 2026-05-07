// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Batch imports multiple configurations from a JSON file.
    /// Useful for initial setup and migrating configurations from other systems.
    /// </summary>
    public class BatchConfigurationImporter
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BatchConfigurationImporter(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        /// <summary>
        /// Import configurations from a JSON file.
        /// File format:
        /// {
        ///   "applicationId": "uuid",
        ///   "environment": "Production",
        ///   "keys": [
        ///     { "key": "Database:Host", "value": "...", "isEncrypted": false },
        ///     { "key": "ApiKey", "value": "...", "isEncrypted": true }
        ///   ]
        /// }
        /// </summary>
        public async Task<BatchImportResult> ImportFromJsonAsync(string jsonFilePath)
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var import = JsonSerializer.Deserialize<BatchImportRequest>(json);
            return await ImportAsync(import);
        }

        /// <summary>
        /// Import configurations from an object.
        /// </summary>
        public async Task<BatchImportResult> ImportAsync(BatchImportRequest request)
        {
            var result = new BatchImportResult();

            foreach (var key in request.Keys)
            {
                try
                {
                    var keyRequest = new { key.Key, key.Value, key.IsEncrypted, key.Description };
                    var response = await _httpClient.PostAsJsonAsync(
                        $"/api/v1/configurations/{request.ConfigurationId}/keys",
                        keyRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        result.SuccessCount++;
                        Console.WriteLine($"✓ Imported key: {key.Key}");
                    }
                    else
                    {
                        result.FailureCount++;
                        result.Errors.Add($"Failed to import {key.Key}: {response.StatusCode}");
                        Console.WriteLine($"✗ Failed to import key: {key.Key} - {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Error importing {key.Key}: {ex.Message}");
                    Console.WriteLine($"✗ Error importing {key.Key}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Export configuration to JSON file for backup.
        /// </summary>
        public async Task ExportToJsonAsync(string configurationId, string outputPath)
        {
            var response = await _httpClient.GetAsync($"/api/v1/configurations/{configurationId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(outputPath, json);
            Console.WriteLine($"Configuration exported to {outputPath}");
        }

        /// <summary>
        /// Clone configuration from one environment to another.
        /// </summary>
        public async Task<BatchImportResult> CloneConfigurationAsync(string sourceConfigId, string targetConfigId)
        {
            // Get source configuration
            var response = await _httpClient.GetAsync($"/api/v1/configurations/{sourceConfigId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var source = JsonSerializer.Deserialize<ConfigurationDto>(json, options);

            // Prepare batch import for target
            var import = new BatchImportRequest
            {
                ConfigurationId = targetConfigId,
                Keys = source.Keys.Select(k => new ConfigurationKeyImport
                {
                    Key = k.Key,
                    Value = k.Value,
                    IsEncrypted = k.IsEncrypted,
                    Description = k.Description
                }).ToList()
            };

            return await ImportAsync(import);
        }

        /// <summary>
        /// Merge configurations, with newer values overwriting older ones.
        /// </summary>
        public async Task<BatchImportResult> MergeConfigurationsAsync(
            string sourceConfigId,
            string targetConfigId,
            bool overwrite = true)
        {
            // Get configurations
            var sourceResponse = await _httpClient.GetAsync($"/api/v1/configurations/{sourceConfigId}");
            var targetResponse = await _httpClient.GetAsync($"/api/v1/configurations/{targetConfigId}");

            sourceResponse.EnsureSuccessStatusCode();
            targetResponse.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var source = JsonSerializer.Deserialize<ConfigurationDto>(
                await sourceResponse.Content.ReadAsStringAsync(), options);
            var target = JsonSerializer.Deserialize<ConfigurationDto>(
                await targetResponse.Content.ReadAsStringAsync(), options);

            // Merge keys
            var targetKeys = target.Keys.ToDictionary(k => k.Key);
            var result = new BatchImportResult();

            foreach (var sourceKey in source.Keys)
            {
                if (targetKeys.ContainsKey(sourceKey.Key) && !overwrite)
                    continue;

                try
                {
                    var keyRequest = new
                    {
                        sourceKey.Key,
                        sourceKey.Value,
                        sourceKey.IsEncrypted,
                        sourceKey.Description
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        $"/api/v1/configurations/{targetConfigId}/keys",
                        keyRequest);

                    if (response.IsSuccessStatusCode)
                        result.SuccessCount++;
                    else
                        result.FailureCount++;
                }
                catch
                {
                    result.FailureCount++;
                }
            }

            return result;
        }
    }

    public class BatchImportRequest
    {
        public string ConfigurationId { get; set; }
        public List<ConfigurationKeyImport> Keys { get; set; } = new();
    }

    public class ConfigurationKeyImport
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    public class BatchImportResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();

        public override string ToString() =>
            $"Success: {SuccessCount}, Failed: {FailureCount}, Errors: {Errors.Count}";
    }

    public class ConfigurationDto
    {
        public Guid Id { get; set; }
        public string Environment { get; set; }
        public List<ConfigurationKeyDto> Keys { get; set; }
    }

    public class ConfigurationKeyDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    // Example usage
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var importer = new BatchConfigurationImporter("https://localhost:5001");

            // Example 1: Import from JSON file
            var importResult = await importer.ImportFromJsonAsync("configurations.json");
            Console.WriteLine($"Import result: {importResult}");

            // Example 2: Export configuration
            await importer.ExportToJsonAsync(
                "550e8400-e29b-41d4-a716-446655440001",
                "backup.json");

            // Example 3: Clone configuration between environments
            var cloneResult = await importer.CloneConfigurationAsync(
                "dev-config-id",
                "prod-config-id");
            Console.WriteLine($"Clone result: {cloneResult}");

            // Example 4: Merge configurations
            var mergeResult = await importer.MergeConfigurationsAsync(
                "source-config-id",
                "target-config-id",
                overwrite: true);
            Console.WriteLine($"Merge result: {mergeResult}");
        }
    }
}

// Example configurations.json file:
/*
{
  "configurationId": "550e8400-e29b-41d4-a716-446655440001",
  "keys": [
    {
      "key": "Database:Host",
      "value": "prod-db.example.com",
      "isEncrypted": false,
      "description": "Database server hostname"
    },
    {
      "key": "Database:Password",
      "value": "SecurePassword123!",
      "isEncrypted": true,
      "description": "Database password"
    },
    {
      "key": "Features:EnableNewCheckout",
      "value": "true",
      "isEncrypted": false,
      "description": "Enable new checkout flow"
    },
    {
      "key": "ApiKey:ThirdParty",
      "value": "sk-1234567890abcdef",
      "isEncrypted": true,
      "description": "Third-party API key"
    }
  ]
}
*/
