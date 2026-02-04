// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Manages configurations across multiple environments (Dev, Staging, Production).
    /// Provides utilities for environment-specific operations and promoting configurations.
    /// </summary>
    public class MultiEnvironmentManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _applicationId;

        public MultiEnvironmentManager(string baseUrl, string applicationId)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _applicationId = applicationId;
        }

        /// <summary>
        /// Get or create a configuration for a specific environment.
        /// </summary>
        public async Task<string> GetOrCreateEnvironmentConfigAsync(
            string environment,
            string description = null)
        {
            // Try to find existing configuration
            var configs = await ListEnvironmentConfigurationsAsync();
            var existing = configs.FirstOrDefault(c => c.Environment == environment);

            if (existing != null)
                return existing.Id;

            // Create new configuration
            var request = new
            {
                applicationId = _applicationId,
                environment,
                description = description ?? $"Configuration for {environment} environment"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/configurations", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config = JsonSerializer.Deserialize<ConfigurationDto>(json, options);
            return config.Id;
        }

        /// <summary>
        /// List all configurations for this application across all environments.
        /// </summary>
        public async Task<List<ConfigurationDto>> ListEnvironmentConfigurationsAsync()
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/configurations?applicationId={_applicationId}&pageSize=100");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PagedResult<ConfigurationDto>>(json, options);
            return result.Items;
        }

        /// <summary>
        /// Promote configuration from one environment to another.
        /// Copies keys from source environment to target environment.
        /// </summary>
        public async Task<int> PromoteAsync(
            string sourceEnvironment,
            string targetEnvironment,
            bool overwrite = true,
            List<string> keyFilter = null)
        {
            Console.WriteLine($"Promoting from {sourceEnvironment} to {targetEnvironment}...");

            // Get source configuration
            var sourceConfigId = await GetOrCreateEnvironmentConfigAsync(sourceEnvironment);
            var sourceResponse = await _httpClient.GetAsync($"/api/v1/configurations/{sourceConfigId}");
            sourceResponse.EnsureSuccessStatusCode();

            var sourceJson = await sourceResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sourceConfig = JsonSerializer.Deserialize<ConfigurationDetailsDto>(sourceJson, options);

            // Get target configuration
            var targetConfigId = await GetOrCreateEnvironmentConfigAsync(targetEnvironment);
            var targetResponse = await _httpClient.GetAsync($"/api/v1/configurations/{targetConfigId}");
            targetResponse.EnsureSuccessStatusCode();

            var targetJson = await targetResponse.Content.ReadAsStringAsync();
            var targetConfig = JsonSerializer.Deserialize<ConfigurationDetailsDto>(targetJson, options);

            // Filter keys if needed
            var keysToPromote = sourceConfig.Keys;
            if (keyFilter != null && keyFilter.Count > 0)
                keysToPromote = keysToPromote.Where(k => keyFilter.Contains(k.Key)).ToList();

            var targetKeys = targetConfig.Keys.ToDictionary(k => k.Key);
            int promoted = 0;

            // Copy keys
            foreach (var sourceKey in keysToPromote)
            {
                if (targetKeys.ContainsKey(sourceKey.Key) && !overwrite)
                {
                    Console.WriteLine($"  ⊘ Skipped {sourceKey.Key} (already exists)");
                    continue;
                }

                try
                {
                    var keyRequest = new
                    {
                        key = sourceKey.Key,
                        value = sourceKey.Value,
                        isEncrypted = sourceKey.IsEncrypted,
                        description = sourceKey.Description
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        $"/api/v1/configurations/{targetConfigId}/keys",
                        keyRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"  ✓ Promoted {sourceKey.Key}");
                        promoted++;
                    }
                    else
                    {
                        Console.WriteLine($"  ✗ Failed to promote {sourceKey.Key}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error promoting {sourceKey.Key}: {ex.Message}");
                }
            }

            Console.WriteLine($"Promotion complete: {promoted} keys promoted");
            return promoted;
        }

        /// <summary>
        /// Compare configurations across two environments.
        /// </summary>
        public async Task DisplayEnvironmentComparisonAsync(string env1, string env2)
        {
            var config1Id = await GetOrCreateEnvironmentConfigAsync(env1);
            var config2Id = await GetOrCreateEnvironmentConfigAsync(env2);

            var response1 = await _httpClient.GetAsync($"/api/v1/configurations/{config1Id}");
            var response2 = await _httpClient.GetAsync($"/api/v1/configurations/{config2Id}");

            response1.EnsureSuccessStatusCode();
            response2.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var config1 = JsonSerializer.Deserialize<ConfigurationDetailsDto>(
                await response1.Content.ReadAsStringAsync(), options);
            var config2 = JsonSerializer.Deserialize<ConfigurationDetailsDto>(
                await response2.Content.ReadAsStringAsync(), options);

            Console.WriteLine($"\n=== Comparison: {env1} vs {env2} ===\n");
            Console.WriteLine("{0,-35} {1,-35} {2,-35}",
                "Key", env1, env2);
            Console.WriteLine(new string('-', 105));

            var allKeys = config1.Keys.Select(k => k.Key).Union(config2.Keys.Select(k => k.Key)).OrderBy(k => k);

            foreach (var key in allKeys)
            {
                var key1 = config1.Keys.FirstOrDefault(k => k.Key == key);
                var key2 = config2.Keys.FirstOrDefault(k => k.Key == key);

                var value1 = key1?.Value ?? "[NOT SET]";
                var value2 = key2?.Value ?? "[NOT SET]";

                var marker = value1 == value2 ? "  " : "→";

                Console.WriteLine($"{marker} {key,-33} {value1,-35} {value2,-35}");
            }
        }

        /// <summary>
        /// Synchronize common keys across all environments (e.g., feature flags).
        /// </summary>
        public async Task SynchronizeKeyAsync(string keyName, string value, bool isEncrypted = false)
        {
            var configs = await ListEnvironmentConfigurationsAsync();

            Console.WriteLine($"Synchronizing key '{keyName}' across all environments...\n");

            foreach (var config in configs)
            {
                try
                {
                    var keyRequest = new
                    {
                        key = keyName,
                        value,
                        isEncrypted,
                        description = $"Synchronized key"
                    };

                    var response = await _httpClient.PostAsJsonAsync(
                        $"/api/v1/configurations/{config.Id}/keys",
                        keyRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✓ {config.Environment}: Updated");
                    }
                    else
                    {
                        Console.WriteLine($"✗ {config.Environment}: Failed ({response.StatusCode})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {config.Environment}: Error - {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Display the environment promotion workflow.
        /// </summary>
        public async Task DisplayPromotionWorkflowAsync()
        {
            var configs = await ListEnvironmentConfigurationsAsync();

            Console.WriteLine("\n=== Environment Promotion Workflow ===\n");
            Console.WriteLine("Development → Staging → Production\n");

            foreach (var config in configs.OrderBy(c => GetEnvironmentOrder(c.Environment)))
            {
                Console.WriteLine($"[{config.Environment}]");
                Console.WriteLine($"  Configuration ID: {config.Id}");
                Console.WriteLine($"  Keys: {config.KeyCount ?? 0}");
                Console.WriteLine($"  Created: {config.CreatedAt:yyyy-MM-dd HH:mm}");
                Console.WriteLine();
            }

            Console.WriteLine("Recommended workflow:");
            Console.WriteLine("  1. Develop and test changes in Development");
            Console.WriteLine("  2. Promote tested config to Staging");
            Console.WriteLine("  3. Run integration tests in Staging");
            Console.WriteLine("  4. Promote approved config to Production");
        }

        private int GetEnvironmentOrder(string environment) =>
            environment.ToLower() switch
            {
                "development" => 0,
                "dev" => 0,
                "staging" => 1,
                "stage" => 1,
                "production" => 2,
                "prod" => 2,
                _ => 999
            };
    }

    public class ConfigurationDto
    {
        public string Id { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
        public int? KeyCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ConfigurationDetailsDto
    {
        public string Id { get; set; }
        public string Environment { get; set; }
        public List<ConfigurationKeyDto> Keys { get; set; }
    }

    public class ConfigurationKeyDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
    }

    // Example usage
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var appId = "550e8400-e29b-41d4-a716-446655440000"; // Your application ID
            var manager = new MultiEnvironmentManager("https://localhost:5001", appId);

            // Example 1: Display workflow
            await manager.DisplayPromotionWorkflowAsync();

            // Example 2: Compare environments
            await manager.DisplayEnvironmentComparisonAsync("Development", "Production");

            // Example 3: Promote from Dev to Staging
            // await manager.PromoteAsync("Development", "Staging", overwrite: true);

            // Example 4: Promote from Staging to Production with specific keys
            // await manager.PromoteAsync(
            //     "Staging",
            //     "Production",
            //     overwrite: false,
            //     keyFilter: new List<string> { "Database:Host", "Features:EnableNewCheckout" });

            // Example 5: Synchronize a feature flag across all environments
            // await manager.SynchronizeKeyAsync("Features:MaintenanceMode", "false");
        }
    }
}
