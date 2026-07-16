# Dotnet Config Server

![CI](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-config-server)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
[![Build](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml)
[![Docker](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/docker.yml/badge.svg)](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/docker.yml)

A production-grade centralized configuration server for .NET microservices with support for hot reload, encryption, versioning, diff tracking, and webhook notifications.

## Table of Contents

- [Quick Start](#quick-start)
- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration Reference](#configuration-reference)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Performance](#performance)
- [Related Projects](#related-projects)
- [Contributing](#contributing)

## Quick Start

```bash
# Clone and run locally (requires .NET 10 SDK and SQL Server LocalDB)
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server
dotnet restore
dotnet ef database update
dotnet run
# API: https://localhost:5001  |  Swagger: https://localhost:5001/swagger
```

Or with Docker Compose (no local SQL Server needed):

```bash
docker-compose up
# API: http://localhost:80  |  Swagger: http://localhost:80/swagger
```

Create your first configuration:

```bash
curl -s -X POST https://localhost:5001/api/v1/applications \
  -H "Content-Type: application/json" \
  -d '{"name":"MyService","description":"My microservice"}' | jq .

curl -s -X POST https://localhost:5001/api/v1/configurations \
  -H "Content-Type: application/json" \
  -d '{"applicationId":"<id>","environment":"Development","description":"Dev config"}' | jq .
```

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
- **Rule-Based Validation**: Regex, length, JSON, URL, numeric range, allowed values, and cross-key validation rules

### Security & Encryption
- **AES-256 Encryption**: Encrypt sensitive configuration values
- **Key Rotation**: Automatic encryption key rotation with version management
- **Key Expiration**: Automatic invalidation of old encryption keys
- **Access Control Ready**: Prepared for integration with authentication systems

### Versioning & History
- **Full Version Control**: Create, update, and manage configuration versions
- **Automatic Snapshots**: Each change creates a snapshot for rollback
- **Version Lifecycle**: Draft → Published → Archived → Deleted
- **Rollback Support**: Instant rollback to any previous version with preview and history
- **Rollback Audit Trail**: Track who performed a rollback, when it happened, and why

### Change Tracking & Diffs
- **Visual Diffs**: See exactly what changed between versions
- **Diff Viewer API**: Dedicated endpoints for enriched diffs, version timelines, and rollback previews
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

For the full picture - module breakdown, design decisions with rationale, data flow, extension points, and known limitations - see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). The short version:

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
- `DiffViewerController.cs` - Enriched diff and version timeline endpoints
- `RollbackController.cs` - Rollback preview, execution, and history
- `ValidationRulesController.cs` - Validation rule CRUD and configuration validation
- `WebhooksController.cs` - Webhook subscriptions
- `AuditLogsController.cs` - Audit trail retrieval

**Business Logic Layer** (Services)
- `ConfigurationService` - Core configuration operations
- `VersioningService` - Version lifecycle management
- `DiffService` - Configuration comparison
- `DiffViewerService` - Enriched diff visualization and rollback preview
- `RollbackService` - Rollback execution with audit history
- `ValidationRuleService` - Rule CRUD and configuration validation
- `EncryptionService` - AES-256 encryption/decryption
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

For more practical, standalone examples, see the [examples/](examples/) directory:
- [BasicUsage.cs](examples/BasicUsage.cs)
- [AdvancedUsage.cs](examples/AdvancedUsage.cs)
- [IntegrationExample.cs](examples/IntegrationExample.cs)

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
// Get enriched diff between two versions
var diffResponse = await client.GetAsync(
    $"/api/v1/configurations/{configId}/diff-viewer/{fromVersionId}/{toVersionId}");

var diff = await diffResponse.Content.ReadAsAsync<EnrichedDiff>();
Console.WriteLine($"Added: {diff.AddedCount}, Modified: {diff.ModifiedCount}, Deleted: {diff.DeletedCount}");

// Inspect the version timeline
var timeline = await client.GetFromJsonAsync<List<VersionTimelineEntry>>(
    $"/api/v1/configurations/{configId}/diff-viewer/timeline");
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
// Preview a rollback before executing it
var rollbackVersionId = "...";
var preview = await client.GetFromJsonAsync<RollbackPreview>(
    $"/api/v1/configurations/{configId}/rollback/preview/{rollbackVersionId}");
Console.WriteLine($"Rollback safe: {preview!.IsRollbackSafe}");

// Execute the rollback with an audit reason
var rollbackResponse = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/rollback/{rollbackVersionId}",
    new { reason = "Restore stable production settings" });
var rollback = await rollbackResponse.Content.ReadAsAsync<RollbackResult>();

// Review rollback history
var history = await client.GetFromJsonAsync<List<RollbackRecord>>(
    $"/api/v1/configurations/{configId}/rollback/history");
```

### Example 7: Validation Rules

```csharp
// Create a validation rule for service URLs
await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/validation-rules",
    new
    {
        name = "Service URL rule",
        description = "All service URL keys must contain absolute URLs",
        ruleType = ValidationRuleType.Url,
        targetKeyPattern = ".*ServiceUrl$"
    });

// Validate the active configuration
var validationResult = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configId}/validation-rules/validate",
    new { });
```

### Example 8: Batch Operations

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

### Example 9: Audit Log Retrieval

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

### Example 10: Configuration Export

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

### Example 11: Health Check & Status

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

#### Get Diff
```http
GET /configurations/{configurationId}/versions/{fromVersionId}/diff/{toVersionId}

Response: 200 OK
```

#### Get Enriched Diff
```http
GET /configurations/{configurationId}/diff-viewer/{fromVersionId}/{toVersionId}

Response: 200 OK
```

#### Get Version Timeline
```http
GET /configurations/{configurationId}/diff-viewer/timeline

Response: 200 OK
```

#### Preview Rollback
```http
GET /configurations/{configurationId}/diff-viewer/rollback-preview/{targetVersionId}
GET /configurations/{configurationId}/rollback/preview/{targetVersionId}

Response: 200 OK
```

#### Rollback Version
```http
POST /configurations/{configurationId}/rollback/{targetVersionId}
Content-Type: application/json

{
  "reason": "Breaking changes in new version"
}

Response: 200 OK
```

#### Rollback History
```http
GET /configurations/{configurationId}/rollback/history

Response: 200 OK
```

### Validation Rules

#### List Rules
```http
GET /configurations/{configurationId}/validation-rules

Response: 200 OK
```

#### Create Rule
```http
POST /configurations/{configurationId}/validation-rules
Content-Type: application/json

{
  "name": "Service URL rule",
  "ruleType": "Url",
  "targetKeyPattern": ".*ServiceUrl$"
}

Response: 201 Created
```

#### Validate Configuration
```http
POST /configurations/{configurationId}/validation-rules/validate
Content-Type: application/json

{
  "versionId": "optional-version-id"
}

Response: 200 OK
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

The application uses the `IOptions<DotnetConfigServerOptions>` pattern for type-safe, validated configuration. Settings must be nested under the `DotnetConfigServer` section in `appsettings.json`.

All configuration options support environment variable overrides using double underscores as separators. For example:

```bash
# Override specific settings
DotnetConfigServer__ApplicationSettings__ApiVersion=v2
DotnetConfigServer__Encryption__Algorithm=AES192
DotnetConfigServer__RateLimit__RequestsPerMinute=200
```

### Complete Configuration Schema

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DotnetConfigServer": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotnetConfigServerDb;Trusted_Connection=true;"
  },
  "DotnetConfigServer": {
    "ApplicationSettings": {
      "ApiVersion": "v1",
      "MaxVersionHistory": 100,
      "EnableCors": true,
      "EnableSwagger": true,
      "EnableDetailedErrors": false,
      "EnableRequestLogging": true,
      "EnablePerformanceMonitoring": true
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
      "BatchSize": 100,
      "EnableSignatureVerification": true,
      "EnableAutoRetry": true
    },
    "RateLimit": {
      "RequestsPerMinute": 100,
      "RetryAfterSeconds": 60,
      "EnableRateLimiting": true,
      "RateLimitExemptPaths": ["/health", "/metrics"]
    },
    "Cache": {
      "DefaultDurationSeconds": 300,
      "EnableDistributedCache": false,
      "DistributedCacheDurationSeconds": 3600
    },
    "Database": {
      "EnableConnectionPooling": true,
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 30,
      "EnableAutomaticMigration": true
    },
    "Performance": {
      "EnableMetrics": true,
      "MetricsSampleRate": 0.1,
      "EnableRequestTracing": false,
      "MaxRequestBodySizeKb": 1024
    },
    "Security": {
      "EnableHttpsRedirection": true,
      "EnableRequestValidation": true,
      "EnableCorsPolicy": true,
      "TrustedOrigins": ["https://your-trusted-domain.com"]
    }
  }
}
```

### Configuration Options Reference

#### ApplicationSettings

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApiVersion` | string | "v1" | API version identifier |
| `MaxVersionHistory` | int | 100 | Maximum number of version history entries to keep (1-1000) |
| `EnableCors` | bool | true | Enable CORS for cross-origin requests |
| `EnableSwagger` | bool | true | Enable Swagger/OpenAPI documentation |
| `EnableDetailedErrors` | bool | false | Enable detailed error responses in development |
| `EnableRequestLogging` | bool | true | Enable request logging middleware |
| `EnablePerformanceMonitoring` | bool | true | Enable performance monitoring middleware |

#### Encryption

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `KeySize` | int | 256 | Encryption key size in bits (128, 192, or 256) |
| `SaltSize` | int | 16 | Salt size in bytes (8-64) |
| `Iterations` | int | 10000 | Number of PBKDF2 iterations (1000-1000000) |
| `Algorithm` | string | "AES256" | Encryption algorithm to use (AES256, AES192, or AES128) |

#### Webhook

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxRetries` | int | 5 | Maximum number of retry attempts for failed deliveries (0-10) |
| `TimeoutSeconds` | int | 30 | Webhook request timeout in seconds (1-300) |
| `BatchSize` | int | 100 | Batch size for webhook delivery processing (1-1000) |
| `EnableSignatureVerification` | bool | true | Enable HMAC signature verification for webhooks |
| `EnableAutoRetry` | bool | true | Enable automatic webhook retries |

#### RateLimit

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RequestsPerMinute` | int | 100 | Maximum requests per minute per client (1-10000) |
| `RetryAfterSeconds` | int | 60 | Retry-After header value in seconds when rate limited (1-3600) |
| `EnableRateLimiting` | bool | true | Enable rate limiting |
| `RateLimitExemptPaths` | string[] | ["/health", "/metrics"] | Paths exempt from rate limiting |

#### Cache

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultDurationSeconds` | int | 300 | Cache duration in seconds (1-3600 seconds / 1 hour) |
| `EnableDistributedCache` | bool | false | Enable distributed caching (Redis, etc.) |
| `DistributedCacheDurationSeconds` | int | 3600 | Distributed cache duration in seconds (1-86400 / 24 hours) |

#### Database

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableConnectionPooling` | bool | true | Enable database connection pooling |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout in seconds |
| `CommandTimeoutSeconds` | int | 30 | Command timeout in seconds |
| `EnableAutomaticMigration` | bool | true | Enable automatic database migration on startup |

#### Performance

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableMetrics` | bool | true | Enable performance metrics collection |
| `MetricsSampleRate` | double | 0.1 | Sample rate for performance monitoring (0.0-1.0) |
| `EnableRequestTracing` | bool | false | Enable request tracing |
| `MaxRequestBodySizeKb` | int | 1024 | Maximum request body size in kilobytes |

#### Security

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableHttpsRedirection` | bool | true | Enable HTTPS redirection |
| `EnableRequestValidation` | bool | true | Enable request validation |
| `EnableCorsPolicy` | bool | true | Enable CORS policy |
| `TrustedOrigins` | string[] | ["https://your-trusted-domain.com"] | Trusted origins for CORS |

### Environment Variables

All settings can be overridden via environment variables using the double underscore (`__`) separator:

```bash
# Example overrides
DotnetConfigServer__ApplicationSettings__ApiVersion=v2
DotnetConfigServer__Encryption__Algorithm=AES192
DotnetConfigServer__Webhook__MaxRetries=10
DotnetConfigServer__RateLimit__RequestsPerMinute=500
```

### Validation

The application performs runtime validation of all configuration options using DataAnnotations. Invalid configurations will cause the application to fail on startup with detailed validation errors.

### Best Practices

1. **Development**: Use `EnableDetailedErrors=true` and `EnableSwagger=true`
2. **Production**: Set `EnableDetailedErrors=false`, `EnableSwagger=false`, and configure proper `TrustedOrigins`
3. **Security**: Always enable `EnableHttpsRedirection` and `EnableRequestValidation` in production
4. **Performance**: Adjust `RequestsPerMinute` based on expected traffic and `Cache.DefaultDurationSeconds` for optimal performance
5. **Encryption**: Use `KeySize=256` and `Iterations=10000` for production environments

Override any setting with environment variables using double underscores as separators:

```bash
# Example overrides
DotnetConfigServer__Encryption__Algorithm=AES256
DotnetConfigServer__Webhook__MaxRetries=10
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

## Testing

Run the full test suite:

```bash
dotnet test
```

Run with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

| Test file | What it covers |
|---|---|
| `ConfigurationDiffTests.cs` | Diff engine — added, modified, deleted key detection |
| `ConfigurationModelTests.cs` | Model validation and JSON serialization round-trips |
| `EncryptionServiceTests.cs` | AES-256 encrypt/decrypt, key rotation, tamper detection |

Individual test project path: `tests/dotnet-config-server.Tests/`

## Performance

See [Performance Benchmarks](./benchmarks/dotnet-config-server.Benchmarks/README.md) for detailed information on running benchmarks and interpreting results.

Benchmarks measured on a single instance (4 vCPUs, 8 GB RAM, SQL Server on the same host):

| Operation | p50 | p99 |
|---|---|---|
| GET configuration (cache hit) | 1 ms | 4 ms |
| GET configuration (cache miss) | 8 ms | 22 ms |
| POST key (with AES-256 encryption) | 6 ms | 18 ms |
| Version diff (50-key config) | 3 ms | 11 ms |
| Webhook dispatch (single endpoint) | 12 ms | 40 ms |

**Throughput**: ~8 000 read requests/sec on a single instance with the memory cache warm.

**Encryption overhead**: AES-256 key encryption adds ~2 ms per write; reads are decrypted in < 1 ms via cached key material.

**Webhook retry worker**: processes up to 500 pending deliveries per cycle (configurable via `Webhook:BatchSize`).

To baseline your own deployment run `GET /metrics` for live request-duration histograms collected by `PerformanceMonitoringMiddleware`.

## Related Projects

- [redis-cache-patterns](https://github.com/sarmkadan/redis-cache-patterns) - Production-ready Redis caching patterns for .NET — cache-aside, write-through, distributed lock

### Integration Examples

**Cache configuration responses with redis-cache-patterns (cache-aside)**

```csharp
// Pull a published config from the server and cache it in Redis.
// On subsequent calls the value is served from Redis until the
// webhook invalidates it — no round-trip to the config server needed.
var cacheKey = CacheKeyGenerator.ForConfiguration(configId, environment);
var config = await _cache.GetOrSetAsync(cacheKey, async () =>
    await _configClient.GetConfigurationAsync(configId),
    TimeSpan.FromMinutes(5));
```

**Hot-reload via webhook + distributed cache invalidation**

```csharp
// Webhook handler: config server calls this endpoint on every publish.
[HttpPost("/config/webhook")]
public async Task<IActionResult> OnConfigChanged([FromBody] WebhookPayload payload)
{
    var cacheKey = CacheKeyGenerator.ForConfiguration(payload.ConfigurationId, payload.Environment);
    await _cache.RemoveAsync(cacheKey);          // bust stale entry
    await _configReloader.ReloadAsync(payload.ConfigurationId);
    return Ok();
}
```

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

Built by [Vladyslav Zaiets](https://sarmkadan.com)
