# PerformanceMonitoringMiddleware

The `PerformanceMonitoringMiddleware` is an ASP.NET Core middleware component designed to track and report performance metrics for HTTP requests processed by the application. It records key request attributes such as path, method, status code, duration, and memory usage, then exposes this data for analysis. This is useful for identifying performance bottlenecks, monitoring trends, and ensuring the application meets performance targets.

## API

### `PerformanceMonitoringMiddleware`
**Purpose**: Constructor for the middleware. Initializes a new instance of the `PerformanceMonitoringMiddleware` class.
**Parameters**:
- `next` (`RequestDelegate`): The next middleware in the pipeline.
**Throws**: `ArgumentNullException` if `next` is `null`.

---

### `public async Task InvokeAsync(HttpContext context)`
**Purpose**: Processes an HTTP request, recording performance metrics before and after invoking the next middleware in the pipeline.
**Parameters**:
- `context` (`HttpContext`): The HTTP context for the current request.
**Returns**: A `Task` representing the asynchronous operation.
**Throws**: None.

---

### `public class PerformanceMetrics`
**Purpose**: A nested class containing aggregated performance metrics derived from recorded request data.
**Members**:
- `public double GetAverageDurationMs()`: Returns the average request duration in milliseconds.
- `public void LogSummary()`: Logs a summary of recent performance metrics to the configured logger.

---

### `public void RecordRequest(string path, string method, int statusCode, long durationMs, long memoryUsedBytes)`
**Purpose**: Records metrics for a single HTTP request.
**Parameters**:
- `path` (`string`): The request path.
- `method` (`string`): The HTTP method (e.g., GET, POST).
- `statusCode` (`int`): The HTTP status code returned by the request.
- `durationMs` (`long`): The request duration in milliseconds.
- `memoryUsedBytes` (`long`): The memory usage attributed to the request.
**Throws**: None.

---

### `public IEnumerable<RequestMetric> GetRecentMetrics()`
**Purpose**: Retrieves a collection of recently recorded request metrics.
**Returns**: An `IEnumerable<RequestMetric>` containing the most recent request data.
**Throws**: None.

---

### `public double GetAverageDurationMs()`
**Purpose**: Calculates the average duration of all recorded requests in milliseconds.
**Returns**: A `double` representing the average duration.
**Throws**: `InvalidOperationException` if no metrics have been recorded.

---

### `public void LogSummary()`
**Purpose**: Logs a summary of performance metrics, including average duration and memory usage.
**Throws**: None.

---

### `RequestMetric` Members
The following properties are part of the `RequestMetric` record, which represents a single recorded request:

- `public string Path` (**Purpose**: The request path.)
- `public string Method` (**Purpose**: The HTTP method.)
- `public int StatusCode` (**Purpose**: The HTTP status code.)
- `public long DurationMs` (**Purpose**: The request duration in milliseconds.)
- `public long MemoryUsedBytes` (**Purpose**: The memory usage in bytes.)
- `public DateTime Timestamp` (**Purpose**: The time at which the request was recorded.)

## Usage

### Example 1: Adding Middleware to the Pipeline
