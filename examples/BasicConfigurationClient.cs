#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Basic client for retrieving configurations from Dotnet Config Server.
    /// Demonstrates simple HTTP-based configuration retrieval.
    /// </summary>
    sealed public class BasicConfigurationClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public BasicConfigurationClient(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        /// <summary>
        /// Get a complete configuration with all keys.
        /// </summary>
        public async Task<Configuration> GetConfigurationAsync(string configurationId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/configurations/{configurationId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize<Configuration>(json);
            return config;
        }

        /// <summary>
        /// Get a specific configuration key value.
        /// </summary>
        public async Task<string> GetConfigurationKeyAsync(string configurationId, string keyName)
        {
            var config = await GetConfigurationAsync(configurationId);
            return config.GetKeyValue(keyName);
        }

        /// <summary>
        /// Retrieve all configurations for an application.
        /// </summary>
        public async Task<IEnumerable<Configuration>> GetApplicationConfigurationsAsync(string applicationId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/configurations?applicationId={applicationId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PagedResult<Configuration>>(json, options);
            return result.Items;
        }

        /// <summary>
        /// Check if the configuration server is healthy.
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    sealed public class Configuration
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
        public List<ConfigurationKey> Keys { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }

        public string GetKeyValue(string keyName) =>
            Keys.Find(k => k.Key == keyName)?.Value ?? throw new KeyNotFoundException($"Key '{keyName}' not found");
    }

    sealed public class ConfigurationKey
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    sealed public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

    // Example usage
    sealed public class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new BasicConfigurationClient("https://localhost:5001");

            // Check server health
            var isHealthy = await client.IsHealthyAsync();
            Console.WriteLine($"Server is healthy: {isHealthy}");

            // Retrieve configuration
            var configId = "550e8400-e29b-41d4-a716-446655440001"; // Replace with actual ID
            var config = await client.GetConfigurationAsync(configId);

            Console.WriteLine($"Configuration: {config.Environment}");
            foreach (var key in config.Keys)
            {
                Console.WriteLine($"  {key.Key}: {key.Value}");
            }

            // Get specific key value
            var dbHost = await client.GetConfigurationKeyAsync(configId, "Database:Host");
            Console.WriteLine($"Database Host: {dbHost}");
        }
    }
}
