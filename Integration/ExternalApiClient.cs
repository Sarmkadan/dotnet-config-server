#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net.Http.Json;
using System.Text;

namespace DotnetConfigServer.Integration;

/// <summary>
/// HTTP client for calling external APIs.
/// Handles retries, timeouts, and error handling with proper logging.
/// </summary>
public sealed class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;
    private readonly ExternalApiClientOptions _options;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger, ExternalApiClientOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
        _httpClient.Timeout = _options.Timeout;
    }

    /// <summary>
    /// Makes a GET request to an external API.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null)
    {
        try
        {
            using var response = await ExecuteWithRetryAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request);
            });
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed for {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Makes a POST request to an external API.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null)
    {
        try
        {
            using var response = await ExecuteWithRetryAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(data)
                };
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request);
            });
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed for {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Makes a PUT request to an external API.
    /// </summary>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null)
    {
        try
        {
            using var response = await ExecuteWithRetryAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = JsonContent.Create(data)
                };
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request);
            });
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT request failed for {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Makes a DELETE request to an external API.
    /// </summary>
    public async Task DeleteAsync(string url, Dictionary<string, string>? headers = null)
    {
        try
        {
            using var response = await ExecuteWithRetryAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request);
            });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE request failed for {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Makes a request with automatic retry logic.
    /// The operation must create a fresh request per attempt because a request message cannot be resent.
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> operation)
    {
        int attempt = 0;

        while (attempt < _options.MaxRetries)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries - 1)
            {
                attempt++;
                var delay = _options.RetryDelay * (int)Math.Pow(2, attempt - 1);
                _logger.LogWarning("Request failed (attempt {Attempt}), retrying in {Delay}ms: {Error}", attempt, delay, ex.Message);
                await Task.Delay(delay);
            }
        }

        return await operation();
    }

    /// <summary>
    /// Adds headers to the HTTP request.
    /// </summary>
    private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
    {
        if (headers is null)
            return;

        foreach (var kvp in headers)
        {
            request.Headers.Add(kvp.Key, kvp.Value);
        }
    }
}

public sealed class ExternalApiClientOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;
}
