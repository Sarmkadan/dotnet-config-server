#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Example showing how to integrate Dotnet Config Server with a typical .NET service.
    /// Demonstrates dependency injection, configuration management, and webhook integration.
    /// </summary>
    public interface IConfigurationManager
    {
        Task<string> GetConfigurationValueAsync(string key);
        Task<T> GetConfigurationAsync<T>(string key) where T : class;
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    public sealed class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Manages local configuration with periodic synchronization from Config Server.
    /// </summary>
    public sealed class CachedConfigurationManager : IConfigurationManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _configurationId;
        private readonly ILogger<CachedConfigurationManager> _logger;
        private Dictionary<string, string> _localCache;
        private DateTime _lastSync;

        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        public CachedConfigurationManager(
            HttpClient httpClient,
            string configServerUrl,
            string configurationId,
            ILogger<CachedConfigurationManager> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configServerUrl);
            _configurationId = configurationId;
            _logger = logger;
            _localCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Get configuration value with fallback to cache if server is unavailable.
        /// </summary>
        public async Task<string> GetConfigurationValueAsync(string key)
        {
            // Try to get from local cache first
            if (_localCache.TryGetValue(key, out var value))
                return value;

            // Sync from server if needed
            if (DateTime.UtcNow.Subtract(_lastSync).TotalSeconds > 300) // 5 minute cache
                await SyncFromServerAsync();

            return _localCache.TryGetValue(key, out var cachedValue)
                ? cachedValue
                : throw new KeyNotFoundException($"Configuration key '{key}' not found");
        }

        /// <summary>
        /// Get typed configuration value.
        /// </summary>
        public async Task<T> GetConfigurationAsync<T>(string key) where T : class
        {
            var value = await GetConfigurationValueAsync(key);
            return ConvertValue<T>(value);
        }

        /// <summary>
        /// Synchronize configuration from server.
        /// </summary>
        private async Task SyncFromServerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/configurations/{_configurationId}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = System.Text.Json.JsonSerializer.Deserialize<ConfigurationDto>(json, options);

                // Compare with local cache and raise events for changes
                foreach (var key in config.Keys)
                {
                    if (_localCache.TryGetValue(key.Key, out var oldValue))
                    {
                        if (oldValue != key.Value)
                        {
                            _localCache[key.Key] = key.Value;
                            OnConfigurationChanged(new ConfigurationChangedEventArgs
                            {
                                Key = key.Key,
                                OldValue = oldValue,
                                NewValue = key.Value,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        _localCache[key.Key] = key.Value;
                    }
                }

                _lastSync = DateTime.UtcNow;
                _logger.LogInformation("Configuration synchronized from server. Keys: {KeyCount}", config.Keys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synchronizing configuration from server");
            }
        }

        private static T ConvertValue<T>(string value) where T : class
        {
            var type = typeof(T);

            if (type == typeof(string))
                return value as T;

            if (type == typeof(int))
                return (int.Parse(value) as T);

            if (type == typeof(bool))
                return (bool.Parse(value) as T);

            if (type == typeof(decimal))
                return (decimal.Parse(value) as T);

            throw new NotSupportedException($"Type {type.Name} is not supported");
        }

        protected void OnConfigurationChanged(ConfigurationChangedEventArgs args)
        {
            ConfigurationChanged?.Invoke(this, args);
        }
    }

    public sealed class ConfigurationDto
    {
        public Guid Id { get; set; }
        public List<ConfigurationKeyDto> Keys { get; set; } = new();
    }

    public sealed class ConfigurationKeyDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Example service that uses the configuration manager.
    /// </summary>
    public sealed class OrderService
    {
        private readonly IConfigurationManager _configManager;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IConfigurationManager configManager, ILogger<OrderService> logger)
        {
            _configManager = configManager;
            _logger = logger;
        }

        public async Task ProcessOrderAsync(Order order)
        {
            try
            {
                // Get configuration values at runtime
                var dbConnectionString = await _configManager.GetConfigurationValueAsync("Database:ConnectionString");
                var enableNewCheckout = await _configManager.GetConfigurationAsync<bool>("Features:EnableNewCheckout");

                if (enableNewCheckout)
                {
                    _logger.LogInformation("Using new checkout flow for order {OrderId}", order.Id);
                    // Use new checkout flow
                }
                else
                {
                    _logger.LogInformation("Using legacy checkout flow for order {OrderId}", order.Id);
                    // Use legacy checkout flow
                }

                // Process order with dynamic configuration
                _logger.LogInformation("Processing order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", order.Id);
                throw;
            }
        }
    }

    /// <summary>
    /// Background task that listens for configuration changes.
    /// </summary>
    public sealed class ConfigurationSyncBackgroundService : BackgroundService
    {
        private readonly IConfigurationManager _configManager;
        private readonly ILogger<ConfigurationSyncBackgroundService> _logger;

        public ConfigurationSyncBackgroundService(
            IConfigurationManager configManager,
            ILogger<ConfigurationSyncBackgroundService> logger)
        {
            _configManager = configManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Configuration sync service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Periodically sync configuration
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    _logger.LogInformation("Syncing configuration from server");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in configuration sync service");
                }
            }

            _logger.LogInformation("Configuration sync service stopped");
        }
    }

    // Extension methods for dependency injection
    public static class ConfigurationManagerExtensions
    {
        public static IServiceCollection AddConfigurationManager(
            this IServiceCollection services,
            string configServerUrl,
            string configurationId)
        {
            services.AddHttpClient<CachedConfigurationManager>(client =>
            {
                client.BaseAddress = new Uri(configServerUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<IConfigurationManager>(sp =>
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                var logger = sp.GetRequiredService<ILogger<CachedConfigurationManager>>();
                return new CachedConfigurationManager(httpClient, configServerUrl, configurationId, logger);
            });

            services.AddHostedService<ConfigurationSyncBackgroundService>();

            return services;
        }
    }

    // Example usage in Program.cs
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices((context, services) =>
            {
                // Add configuration manager
                services.AddConfigurationManager(
                    configServerUrl: "https://config-server.example.com",
                    configurationId: "550e8400-e29b-41d4-a716-446655440001");

                // Add your services
                services.AddScoped<OrderService>();

                // Listen for configuration changes
                var provider = services.BuildServiceProvider();
                var configManager = provider.GetRequiredService<IConfigurationManager>();
                configManager.ConfigurationChanged += (sender, args) =>
                {
                    Console.WriteLine($"Configuration changed: {args.Key} = {args.NewValue}");
                    // Trigger application-specific reload logic
                };
            });

            var host = builder.Build();

            // Use the service
            using (var scope = host.Services.CreateScope())
            {
                var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
                var order = new Order { Id = Guid.NewGuid(), Total = 99.99m };
                await orderService.ProcessOrderAsync(order);
            }

            await host.RunAsync();
        }
    }

    public sealed class Order
    {
        public Guid Id { get; set; }
        public decimal Total { get; set; }
    }
}
