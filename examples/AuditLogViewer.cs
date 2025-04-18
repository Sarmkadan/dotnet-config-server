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
    /// Retrieves and displays audit logs for configurations.
    /// Useful for compliance, debugging, and understanding configuration history.
    /// </summary>
    public sealed class AuditLogViewer
    {
        private readonly HttpClient _httpClient;

        public AuditLogViewer(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        /// <summary>
        /// Get audit logs for a configuration with optional filtering.
        /// </summary>
        public async Task<List<AuditLog>> GetAuditLogsAsync(
            string configurationId,
            string action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageSize = 50,
            int pageNumber = 1)
        {
            var query = $"/api/v1/configurations/{configurationId}/audit-logs?pageSize={pageSize}&pageNumber={pageNumber}";

            if (!string.IsNullOrEmpty(action))
                query += $"&action={action}";

            if (fromDate.HasValue)
                query += $"&fromDate={fromDate:yyyy-MM-dd}";

            if (toDate.HasValue)
                query += $"&toDate={toDate:yyyy-MM-dd}";

            var response = await _httpClient.GetAsync(query);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<PagedResult<AuditLog>>(json, options);
            return result.Items;
        }

        /// <summary>
        /// Display audit logs in a formatted table.
        /// </summary>
        public async Task DisplayAuditLogsAsync(
            string configurationId,
            string action = null,
            int pageSize = 20)
        {
            var logs = await GetAuditLogsAsync(configurationId, action, pageSize: pageSize);

            Console.WriteLine("\n=== Audit Log ===\n");
            Console.WriteLine("{0,-25} {1,-20} {2,-20} {3,-25} {4,-40}",
                "Timestamp", "Action", "User", "IP Address", "Details");
            Console.WriteLine(new string('-', 130));

            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                var details = log.Details?.Substring(0, Math.Min(40, log.Details?.Length ?? 0)) ?? "";
                Console.WriteLine("{0,-25} {1,-20} {2,-20} {3,-25} {4,-40}",
                    log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    log.Action,
                    log.User ?? "System",
                    log.IpAddress ?? "Unknown",
                    details);
            }

            Console.WriteLine($"\nTotal logs: {logs.Count}");
        }

        /// <summary>
        /// Get changes made by a specific user.
        /// </summary>
        public async Task<List<AuditLog>> GetUserChangesAsync(string configurationId, string user)
        {
            var logs = await GetAuditLogsAsync(configurationId, pageSize: 1000);
            return logs.Where(l => l.User == user).ToList();
        }

        /// <summary>
        /// Get all configuration changes within a date range.
        /// </summary>
        public async Task<List<AuditLog>> GetChangesInDateRangeAsync(
            string configurationId,
            DateTime fromDate,
            DateTime toDate)
        {
            return await GetAuditLogsAsync(configurationId, fromDate: fromDate, toDate: toDate, pageSize: 1000);
        }

        /// <summary>
        /// Display changes to a specific configuration key.
        /// </summary>
        public async Task DisplayKeyChangeHistoryAsync(string configurationId, string keyName)
        {
            var logs = await GetAuditLogsAsync(configurationId, pageSize: 1000);
            var keyChanges = logs.Where(l =>
                l.Details?.Contains(keyName, StringComparison.OrdinalIgnoreCase) == true ||
                l.Changes?.ContainsKey(keyName) == true
            ).ToList();

            Console.WriteLine($"\n=== Change History for Key: {keyName} ===\n");

            foreach (var log in keyChanges.OrderByDescending(l => l.Timestamp))
            {
                Console.WriteLine($"Timestamp: {log.Timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Action: {log.Action}");
                Console.WriteLine($"User: {log.User ?? "System"}");

                if (log.Changes?.ContainsKey(keyName) == true)
                {
                    var change = log.Changes[keyName];
                    Console.WriteLine($"  Old: {change?.OldValue ?? "[null]"}");
                    Console.WriteLine($"  New: {change?.NewValue ?? "[null]"}");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Generate an audit report showing activity summary.
        /// </summary>
        public async Task DisplayAuditReportAsync(string configurationId, int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var logs = await GetChangesInDateRangeAsync(configurationId, fromDate, DateTime.UtcNow);

            Console.WriteLine($"\n=== Audit Report (Last {days} Days) ===\n");

            // Summary by action
            var byAction = logs.GroupBy(l => l.Action)
                .OrderByDescending(g => g.Count())
                .ToList();

            Console.WriteLine("Changes by Action:");
            foreach (var group in byAction)
            {
                Console.WriteLine($"  {group.Key,-20} {group.Count(),5} changes");
            }

            // Summary by user
            Console.WriteLine("\nChanges by User:");
            var byUser = logs.GroupBy(l => l.User ?? "System")
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in byUser)
            {
                Console.WriteLine($"  {group.Key,-30} {group.Count(),5} changes");
            }

            // Most changed keys
            Console.WriteLine("\nMost Changed Keys:");
            var keyChanges = logs
                .Where(l => l.Changes is not null)
                .SelectMany(l => l.Changes.Keys)
                .GroupBy(k => k)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();

            foreach (var group in keyChanges)
            {
                Console.WriteLine($"  {group.Key,-40} {group.Count(),5} changes");
            }

            // Activity timeline
            Console.WriteLine("\nActivity by Date:");
            var byDate = logs.GroupBy(l => l.Timestamp.Date)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in byDate)
            {
                Console.WriteLine($"  {group.Key:yyyy-MM-dd} {group.Count(),5} changes");
            }

            Console.WriteLine($"\nTotal changes in period: {logs.Count}");
        }

        /// <summary>
        /// Export audit logs to CSV format.
        /// </summary>
        public async Task ExportAuditLogsAsync(string configurationId, string filePath)
        {
            var logs = await GetAuditLogsAsync(configurationId, pageSize: 1000);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Timestamp,Action,User,IpAddress,Details");

            foreach (var log in logs.OrderByDescending(l => l.Timestamp))
            {
                var details = (log.Details ?? "").Replace(",", ";").Replace("\"", "'");
                csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Action}\",\"{log.User ?? ""}\",\"{log.IpAddress ?? ""}\",\"{details}\"");
            }

            await System.IO.File.WriteAllTextAsync(filePath, csv.ToString());
            Console.WriteLine($"Audit logs exported to {filePath}");
        }

        /// <summary>
        /// Monitor for suspicious activity patterns.
        /// </summary>
        public async Task DetectAnomaliesAsync(string configurationId)
        {
            var logs = await GetAuditLogsAsync(configurationId, pageSize: 1000);

            Console.WriteLine("\n=== Anomaly Detection ===\n");

            // Detect rapid changes
            var recentLogs = logs.Where(l => l.Timestamp > DateTime.UtcNow.AddHours(-1)).ToList();
            if (recentLogs.Count > 20)
                Console.WriteLine($"⚠ WARNING: {recentLogs.Count} changes in the last hour (unusual activity)");

            // Detect unusual users
            var userActivity = logs.GroupBy(l => l.User)
                .Where(g => g.Count() > 50)
                .ToList();

            if (userActivity.Any())
                Console.WriteLine($"⚠ WARNING: High activity from specific users: {string.Join(", ", userActivity.Select(g => g.Key))}");

            // Detect bulk changes
            var bulkChanges = logs.Where(l => l.Action == "ConfigurationUpdated")
                .ToList();

            if (bulkChanges.Count > 100)
                Console.WriteLine($"⚠ WARNING: {bulkChanges.Count} configuration updates detected");

            // Detect deleted configurations
            var deletions = logs.Where(l => l.Action == "ConfigurationDeleted").ToList();
            if (deletions.Any())
                Console.WriteLine($"⚠ WARNING: {deletions.Count} deletions detected");

            Console.WriteLine("\nAnomaly scan complete");
        }
    }

    public sealed class AuditLog
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string IpAddress { get; set; }
        public string Details { get; set; }
        public Dictionary<string, AuditChange> Changes { get; set; }
    }

    public sealed class AuditChange
    {
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    public sealed class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
    }

    // Example usage
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            var viewer = new AuditLogViewer("https://localhost:5001");
            var configId = "550e8400-e29b-41d4-a716-446655440001";

            // Example 1: Display recent audit logs
            await viewer.DisplayAuditLogsAsync(configId);

            // Example 2: Show change history for a specific key
            // await viewer.DisplayKeyChangeHistoryAsync(configId, "Database:Host");

            // Example 3: Display audit report
            // await viewer.DisplayAuditReportAsync(configId, days: 30);

            // Example 4: Detect anomalies
            // await viewer.DetectAnomaliesAsync(configId);

            // Example 5: Export audit logs
            // await viewer.ExportAuditLogsAsync(configId, "audit-logs.csv");

            // Example 6: Get changes by specific user
            // var userChanges = await viewer.GetUserChangesAsync(configId, "admin@example.com");
            // Console.WriteLine($"User made {userChanges.Count} changes");
        }
    }
}
