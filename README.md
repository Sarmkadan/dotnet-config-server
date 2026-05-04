# Dotnet Config Server

A comprehensive, production-grade centralized configuration server for .NET microservices with support for hot reload, encryption, versioning, diff tracking, and webhook notifications.

## Features

- **Centralized Configuration Management**: Manage configurations for multiple applications and environments in one place
- **Hot Reload Support**: Automatically reload configurations without restarting services
- **Encryption**: Secure sensitive configuration values with AES-256 encryption
- **Versioning**: Full version control with create, publish, archive, and rollback capabilities
- **Diff Tracking**: Compare versions and track changes between configurations
- **Webhook Notifications**: Receive real-time notifications when configurations change
- **Audit Logging**: Complete audit trail of all configuration changes
- **Key Management**: Secure encryption key rotation and management
- **RESTful API**: Comprehensive REST API with OpenAPI/Swagger documentation
- **CORS Support**: Built-in CORS handling for client applications
- **Health Checks**: Built-in health check endpoints

## Technology Stack

- **.NET 10.0**: Latest .NET framework
- **Entity Framework Core 10**: ORM for data access
- **SQL Server**: Relational database
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation

## Project Structure

```
dotnet-config-server/
├── Models/                      # Domain models
│   ├── Configuration.cs
│   ├── ConfigurationKey.cs
│   ├── ConfigurationVersion.cs
│   ├── WebhookSubscription.cs
│   ├── ConfigurationDiff.cs
│   ├── AuditLog.cs
│   ├── EncryptionKey.cs
│   └── Application.cs
├── Services/                    # Business logic layer
│   ├── IConfigurationService.cs
│   ├── ConfigurationService.cs
│   ├── IEncryptionService.cs
│   ├── EncryptionService.cs
│   ├── IVersioningService.cs
│   ├── VersioningService.cs
│   ├── IDiffService.cs
│   ├── DiffService.cs
│   ├── IWebhookService.cs
│   └── WebhookService.cs
├── Repositories/                # Data access layer
│   ├── IRepository.cs
│   ├── BaseRepository.cs
│   ├── ConfigurationRepository.cs
│   ├── RepositoryImplementations.cs
│   └── [Other repositories]
├── Data/                        # Entity Framework
│   └── ApplicationDbContext.cs
├── Controllers/                 # API endpoints
│   ├── ConfigurationsController.cs
│   └── VersionsController.cs
├── Exceptions/                  # Custom exceptions
│   └── ConfigurationException.cs
├── Common/                      # Shared utilities
│   ├── Constants.cs
│   └── Enums.cs
├── Infrastructure/              # DI and configuration
│   └── ServiceExtensions.cs
├── Program.cs                   # Application entry point
├── appsettings.json            # Configuration
└── README.md

```

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- SQL Server (LocalDB or full installation)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server
```

2. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DotnetConfigServerDb;Trusted_Connection=true;"
  }
}
```

3. Apply database migrations:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

The API will be available at `https://localhost:5001` and Swagger UI at `https://localhost:5001/swagger`.

## API Endpoints

### Configurations

- `POST /api/v1/configurations` - Create a new configuration
- `GET /api/v1/configurations/{id}` - Get a configuration by ID
- `GET /api/v1/configurations/application/{applicationId}` - Get all configurations for an application
- `PUT /api/v1/configurations/{id}` - Update a configuration
- `DELETE /api/v1/configurations/{id}` - Delete a configuration
- `GET /api/v1/configurations/{configurationId}/keys` - Get all keys in a configuration
- `POST /api/v1/configurations/{configurationId}/keys` - Add a key to a configuration

### Versions

- `GET /api/v1/configurations/{configurationId}/versions` - Get version history
- `GET /api/v1/configurations/{configurationId}/versions/active` - Get active version
- `POST /api/v1/configurations/{configurationId}/versions` - Create new version
- `POST /api/v1/configurations/{configurationId}/versions/{versionId}/publish` - Publish a version
- `POST /api/v1/configurations/{configurationId}/versions/{versionId}/archive` - Archive a version
- `POST /api/v1/configurations/{configurationId}/versions/{previousVersionId}/rollback` - Rollback to previous version
- `GET /api/v1/configurations/{configurationId}/versions/{fromVersionId}/diff/{toVersionId}` - Get diff between versions

## Encryption

Sensitive configuration values are encrypted using AES-256. The service automatically:

- Generates and manages encryption keys
- Rotates keys securely
- Encrypts/decrypts values transparently
- Validates key expiration

## Webhooks

Configure webhooks to receive notifications when configurations change:

1. Subscribe to configuration changes:
```http
POST /api/v1/configurations/{configurationId}/webhooks
Content-Type: application/json

{
  "name": "My Webhook",
  "url": "https://example.com/webhook",
  "verifySignature": true
}
```

2. Receive notifications with HMAC-SHA256 signature verification

## Audit Logging

All configuration changes are logged with:
- Timestamp
- User who made the change
- Type of action (create, update, delete, publish, etc.)
- Old and new values
- IP address and user agent

## Database Schema

The solution uses Entity Framework Core with the following main entities:

- **Applications**: Manage multiple applications
- **Configurations**: Configuration profiles per application
- **ConfigurationKeys**: Individual key-value pairs
- **ConfigurationVersions**: Version history
- **WebhookSubscriptions**: Webhook endpoints
- **WebhookDeliveries**: Delivery history
- **ConfigurationDiffs**: Change tracking
- **AuditLogs**: Audit trail
- **EncryptionKeys**: Key management

## Development

### Logging

The application uses Serilog for structured logging with:
- Console sink for development
- File sink with daily rolling intervals
- Structured data for better querying

Logs are stored in the `logs/` directory.

### Configuration

Application settings can be configured through:
- `appsettings.json` for shared settings
- `appsettings.Development.json` for development overrides
- Environment variables (override any setting)

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

## Contributing

This is an open-source project. Contributions are welcome through pull requests.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- CTO & Software Architect
