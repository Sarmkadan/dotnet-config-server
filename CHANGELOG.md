# Changelog

All notable changes to Dotnet Config Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.2] - 2026-03-20

### Fixed
- Fix hot reload not triggering when only nested config values change
- Added regression test for the fix

## [2.0.0] - 2026-03-18

### Added
- Multi-stage Docker image with .NET 10 runtime optimization
- Docker Compose orchestration with SQL Server, health checks, and volume management
- Production-ready HEALTHCHECK directive in Dockerfile with configurable intervals and timeouts
- Comprehensive migration guide from v1.x to v2.0 with breaking changes documentation
- Enhanced deployment documentation covering Docker, Kubernetes, and cloud platforms
- Layer caching optimization for faster builds in CI/CD pipelines

### Changed
- Dockerfile refactored for better build performance and layer caching
- Docker Compose configuration now includes automatic health checks for all services
- API versioning and response envelope structure updated for consistency
- Deployment guidelines now emphasize containerization as the primary deployment method

### Fixed
- Container startup time optimized by reducing SDK layer size
- Health check endpoint timeout configuration standardized across environments

### Security
- Docker base images pinned to specific versions for supply chain security
- SQL Server container password complexity enforced in production profiles
- Environment variable management documented for secrets handling

## [1.0.0] - 2025-04-22

### Added
- Configuration export in JSON, YAML, and XML formats
- Batch configuration import with per-key error reporting
- Configuration search and filtering by key prefix, environment, and application
- Diff viewer with side-by-side and unified display modes
- Performance metrics endpoint backed by `PerformanceMonitoringMiddleware`
- Health check enhancements: database reachability and cache status
- Configuration preview mode for staged-but-unpublished changes

### Changed
- Stabilised public API surface; all endpoints under `/api/v1`
- Improved OpenAPI/Swagger documentation with request/response examples
- Optimised EF Core queries for configurations with large key sets
- Enhanced error responses with actionable troubleshooting details

### Fixed
- Race condition when two requests updated the same key concurrently
- Audit log timestamps were stored in local time instead of UTC
- Webhook retry delay calculation was off by one interval on first retry
- Memory leak in `MemoryCacheService` on cache eviction callback

### Security
- AES-256 encryption enforced for all keys marked `isEncrypted`
- Input validation on every API endpoint
- HTTPS redirect enforced in production profile

## [0.3.0] - 2025-03-17

### Added
- Webhook subscription management with HMAC-SHA256 signature verification
- Automatic retry worker for failed webhook deliveries (`WebhookRetryWorker`)
- Configuration versioning: create, publish, archive, and rollback versions
- Version diff generation comparing any two versions of a configuration
- Automatic snapshot on every configuration publish
- Rate limiting middleware with per-IP request throttling
- In-memory caching with configurable TTL (`MemoryCacheService`)
- Event bus for internal domain events

### Changed
- Refactored repository layer to use the generic `BaseRepository<T>`
- Improved service-layer separation; controllers no longer access the DbContext directly
- Updated API response envelopes for consistency across all endpoints

### Fixed
- Encoding issue with special characters in configuration values
- Concurrent access errors when the repository was queried under load

## [0.2.0] - 2025-02-24

### Added
- AES-256 encryption service with automatic key generation and rotation
- Audit logging for all create, update, and delete operations
- Application-level grouping of configurations
- Multi-environment support: Development, Staging, Production (and custom labels)
- Structured logging via Serilog with console and file sinks
- Error handling middleware returning RFC 7807 problem-detail responses
- Request logging middleware with correlation IDs

### Changed
- Upgraded to .NET 10.0
- Refactored controller actions to delegate business logic to service classes
- Database schema extended with `EncryptionKeys`, `AuditLogs`, and `WebhookSubscriptions` tables

### Fixed
- CORS configuration was not applied to preflight requests
- JSON serialisation of nullable properties produced incorrect output

## [0.1.0] - 2025-01-20

### Added
- Initial project scaffold with ASP.NET Core Web API on .NET 10
- SQL Server integration via Entity Framework Core
- Configuration and ConfigurationKey CRUD endpoints
- Application and environment management endpoints
- OpenAPI/Swagger UI via Swashbuckle
- Basic health check endpoint (`/health`)
- Dockerfile and docker-compose.yml for local development
- MIT licence, README, CONTRIBUTING, and CODE_OF_CONDUCT

---

## Version Numbering

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Reporting Issues

Found a bug? Please report it on [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues).

Include the version number, steps to reproduce, expected and actual behaviour, and environment details.

## Contributing

We welcome contributions! See [CONTRIBUTING](CONTRIBUTING.md) for guidelines.

---

Built by [Vladyslav Zaiets](https://sarmkadan.com)
