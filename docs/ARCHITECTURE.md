# Architecture

This document describes how Dotnet Config Server is actually put together - the modules that exist in the codebase, why they are shaped the way they are, and where the deliberate trade-offs live. If the doc and the code disagree, the code wins; please fix the doc.

## Overview

Dotnet Config Server is a single ASP.NET Core (.NET 10) web application backed by SQL Server via Entity Framework Core. It stores configurations per application/environment, versions them, encrypts sensitive keys, tracks diffs and audit history, and notifies subscribers of changes via webhooks.

There is exactly one deployable unit - one csproj (`dotnet-config-server.csproj`). Layering is done with folders and interfaces, not with separate assemblies. That is intentional: for a service this size, project-per-layer adds build ceremony without stopping anyone from taking a shortcut. The layering rules are enforced by convention and code review instead.

```
HTTP request
  → Middleware pipeline (error handling → request logging → perf monitoring → rate limiting)
  → Controller (Controllers/)
  → Service (Services/)          ← business rules, encryption, versioning
  → Repository (Repositories/)   ← EF Core queries
  → ApplicationDbContext (Data/) ← SQL Server

Side effects fan out through the in-process EventBus (Events/)
and two hosted BackgroundServices (BackgroundWorkers/).
```

## Module breakdown

| Folder | Role |
|--------|------|
| `Controllers/` | Thin REST endpoints (`api/v1/...`): applications, configurations, versions, rollback, diff viewer, change requests, validation rules, batch operations, webhooks, audit logs, health |
| `Services/` | Business logic. Interface-per-service (`IConfigurationService`, `IEncryptionService`, `IVersioningService`, `IWebhookService`, ...) |
| `Repositories/` | Data access. Generic `IRepository<T>`/`BaseRepository<T>` plus per-aggregate repositories with specific queries |
| `Data/` | `ApplicationDbContext` - entity mappings, indexes, relationships |
| `Models/` | EF entities and request/response DTOs, plus `DotnetConfigServerOptions` (validated options) |
| `Events/` | `IEventBus` + in-memory `EventBus`, domain events, `ConfigurationEventHandlers` |
| `BackgroundWorkers/` | `ConfigurationSyncWorker` (hourly archival of versions older than 30 days), `WebhookRetryWorker` (retries failed deliveries with a retry/age cap) |
| `Middleware/` | `ErrorHandlingMiddleware`, `RequestLoggingMiddleware`, `PerformanceMonitoringMiddleware`, `RateLimitingMiddleware` |
| `Caching/` | `ICacheService`, `MemoryCacheService`, `CacheKeyGenerator` |
| `Infrastructure/` | `ServiceExtensions` - the DI composition root helpers (`AddDataServices`, `AddBusinessServices`, `AddWebhookClient`, `AddSwaggerConfiguration`) |
| `Integration/` | `ExternalApiClient` (typed HttpClient) and `ApiResponseTransformer` |
| `Exceptions/` | Exception hierarchy rooted at `DotnetConfigServerException` (`NotFoundException`, `ValidationException`, `ConcurrencyException`, ...) mapped to status codes by `ErrorHandlingMiddleware` |
| `Filters/`, `Formatters/`, `Utilities/`, `Common/` | Model-validation action filter, configuration export, extension helpers, constants and enums |

Tests live in `tests/`, BenchmarkDotNet benchmarks in `benchmarks/`, client-side usage samples in `examples/`.

## Key design decisions

### Interface-per-service with constructor injection
Every service and repository is consumed through an interface registered in `Infrastructure/ServiceExtensions.cs` (plus a few registrations directly in `Program.cs`). Rationale: unit tests mock at the interface seam, and swapping an implementation (e.g. a distributed cache behind `ICacheService`) is a one-line DI change. Trade-off: some interfaces have exactly one implementation and probably always will; we accept the small ceremony for uniformity.

### Repository pattern on top of EF Core
EF Core is already a repository/unit-of-work, so wrapping it is a debatable call. We do it anyway because (a) query logic for each aggregate lives in one named place (`GetFailedDeliveriesAsync`, `GetOlderThanAsync`, ...) instead of being scattered through services, and (b) services can be tested without an in-memory DbContext. Trade-off: pass-through methods on `BaseRepository<T>` and the risk of leaking `IQueryable`-shaped decisions into repositories - kept in check by returning materialized lists.

### In-memory event bus, not a message broker
`EventBus` is a `ConcurrentDictionary<Type, List<Delegate>>`. `Program.cs` subscribes `ConfigurationEventHandlers` to the configuration lifecycle events (`ConfigurationCreatedEvent`, `ConfigurationUpdatedEvent`, `ConfigurationKeyChangedEvent`, `ConfigurationDeletedEvent`); each dispatch creates a DI scope so handlers get scoped services (DbContext etc.).

Rationale: the events decouple "config changed" from "invalidate cache / write audit / queue webhook" without forcing a broker on every deployment. Trade-off: events are lost on process crash and are not shared between instances. That is acceptable because the durable side effect - webhook delivery - is persisted as `WebhookDelivery` rows and re-driven by `WebhookRetryWorker`, so the database, not the bus, is the source of truth for delivery state.

### Webhook delivery: write-ahead rows + retry worker
Webhook sends are recorded in the `WebhookDelivery` table. `WebhookRetryWorker` periodically picks up failed deliveries under the retry/age limits and re-sends them through `IWebhookService` (a typed `HttpClient` with a 30s timeout and a `DotnetConfigServer/1.0` user agent). This makes delivery at-least-once and safe with multiple app instances, at the cost of possible duplicate notifications - receivers must be idempotent.

### Encryption per configuration, keys in the database
`EncryptionService` does AES with PBKDF2-derived keys (sizes/iterations in `Common/Constants.cs`), supports per-configuration primary keys, key generation, rotation (`RotateKeyAsync`) and bulk re-encryption (`ReEncryptConfigurationAsync`). Key material lives in the `EncryptionKey` table. Trade-off: keys next to data means DB access compromises both; for stricter environments the seam to replace is `IEncryptionService` + `IEncryptionKeyRepository` (e.g. back them with a KMS/Key Vault).

### Change requests as an approval gate
`ChangeRequestService` implements a submit → approve/reject/cancel workflow over `ChangeRequest` entities so config changes can require a second pair of eyes before being applied (optionally applied immediately on approval).

### Custom middleware instead of framework equivalents
Error handling, request logging, perf timing, and rate limiting are hand-rolled middleware. `RateLimitingMiddleware` is a per-IP token bucket held in a `ConcurrentDictionary` - simple and dependency-free, but per-instance only (a client can get N× the limit behind a load balancer) and unaware of proxies unless forwarded headers are configured. `PerformanceMonitoringMiddleware` feeds a singleton `PerformanceMetrics` aggregate.

### Caching
`MemoryCacheService` wraps `IMemoryCache` behind `ICacheService` with keys built by `CacheKeyGenerator`. Instance-local by design; cache invalidation is driven by the event handlers. Multi-instance deployments will serve briefly-stale reads from other instances' caches until TTL expiry - considered acceptable for configuration data, and fixable by swapping the `ICacheService` implementation.

## Data flow: updating a configuration key

1. `PUT /api/v1/configurations/{id}/keys/...` hits `ConfigurationsController` after passing the middleware chain.
2. `ConfigurationService` validates, encrypts the value via `IEncryptionService` when the key is marked sensitive, persists through the repositories, and lets `VersioningService`/`DiffService` record version and diff data.
3. A `ConfigurationKeyChangedEvent` is published on the `IEventBus`.
4. `ConfigurationEventHandlers` (in a fresh DI scope) invalidates cache entries, writes audit rows, and queues webhook deliveries.
5. `WebhookRetryWorker` later re-drives any deliveries that failed.

Reads are the mirror image: controller → cache check → repository query → decrypt sensitive values → cache with TTL → respond.

## Extension points

- **`ICacheService`** - swap `MemoryCacheService` for Redis/distributed cache.
- **`IEventBus`** - swap the in-memory bus for a broker-backed one; subscribers in `Program.cs` don't change.
- **`IEncryptionService` / `IEncryptionKeyRepository`** - external key management.
- **`IRepository<T>`** - the DB seam, though EF Core specifics do shape the repositories.
- **Middleware pipeline** - cross-cutting behavior slots in at `Program.cs` without touching business code.
- **`DotnetConfigServerOptions`** - bound from the `DotnetConfigServer` config section with data-annotation validation at startup (`ValidateOnStart`), so bad config fails fast instead of at first use.

## Known limitations

- **No authentication/authorization.** `UseAuthorization()` is in the pipeline but no auth scheme is registered, and CORS is `AllowAll`. The API must sit behind a gateway or get JWT/API-key auth before facing anything untrusted.
- **In-memory event bus and cache are per-instance** (see trade-offs above).
- **Rate limiting is per-instance and per-IP** with no forwarded-header awareness.
- **Serilog config is code-first** in `Program.cs` (console + rolling file), not driven by `appsettings.json`.
- **SQL Server only** - `AddDataServices` calls `UseSqlServer` directly; another provider means touching that method and reviewing the migrations.
