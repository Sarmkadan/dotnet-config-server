#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using DotnetConfigServer.Common;
using DotnetConfigServer.Events;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.AspNetCore.Mvc;
using Env = DotnetConfigServer.Common.Environment;

namespace DotnetConfigServer.Controllers;

/// <summary>
/// API controller for watching configuration changes using long-polling or Server-Sent Events
/// </summary>
[ApiController]
[Route("api/v1/config/{appId:guid}/{env}")]
[Produces("application/json")]
public sealed class ConfigWatchController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly IVersioningService _versioningService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigWatchController> _logger;

    // Track active watch requests to prevent memory leaks
    private static readonly ConcurrentDictionary<Guid, WatchRequestContext> _activeWatchRequests = new();

    // Configuration for watch behavior
    private const int MaxConcurrentWatchesPerConnection = 10;
    private const int DefaultTimeoutSeconds = 30;
    private const int MaxTimeoutSeconds = 60;

    public ConfigWatchController(
        IEventBus eventBus,
        IVersioningService versioningService,
        IConfigurationService configurationService,
        ILogger<ConfigWatchController> logger)
    {
        _eventBus = eventBus;
        _versioningService = versioningService;
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Watches for configuration changes using long-polling
    /// </summary>
    /// <param name="appId">The application ID to watch</param>
    /// <param name="env">The environment (Development, Staging, Production)</param>
    /// <param name="sinceVersion">Optional version number to start watching from</param>
    /// <param name="timeout">Optional timeout in seconds (default: 30, max: 60)</param>
    /// <returns>Configuration changes or 204 No Content if timeout occurs</returns>
    [HttpGet("watch")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WatchConfigChanges(
        [FromRoute] Guid appId,
        [FromRoute] Env env,
        [FromQuery] string? sinceVersion = null,
        [FromQuery] int? timeout = null)
    {
        try
        {
            // Validate timeout
            var watchTimeout = timeout switch
            {
                null => DefaultTimeoutSeconds,
                <= 0 => DefaultTimeoutSeconds,
                >= MaxTimeoutSeconds => MaxTimeoutSeconds,
                _ => timeout.Value
            };

            // Validate environment
            if (!Enum.IsDefined(typeof(Env), env))
            {
                return BadRequest(new { error = "Invalid environment specified" });
            }

            // Get current configuration version
            var currentVersion = await _versioningService.GetActiveVersionAsync(appId);

            if (currentVersion is null)
            {
                return NotFound(new { error = "No active configuration version found for this application" });
            }

            // Check if there's already a newer version
            if (sinceVersion is not null && int.TryParse(sinceVersion, out var expectedVersion))
            {
                var currentVersionNumber = int.Parse(currentVersion.VersionNumber.Split('.')[0]);
                if (currentVersionNumber > expectedVersion)
                {
                    // Newer version already exists, return immediately
                    var config = await _configurationService.GetByApplicationAsync(appId);
                    var response = new WatchResponse
                    {
                        Version = currentVersion.VersionNumber,
                        ChangedAt = DateTime.UtcNow,
                        Configurations = config,
                        IsImmediateResponse = true
                    };
                    return Ok(response);
                }
            }

            // Create watch request context
            var requestId = Guid.NewGuid();
            var context = new WatchRequestContext
            {
                RequestId = requestId,
                ApplicationId = appId,
                Environment = env,
                SinceVersion = sinceVersion,
                Timeout = TimeSpan.FromSeconds(watchTimeout),
                StartedAt = DateTime.UtcNow,
                CancellationTokenSource = new CancellationTokenSource(watchTimeout * 1000)
            };

            // Register the watch request
            _activeWatchRequests.TryAdd(requestId, context);

            _logger.LogInformation("Watch request {RequestId} started for app {AppId} env {Env} since version {SinceVersion}",
                requestId, appId, env, sinceVersion ?? "latest");

            try
            {
                // Wait for either a change event or timeout
                var changeTask = WaitForConfigurationChangeAsync(context);
                var timeoutTask = Task.Delay(context.Timeout, context.CancellationTokenSource.Token);

                var completedTask = await Task.WhenAny(changeTask, timeoutTask);

                if (completedTask == changeTask)
                {
                    // Configuration changed, return the new state
                    var result = await changeTask;
                    return Ok(result);
                }
                else
                {
                    // Timeout occurred
                    _logger.LogDebug("Watch request {RequestId} timed out after {Timeout}s", requestId, watchTimeout);
                    return NoContent();
                }
            }
            finally
            {
                // Clean up the watch request
                _activeWatchRequests.TryRemove(requestId, out _);
                context.CancellationTokenSource.Dispose();
                _logger.LogDebug("Watch request {RequestId} completed", requestId);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Watch request was cancelled");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in watch configuration changes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Watches for configuration changes using Server-Sent Events (SSE)
    /// </summary>
    /// <param name="appId">The application ID to watch</param>
    /// <param name="env">The environment (Development, Staging, Production)</param>
    /// <param name="sinceVersion">Optional version number to start watching from</param>
    [HttpGet("watch/sse")]
    [ProducesResponseType(typeof(WatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WatchConfigChangesSse(
        [FromRoute] Guid appId,
        [FromRoute] Env env,
        [FromQuery] string? sinceVersion = null)
    {
        try
        {
            // Validate environment
            if (!Enum.IsDefined(typeof(Env), env))
            {
                return BadRequest(new { error = "Invalid environment specified" });
            }

            // Get current configuration version
            var currentVersion = await _versioningService.GetActiveVersionAsync(appId);

            if (currentVersion is null)
            {
                return NotFound(new { error = "No active configuration version found for this application" });
            }

            // Check if there's already a newer version
            if (sinceVersion is not null && int.TryParse(sinceVersion, out var expectedVersion))
            {
                var currentVersionNumber = int.Parse(currentVersion.VersionNumber.Split('.')[0]);
                if (currentVersionNumber > expectedVersion)
                {
                    // Newer version already exists, return immediately
                    var config = await _configurationService.GetByApplicationAsync(appId);
                    var response = new WatchResponse
                    {
                        Version = currentVersion.VersionNumber,
                        ChangedAt = DateTime.UtcNow,
                        Configurations = config,
                        IsImmediateResponse = true
                    };
                    return Ok(response);
                }
            }

            // Create SSE response
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // Create watch request context
            var requestId = Guid.NewGuid();
            var context = new WatchRequestContext
            {
                RequestId = requestId,
                ApplicationId = appId,
                Environment = env,
                SinceVersion = sinceVersion,
                IsSse = true,
                Response = Response,
                StartedAt = DateTime.UtcNow
            };

            // Register the watch request
            _activeWatchRequests.TryAdd(requestId, context);

            _logger.LogInformation("SSE watch request {RequestId} started for app {AppId} env {Env}",
                requestId, appId, env);

            try
            {
                // Wait for configuration changes
                await WaitForConfigurationChangeSseAsync(context);

                // If we get here, the client disconnected
                return new EmptyResult();
            }
            finally
            {
                // Clean up the watch request
                _activeWatchRequests.TryRemove(requestId, out _);
                _logger.LogDebug("SSE watch request {RequestId} completed", requestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE watch configuration changes");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Waits for a configuration change event for the given application/environment
    /// </summary>
    private async Task<WatchResponse> WaitForConfigurationChangeAsync(WatchRequestContext context)
    {
        var tcs = new TaskCompletionSource<WatchResponse>();
        var cancellationToken = context.CancellationTokenSource.Token;

        // Create event handler
        async Task<Task> ConfigurationChangedHandler(ConfigurationVersionCreatedEvent @event)
        {
            try
            {
                // Check if this event is for our application and environment
                if (@event.ConfigurationId == context.ApplicationId)
                {
                    var currentVersion = await _versioningService.GetActiveVersionAsync(context.ApplicationId);

                    if (currentVersion is not null)
                    {
                        var config = await _configurationService.GetByApplicationAsync(context.ApplicationId);
                        var response = new WatchResponse
                        {
                            Version = currentVersion.VersionNumber,
                            ChangedAt = DateTime.UtcNow,
                            Configurations = config,
                            IsImmediateResponse = false
                        };
                        tcs.TrySetResult(response);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling configuration changed event");
            }

            return Task.CompletedTask;
        }

        // Subscribe to events
        _eventBus.Subscribe<ConfigurationVersionCreatedEvent>(ConfigurationChangedHandler);

        // Set up cancellation
        cancellationToken.Register(() =>
        {
            _logger.LogDebug("Watch request {RequestId} cancellation requested", context.RequestId);
            tcs.TrySetCanceled();
        });

        // Wait for either a change or cancellation
        return await tcs.Task;
    }

    /// <summary>
    /// Waits for configuration changes and streams via SSE
    /// </summary>
    private async Task WaitForConfigurationChangeSseAsync(WatchRequestContext context)
    {
        var cancellationTokenSource = new CancellationTokenSource(MaxTimeoutSeconds * 1000);

        // Create event handler
        async Task<Task> ConfigurationChangedHandler(ConfigurationVersionCreatedEvent @event)
        {
            try
            {
                // Check if this event is for our application
                if (@event.ConfigurationId == context.ApplicationId)
                {
                    var currentVersion = await _versioningService.GetActiveVersionAsync(context.ApplicationId);

                    if (currentVersion is not null)
                    {
                        var config = await _configurationService.GetByApplicationAsync(context.ApplicationId);
                        var response = new WatchResponse
                        {
                            Version = currentVersion.VersionNumber,
                            ChangedAt = DateTime.UtcNow,
                            Configurations = config,
                            IsImmediateResponse = false
                        };

                        // Stream via SSE
                        var sseData = new
                        {
                            eventType = "config.changed",
                            data = response,
                            id = context.RequestId.ToString(),
                            retry = 10000
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(sseData);
                        await context.Response.WriteAsync($"data: {json}\n\n");
                        await context.Response.Body.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming SSE event");
                var errorJson = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
                await context.Response.WriteAsync($"event: error\ndata: {errorJson}\n\n");
                await context.Response.Body.FlushAsync();
            }

            return Task.CompletedTask;
        }

        // Subscribe to events
        _eventBus.Subscribe<ConfigurationVersionCreatedEvent>(ConfigurationChangedHandler);

        // Set up cancellation
        cancellationTokenSource.Token.Register(async () =>
        {
            try
            {
                await context.Response.WriteAsync(": heartbeat\n\n");
                await context.Response.Body.FlushAsync();
            }
            catch
            {
                // Client may have disconnected
            }
        });

        // Keep the connection alive
        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationTokenSource.Token);
                await context.Response.WriteAsync(": heartbeat\n\n");
                await context.Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal disconnect
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE connection");
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Gets the current status of active watch requests (for monitoring)
    /// </summary>
    [HttpGet("watch/status")]
    [ProducesResponseType(typeof(Dictionary<Guid, WatchRequestStatus>), StatusCodes.Status200OK)]
    public IActionResult GetWatchStatus()
    {
        var status = _activeWatchRequests.ToDictionary(
            kvp => kvp.Key,
            kvp => new WatchRequestStatus
            {
                ApplicationId = kvp.Value.ApplicationId,
                Environment = kvp.Value.Environment,
                SinceVersion = kvp.Value.SinceVersion,
                StartedAt = kvp.Value.StartedAt,
                IsSse = kvp.Value.IsSse,
                Duration = DateTime.UtcNow - kvp.Value.StartedAt
            }
        );

        return Ok(status);
    }

    /// <summary>
    /// Watch request context to track active connections
    /// </summary>
    private sealed class WatchRequestContext
    {
        public Guid RequestId { get; set; }
        public Guid ApplicationId { get; set; }
        public Env Environment { get; set; }
        public string? SinceVersion { get; set; }
        public TimeSpan Timeout { get; set; }
        public DateTime StartedAt { get; set; }
        public bool IsSse { get; set; }
        public HttpResponse? Response { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }

    /// <summary>
    /// Status information for a watch request
    /// </summary>
    public sealed class WatchRequestStatus
    {
        public Guid ApplicationId { get; set; }
        public Env Environment { get; set; }
        public string? SinceVersion { get; set; }
        public DateTime StartedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSse { get; set; }
    }
}

/// <summary>
/// Response object for watch requests
/// </summary>
public sealed class WatchResponse
{
    /// <summary>
    /// The new configuration version number
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// When the change occurred
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// The list of configurations for the application
    /// </summary>
    public List<Models.Configuration>? Configurations { get; set; }

    /// <summary>
    /// Whether this is an immediate response (no wait) or a delayed response
    /// </summary>
    public bool IsImmediateResponse { get; set; }
}
