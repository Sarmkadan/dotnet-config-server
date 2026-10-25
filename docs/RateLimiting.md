# Rate Limiting

Rate limiting protects the configuration server from abuse and runaway clients by capping how many requests a single caller can make within a time window. The feature is made of two parts:

- **`RateLimitingMiddleware`** (`Middleware/RateLimitingMiddleware.cs`) - the ASP.NET Core middleware that identifies the caller, asks the counter store whether the request is allowed, sets the rate-limit response headers, and short-circuits rejected requests with `429 Too Many Requests`.
- **Counter stores** (`Middleware/RateLimiting/`) - pluggable backends that hold the per-client counters. `IRateLimitStore` is the abstraction, `InMemoryRateLimitStore` keeps counters in process memory, and `DistributedRateLimitStore` keeps them in an `IDistributedCache` so that several application instances can share one limit.

The algorithm is a **fixed window** with a window length of one minute. Each client key gets a counter that resets when its window ends.

## Configuration

Settings are read from the `DotnetConfigServer:RateLimit` section, bound to `RateLimitOptions` (`Models/DotnetConfigServerOptions.cs`).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequestsPerMinute` | `int` | `100` | Maximum requests per client within one one-minute window. Declared range: 1-10000. |
| `RetryAfterSeconds` | `int` | `60` | Minimum value of the `Retry-After` header on a `429` response. Declared range: 1-3600. |
| `EnableRateLimiting` | `bool` | `true` | When `false`, the middleware passes every request straight through and sets no rate-limit headers. |
| `RateLimitExemptPaths` | `string[]` | empty | Present in the options class and in `appsettings.example.json`, but **not read by the middleware**. Setting it has no effect today. |
| `UseDistributedStore` | `bool` | `false` | When `true`, counters are kept in `IDistributedCache` and `DistributedRateLimitStore` is registered instead of `InMemoryRateLimitStore`. |

Example `appsettings.json` section:

```json
{
  "DotnetConfigServer": {
    "RateLimit": {
      "RequestsPerMinute": 100,
      "RetryAfterSeconds": 60,
      "EnableRateLimiting": true,
      "UseDistributedStore": false
    }
  }
}
```

Environment variables use the usual `__` separator for nested keys, for example `DotnetConfigServer__RateLimit__RequestsPerMinute=300`.

Note that `[Range]` attributes on `RateLimitOptions` only describe the intended bounds. The middleware does not clamp values, so the bounds are enforced only where the options object is validated.

## Client Identity

Each request is mapped to a client key by `RateLimitingMiddleware` before the counter is consulted:

1. If the request has a non-blank `X-API-Key` header, the key is `apikey:{value}`.
2. Otherwise the key is `ip:{remote address}`, or `ip:unknown` when the remote address is not available.

Keying by API key means that clients behind one NAT or proxy do not share a single IP-based bucket. The header value is used as supplied: the rate limiter does not check whether the key is valid. A caller that sends a different `X-API-Key` on every request gets a new bucket every time, so the limit only holds for keys the caller cannot vary. Behind a reverse proxy, the IP fallback depends on forwarded headers being configured for the host, which this middleware does not handle itself.

## Response Headers

Every request that reaches the middleware with rate limiting enabled receives these headers, whether it is allowed or rejected:

| Header | Meaning |
|--------|---------|
| `X-RateLimit-Limit` | Configured maximum requests per window (`RequestsPerMinute`). |
| `X-RateLimit-Remaining` | Requests left in the current window. Never negative. |
| `X-RateLimit-Reset` | Unix timestamp (seconds, UTC) when the current window ends. |

A rejected request additionally receives:

| Header | Meaning |
|--------|---------|
| `Retry-After` | Seconds to wait before retrying. The value is the larger of `RetryAfterSeconds` and the time remaining until the window resets, rounded up. |

The body of a rejected request is JSON:

```json
{ "error": "Rate limit exceeded" }
```

Rejected requests do not consume a slot in the window, so a client that keeps retrying does not push its own reset time further away.

## API

### `RateLimitingMiddleware`
**Purpose**: Constructor for the middleware. It is created by the ASP.NET Core pipeline (`app.UseMiddleware<RateLimitingMiddleware>()`).
**Parameters**:
- `next` (`RequestDelegate`): The next middleware in the pipeline.
- `options` (`IOptions<DotnetConfigServerOptions>`): Application options. The `RateLimit` section is read once, at construction.
- `logger` (`ILogger<RateLimitingMiddleware>`): Logger for rejections and diagnostics.
- `store` (`IRateLimitStore?`, optional): Counter store. When no store is supplied through dependency injection, a new `InMemoryRateLimitStore` is created.
**Throws**: `ArgumentNullException` if `next`, `options`, or `logger` is `null`.

---

### `public async Task InvokeAsync(HttpContext context)`
**Purpose**: Resolves the client key, evaluates the limit, and either forwards the request to the next middleware or returns `429 Too Many Requests`.
**Parameters**:
- `context` (`HttpContext`): The HTTP context for the current request.
**Returns**: A `Task` representing the asynchronous operation.
**Throws**: `ArgumentNullException` if `context` is `null`.

---

### `public interface IRateLimitStore`
**Purpose**: Abstraction over the storage of per-client fixed-window counters. Implementations decide whether counters are local to one process or shared across instances.

### `Task<RateLimitDecision> EvaluateAsync(string clientKey, int limit, TimeSpan window, CancellationToken cancellationToken = default)`
**Purpose**: Registers one request attempt for `clientKey` in the current fixed window and reports whether it is allowed.
**Parameters**:
- `clientKey` (`string`): Stable identifier for the caller (`apikey:...` or `ip:...`).
- `limit` (`int`): Maximum requests allowed within `window`.
- `window` (`TimeSpan`): Length of the fixed window.
- `cancellationToken` (`CancellationToken`): Cancels the store operation.
**Returns**: A `RateLimitDecision` describing the outcome.
**Throws**: `ArgumentException` if `clientKey` is null or empty, or if `limit` is not positive.

---

### `public readonly record struct RateLimitDecision(bool IsAllowed, int Limit, int Remaining, DateTimeOffset ResetsAtUtc)`
**Purpose**: The result of a single `EvaluateAsync` call. It holds what the middleware needs to write the response headers.
**Members**:
- `bool IsAllowed`: `true` when the request is within the limit.
- `int Limit`: The configured maximum for the window.
- `int Remaining`: Requests left in the window, never negative.
- `DateTimeOffset ResetsAtUtc`: The instant the current window ends.
- `TimeSpan RetryAfter` (get-only): Time from now until `ResetsAtUtc`, never negative.

---

### `public sealed class InMemoryRateLimitStore : IRateLimitStore`
**Purpose**: Process-local store backed by a `ConcurrentDictionary<string, ...>`. Each client key has its own counter guarded by a lock.
**Behavior**:
- A window starts at the first request after the previous window expired. Windows are not aligned to clock minutes.
- Entries are never removed, so memory grows with the number of distinct client keys seen since the process started.
- With N instances behind a load balancer, the effective limit for one client is up to N times `RequestsPerMinute`.
**Throws**: `ArgumentException` as described for `EvaluateAsync`.

---

### `public sealed class DistributedRateLimitStore : IRateLimitStore`
**Purpose**: Store that keeps counters in the registered `IDistributedCache`, so instances that share the cache share one limit.

### `public DistributedRateLimitStore(IDistributedCache cache)`
**Purpose**: Creates the store over the given cache.
**Parameters**:
- `cache` (`IDistributedCache`): Shared cache backend used for the counters.
**Throws**: `ArgumentNullException` if `cache` is `null`.

**Behavior**:
- Each client's state is stored under the key `ratelimit:{clientKey}` as JSON (`WindowStart`, `Count`), with an absolute expiration equal to the end of the window.
- `IDistributedCache` has no atomic increment, so the store performs a read, increment, and write. Under heavy concurrent load from several instances, two requests can read the same count and both be admitted. The overshoot is small, but the limit is not strict. For a strict limit, use a cache with a server-side atomic increment (for example a Redis Lua script) at the infrastructure layer.
- The raw API key is part of the cache key, so it is visible to anyone who can inspect the cache backend.

## Usage

### Example 1: Registering the Middleware

The middleware is already registered in `Program.cs`, after `PerformanceMonitoringMiddleware`:

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
```

### Example 2: Choosing a Store

The store is chosen at startup from `RateLimit:UseDistributedStore`:

```csharp
var rateLimitOptions = builder.Configuration
    .GetSection("DotnetConfigServer:RateLimit")
    .Get<RateLimitOptions>() ?? new RateLimitOptions();

if (rateLimitOptions.UseDistributedStore)
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IRateLimitStore, DistributedRateLimitStore>();
}
else
{
    builder.Services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
}
```

`AddDistributedMemoryCache()` only registers an in-process cache if no `IDistributedCache` is registered yet. Its counters are therefore **not** shared across instances. To share a limit between instances, register a shared implementation (for example Redis or SQL Server) before this code runs.

### Example 3: Using the Store Directly

The store can be used outside the middleware, for example in a background job or a custom filter:

```csharp
IRateLimitStore store = new InMemoryRateLimitStore();

var decision = await store.EvaluateAsync(
    clientKey: "ip:203.0.113.10",
    limit: 5,
    window: TimeSpan.FromMinutes(1));

if (!decision.IsAllowed)
{
    Console.WriteLine($"Retry in {decision.RetryAfter.TotalSeconds:F0} s");
}
```

### Example 4: Handling a Rejected Request in a Client

```csharp
var response = await httpClient.GetAsync("/api/configurations");

if (response.StatusCode == HttpStatusCode.TooManyRequests)
{
    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
    await Task.Delay(retryAfter);
}
```

### Example 5: Disabling Rate Limiting

For local development or tests, set the flag to `false`. The middleware then passes every request through without setting headers:

```json
{
  "DotnetConfigServer": {
    "RateLimit": {
      "EnableRateLimiting": false
    }
  }
}
```
