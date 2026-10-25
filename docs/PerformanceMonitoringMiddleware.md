# PerformanceMonitoringMiddleware

The `PerformanceMonitoringMiddleware` is an ASP.NET Core middleware component that times every HTTP request, estimates the memory it touched, and stores the result in a shared, bounded, in-memory buffer. The buffer is owned by the `PerformanceMetrics` singleton, which can be queried for recent requests, average latency, and a log summary. Requests slower than a configurable threshold are logged as warnings.

The middleware and its metrics store live in `Middleware/PerformanceMonitoringMiddleware.cs`. The options class lives in `Middleware/PerformanceMonitoringOptions.cs`.

## API

### `public sealed class PerformanceMonitoringMiddleware`

#### `public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, PerformanceMetrics metrics, IOptions<PerformanceMonitoringOptions> options)`
**Purpose**: Creates the middleware. The options value is read once here (`options.Value`), so later changes to the configuration are not seen until the application restarts.
**Parameters**:
- `next` (`RequestDelegate`): The next middleware in the pipeline.
- `logger` (`ILogger<PerformanceMonitoringMiddleware>`): Logger for request start, finish, error, and slow-request messages.
- `metrics` (`PerformanceMetrics`): The store that receives each completed request's metric.
- `options` (`IOptions<PerformanceMonitoringOptions>`): Source of `SlowRequestThresholdMs`.
**Throws**: `ArgumentNullException` if any parameter is `null`.

---

#### `public async Task InvokeAsync(HttpContext context)`
**Purpose**: Runs the rest of the pipeline and records a `RequestMetric` for the request. The steps are:
1. Starts a `Stopwatch` and reads `GC.GetTotalMemory(false)`.
2. Logs an `Information` message for the start of the request.
3. Awaits `next(context)`. If it throws, logs an `Error` with the exception and rethrows.
4. In `finally`, stops the timer, takes a second memory reading, and builds a `RequestMetric` from the path, method, current response status code, elapsed milliseconds, memory delta, and `DateTime.UtcNow`.
5. Calls `PerformanceMetrics.RecordRequest`.
6. Logs a `Warning` if the elapsed time is greater than `SlowRequestThresholdMs`.
7. Logs an `Information` message for the end of the request.

**Parameters**:
- `context` (`HttpContext`): The HTTP context for the current request.
**Returns**: A `Task` that completes when the rest of the pipeline completes.
**Throws**: `ArgumentNullException` if `context` is `null`. Exceptions from downstream middleware are logged and rethrown unchanged. The metric is still recorded.

---

### `public sealed class PerformanceMetrics`
A singleton (registered with `AddSingleton<PerformanceMetrics>()` in `Program.cs`) that holds recent `RequestMetric` values in a `ConcurrentQueue<RequestMetric>`. Its size is bounded by `MaxMetrics`.

#### `public PerformanceMetrics(ILogger<PerformanceMetrics> logger, IOptions<PerformanceMonitoringOptions> options)`
**Purpose**: Creates the store. Like the middleware, it reads `options.Value` once.
**Parameters**:
- `logger` (`ILogger<PerformanceMetrics>`): Logger used by `LogSummary`.
- `options` (`IOptions<PerformanceMonitoringOptions>`): Source of `MaxMetrics`, `RecentMetricsCount`, and `SlowRequestThresholdMs`.
**Throws**: `ArgumentNullException` if any parameter is `null`.

---

#### `public void RecordRequest(RequestMetric metric)`
**Purpose**: Adds a metric to the queue. After enqueuing, it removes the oldest entries until the count is no greater than `MaxMetrics`.
**Parameters**:
- `metric` (`RequestMetric`): The metric to record. The method does not check for `null`. A `null` value is stored as-is and will later cause a `NullReferenceException` in `GetAverageDurationMs` and `LogSummary`.
**Throws**: None.

---

#### `public IEnumerable<RequestMetric> GetRecentMetrics(int count = 100)`
**Purpose**: Returns the most recent metrics in the order they were recorded (oldest first among the returned items).
**Parameters**:
- `count` (`int`, default `100`): The number of metrics to return. If `count` is `0` or negative, the value of `RecentMetricsCount` is used instead.
**Returns**: An `IEnumerable<RequestMetric>`. The `TakeLast` call is deferred, so the snapshot is taken when the sequence is enumerated, not when this method is called. Call `ToList()` if you need a stable copy.
**Throws**: None.

---

#### `public double GetAverageDurationMs(string? path = null)`
**Purpose**: Returns the average `DurationMs` across all metrics currently held in the buffer. If `path` is given, only metrics whose `Path` is an exact, case-sensitive string match are included.
**Parameters**:
- `path` (`string?`, optional): The exact request path to filter on, such as `/api/configurations`. When `null`, every metric is included.
**Returns**: A `double` with the average duration in milliseconds. Returns `0` when no metrics match. A `0` result can therefore mean "no data" as well as "0 ms".
**Throws**: None.

---

#### `public void LogSummary()`
**Purpose**: Writes one `Information` message with the average duration, average memory use in MB (two decimal places), and count of slow requests. It uses the last `RecentMetricsCount` metrics.
**Behavior**: Does nothing when no metrics have been recorded.
**Throws**: None.

---

### `public sealed class RequestMetric`
One completed request. All properties have public setters.

- `public string Path` - The raw request path (`context.Request.Path.Value`), not a route template. Empty if the path is `null`.
- `public string Method` - The HTTP method, such as `GET` or `POST`.
- `public int StatusCode` - The response status code at the time the middleware's `finally` block runs. See the pitfalls below.
- `public long DurationMs` - Elapsed time for the rest of the pipeline in whole milliseconds.
- `public long MemoryUsedBytes` - Difference in `GC.GetTotalMemory(false)` before and after the request. It can be negative. See the pitfalls below.
- `public DateTime Timestamp` - The time the metric was created, in UTC. This is the request completion time, not the start time.

`RequestMetric` overrides `ToString()` to return a single line with every property, for example `PerformanceMonitoringMiddleware { Path = /api/health, Method = GET, StatusCode = 200, DurationMs = 3, MemoryUsedBytes = 1024, Timestamp = ... }`.

---

### `public sealed class PerformanceMonitoringOptions`
Bound from the root-level `PerformanceMonitoring` configuration section (see [Configuration](#configuration)). Each property has a `[Range]` attribute, and `PerformanceMonitoringOptionsValidator` runs at startup.

| Property | Type | Default | Validation | Purpose |
|---|---|---|---|---|
| `SlowRequestThresholdMs` | `int` | `500` | `>= 0` | Requests slower than this are logged as warnings. |
| `MaxMetrics` | `int` | `1000` | `>= 1` | Maximum number of metrics kept in memory. |
| `RecentMetricsCount` | `int` | `100` | `>= 1` | Default count for `GetRecentMetrics` and the window used by `LogSummary`. |

---

## Configuration

The options are bound in `Program.cs` from the root-level `PerformanceMonitoring` section:

```json
"PerformanceMonitoring": {
  "SlowRequestThresholdMs": 500,
  "MaxMetrics": 1000,
  "RecentMetricsCount": 100
}
```

Note that this section is **not** under `DotnetConfigServer:`. The `DotnetConfigServerOptions.EnablePerformanceMonitoring` flag (default `true`, present in `appsettings.example.json`) is not read anywhere in the code. `Program.cs` always registers the middleware with `app.UseMiddleware<PerformanceMonitoringMiddleware>()`. Turning the flag off has no effect.

## Usage

### Example 1: Registering the middleware

The middleware is already registered in `Program.cs`, between `RequestLoggingMiddleware` and `RateLimitingMiddleware`:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
```

The position matters. `ErrorHandlingMiddleware` is outside this middleware, so exceptions pass through its `finally` block before they are converted into a response. `RateLimitingMiddleware` is inside it, so any status code it sets is visible when the metric is recorded.

### Example 2: Reading metrics from a diagnostics endpoint

`PerformanceMetrics` is a singleton, so it can be injected into any endpoint or service:

```csharp
app.MapGet("/diagnostics/performance", (PerformanceMetrics metrics) =>
{
    var recent = metrics.GetRecentMetrics(20).ToList();

    return Results.Ok(new
    {
        AverageDurationMs = metrics.GetAverageDurationMs(),
        ConfigurationsListAverageMs = metrics.GetAverageDurationMs("/api/configurations"),
        Recent = recent
    });
});
```

No endpoint like this is included in the codebase, although `CHANGELOG.md` mentions a performance metrics endpoint. If you add one, protect it with authorization. The output includes every request path and its timing.

### Example 3: Logging a periodic summary

`LogSummary` can be called from a background worker to write a summary to the logs at a fixed interval:

```csharp
public sealed class PerformanceSummaryWorker : BackgroundService
{
    private readonly PerformanceMetrics _metrics;

    public PerformanceSummaryWorker(PerformanceMetrics metrics)
    {
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _metrics.LogSummary();
        }
    }
}
```

## Notes

- **Thread safety**: `PerformanceMetrics` uses a `ConcurrentQueue<RequestMetric>`, so `RecordRequest` and the read methods can be called from concurrent requests. The size limit is enforced after each enqueue, so under heavy concurrency the count can briefly exceed `MaxMetrics`. It never grows without bound.
- **Memory is approximate**: `GC.GetTotalMemory(false)` measures the managed heap for the whole process, not for one request. The delta includes allocations made by other concurrent requests and background work. A garbage collection during the request can make it negative. Treat `MemoryUsedBytes` as a rough signal for spotting heavy requests, not as an allocation profile.
- **Logging volume**: Each request writes two `Information` messages from this middleware. `RequestLoggingMiddleware` also logs each request, so the logs contain some duplicate information. The memory value in the slow-request warning uses integer division by 1 MB, so it shows `0` for requests that allocate less than 1 MB.
- **Options are read once**: Both the middleware and `PerformanceMetrics` take `IOptions<T>` and copy `.Value` in their constructors. Configuration changes require a restart to take effect.

## Common Pitfalls

- **Failed requests can be recorded with a success status.** The middleware records `context.Response.StatusCode` in its `finally` block, before `ErrorHandlingMiddleware` (outside it) has set the error status. If the downstream code throws before the response has started, the metric usually shows `200`. Look for `Error` log entries from this middleware to find failures. To record the final status, register this middleware outside `ErrorHandlingMiddleware` instead.
- **`Path` is the raw URL, not the route.** `/api/configurations/42` and `/api/configurations/43` are separate values, and `GetAverageDurationMs(path)` matches only one exact path. To get per-endpoint averages, group `GetRecentMetrics()` results by a normalized path in your own code, and treat the path as user-controlled input when you show it.
- **`GetAverageDurationMs` returns `0` when there is no data.** An empty buffer and a buffer of very fast requests give the same result. Check for data first with `GetRecentMetrics().Any()` if the difference matters, and don't rely on it in alerts without that check.
