#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
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
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _httpClient = httpClient;
        _logger = logger;
        _options = options;
        _httpClient.Timeout = _options.Timeout;
    }

    public override string ToString() => $"ExternalApiClient {{ Timeout = {_options.Timeout}, MaxRetries = {_options.MaxRetries}, RetryDelay = {_options.RetryDelay} }}";

    /// <summary>
    /// Makes a GET request to an external API.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogDebug("Starting GET request to {Url}", url);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await ExecuteWithRetryAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request, token);
            }, url, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("{Method} request to {Url} succeeded with status code {StatusCode} in {ElapsedMs}ms", HttpMethod.Get, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
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
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogDebug("Starting POST request to {Url}", url);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await ExecuteWithRetryAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(data)
                };
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request, token);
            }, url, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("{Method} request to {Url} succeeded with status code {StatusCode} in {ElapsedMs}ms", HttpMethod.Post, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
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
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogDebug("Starting PUT request to {Url}", url);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await ExecuteWithRetryAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = JsonContent.Create(data)
                };
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request, token);
            }, url, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("{Method} request to {Url} succeeded with status code {StatusCode} in {ElapsedMs}ms", HttpMethod.Put, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
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
    public async Task DeleteAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        _logger.LogDebug("Starting DELETE request to {Url}", url);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await ExecuteWithRetryAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                AddHeaders(request, headers);
                return await _httpClient.SendAsync(request, token);
            }, url, cancellationToken);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("{Method} request to {Url} succeeded with status code {StatusCode} in {ElapsedMs}ms", HttpMethod.Delete, url, response.StatusCode, stopwatch.ElapsedMilliseconds);
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
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<CancellationToken, Task<HttpResponseMessage>> operation, string url, CancellationToken cancellationToken)
    {
        int attempt = 0;

        while (attempt < _options.MaxRetries)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries - 1)
            {
                attempt++;
                var delay = _options.RetryDelay * (int)Math.Pow(2, attempt - 1);
                _logger.LogWarning("Request failed (attempt {Attempt} of {MaxRetries}) for {Url}, retrying in {Delay}ms: {Error}", attempt, _options.MaxRetries, url, delay, ex.Message);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return await operation(cancellationToken);
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
            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }
    }
}

public sealed class ExternalApiClientOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public int RetryDelay { get; set; } = 1000;

    public void Validate()
    {
        if (MaxRetries < 1)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), MaxRetries, "MaxRetries must be at least 1.");

        if (RetryDelay < 0)
            throw new ArgumentOutOfRangeException(nameof(RetryDelay), RetryDelay, "RetryDelay cannot be negative.");

        if (Timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Timeout), Timeout, "Timeout must be positive.");
    }
}
