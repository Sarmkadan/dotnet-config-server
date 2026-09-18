# dotnet-config-server

Centralized configuration server: ASP.NET Core 10 Web API with versioning, diffs, rollback, change requests, encryption, webhooks and audit logs on top of EF Core (SQL Server).

## Build

```bash
dotnet restore
dotnet build --configuration Release      # make build
dotnet run                                # make run (needs DB; see appsettings.example.json)
dotnet ef database update                 # make db-migrate
make docker-build && make docker-up       # Dockerfile + docker-compose.yml
```

## Test

```bash
dotnet test --configuration Release --verbosity normal   # make test
dotnet test --filter "FullyQualifiedName~ConfigurationService"
```

Tests: xUnit + FluentAssertions + Moq in `tests/dotnet-config-server.Tests/` (the root `Tests/` folder holds a stray extension test). Benchmarks: BenchmarkDotNet in `benchmarks/`.

## Lint / format

```bash
make lint      # dotnet build /p:TreatWarningsAsErrors=true
make format    # dotnet format (rules in .editorconfig)
```

## Layout

- `Program.cs` - entry point, all DI registrations, Serilog, middleware pipeline
- `Controllers/` - REST endpoints (Configurations, Versions, Rollback, ChangeRequests, Webhooks, Encryption, Diff, Batch, AuditLogs, Health)
- `Services/` - business logic; every service has an `I*` interface next to it
- `Repositories/` - EF Core data access, `BaseRepository`
- `Data/ApplicationDbContext.cs` - DbContext
- `Models/` - entities and `*Options` classes bound from `DotnetConfigServer:*` config sections
- `Middleware/` - error handling, request logging, performance, `RateLimiting/`
- `Caching/`, `Events/` (in-process event bus), `BackgroundWorkers/`, `Integration/`, `Infrastructure/` (ServiceExtensions, health checks)
- `Exceptions/`, `Filters/`, `Formatters/`, `Utilities/`, `Common/`
- `docs/` - per-class markdown docs, `ARCHITECTURE.md`; `examples/`, `k8s/`

## Conventions

- Namespace `DotnetConfigServer.<Folder>`, `#nullable enable` at top of each file, author header banner kept
- Interfaces prefixed `I`, private fields `_camelCase`, 4-space indent (enforced by `.editorconfig`)
- Services registered in `Program.cs`: singletons for cache/event bus/metrics, scoped for DB-backed services
- Options pattern: `Models/*Options.cs` bound from `appsettings.json` section `DotnetConfigServer`
- Logging via Serilog (`Log.Information`, injected `ILogger<T>`), structured messages
- `appsettings.json` is committed; secrets go in env vars / `appsettings.example.json` is the template
