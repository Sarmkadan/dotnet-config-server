# Dotnet Config Server

[![Build](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

A production-grade centralized configuration server for .NET microservices with support for hot reload, encryption, versioning, diff tracking, and webhook notifications.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration Reference](#configuration-reference)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

## Overview

**Dotnet Config Server** is an enterprise-ready centralized configuration management solution designed for modern microservice architectures. It addresses the complexity of managing configurations across multiple applications, environments, and deployment stages by providing a single source of truth with advanced features like encryption, versioning, and real-time notifications.

### The Problem

In distributed systems, configuration management becomes increasingly complex:
- Multiple services need consistent configurations
- Environment-specific settings require careful management
- Sensitive data (API keys, connection strings) needs encryption
- Configuration changes must be audited for compliance
- Services need to react to configuration updates in real-time
- Rolling back a bad configuration should be instant
- Teams need to understand what changed and why

### The Solution

Dotnet Config Server provides:
1. **Centralized Management**: Single REST API for all configuration needs
2. **Type-Safe Access**: Strongly-typed configuration models
3. **Encryption by Default**: Automatic AES-256 encryption for sensitive values
4. **Complete Versioning**: Create, publish, archive, and rollback configurations
5. **Change Tracking**: Detailed diffs show exactly what changed
6. **Real-Time Notifications**: Webhooks alert services of configuration changes
7. **Audit Trail**: Complete history of all changes for compliance
8. **Hot Reload Support**: Update configurations without service restarts

## Features

### Core Configuration Management
- **Multi-Application Support**: Manage configurations for multiple applications simultaneously
- **Environment Isolation**: Separate configurations per environment (dev, staging, prod)
- **Centralized API**: RESTful API with OpenAPI/Swagger documentation
- **Configuration Validation**: Built-in validation with custom rules

### Security & Encryption
- **AES-256 Encryption**: Encrypt sensitive configuration values
- **Key Rotation**: Automatic encryption key rotation with version management
- **Key Expiration**: Automatic invalidation of old encryption keys
- **Access Control Ready**: Prepared for integration with authentication systems

### Versioning & History
- **Full Version Control**: Create, update, and manage configuration versions
- **Automatic Snapshots**: Each change creates a snapshot for rollback
- **Version Lifecycle**: Draft → Published → Archived → Deleted
- **Rollback Support**: Instant rollback to any previous version

### Change Tracking & Diffs
- **Visual Diffs**: See exactly what changed between versions
- **Change History**: Complete audit trail with timestamps
- **Comparison Tools**: Built-in diff engine for quick analysis
- **Change Notifications**: Immediate alerts on configuration changes

### Webhook System
- **Event Subscriptions**: Subscribe to configuration change events
- **HMAC Signatures**: Verify webhook authenticity with HMAC-SHA256
- **Retry Logic**: Automatic retry mechanism for failed deliveries
- **Batch Processing**: Efficient batch delivery of notifications

### Monitoring & Reliability
- **Health Checks**: Built-in health check endpoints
- **Performance Monitoring**: Request duration tracking and analysis
- **Rate Limiting**: Configurable rate limiting to prevent abuse
- **Structured Logging**: Comprehensive logging with Serilog

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          Client Services                         │
│  (Web, Mobile, API, Background Workers)                        │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                    HTTPS / REST API
                           │
        ┌──────────────────▼──────────────────┐
        │   Dotnet Config Server (Asp.Net)   │
        │                                     │
        │  ┌─────────────────────────────┐   │
        │  │    API Controllers          │   │
        │  │ - Configurations            │   │
        │  │ - Versions                  │   │
        │  │ - Webhooks                  │   │
        │  │ - Audit Logs                │   │
        │  └─────────────────────────────┘   │
        │                                     │
        │  ┌─────────────────────────────┐   │
        │  │   Business Services         │   │
        │  │ - Configuration Service     │   │
        │  │ - Encryption Service        │   │
        │  │ - Versioning Service        │   │
        │  │ - Webhook Service           │   │
        │  │ - Diff Service              │   │
        │  └─────────────────────────────┘   │
        │                                     │
        │  ┌─────────────────────────────┐   │
        │  │  Middleware Layer           │   │
        │  │ - Error Handling            │   │
        │  │ - Request Logging           │   │
        │  │ - Rate Limiting             │   │
        │  │ - Performance Monitoring    │   │
        │  └─────────────────────────────┘   │
        │                                     │
        │  ┌─────────────────────────────┐   │
        │  │  Caching & Events           │   │
        │  │ - Memory Cache              │   │
        │  │ - Event Bus                 │   │
        │  │ - Background Workers        │   │
        │  └─────────────────────────────┘   │
        │                                     │
        │  ┌─────────────────────────────┐   │
        │  │   Data Access Layer         │   │
        │  │ - Entity Framework Core     │   │
        │  │ - Repository Pattern        │   │
        │  └─────────────────────────────┘   │
        └──────────────────┬──────────────────┘
                           │
        ┌──────────────────▼──────────────────┐
        │   SQL Server Database               │
        │ - Configurations                     │
        │ - Versions & Snapshots               │
        │ - Encryption Keys                    │
        │ - Audit Logs                         │
        │ - Webhook Subscriptions              │
        └─────────────────────────────────────┘
                           │
                ┌──────────┴──────────┐
                │                     │
        ┌───────▼────────┐   ┌───────▼────────┐
        │  Webhook       │   │  External      │
        │  Endpoints     │   │  Systems       │
        │  (Client Code) │   │  (Analytics)   │
        └────────────────┘   └────────────────┘
```

### Layered Architecture

**Presentation Layer** (Controllers)
- `ConfigurationsController.cs` - Configuration CRUD operations
- `VersionsController.cs` - Version management
- `WebhooksController.cs` - Webhook subscriptions
- `AuditLogsController.cs` - Audit trail retrieval

**Business Logic Layer** (Services)
- `ConfigurationService` - Core configuration operations
- `VersioningService` - Version lifecycle management
- `EncryptionService` - AES-256 encryption/decryption
- `DiffService` - Configuration comparison
- `WebhookService` - Event notification delivery

**Data Access Layer** (Repositories)
- `BaseRepository<T>` - Generic repository base
- `ConfigurationRepository` - Configuration-specific queries
- Entity Framework Core for ORM

**Infrastructure**
- Dependency injection configuration
- Middleware setup and composition
- Caching (Memory cache)
- Event bus for async operations
- Background workers for maintenance tasks

## Installation

### Prerequisites

- **.NET 10.0 SDK** or later - [Download](https://dotnet.microsoft.com/download)
- **SQL Server** (LocalDB, Express, or Full Edition)
  - LocalDB: included with Visual Studio
  - Express: [Free download](https://www.microsoft.com/en-us/sql-server/sql-server-express)
  - Docker: `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=MyStrongPassword123!' mcr.microsoft.com/mssql/server`

### Method 1: Local Development Setup

```bash
# 1. Clone the repository
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server

# 2. Install dependencies
dotnet restore

# 3. Update connection string (if needed)
# Edit appsettings.json - DefaultConnection
# Default uses LocalDB: (localdb)\mssqllocaldb

# 4. Create and apply database
dotnet ef database update

# 5. Run the application
dotnet run

# 6. Open browser
# API: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
# Health Check: https://localhost:5001/health
```

### Method 2: Docker & Docker Compose

```bash
# Navigate to project directory
cd dotnet-config-server

# Build and run with compose
docker-compose up

# Application available at http://localhost:80
# Swagger UI at http://localhost:80/swagger
```

**docker-compose.yml** includes:
- SQL Server container (latest)
- Dotnet Config Server application
- Pre-configured networking and volumes

### Method 3: Kubernetes Deployment

```bash
# Create namespace
kubectl create namespace config-server

# Build Docker image
docker build -t dotnet-config-server:latest .

# Push to registry
docker tag dotnet-config-server:latest yourregistry/dotnet-config-server:latest
docker push yourregistry/dotnet-config-server:latest

# Deploy using Helm (see deployment documentation)
helm install config-server ./k8s/helm-chart -n config-server
```

## Usage Examples

### Example 1: Creating a Configuration

```csharp
using HttpClient client = new HttpClient();
client.BaseAddress = new Uri("https://localhost:5001");

// Create a new application
var appRequest = new { name = "OrderService", description = "Order processing service" };
var appResponse = await client.PostAsJsonAsync("/api/v1/applications", appRequest);
var appId = (await appResponse.Content.ReadAsAsync<dynamic>()).id;

// Create configuration for the application
var configRequest = new
{
    applicationId = appId,
    environment = "Production",
    description = "Production configuration"
};

var configResponse = await client.PostAsJsonAsync("/api/v1/configurations", configRequest);
var configId = (await configResponse.Content.ReadAsAsync<dynamic>()).id;

Console.WriteLine($"Created configuration: {configId}");
```

### Example 2: Adding Encrypted Configuration Keys

```csharp
// Add a sensitive database connection string (will be encrypted)
var keyRequest = new
{
    key = "Database:ConnectionString",
    value = "Server=prod-db.example.com;Database=Orders;...",
    isEncrypted = true,
    description = "Production database connection"
};

var keyResponse = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/keys",
    keyRequest);

// Add a regular configuration value
var settingRequest = new
{
    key = "Features:EnableNewCheckout",
    value = "true",
    isEncrypted = false,
    description = "Enable new checkout flow"
};

await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/keys",
    settingRequest);
```

### Example 3: Versioning and Publishing

```csharp
// Create a new version from the current configuration
var versionRequest = new { description = "Q2 2026 release with new features" };
var versionResponse = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/versions",
    versionRequest);
var versionId = (await versionResponse.Content.ReadAsAsync<dynamic>()).id;

// Get version details
var version = await client.GetAsync(
    $"/api/v1/configurations/{configId}/versions/{versionId}");
Console.WriteLine($"Version created with {version.keysCount} keys");

// Publish the version to make it active
await client.PostAsync(
    $"/api/v1/configurations/{configId}/versions/{versionId}/publish",
    null);

Console.WriteLine("Version published successfully");

// Archive old version
var oldVersionId = "...";
await client.PostAsync(
    $"/api/v1/configurations/{configId}/versions/{oldVersionId}/archive",
    null);
```

### Example 4: Comparing Versions (Diffs)

```csharp
// Get diff between two versions
var diffResponse = await client.GetAsync(
    $"/api/v1/configurations/{configId}/versions/{fromVersionId}/diff/{toVersionId}");

var diffs = await diffResponse.Content.ReadAsAsync<List<ConfigurationDiff>>();

foreach (var diff in diffs)
{
    Console.WriteLine($"Key: {diff.Key}");
    Console.WriteLine($"  Type: {diff.ChangeType}"); // Created, Modified, Deleted
    Console.WriteLine($"  Old: {diff.OldValue}");
    Console.WriteLine($"  New: {diff.NewValue}");
}
```

### Example 5: Setting Up Webhooks

```csharp
// Subscribe to configuration changes
var webhookRequest = new
{
    name = "OrderService Webhook",
    url = "https://order-service.example.com/config/webhook",
    description = "Notify on configuration changes",
    verifySignature = true,
    isActive = true
};

var webhookResponse = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/webhooks",
    webhookRequest);

var webhookId = (await webhookResponse.Content.ReadAsAsync<dynamic>()).id;
Console.WriteLine($"Webhook registered: {webhookId}");

// In your service, receive the webhook:
[HttpPost("/config/webhook")]
public async Task ReceiveConfigurationChange([FromBody] WebhookPayload payload)
{
    // Verify HMAC signature
    if (!VerifyWebhookSignature(payload, secret))
    {
        return Unauthorized();
    }

    // Process configuration change
    await _configurationClient.ReloadConfiguration();
    Console.WriteLine($"Configuration reloaded - Version: {payload.VersionId}");
}
```

### Example 6: Rollback Operations

```csharp
// List all versions to find one to rollback to
var versions = await client.GetAsync(
    $"/api/v1/configurations/{configId}/versions");
var versionList = await versions.Content.ReadAsAsync<List<ConfigurationVersion>>();

// Display version history
foreach (var v in versionList)
{
    Console.WriteLine($"{v.Version}: {v.Description} (Created: {v.CreatedAt})");
}

// Rollback to a specific version
var previousVersionId = "...";
var rollbackRequest = new { reason = "Reverting breaking configuration changes" };

await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/versions/{previousVersionId}/rollback",
    rollbackRequest);

Console.WriteLine("Rollback completed successfully");
```

### Example 7: Batch Operations

```csharp
// Import multiple configurations at once
var batchRequest = new
{
    configurations = new object[]
    {
        new { key = "Logging:Level", value = "Debug" },
        new { key = "Cache:Duration", value = "300" },
        new { key = "Api:Timeout", value = "30" }
    }
};

var response = await client.PostAsJsonAsync(
    $"/api/v1/batch/import/{configId}",
    batchRequest);

var result = await response.Content.ReadAsAsync<BatchOperationResult>();
Console.WriteLine($"Imported {result.SuccessCount} configurations");
if (result.FailureCount > 0)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  Error: {error}");
    }
}
```

### Example 8: Audit Log Retrieval

```csharp
// Get audit logs for a configuration
var auditResponse = await client.GetAsync(
    $"/api/v1/configurations/{configId}/audit-logs?pageSize=50&pageNumber=1");

var auditLogs = await auditResponse.Content.ReadAsAsync<PagedResult<AuditLog>>();

foreach (var log in auditLogs.Items)
{
    Console.WriteLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss} | {log.Action} | {log.User}");
    Console.WriteLine($"  Details: {log.Details}");
}
```

### Example 9: Configuration Export

```csharp
// Export configuration as JSON
var exportResponse = await client.GetAsync(
    $"/api/v1/configurations/{configId}/export?format=json");

var json = await exportResponse.Content.ReadAsStringAsync();
File.WriteAllText("config-backup.json", json);

// Export as YAML
var yamlResponse = await client.GetAsync(
    $"/api/v1/configurations/{configId}/export?format=yaml");

var yaml = await yamlResponse.Content.ReadAsStringAsync();
File.WriteAllText("config-backup.yaml", yaml);
```

### Example 10: Health Check & Status

```csharp
// Check server health
var healthResponse = await client.GetAsync("/health");

if (healthResponse.IsSuccessStatusCode)
{
    var health = await healthResponse.Content.ReadAsAsync<HealthCheckResult>();
    Console.WriteLine($"Status: {health.Status}");
    Console.WriteLine($"Database: {health.Components["database"]}");
    Console.WriteLine($"Cache: {health.Components["cache"]}");
}
else
{
    Console.WriteLine("Server is not healthy");
}
```

## API Reference

### Authentication
Currently no authentication is enforced. For production use, integrate with:
- Azure AD / OAuth2
- API Keys
- JWT Bearer tokens

Recommended: Use API Gateway (Azure API Management, AWS API Gateway) for authentication.

### Base URL
```
https://localhost:5001/api/v1
```

### Configurations

#### Create Configuration
```http
POST /configurations
Content-Type: application/json

{
  "applicationId": "uuid",
  "environment": "Production",
  "description": "Production settings"
}

Response: 201 Created
{
  "id": "uuid",
  "applicationId": "uuid",
  "environment": "Production",
  "createdAt": "2026-05-04T10:30:00Z",
  "keyCount": 0
}
```

#### Get Configuration
```http
GET /configurations/{id}

Response: 200 OK
{
  "id": "uuid",
  "applicationId": "uuid",
  "environment": "Production",
  "description": "Production settings",
  "keys": [
    {
      "id": "uuid",
      "key": "Database:ConnectionString",
      "value": "[ENCRYPTED]",
      "isEncrypted": true,
      "description": "Database connection"
    }
  ],
  "createdAt": "2026-05-04T10:30:00Z",
  "lastModifiedAt": "2026-05-04T10:35:00Z"
}
```

#### Update Configuration
```http
PUT /configurations/{id}
Content-Type: application/json

{
  "description": "Updated description",
  "metadata": { "team": "backend", "owner": "john@example.com" }
}

Response: 200 OK
```

#### Delete Configuration
```http
DELETE /configurations/{id}

Response: 204 No Content
```

#### List Configurations
```http
GET /configurations?applicationId={appId}&environment={env}&pageSize=10&pageNumber=1

Response: 200 OK
{
  "items": [...],
  "totalCount": 25,
  "pageSize": 10,
  "pageNumber": 1
}
```

### Configuration Keys

#### Add Key
```http
POST /configurations/{configurationId}/keys
Content-Type: application/json

{
  "key": "Feature:NewCheckout",
  "value": "true",
  "isEncrypted": false,
  "description": "Enable new checkout flow"
}

Response: 201 Created
```

#### Update Key
```http
PUT /configurations/{configurationId}/keys/{keyId}
Content-Type: application/json

{
  "value": "false",
  "description": "Disabled for maintenance"
}

Response: 200 OK
```

#### Delete Key
```http
DELETE /configurations/{configurationId}/keys/{keyId}

Response: 204 No Content
```

#### Get All Keys
```http
GET /configurations/{configurationId}/keys?includeEncrypted=false

Response: 200 OK
```

### Versions

#### Create Version
```http
POST /configurations/{configurationId}/versions
Content-Type: application/json

{
  "description": "Q2 2026 release",
  "changeNotes": "Added new feature flags"
}

Response: 201 Created
{
  "id": "uuid",
  "version": 2,
  "status": "Draft",
  "keyCount": 15,
  "createdAt": "2026-05-04T10:30:00Z"
}
```

#### Publish Version
```http
POST /configurations/{configurationId}/versions/{versionId}/publish
Content-Type: application/json

{
  "notes": "Production release"
}

Response: 200 OK
```

#### Rollback Version
```http
POST /configurations/{configurationId}/versions/{versionId}/rollback
Content-Type: application/json

{
  "reason": "Breaking changes in new version",
  "notifyWebhooks": true
}

Response: 200 OK
```

#### Get Diff
```http
GET /configurations/{configurationId}/versions/{fromVersionId}/diff/{toVersionId}

Response: 200 OK
[
  {
    "key": "Database:Pool",
    "changeType": "Modified",
    "oldValue": "10",
    "newValue": "20"
  },
  {
    "key": "Feature:Beta",
    "changeType": "Added",
    "newValue": "true"
  }
]
```

### Webhooks

#### Create Subscription
```http
POST /configurations/{configurationId}/webhooks
Content-Type: application/json

{
  "name": "Service Webhook",
  "url": "https://example.com/webhook",
  "events": ["ConfigurationUpdated", "VersionPublished"],
  "verifySignature": true,
  "retryPolicy": {
    "maxRetries": 5,
    "delaySeconds": 60
  }
}

Response: 201 Created
```

#### Get Deliveries
```http
GET /webhooks/{webhookId}/deliveries?status=failed&pageSize=20

Response: 200 OK
{
  "items": [
    {
      "id": "uuid",
      "timestamp": "2026-05-04T10:30:00Z",
      "status": "Failed",
      "statusCode": 500,
      "errorMessage": "Internal Server Error",
      "retryCount": 2,
      "nextRetryAt": "2026-05-04T11:30:00Z"
    }
  ]
}
```

### Audit Logs

#### Get Audit Trail
```http
GET /configurations/{configurationId}/audit-logs?pageSize=50&pageNumber=1

Response: 200 OK
{
  "items": [
    {
      "id": "uuid",
      "timestamp": "2026-05-04T10:30:00Z",
      "action": "ConfigurationUpdated",
      "user": "admin@example.com",
      "ipAddress": "192.168.1.1",
      "details": "Updated 3 keys",
      "changes": {
        "Database:Host": { "old": "localhost", "new": "prod-db.example.com" }
      }
    }
  ],
  "totalCount": 150
}
```

## Configuration Reference

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotnetConfigServerDb;Trusted_Connection=true;"
  },
  "ApplicationSettings": {
    "ApiVersion": "v1",
    "MaxVersionHistory": 100,
    "EnableCors": true,
    "EnableSwagger": true
  },
  "Encryption": {
    "KeySize": 256,
    "SaltSize": 16,
    "Iterations": 10000,
    "Algorithm": "AES256"
  },
  "Webhook": {
    "MaxRetries": 5,
    "TimeoutSeconds": 30,
    "BatchSize": 100
  },
  "RateLimit": {
    "RequestsPerMinute": 100
  },
  "Cache": {
    "DefaultDurationSeconds": 300
  }
}
```

### Environment Variables

Override any setting with environment variables:

```bash
# Database
ConnectionStrings__DefaultConnection=Server=prod-db;Database=ConfigServer;...

# Encryption
Encryption__Algorithm=AES256
Encryption__KeySize=256

# Logging
Logging__LogLevel__Default=Warning

# Webhook
Webhook__MaxRetries=10
Webhook__TimeoutSeconds=60

# Rate Limiting
RateLimit__RequestsPerMinute=200
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ApplicationSettings": {
    "EnableSwagger": true,
    "EnableDetailedErrors": true
  },
  "Webhook": {
    "TimeoutSeconds": 60
  }
}
```

## Deployment

### Docker

```bash
# Build image
docker build -t dotnet-config-server:latest .

# Run container
docker run -d \
  --name config-server \
  -p 80:8080 \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=ConfigServer;..." \
  dotnet-config-server:latest
```

### Azure App Service

```bash
# Create resource group
az group create --name rg-config-server --location eastus

# Create App Service Plan
az appservice plan create \
  --name asp-config-server \
  --resource-group rg-config-server \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group rg-config-server \
  --plan asp-config-server \
  --name config-server-app \
  --runtime "DOTNET|10.0"

# Configure connection string
az webapp config appsettings set \
  --resource-group rg-config-server \
  --name config-server-app \
  --settings ConnectionStrings__DefaultConnection="Server=...;"
```

### Kubernetes

```bash
# Create namespace
kubectl create namespace config-server

# Create secrets
kubectl create secret generic db-credentials \
  -n config-server \
  --from-literal=connection-string='Server=...;'

# Deploy
kubectl apply -f k8s/deployment.yaml -n config-server

# Check deployment
kubectl get pods -n config-server
kubectl logs -n config-server -l app=config-server
```

## Troubleshooting

### Issue: Database Connection Fails

**Symptom**: `SqlException: A network-related or instance-specific error occurred`

**Solution**:
1. Verify SQL Server is running
2. Check connection string format
3. Ensure user has database access
4. For LocalDB: `sqllocaldb start mssqllocaldb`

```bash
# Test connection with sqlcmd
sqlcmd -S (localdb)\mssqllocaldb -E -Q "SELECT 1"
```

### Issue: Migrations Not Applied

**Symptom**: `InvalidOperationException: No migrations have been applied`

**Solution**:
```bash
# List pending migrations
dotnet ef migrations list

# Apply migrations
dotnet ef database update

# Or specific migration
dotnet ef database update InitialCreate
```

### Issue: Swagger Documentation Not Displaying

**Symptom**: Swagger UI shows blank page or 404

**Solution**:
1. Ensure `EnableSwagger: true` in appsettings
2. Check middleware is registered in Program.cs
3. Clear browser cache (Ctrl+Shift+Delete)
4. Verify application is running in Development environment

```bash
# Check environment
echo $ASPNETCORE_ENVIRONMENT
# Set if needed
export ASPNETCORE_ENVIRONMENT=Development
```

### Issue: Webhook Deliveries Failing

**Symptom**: Webhooks not being delivered to client service

**Solution**:
1. Verify webhook URL is publicly accessible
2. Check firewall rules allow outbound HTTPS
3. Verify client service is accepting POST requests
4. Check webhook delivery status: `GET /webhooks/{id}/deliveries`
5. Review application logs for errors

```bash
# Test webhook endpoint manually
curl -X POST https://your-webhook-endpoint/webhook \
  -H "Content-Type: application/json" \
  -H "X-Webhook-Signature: ..." \
  -d '{"configurationId":"...","event":"ConfigurationUpdated"}'
```

### Issue: Encryption Keys Not Found

**Symptom**: `InvalidOperationException: No active encryption key found`

**Solution**:
1. Ensure encryption keys are created in database
2. Check key expiration dates
3. Manually create encryption key:

```csharp
var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
await encryptionService.GenerateNewKeyAsync();
```

### Issue: Rate Limiting Too Strict

**Symptom**: `429 Too Many Requests` errors

**Solution**:
```json
{
  "RateLimit": {
    "RequestsPerMinute": 200
  }
}
```

Or via environment variable:
```bash
RateLimit__RequestsPerMinute=500
```

### Issue: Performance Degradation

**Symptom**: Slow API responses

**Solution**:
1. Monitor performance metrics: `GET /metrics`
2. Check database query performance
3. Enable caching: `Caching.DefaultDurationSeconds`
4. Scale horizontally with multiple instances
5. Use connection pooling optimization

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** changes (`git commit -m 'Add amazing feature'`)
4. **Push** to branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Guidelines

- Follow C# coding standards
- Write unit tests for new features
- Update documentation
- Ensure all tests pass
- Keep commits focused and descriptive

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

## Support & Contact

- **Issues**: [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)
- **Discussions**: [GitHub Discussions](https://github.com/sarmkadan/dotnet-config-server/discussions)
- **Email**: [Contact](mailto:rutova2@gmail.com)

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
