# HealthCheckController

The `HealthCheckController` exposes HTTP endpoints for reporting the health, readiness, and liveness of the dotnet-config-server instance. It delegates all evaluation to `IHealthCheckService` and only shapes the HTTP responses. It is intended for monitoring systems, load balancers, and orchestrator probes (for example, Kubernetes `livenessProbe` and `readinessProbe`).

The controller is mounted at `/HealthCheck` (route `[controller]`), without the `api/v1` prefix used by the other controllers.

## API

### `HealthCheckController`
The controller class exposing health endpoints. It is decorated with `[ApiController]` and `[Route("[controller]")]` and derives from `ControllerBase`.

**Constructor**
- `HealthCheckController(IHealthCheckService healthCheckService)`: Receives the health service via dependency injection. Throws `ArgumentNullException` if `healthCheckService` is null.

---

### `public async Task<IActionResult> Get()`
Returns the full health report, including the overall status and the result of every individual check.

**Route:** `GET /HealthCheck`

**Parameters**
- None.

**Response body** (`HealthReport`, serialized with camelCase property names)

| Property | Type | Description |
|----------|------|-------------|
| `status` | `string` | Overall status: `healthy` or `degraded`. |
| `timestamp` | `DateTime` (UTC) | Time at which the report was generated. |
| `uptime` | `number` | Seconds elapsed since the service instance started. |
| `processId` | `number` | Operating system process identifier. |
| `memoryMb` | `number` | Working set of the process, in megabytes. |
| `threadCount` | `number` | Number of threads in the process. |
| `checks` | `object` | Map of check name to `HealthCheck` result. |

Each entry in `checks` is a `HealthCheck` object:

| Property | Type | Description |
|----------|------|-------------|
| `status` | `string` | `healthy`, `warning`, or `unhealthy`. |
| `message` | `string` | Human-readable description of the result. |
| `metrics` | `object` | Check-specific metrics (may be empty). |

The check names produced by `HealthCheckService` are `cache`, `database`, `webhooks`, `snapshots`, and `memory`. A check is omitted when its dependency is not registered (for example, `cache` when no `ICacheService` is available), except `database` and `memory`, which are always reported.

**Return value**
- `200 OK` with the `HealthReport` body. The HTTP status does not change when the report is `degraded`; inspect `status` instead.

**Exceptions**
- Exceptions thrown by `GetHealthReportAsync` are not caught by this controller and are handled by the global `ErrorHandlingMiddleware`.

**Example response**

```json
{
  "status": "healthy",
  "timestamp": "2026-10-25T10:15:00Z",
  "uptime": 86400.5,
  "processId": 4312,
  "memoryMb": 182,
  "threadCount": 27,
  "checks": {
    "cache": {
      "status": "healthy",
      "message": "Cache operational. Hit rate: 92.50%",
      "metrics": { "hitRate": 0.925, "hits": 1850, "misses": 150, "size": 412 }
    },
    "database": {
      "status": "healthy",
      "message": "Database is reachable",
      "metrics": { "connected": true, "sizeMb": 64 }
    },
    "memory": {
      "status": "healthy",
      "message": "Memory usage is normal",
      "metrics": {}
    }
  }
}
```

---

### `public async Task<IActionResult> GetReady()`
Reports whether the service is ready to accept traffic. Intended for readiness probes.

**Route:** `GET /HealthCheck/ready`

**Parameters**
- None.

**Return value**
- `200 OK` with a JSON object `{ "status": "ready" }` when `IsReadyAsync` returns `true`.
- `200 OK` with `{ "status": "not_ready" }` when `IsReadyAsync` returns `false`.

The HTTP status is always `200 OK`. Callers must read the `status` field; a probe that only checks the HTTP status code will not detect a not-ready instance.

---

### `public async Task<IActionResult> GetAlive()`
Reports whether the process is running and able to respond. Intended for liveness probes.

**Route:** `GET /HealthCheck/alive`

**Parameters**
- None.

**Return value**
- `200 OK` with `{ "status": "alive" }` when `IsAliveAsync` returns `true`.
- `200 OK` with `{ "status": "dead" }` when `IsAliveAsync` returns `false`.

The HTTP status is always `200 OK`. The liveness check does not evaluate dependencies; it only confirms that the process is responding.

---

## Usage

```csharp
// Example 1: Check the overall health and inspect failing checks
var response = await client.GetAsync("/HealthCheck");
response.EnsureSuccessStatusCode();

var report = await response.Content.ReadFromJsonAsync<HealthReport>();

if (report?.Status != "healthy")
{
    foreach (var (name, check) in report!.Checks.Where(c => c.Value.Status != "healthy"))
    {
        Console.WriteLine($"{name}: {check.Status} - {check.Message}");
    }
}
```

```csharp
// Example 2: Gate traffic on readiness, using the response body rather than the status code
var readyResponse = await client.GetAsync("/HealthCheck/ready");
readyResponse.EnsureSuccessStatusCode();

var body = await readyResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
var isReady = body?["status"] == "ready";
```

```yaml
# Example 3: Kubernetes probes
livenessProbe:
  httpGet:
    path: /HealthCheck/alive
    port: 8080
readinessProbe:
  httpGet:
    path: /HealthCheck/ready
    port: 8080
```

Note that the Kubernetes example relies on the HTTP status code, which is always `200` for these endpoints. Use an `exec` probe or a sidecar that parses the body if the orchestrator must react to `not_ready`.

## Notes

- `HealthCheckController` is separate from the ASP.NET Core health check middleware registered in `Program.cs`. The endpoints `/health`, `/health/live`, and `/health/ready` are mapped by `MapHealthChecks` and use the `AddHealthChecks()` registrations (`process`, `config-store`, `encryption-keys`, and the config server check). They do not use this controller.
- `IsReadyAsync` returns `false` only if `GetHealthReportAsync` throws. `GetHealthReportAsync` sets the overall `status` to `degraded` for failing dependencies and does not set `unhealthy`, so `ready` remains `true` in normal failure modes such as a database outage. Use the `database` entry of `GET /HealthCheck` to detect dependency failures.
- `IsAliveAsync` always returns `true`, so `alive` reflects only that the process is serving requests.
- `GetHealthReportAsync` runs several dependency queries on each call, including a database query and webhook and snapshot lookups. Avoid calling `GET /HealthCheck` at high frequency; probes should use `/HealthCheck/alive` or `/HealthCheck/ready`, and `/HealthCheck/ready` also runs the full report.
- The health service is registered as scoped (`Program.cs`), so the controller receives a new instance per request.
- The endpoints are unauthenticated in the controller itself. Restrict external access at the reverse proxy or network layer if the report's process and dependency details should not be public.
