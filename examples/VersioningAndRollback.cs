#nullable enable
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
    /// Demonstrates configuration versioning, comparison, and rollback operations.
    /// Versions allow you to track all configuration changes and roll back if needed.
    /// </summary>
    sealed public class VersioningAndRollback
    {
        private readonly HttpClient _httpClient;

        public VersioningAndRollback(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        /// <summary>
        /// Create a new configuration version from current state.
        /// </summary>
        public async Task<ConfigurationVersion> CreateVersionAsync(
            string configurationId,
            string description,
            string changeNotes = null)
        {
            var request = new
            {
                description,
                changeNotes
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/configurations/{configurationId}/versions",
                request);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ConfigurationVersion>(json, options);
        }

        /// <summary>
        /// List all versions for a configuration.
        /// </summary>
        public async Task<List<ConfigurationVersion>> ListVersionsAsync(string configurationId)
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/configurations/{configurationId}/versions");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PagedResult<ConfigurationVersion>>(json, options);
            return result.Items;
        }

        /// <summary>
        /// Get the currently active version.
        /// </summary>
        public async Task<ConfigurationVersion> GetActiveVersionAsync(string configurationId)
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/configurations/{configurationId}/versions/active");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ConfigurationVersion>(json, options);
        }

        /// <summary>
        /// Publish a version to make it the active version.
        /// </summary>
        public async Task<ConfigurationVersion> PublishVersionAsync(
            string configurationId,
            string versionId,
            string notes = null)
        {
            var request = new { notes };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/configurations/{configurationId}/versions/{versionId}/publish",
                request);

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ConfigurationVersion>(json, options);
        }

        /// <summary>
        /// Compare two versions and get the differences.
        /// </summary>
        public async Task<List<ConfigurationDiff>> CompareVersionsAsync(
            string configurationId,
            string fromVersionId,
            string toVersionId)
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/configurations/{configurationId}/versions/{fromVersionId}/diff/{toVersionId}");

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<ConfigurationDiff>>(json, options);
        }

        /// <summary>
        /// Roll back to a previous version.
        /// </summary>
        public async Task RollbackVersionAsync(
            string configurationId,
            string targetVersionId,
            string reason = null)
        {
            var request = new
            {
                reason,
                notifyWebhooks = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/configurations/{configurationId}/versions/{targetVersionId}/rollback",
                request);

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Archive a version (removes it from active management).
        /// </summary>
        public async Task ArchiveVersionAsync(string configurationId, string versionId)
        {
            var response = await _httpClient.PostAsync(
                $"/api/v1/configurations/{configurationId}/versions/{versionId}/archive",
                null);

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Display version history in a formatted table.
        /// </summary>
        public async Task DisplayVersionHistoryAsync(string configurationId)
        {
            var versions = await ListVersionsAsync(configurationId);

            Console.WriteLine("\n=== Configuration Version History ===\n");
            Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-20} {4,-40}",
                "Version", "Status", "Key Count", "Created At", "Description");
            Console.WriteLine(new string('-', 105));

            foreach (var version in versions.OrderByDescending(v => v.Version))
            {
                Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-20} {4,-40}",
                    version.Version,
                    version.Status,
                    version.KeyCount,
                    version.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    version.Description?.Substring(0, Math.Min(40, version.Description?.Length ?? 0)) ?? "");
            }
        }

        /// <summary>
        /// Display differences between two versions in a formatted table.
        /// </summary>
        public async Task DisplayDifferencesAsync(
            string configurationId,
            string fromVersionId,
            string toVersionId)
        {
            var diffs = await CompareVersionsAsync(configurationId, fromVersionId, toVersionId);

            Console.WriteLine("\n=== Configuration Changes ===\n");
            Console.WriteLine("{0,-30} {1,-10} {2,-40} {3,-40}",
                "Key", "Type", "Old Value", "New Value");
            Console.WriteLine(new string('-', 120));

            foreach (var diff in diffs)
            {
                var changeType = diff.ChangeType;
                var oldValue = diff.OldValue?.Substring(0, Math.Min(40, diff.OldValue?.Length ?? 0)) ?? "";
                var newValue = diff.NewValue?.Substring(0, Math.Min(40, diff.NewValue?.Length ?? 0)) ?? "";

                Console.WriteLine("{0,-30} {1,-10} {2,-40} {3,-40}",
                    diff.Key, changeType, oldValue, newValue);
            }

            Console.WriteLine($"\nTotal changes: {diffs.Count}");
        }

        /// <summary>
        /// Automated blue-green deployment pattern for configuration updates.
        /// Create a new version, test it, then switch traffic to it.
        /// </summary>
        public async Task<string> BlueGreenDeploymentAsync(
            string configurationId,
            string changeDescription,
            Func<string, Task<bool>> validateAsync = null)
        {
            // 1. Get current active version (Blue)
            var blueVersion = await GetActiveVersionAsync(configurationId);
            Console.WriteLine($"Current active version: {blueVersion.Version}");

            // 2. Create new version (Green)
            var greenVersion = await CreateVersionAsync(configurationId, changeDescription);
            Console.WriteLine($"Created new version: {greenVersion.Version} (Green)");

            // 3. Run validation tests if provided
            if (validateAsync != null)
            {
                var isValid = await validateAsync(greenVersion.Id);
                if (!isValid)
                {
                    Console.WriteLine("Validation failed, rolling back");
                    throw new Exception("Green version failed validation");
                }
            }

            // 4. Compare versions
            await DisplayDifferencesAsync(configurationId, blueVersion.Id, greenVersion.Id);

            // 5. Publish green version (switch traffic)
            Console.WriteLine("Publishing green version...");
            await PublishVersionAsync(configurationId, greenVersion.Id);
            Console.WriteLine("Green version is now active");

            // 6. Archive old version
            await ArchiveVersionAsync(configurationId, blueVersion.Id);
            Console.WriteLine($"Archived blue version {blueVersion.Version}");

            return greenVersion.Id;
        }

        /// <summary>
        /// Canary deployment: gradually roll out configuration changes.
        /// </summary>
        public async Task CanaryDeploymentAsync(
            string configurationId,
            string changeDescription,
            Func<string, int, Task<bool>> validateAsync = null)
        {
            // Create new version
            var canaryVersion = await CreateVersionAsync(configurationId, changeDescription);
            Console.WriteLine($"Created canary version: {canaryVersion.Version}");

            // Stage 1: 10% traffic (test with 10% of requests)
            for (int stage = 10; stage <= 100; stage += 10)
            {
                Console.WriteLine($"\nCanary stage: {stage}% traffic");

                if (validateAsync != null)
                {
                    var isValid = await validateAsync(canaryVersion.Id, stage);
                    if (!isValid)
                    {
                        Console.WriteLine($"Validation failed at {stage}%, rolling back");
                        throw new Exception($"Canary deployment failed at {stage}%");
                    }
                }

                Console.WriteLine($"✓ {stage}% validated successfully");
                await Task.Delay(1000); // Simulate waiting between stages
            }

            // Final: Promote to production
            Console.WriteLine("\nPromoting canary to production...");
            await PublishVersionAsync(configurationId, canaryVersion.Id);
            Console.WriteLine("Canary deployment completed successfully!");
        }
    }

    sealed public class ConfigurationVersion
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } // Draft, Published, Archived
        public int KeyCount { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    sealed public class ConfigurationDiff
    {
        public string Key { get; set; }
        public string ChangeType { get; set; } // Added, Modified, Deleted
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    sealed public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
    }

    // Example usage
    sealed public class Program
    {
        public static async Task Main(string[] args)
        {
            var versioning = new VersioningAndRollback("https://localhost:5001");
            var configId = "550e8400-e29b-41d4-a716-446655440001";

            // Example 1: Show version history
            await versioning.DisplayVersionHistoryAsync(configId);

            // Example 2: Create a new version
            var newVersion = await versioning.CreateVersionAsync(
                configId,
                "Q2 2026 release with performance improvements",
                "Updated database pooling and cache settings");

            // Example 3: Compare versions
            var activeVersion = await versioning.GetActiveVersionAsync(configId);
            await versioning.DisplayDifferencesAsync(configId, activeVersion.Id, newVersion.Id);

            // Example 4: Blue-green deployment
            // var result = await versioning.BlueGreenDeploymentAsync(
            //     configId,
            //     "Production release",
            //     validateAsync: async (versionId) =>
            //     {
            //         // Run your validation tests here
            //         return true;
            //     });

            // Example 5: Canary deployment
            // await versioning.CanaryDeploymentAsync(
            //     configId,
            //     "Canary release with new feature",
            //     validateAsync: async (versionId, percentage) =>
            //     {
            //         Console.WriteLine($"Running validation for {percentage}% traffic");
            //         // Run tests against the canary version
            //         return true;
            //     });
        }
    }
}
