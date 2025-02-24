# Changelog

All notable changes to Dotnet Config Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- Multi-region deployment support with data replication
- Advanced encryption key rotation with automatic scheduling
- Configuration snapshot comparison with visual diff
- Webhook delivery retry analytics and reporting
- Performance metrics endpoint with request profiling
- Batch operation status tracking and progress reporting
- Configuration import/export in JSON, YAML, and XML formats
- Audit log filtering and advanced search capabilities
- Health check enhancements with dependency status
- Configuration preview mode for staged changes

### Changed
- Improved webhook signature verification security
- Enhanced error messages with actionable troubleshooting info
- Optimized database queries for large configuration sets
- Updated middleware pipeline for better performance
- Refactored service layer for improved testability

### Fixed
- Fixed race condition in concurrent configuration updates
- Resolved memory leak in cache service
- Fixed encryption key validation edge cases
- Corrected webhook retry delay calculation
- Fixed audit log timestamp timezone handling

## [1.1.0] - 2026-04-20

### Added
- Configuration versioning with full history tracking
- Version comparison and diff generation
- Automatic configuration snapshots on changes
- Rollback to previous versions
- Webhook subscription management
- HMAC-SHA256 signature verification for webhooks
- Audit logging for all configuration changes
- Encryption key management UI
- Performance monitoring middleware
- Rate limiting with configurable thresholds
- In-memory caching with TTL
- Event-driven architecture with event bus
- Background workers for webhook delivery retry
- Configuration validation rules
- Multi-environment support (Dev, Staging, Prod)
- Application-level configuration grouping

### Changed
- Upgraded to .NET 10.0
- Improved API response formats for consistency
- Enhanced validation error messages
- Optimized database schema for performance
- Refactored controller logic into services

### Fixed
- Fixed CORS configuration for cross-origin requests
- Resolved encoding issues with special characters
- Fixed concurrent access to configuration repository
- Corrected JSON serialization of complex objects

## [1.0.0] - 2026-04-01

### Added
- Basic configuration CRUD operations
- RESTful API with OpenAPI/Swagger documentation
- Entity Framework Core integration with SQL Server
- Configuration key management
- Simple encryption for sensitive values
- Application and environment management
- Health check endpoint
- Structured logging with Serilog
- CORS support for client applications
- Basic error handling middleware
- Request logging middleware
- JSON configuration response formatting

### Changed
- Initial stable release

### Security
- AES-256 encryption for sensitive configuration values
- Input validation on all API endpoints
- HTTPS enforcement in production
- Connection string security best practices

## [0.3.0] - 2026-03-25

### Added
- Configuration export functionality
- Batch configuration operations
- Configuration search and filtering
- Improved swagger documentation
- Additional unit tests

### Fixed
- Database migration issues
- Configuration update concurrency

## [0.2.0] - 2026-03-10

### Added
- Webhook subscription endpoints
- Configuration diff service
- Audit log retrieval endpoints
- Versioning service implementation

### Changed
- Refactored repository implementations
- Improved service layer architecture

## [0.1.0] - 2026-02-20

### Added
- Initial project setup with .NET 10.0
- Database schema design
- Basic entity models
- Core service interfaces
- Initial API controller implementations
- Project documentation
- GitHub repository setup
- MIT License

---

## Unreleased

### Planned for v1.3.0
- Configuration import from external sources
- Real-time configuration updates via WebSocket
- Configuration templates and inheritance
- Advanced analytics dashboard
- Performance optimizations for large datasets
- GraphQL API support
- Configuration validation rules engine
- Custom notification channels (Slack, Teams, etc.)

### In Consideration
- Configuration change approval workflows
- Role-based access control (RBAC)
- Configuration testing and simulation
- Configuration deployment pipelines
- Integration with CI/CD platforms
- Multi-tenant support enhancements

## Version Numbering

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Release Schedule

- **LTS (Long Term Support)**: Every major version, 2 years of support
- **Regular**: Every 2 months
- **Security patches**: As needed

## Support Status

| Version | Status | Released | Support Until |
|---------|--------|----------|---------------|
| 1.2.0   | Current | 2026-05-04 | 2028-05-04 |
| 1.1.0   | Supported | 2026-04-20 | 2027-04-20 |
| 1.0.0   | LTS | 2026-04-01 | 2028-04-01 |
| 0.3.0 and earlier | Unsupported | - | - |

## Migration Guides

### From v1.1.0 to v1.2.0
- No breaking changes
- Run database migrations: `dotnet ef database update`
- New encryption keys will be generated automatically
- Existing configurations remain compatible

### From v1.0.0 to v1.1.0
- Database schema changes required
- Run migrations before updating
- New webhook system is backward compatible
- Existing configurations remain unchanged

## Reporting Issues

Found a bug? Please report it on [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)

Include:
- Version number
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details

## Contributing

We welcome contributions! See [CONTRIBUTING](CONTRIBUTING.md) for guidelines.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
