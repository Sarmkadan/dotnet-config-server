# IHealthCheckService

The `IHealthCheckService` interface provides a centralized contract for monitoring the health and readiness of a service instance. It exposes both synchronous snapshot properties (status, uptime, resource usage) and asynchronous methods to obtain a full health report or simple boolean checks for liveness and readiness. Implementations typically aggregate results from multiple registered health checks and cache the latest state for efficient access.

## API

### Properties

#### `HealthCheckService HealthCheckService`
Gets the underlying `HealthCheckService` instance that implements this interface. This property is primarily used for advanced scenarios where direct access to the concrete service is required (e.g., for registration or configuration).  
**Throws:** Not applicable (property getter).

#### `string Status`
The overall health status of the service. Common values include `"Healthy"`, `"Degraded"`, and `"Unhealthy"`. This value is derived from the aggregated results of all individual health checks.  
**Throws:** Not applicable.

#### `DateTime Timestamp`
The UTC timestamp when the current health state was last evaluated. This can be used to determine the freshness of the snapshot.  
**Throws:** Not applicable.

#### `double Uptime`
The total time, in seconds, that the service has been running since its last start.  
**Throws:** Not applicable.

#### `int ProcessId`
The operating system process identifier (PID) of the current service instance.  
**Throws:** Not applicable.

#### `long MemoryMb`
The current memory usage of the process, expressed in megabytes.  
**Throws:** Not applicable.

#### `int ThreadCount`
The number of active threads in the process at the time of the last health evaluation.  
**Throws:** Not applicable.

#### `Dictionary<string, HealthCheck> Checks`
A dictionary mapping each registered health check name to its corresponding `HealthCheck` result. Each `HealthCheck` object contains its own `Status`, `Message`, and `Metrics` properties.  
**Throws:** Not applicable.

#### `string Message`
A human-readable message providing additional context about the overall health status. This may contain warnings or descriptions of degraded components.  
**Throws:** Not applicable.

#### `Dictionary<string, object> Metrics`
A collection of additional metrics associated with the overall health state. The keys are metric names and values can be of any type (e.g., numbers, strings, or nested objects).  
**Throws:** Not applicable.

### Methods

#### `Task<HealthReport> GetHealthReportAsync()`
Asynchronously retrieves a complete `HealthReport` containing detailed information about all registered health checks, including their status, description, duration, and any exception data.  
**Parameters:** None.  
**Returns:** A `Task<HealthReport>` that resolves to a full health report.  
**Throws:** `OperationCanceledException` if the operation is cancelled via the default cancellation token (if supported by the implementation). Other exceptions may be thrown if an individual health check fails catastrophically.

#### `Task<bool> IsReadyAsync()`
Asynchronously determines whether the service is ready to accept requests. Typically returns `true` only when the overall status is `"Healthy"` and all critical dependencies are available.  
**Parameters:** None.  
**Returns:** A `Task<bool>` that resolves to `true` if the service is ready; otherwise, `false`.  
**Throws:** `OperationCanceledException` if the operation is cancelled.

#### `Task<bool> IsAliveAsync()`
Asynchronously determines whether the service is alive (i.e., the process is running and responding). This is a lightweight check that usually returns `true` even if some non-critical components are degraded.  
**Parameters:** None.  
**Returns:** A `Task<bool>` that resolves to `true` if the service is alive; otherwise, `false`.  
**Throws:** `OperationCanceledException` if the operation is cancelled.

## Usage

### Example 1: Exposing health endpoints in an ASP.NET Core controller

```csharp
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthService;

    public HealthController(IHealthCheckService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet("live")]
    public async Task<IActionResult> Live()
    {
        bool alive = await _healthService.IsAliveAsync();
        return alive ? Ok() : StatusCode(503);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        bool ready = await _healthService.IsReadyAsync();
        return ready ? Ok() : StatusCode(503);
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report()
    {
        var report = await _healthService.GetHealthReportAsync();
        return Ok(report);
    }
}
```

### Example 2: Periodic health logging in a background service

```csharp
public class HealthLogger : BackgroundService
{
    private readonly IHealthCheckService _healthService;
    private readonly ILogger<HealthLogger> _logger;

    public HealthLogger(IHealthCheckService healthService, ILogger<HealthLogger> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Access snapshot properties (no async overhead)
            _logger.LogInformation(
                "Status: {Status}, Uptime: {Uptime}s, Memory: {MemoryMb} MB, Threads: {ThreadCount}",
                _healthService.Status,
                _healthService.Uptime,
                _healthService.MemoryMb,
                _healthService.ThreadCount);

            // Log individual check results
            foreach (var (name, check) in _healthService.Checks)
            {
                _logger.LogDebug("Check '{Name}': {Status} - {Message}", name, check.Status, check.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

## Notes

- **Thread safety:** All properties are safe to read concurrently from multiple threads. The underlying state is typically updated atomically by the implementation. However, the snapshot represented by the properties may become stale if the health evaluation interval is long. Use `GetHealthReportAsync()` for the most up-to-date information.
- **Edge cases:** If no health checks have been registered, `Checks` will be an empty dictionary, and `Status` will reflect the default healthy state. `Uptime` may be zero immediately after service start. `MemoryMb` and `ThreadCount` are snapshots and may not reflect real-time values if the health evaluation is delayed.
- **Exceptions:** The asynchronous methods (`GetHealthReportAsync`, `IsReadyAsync`, `IsAliveAsync`) may throw if the underlying health check pipeline encounters an unhandled exception. Implementations should catch and report such failures within the `HealthReport`, but some exceptions may propagate. Always use try-catch when calling these methods in production code.
- **Property `HealthCheckService`:** This property returns the concrete service instance. It is intended for advanced use cases (e.g., dynamic registration of checks) and should not be relied upon for normal health queries.
