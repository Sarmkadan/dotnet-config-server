// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotnetConfigServer.Examples
{
    /// <summary>
    /// Factory for creating and managing HTTP clients for Config Server API.
    /// Handles authentication, retry logic, and error handling.
    /// </summary>
    public class ConfigurationClientFactory
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly int _timeoutSeconds;
        private readonly int _maxRetries;

        public ConfigurationClientFactory(
            string baseUrl,
            string apiKey = null,
            int timeoutSeconds = 30,
            int maxRetries = 3)
        {
            _baseUrl = baseUrl;
            _apiKey = apiKey;
            _timeoutSeconds = timeoutSeconds;
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Create a configured HTTP client for Config Server.
        /// </summary>
        public HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();

            // Configure SSL for development if needed
            if (_baseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase))
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            var client = new HttpClient(new RetryHandler(handler, _maxRetries))
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add authentication if API key provided
            if (!string.IsNullOrEmpty(_apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }

            return client;
        }

        /// <summary>
        /// Create a strongly-typed client wrapper.
        /// </summary>
        public IConfigurationServerClient CreateTypedClient()
        {
            var httpClient = CreateClient();
            return new ConfigurationServerClient(httpClient);
        }

        /// <summary>
        /// HTTP handler with automatic retry logic for transient failures.
        /// </summary>
        private class RetryHandler : DelegatingHandler
        {
            private readonly int _maxRetries;

            public RetryHandler(HttpMessageHandler innerHandler, int maxRetries) : base(innerHandler)
            {
                _maxRetries = maxRetries;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                System.Threading.CancellationToken cancellationToken)
            {
                HttpResponseMessage response = null;
                Exception lastException = null;

                for (int i = 0; i < _maxRetries; i++)
                {
                    try
                    {
                        response = await base.SendAsync(request, cancellationToken);

                        // Don't retry on success or client errors
                        if (response.IsSuccessStatusCode || IsClientError(response.StatusCode))
                            return response;

                        // Retry on server errors
                        response.Dispose();
                    }
                    catch (HttpRequestException ex)
                    {
                        lastException = ex;

                        if (i == _maxRetries - 1)
                            throw;
                    }

                    // Wait before retrying (exponential backoff)
                    if (i < _maxRetries - 1)
                    {
                        var delay = (int)Math.Pow(2, i) * 100; // 100ms, 200ms, 400ms
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                if (lastException != null)
                    throw lastException;

                return response;
            }

            private static bool IsClientError(System.Net.HttpStatusCode statusCode)
            {
                return statusCode >= System.Net.HttpStatusCode.BadRequest &&
                       statusCode < System.Net.HttpStatusCode.InternalServerError;
            }
        }
    }

    /// <summary>
    /// Strongly-typed client for Config Server API.
    /// </summary>
    public interface IConfigurationServerClient
    {
        Task<Configuration> GetConfigurationAsync(Guid configurationId);
        Task<Configuration> CreateConfigurationAsync(CreateConfigurationRequest request);
        Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKeyRequest request);
        Task<bool> HealthCheckAsync();
    }

    /// <summary>
    /// Implementation of strongly-typed client.
    /// </summary>
    public class ConfigurationServerClient : IConfigurationServerClient
    {
        private readonly HttpClient _httpClient;

        public ConfigurationServerClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Configuration> GetConfigurationAsync(Guid configurationId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/configurations/{configurationId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Configuration>(json, options);
        }

        public async Task<Configuration> CreateConfigurationAsync(CreateConfigurationRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/configurations", request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Configuration>(json, options);
        }

        public async Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKeyRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/configurations/{configurationId}/keys",
                request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<ConfigurationKey>(json, options);
        }

        public async Task<bool> HealthCheckAsync()
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

    // DTOs
    public class Configuration
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
        public System.Collections.Generic.List<ConfigurationKey> Keys { get; set; } = new();
    }

    public class ConfigurationKey
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    public class CreateConfigurationRequest
    {
        public Guid ApplicationId { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
    }

    public class ConfigurationKeyRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncrypted { get; set; }
        public string Description { get; set; }
    }

    // Usage example
    public static class UsageExample
    {
        public static async Task RunAsync()
        {
            // Create factory
            var factory = new ConfigurationClientFactory("https://localhost:5001");

            // Create typed client
            var client = factory.CreateTypedClient();

            // Check health
            var isHealthy = await client.HealthCheckAsync();
            Console.WriteLine($"Server is healthy: {isHealthy}");

            // Create configuration
            var createRequest = new CreateConfigurationRequest
            {
                ApplicationId = Guid.NewGuid(),
                Environment = "Production",
                Description = "Production configuration"
            };

            var config = await client.CreateConfigurationAsync(createRequest);
            Console.WriteLine($"Created configuration: {config.Id}");

            // Add a key
            var keyRequest = new ConfigurationKeyRequest
            {
                Key = "Database:Host",
                Value = "localhost",
                IsEncrypted = false,
                Description = "Database hostname"
            };

            var key = await client.AddKeyAsync(config.Id, keyRequest);
            Console.WriteLine($"Added key: {key.Key}");

            // Get configuration
            var retrieved = await client.GetConfigurationAsync(config.Id);
            Console.WriteLine($"Retrieved configuration with {retrieved.Keys.Count} keys");
        }
    }
}
