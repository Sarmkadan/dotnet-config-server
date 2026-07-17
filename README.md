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

## Application

The `Application` class represents a client application that can have configurations managed by the Dotnet Config Server. It serves as the container for all configuration data and provides essential metadata including API keys, webhook URLs, and version history settings. Applications can be activated or deactivated, and each maintains a count of its configurations for quick reference.

Each application has a unique identifier, a human-readable name, a URL-friendly slug, and optional description. It includes security credentials (API key and optional secret key), configuration limits (max version history), and auto-reload capabilities for real-time configuration updates.



### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a new application for an order processing service
var orderServiceApp = new Application
{
    Name = "OrderProcessingService",
    Description = "Order processing microservice with payment integration",
    Slug = "order-processing-service",
    CreatedBy = "backend-team@example.com",
    IsActive = true,
    EnableAutoReload = true,
    MaxVersionHistory = 50, // Keep 50 versions of configuration history
    WebhookUrl = "https://order-service.example.com/config/webhook"
};

// Generate secure API keys for authentication
orderServiceApp.GenerateNewApiKey();
orderServiceApp.GenerateNewSecretKey(); // Optional for additional security

Console.WriteLine($"Created application: {orderServiceApp.Name}");
Console.WriteLine($"API Key: {orderServiceApp.ApiKey}");
Console.WriteLine($"Secret Key: {orderServiceApp.SecretKey}");
Console.WriteLine($"Configuration Count: {orderServiceApp.ConfigurationCount}");

// Validate the application configuration
try
{
    orderServiceApp.Validate();
    Console.WriteLine("Application configuration is valid!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  {error.Key}: {error.Value}");
    }
}

// Update last accessed timestamp
orderServiceApp.UpdateLastAccess();

// Deactivate the application when it's no longer needed
orderServiceApp.Deactivate("admin@example.com");
Console.WriteLine($"Application active status: {orderServiceApp.IsActive}");

// Activate the application when needed
orderServiceApp.Activate("admin@example.com");
Console.WriteLine($"Application active status: {orderServiceApp.IsActive}");

// Get a summary view (without sensitive data)
var summary = orderServiceApp.GetSummary();
Console.WriteLine($"Application summary - {summary.Name} ({summary.Id}): {summary.ConfigurationCount} configurations");
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

## EncryptionService

The `EncryptionService` provides AES-256 encryption and decryption capabilities for securing sensitive configuration values. It handles key management, encryption/decryption operations, and key rotation workflows.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI container
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());
services.AddSingleton<IEncryptionKeyRepository, EncryptionKeyRepository>();
services.AddSingleton<EncryptionService>();

var serviceProvider = services.BuildServiceProvider();
var encryptionService = serviceProvider.GetRequiredService<EncryptionService>();

// Generate a new encryption key
var encryptionKey = encryptionService.GenerateNewKey("MyServiceKey");

// Encrypt sensitive data
var plainText = "my-secret-password";
var encryptedValue = encryptionService.Encrypt(plainText, encryptionKey);
Console.WriteLine($"Encrypted: {encryptedValue}");

// Decrypt the value
var decryptedValue = encryptionService.Decrypt(encryptedValue, encryptionKey);
Console.WriteLine($"Decrypted: {decryptedValue}");

// Encrypt asynchronously using a configuration ID
var configurationId = Guid.NewGuid();
var encryptedAsync = await encryptionService.EncryptAsync(plainText, configurationId);

// Decrypt asynchronously
var decryptedAsync = await encryptionService.DecryptAsync(encryptedAsync, configurationId);

// Validate a key before use
var isValid = encryptionService.ValidateKey(encryptionKey);

// Get keys by ID
var keyById = await encryptionService.GetKeyAsync(encryptionKey.KeyId);

// Get primary key for a configuration
var primaryKey = await encryptionService.GetPrimaryKeyAsync(configurationId);

// Rotate an encryption key
await encryptionService.RotateKeyAsync(encryptionKey.KeyId, "admin@example.com");

// Re-encrypt all configuration values with the new primary key
// Note: Requires access to configuration keys repository
// var configurationKeys = await _configurationKeyRepository.GetByConfigurationIdAsync(configurationId);
// await encryptionService.ReEncryptConfigurationAsync(configurationId, configurationKeys, "admin@example.com");
```

### Key Management

The service supports:
- **Key Generation**: Create new AES-256 encryption keys with automatic expiration
- **Key Rotation**: Safely rotate keys while maintaining backward compatibility
- **Key Validation**: Verify key integrity before use
- **Multi-Key Support**: Decrypt values encrypted with any active key
- **Configuration-Scoped Keys**: Manage keys per configuration for better isolation

### Encryption Workflow

1. **Synchronous Encryption**: Use `Encrypt()` with a specific key for immediate operations
2. **Asynchronous Encryption**: Use `EncryptAsync()` with configuration ID to automatically use the primary key
3. **Decryption**: The service automatically tries the primary key first, then falls back to all active keys
4. **Key Rotation**: Mark old keys as rotated, then re-encrypt stored values with the new primary key

### Best Practices

- Store encryption keys securely in the database
- Rotate keys periodically (e.g., every 90 days)
- Use configuration-scoped keys for multi-tenant scenarios
- Monitor key usage and expiration dates
- Always validate keys before use with `ValidateKey()`

### Versioning & History

- **Full Version Control**: Create, update, and manage configuration versions
- **Automatic Snapshots**: Each change creates a snapshot for rollback
- **Version Lifecycle**: Draft → Published → Archived → Deleted
- **Rollback Support**: Instant rollback to any previous version with preview and history
- **Rollback Audit Trail**: Track who performed a rollback, when it happened, and why
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

## ConfigurationVersion

The `ConfigurationVersion` class represents a version of a configuration with associated keys. It manages the complete lifecycle of configuration versions including creation, publishing, archiving, and deprecation. Configuration versions track metadata such as version numbers, release notes, timestamps, user actions, and key statistics to support versioning, rollback, and audit operations.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a new configuration version for a production service
var productionVersion = new ConfigurationVersion
{
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    VersionNumber = "2.0.0",
    Status = ConfigurationVersionStatus.Draft,
    ReleaseNotes = "Q3 2026 release with new features and performance improvements",
    KeyCount = 42,
    HasEncryptedKeys = true,
    PreviousVersionId = Guid.Parse("456e4567-e89b-12d3-a456-426614174001").ToString(),
    ChangesSummary = "Added payment gateway integration, updated timeout settings, removed deprecated features",
    CreatedBy = "backend-team@example.com",
    Keys = new List<ConfigurationKey>
    {
        new ConfigurationKey { Key = "Database:ConnectionString", Value = "Server=prod-db.example.com;...", IsEncrypted = true },
        new ConfigurationKey { Key = "Api:TimeoutSeconds", Value = "30" },
        new ConfigurationKey { Key = "Feature:EnableNewCheckout", Value = "true" }
    }
};

// Validate the configuration version
try
{
    productionVersion.Validate();
    Console.WriteLine("Configuration version is valid!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Publish the version to make it active
productionVersion.Publish("backend-team@example.com");
Console.WriteLine($"Version published at: {productionVersion.PublishedAt}");
Console.WriteLine($"Status: {productionVersion.Status}");

// Archive an old version
var oldVersion = new ConfigurationVersion
{
    Id = Guid.Parse("789e4567-e89b-12d3-a456-426614174002"),
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    VersionNumber = "1.5.0",
    Status = ConfigurationVersionStatus.Active,
    KeyCount = 38,
    CreatedBy = "backend-team@example.com"
};

oldVersion.Archive("admin@example.com");
Console.WriteLine($"Version archived at: {oldVersion.ArchivedAt}");

// Deprecate a version
var deprecatedVersion = new ConfigurationVersion
{
    Id = Guid.Parse("abc-e4567-e89b-12d3-a456-426614174003"),
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    VersionNumber = "1.0.0",
    Status = ConfigurationVersionStatus.Active,
    KeyCount = 25,
    CreatedBy = "backend-team@example.com"
};

deprecatedVersion.Deprecate();
Console.WriteLine($"Version deprecated: {deprecatedVersion.Status}");

// Increment version numbers
var nextMajorVersion = ConfigurationVersion.IncrementVersion("2.5.3", VersionIncrementType.Major);
Console.WriteLine($"Next major version: {nextMajorVersion}"); // "3.0.0"

var nextMinorVersion = ConfigurationVersion.IncrementVersion("2.5.3", VersionIncrementType.Minor);
Console.WriteLine($"Next minor version: {nextMinorVersion}"); // "2.6.0"

var nextPatchVersion = ConfigurationVersion.IncrementVersion("2.5.3", VersionIncrementType.Patch);
Console.WriteLine($"Next patch version: {nextPatchVersion}"); // "2.5.4"

// Get a summary view
var summary = productionVersion.GetSummary();
Console.WriteLine($"Version summary - v{summary.VersionNumber} ({summary.Status}): {summary.KeyCount} keys");
```

### Public Members

- `Id` - Unique identifier for the configuration version
- `ConfigurationId` - The configuration this version belongs to
- `VersionNumber` - Version identifier in format major.minor.patch
- `Status` - Current status (Draft, Active, Archived, Deprecated)
- `ReleaseNotes` - Optional release notes describing changes in this version
- `CreatedAt` - When the version was created
- `PublishedAt` - When the version was published (null if not published)
- `ArchivedAt` - When the version was archived (null if not archived)
- `CreatedBy` - Who created the version
- `PublishedBy` - Who published the version (null if not published)
- `ArchivedBy` - Who archived the version (null if not archived)
- `KeyCount` - Number of configuration keys in this version
- `HasEncryptedKeys` - Whether this version contains encrypted keys
- `PreviousVersionId` - ID of the previous version (null if first version)
- `ChangesSummary` - Summary of changes from previous version
- `Keys` - List of configuration keys in this version
- `Publish(publishedBy)` - Publishes the version, making it active
- `Archive(archivedBy)` - Archives the version
- `Deprecate()` - Marks version as deprecated
- `IncrementVersion(currentVersion, incrementType)` - Static method to increment version numbers
- `Validate()` - Validates the configuration version
- `GetSummary()` - Returns a summary view of the version


## MultiEnvironmentManager

The `MultiEnvironmentManager` class provides utilities for managing configurations across multiple environments (Development, Staging, Production). It simplifies common multi-environment operations like promoting configurations between environments, synchronizing feature flags, and comparing environment configurations. This manager handles environment-specific configuration management through a centralized API interface.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Initialize the manager with your application ID and server URL
var appId = "550e8400-e29b-41d4-a716-446655440000"; // Your application ID
var manager = new MultiEnvironmentManager("https://localhost:5001", appId);

// Example 1: Display promotion workflow
await manager.DisplayPromotionWorkflowAsync();

// Example 2: Compare configurations between two environments
await manager.DisplayEnvironmentComparisonAsync("Development", "Production");

// Example 3: Promote configuration from Development to Staging
// int promotedCount = await manager.PromoteAsync(
//     "Development", 
//     "Staging", 
//     overwrite: true
// );

// Example 4: Promote specific keys from Staging to Production
// int promotedCount = await manager.PromoteAsync(
//     "Staging",
//     "Production",
//     overwrite: false,
//     keyFilter: new List<string> { "Database:Host", "Features:EnableNewCheckout" }
// );

// Example 5: Synchronize a feature flag across all environments
// await manager.SynchronizeKeyAsync(
//     "Features:MaintenanceMode", 
//     "false",
//     isEncrypted: false
// );

// Example 6: Get or create environment configuration
// string configId = await manager.GetOrCreateEnvironmentConfigAsync("Development", "Dev configuration");

// Example 7: List all environment configurations
// var configs = await manager.ListEnvironmentConfigurationsAsync();
// foreach (var config in configs) {
//     Console.WriteLine($"[{config.Environment}] {config.Id} - {config.KeyCount} keys");
// }
```

### Public Members

- `MultiEnvironmentManager(string baseUrl, string applicationId)` - Constructor that initializes the manager with server URL and application ID
- `GetOrCreateEnvironmentConfigAsync(string environment, string description = null)` - Get existing configuration or create new one for the specified environment
- `ListEnvironmentConfigurationsAsync()` - List all configurations for this application across all environments
- `PromoteAsync(string sourceEnvironment, string targetEnvironment, bool overwrite = true, List<string> keyFilter = null)` - Promote configuration from source to target environment
- `DisplayEnvironmentComparisonAsync(string env1, string env2)` - Compare configurations between two environments
- `SynchronizeKeyAsync(string keyName, string value, bool isEncrypted = false)` - Synchronize a specific key across all environments
- `DisplayPromotionWorkflowAsync()` - Display the environment promotion workflow

### DTO Classes

The manager uses the following data transfer objects:

- `ConfigurationDto` - Basic configuration information with properties: `Id`, `Environment`, `Description`, `KeyCount`, `CreatedAt`
- `ConfigurationDetailsDto` - Detailed configuration with properties: `Id`, `Environment`, `Keys` (list of `ConfigurationKeyDto`)
- `ConfigurationKeyDto` - Configuration key with properties: `Key`, `Value`, `IsEncrypted`, `Description`
- `PagedResult<T>` - Pagination wrapper with properties: `Items` (list of T), `TotalCount`

## RollbackResult

The `RollbackResult` class represents the outcome of an executed rollback operation. It contains comprehensive information about the rollback including the configuration involved, the new version created, the version that was restored from, the reason for the rollback, who performed it, when it occurred, and how many keys were restored.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a rollback result representing a successful rollback operation
var rollbackResult = new RollbackResult
{
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    NewVersion = new ConfigurationVersionSummary
    {
        Id = Guid.NewGuid(),
        Version = 3,
        Status = "Published",
        KeyCount = 42,
        CreatedAt = DateTime.UtcNow,
        Description = "Rollback to stable version"
    },
    RestoredFromVersion = new ConfigurationVersionSummary
    {
        Id = Guid.Parse("456e4567-e89b-12d3-a456-426614174001"),
        Version = 2,
        Status = "Archived",
        KeyCount = 42,
        CreatedAt = DateTime.UtcNow.AddHours(-2),
        Description = "Stable production configuration"
    },
    Reason = "Reverting breaking changes introduced in version 3",
    PerformedBy = "admin@example.com",
    PerformedAt = DateTime.UtcNow,
    KeysRestored = 42
};

Console.WriteLine($"Rollback successful!");
Console.WriteLine($"Configuration: {rollbackResult.ConfigurationId}");
Console.WriteLine($"New version: {rollbackResult.NewVersion.Version} ({rollbackResult.NewVersion.Status})");
Console.WriteLine($"Restored from: v{rollbackResult.RestoredFromVersion.Version}");
Console.WriteLine($"Keys restored: {rollbackResult.KeysRestored}");
Console.WriteLine($"Performed by: {rollbackResult.PerformedBy} at {rollbackResult.PerformedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Reason: {rollbackResult.Reason}");
```

### Public Members

- `ConfigurationId` - The identifier of the configuration that was rolled back
- `NewVersion` - The newly created version produced by the rollback operation
- `RestoredFromVersion` - The version that was restored during the rollback
- `Reason` - The reason provided for performing the rollback
- `PerformedBy` - The user who executed the rollback
- `PerformedAt` - When the rollback was executed
- `KeysRestored` - The number of keys restored into the new version

### WebhookConfigurationReloader

The `WebhookConfigurationReloader` class provides a client-side implementation for receiving and processing webhook notifications from the Dotnet Config Server. When configuration changes occur on the server, clients subscribed to webhook notifications can automatically reload their in-memory configuration without requiring service restarts.

This class handles webhook signature verification, configuration change processing, and provides methods for accessing the current configuration state. It's designed to be integrated into ASP.NET Core applications using dependency injection.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// In your ASP.NET Core application's Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register the reloader with your webhook secret
builder.Services.AddWebhookConfigurationReloader("your-webhook-secret-here");

// Register the startup class
builder.Services.AddSingleton<WebhookStartup>();

var app = builder.Build();

// Configure the webhook endpoint
var reloader = app.Services.GetRequiredService<WebhookConfigurationReloader>();
reloader.SetInitialConfiguration(new Dictionary<string, string>
{
    ["Database:Host"] = "localhost",
    ["Database:Port"] = "5432",
    ["Features:EnableNewCheckout"] = "true",
    ["Logging:Level"] = "Information"
});

// Use the reloader (typically in your startup class)
app.Services.GetRequiredService<WebhookStartup>().Configure(app, reloader);

Console.WriteLine("Configuration reloader started. Ready to receive webhook notifications.");
Console.WriteLine($"Current database host: {reloader.GetConfigurationValue("Database:Host")}");
```

### Public Members

- `HandleWebhookAsync(HttpContext context)` - ASP.NET Core middleware endpoint that receives and processes webhook notifications
- `GetConfigurationValue(string key)` - Retrieves the current value of a configuration key
- `SetInitialConfiguration(Dictionary<string, string> configuration)` - Sets the initial configuration state when the application starts
- `OnConfigurationReloadedAsync()` - Virtual method called after configuration is reloaded (override for application-specific actions)

### Related Classes

- `WebhookPayload` - Contains webhook event data including configuration changes
- `ConfigurationChange` - Represents a single configuration key change with old and new values
- `WebhookStartup` - ASP.NET Core startup class demonstrating endpoint registration
- `WebhookExtensions` - Extension methods for dependency injection registration

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

## IConfigurationManager

The `IConfigurationManager` interface provides a type-safe abstraction for accessing configuration values from the Dotnet Config Server. It supports both synchronous access to cached values and event-based notifications when configuration changes occur. This interface is designed for integration into .NET services that need to dynamically access configuration without direct HTTP calls.

The manager maintains a local cache that synchronizes with the server periodically (every 5 minutes) and provides fallback to cached values when the server is unavailable. Configuration changes are automatically detected and can trigger application-specific reload logic through the `ConfigurationChanged` event.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// In your service's Program.cs or Startup.cs
var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    // Add configuration manager with server URL and configuration ID
    services.AddConfigurationManager(
        configServerUrl: "https://config-server.example.com",
        configurationId: "550e8400-e29b-41d4-a716-446655440001");

    // Add your services
    services.AddScoped<OrderService>();

    // Listen for configuration changes
    var provider = services.BuildServiceProvider();
    var configManager = provider.GetRequiredService<IConfigurationManager>();
    
    configManager.ConfigurationChanged += (sender, args) =>
    {
        Console.WriteLine($"Configuration changed: {args.Key} = {args.NewValue}");
        // Trigger application-specific reload logic
    };
});

var host = builder.Build();

// Use the configuration in your services
using (var scope = host.Services.CreateScope())
{
    var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
    var order = new Order { Id = Guid.NewGuid(), Total = 99.99m };
    await orderService.ProcessOrderAsync(order);
}

await host.RunAsync();
```

### Public Members

- `Task<string> GetConfigurationValueAsync(string key)` - Retrieves configuration value by key
- `Task<T> GetConfigurationAsync<T>(string key) where T : class` - Retrieves typed configuration value
- `event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged` - Event fired when configuration changes

### Related Classes

- `CachedConfigurationManager` - Implementation of IConfigurationManager with caching
- `ConfigurationChangedEventArgs` - Event arguments containing change details:
  - `Key` - The configuration key that changed
  - `OldValue` - Previous value
  - `NewValue` - New value
  - `Timestamp` - When the change occurred
- `ConfigurationSyncBackgroundService` - Background service for periodic synchronization
- `OrderService` - Example service demonstrating usage
- `ConfigurationManagerExtensions` - Extension methods for dependency injection

## ConfigurationClientFactory

The `ConfigurationClientFactory` class provides a convenient way to create and manage HTTP clients for interacting with the Dotnet Config Server API. It handles authentication, automatic retry logic for transient failures, timeout configuration, and provides both raw HTTP clients and strongly-typed client wrappers.

The factory is particularly useful for:

- Creating HTTP clients with built-in retry logic
- Managing authentication via API keys
- Providing strongly-typed client interfaces
- Handling SSL certificate validation for development environments
- Configuring timeouts and retry policies

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Create the factory with server URL and optional API key
var factory = new ConfigurationClientFactory(
    baseUrl: "https://localhost:5001",
    apiKey: "your-api-key-here",
    timeoutSeconds: 30,
    maxRetries: 3
);

// Option 1: Create a raw HTTP client with retry logic
var httpClient = factory.CreateClient();

// Use the client directly
var response = await httpClient.GetAsync("/api/v1/configurations");
response.EnsureSuccessStatusCode();

// Option 2: Create a strongly-typed client wrapper
var typedClient = factory.CreateTypedClient();

// Use the strongly-typed client
var isHealthy = await typedClient.HealthCheckAsync();
Console.WriteLine($"Server is healthy: {isHealthy}");

// Create a new configuration
var config = await typedClient.CreateConfigurationAsync(new CreateConfigurationRequest
{
    ApplicationId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
    Environment = "Production",
    Description = "Production configuration"
});

// Add configuration keys
var key = await typedClient.AddKeyAsync(config.Id, new ConfigurationKeyRequest
{
    Key = "Database:Host",
    Value = "prod-db.example.com",
    IsEncrypted = false,
    Description = "Database hostname"
});

// Retrieve configuration
var configuration = await typedClient.GetConfigurationAsync(config.Id);
Console.WriteLine($"Configuration has {configuration.Keys.Count} keys");
```

### Public Members

#### ConfigurationClientFactory

- `ConfigurationClientFactory(string baseUrl, string apiKey = null, int timeoutSeconds = 30, int maxRetries = 3)` - Constructor that initializes the factory with server URL, optional API key, timeout, and retry configuration
- `HttpClient CreateClient()` - Creates an HTTP client with automatic retry logic, timeout, and authentication headers
- `IConfigurationServerClient CreateTypedClient()` - Creates a strongly-typed client wrapper for type-safe API access

#### RetryHandler (nested class)

- `RetryHandler(HttpMessageHandler innerHandler, int maxRetries)` - Constructor that configures retry behavior

#### IConfigurationServerClient (interface)

- `Task<Configuration> GetConfigurationAsync(Guid configurationId)` - Retrieves a configuration by ID
- `Task<Configuration> CreateConfigurationAsync(CreateConfigurationRequest request)` - Creates a new configuration
- `Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKeyRequest request)` - Adds a configuration key
- `Task<bool> HealthCheckAsync()` - Checks server health status

#### ConfigurationServerClient (implementation)

- `ConfigurationServerClient(HttpClient httpClient)` - Constructor that accepts an HTTP client

#### DTO Classes

- `Configuration` - Configuration object with properties: `Id`, `ApplicationId`, `Environment`, `Description`, `Keys`
- `ConfigurationKey` - Configuration key with properties: `Id`, `Key`, `Value`, `IsEncrypted`, `Description`
- `CreateConfigurationRequest` - Request object for creating configurations with properties: `ApplicationId`, `Environment`, `Description`
- `ConfigurationKeyRequest` - Request object for adding keys with properties: `Key`, `Value`, `IsEncrypted`, `Description`

## BasicConfigurationClient

The `BasicConfigurationClient` class provides a simple HTTP client for retrieving configurations from the Dotnet Config Server. It's designed for lightweight applications that need basic configuration access without complex abstractions or dependency injection setup. This client demonstrates fundamental patterns for interacting with the configuration server's REST API.

The client supports retrieving complete configurations, individual key values, and application-wide configurations. It also includes a health check method to verify server availability before making requests.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Create a new client instance with the server base URL
var client = new BasicConfigurationClient("https://localhost:5001");

// Check if the configuration server is healthy before making requests
bool isHealthy = await client.IsHealthyAsync();
Console.WriteLine($"Server is healthy: {isHealthy}");

if (isHealthy)
{
    // Retrieve a complete configuration with all keys
    var configId = "550e8400-e29b-41d4-a716-446655440001"; // Replace with actual configuration ID
    var configuration = await client.GetConfigurationAsync(configId);
    
    Console.WriteLine($"Configuration Environment: {configuration.Environment}");
    Console.WriteLine($"Created At: {configuration.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Last Modified: {configuration.LastModifiedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Total Keys: {configuration.Keys.Count}");
    
    // Access configuration metadata
    Console.WriteLine($"Application ID: {configuration.ApplicationId}");
    Console.WriteLine($"Description: {configuration.Description}");
    
    // Retrieve a specific configuration key value
    var dbHost = await client.GetConfigurationKeyAsync(configId, "Database:Host");
    Console.WriteLine($"Database Host: {dbHost}");
    
    // Get all configuration keys
    foreach (var key in configuration.Keys)
    {
        Console.WriteLine($"  {key.Key}: {(key.IsEncrypted ? "[ENCRYPTED]" : key.Value)}");
    }
    
    // Retrieve all configurations for an application
    var appId = "550e8400-e29b-41d4-a716-446655440002"; // Replace with actual application ID
    var appConfigs = await client.GetApplicationConfigurationsAsync(appId);
    
    Console.WriteLine($"\nApplication Configurations ({appConfigs.Count()} total):");
    foreach (var config in appConfigs)
    {
        Console.WriteLine($"  [{config.Environment}] {config.Description}");
    }
}
```

### Public Members

- `BasicConfigurationClient(string baseUrl)` - Constructor that initializes the client with the server base URL
- `Task<Configuration> GetConfigurationAsync(string configurationId)` - Retrieves a complete configuration with all keys
- `Task<string> GetConfigurationKeyAsync(string configurationId, string keyName)` - Retrieves a specific configuration key value
- `Task<IEnumerable<Configuration>> GetApplicationConfigurationsAsync(string applicationId)` - Retrieves all configurations for an application
- `Task<bool> IsHealthyAsync()` - Checks if the configuration server is healthy

### Related Classes

- `Configuration` - Represents a configuration with properties: `Id`, `ApplicationId`, `Environment`, `Description`, `Keys`, `CreatedAt`, `LastModifiedAt`
- `ConfigurationKey` - Represents a configuration key with properties: `Id`, `Key`, `Value`, `IsEncrypted`, `Description`
- `PagedResult<T>` - Pagination wrapper with properties: `Items` (list of T), `TotalCount`, `PageSize`, `PageNumber`

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class is a global exception handling middleware that catches all unhandled exceptions during HTTP request processing and returns consistent, structured error responses with appropriate HTTP status codes. It centralizes error handling logic, provides detailed error messages, validation error details, and includes tracing information via the TraceId. The middleware automatically logs exceptions and transforms them into a standardized JSON error response format.

### Usage Example

```csharp
using DotnetConfigServer.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register the middleware (automatically added in Program.cs by builder)
builder.Services.AddControllers();

var app = builder.Build();

// Use the error handling middleware
// Typically registered as the first middleware to catch all exceptions
app.UseMiddleware<ErrorHandlingMiddleware>();

// Register other middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Example controller that might throw exceptions
[ApiController]
[Route("api/[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _configService;
    
    public ConfigurationsController(IConfigurationService configService)
    {
        _configService = configService;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetConfiguration(Guid id)
    {
        // If this throws ConfigurationNotFoundException, it will be caught by ErrorHandlingMiddleware
        var config = await _configService.GetConfigurationAsync(id);
        return Ok(config);
    }
    
    [HttpPost("{configId}/keys")]
    public async Task<IActionResult> AddKey(Guid configId, [FromBody] ConfigurationKeyRequest request)
    {
        // If this throws ValidationException, it will be caught and formatted by ErrorHandlingMiddleware
        var key = await _configService.AddConfigurationKeyAsync(configId, request);
        return CreatedAtAction(nameof(GetConfiguration), new { id = configId }, key);
    }
}

// Example of how the middleware transforms exceptions
// Before: Unhandled exception with stack trace
// After: Clean JSON response with status code

// Request: GET /api/configurations/00000000-0000-0000-0000-000000000000
// Response (404 Not Found):
// {
//   "message": "Configuration not found",
//   "timestamp": "2026-07-19T14:30:00Z",
//   "traceId": "00-1234567890abcdef1234567890abcdef-1234567890abcdef-00",
//   "errors": null
// }

// Request: POST /api/configurations/config123/keys
// Body: { "key": "", "value": "test", "isEncrypted": false }
// Response (422 Unprocessable Entity):
// {
//   "message": "Validation failed",
//   "timestamp": "2026-07-19T14:31:00Z",
//   "traceId": "00-fedcba0987654321fedcba098765432-987654321fedcba0-01",
//   "errors": {
//     "key": ["The key field is required."]
//   }
// }
```

### Public Members

- `ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)` - Constructor that accepts the next middleware in the pipeline and a logger instance
- `InvokeAsync(HttpContext context)` - Main middleware entry point that processes HTTP requests and catches exceptions
- `HandleExceptionAsync(HttpContext context, Exception exception)` - Static method that handles exception transformation and response generation

### ErrorResponse Class

The middleware returns an `ErrorResponse` object with the following properties:

- `Message` (string) - Human-readable error message describing what went wrong
- `Timestamp` (DateTime) - When the error occurred (UTC)
- `TraceId` (string) - Correlation ID for tracing the request through logs and monitoring systems
- `Errors` (Dictionary<string, List<string>>?) - Optional validation errors dictionary where keys are field names and values are lists of error messages (only present for ValidationException)

### Exception Handling

The middleware handles specific exception types with appropriate HTTP status codes:

- `ValidationException` → 422 Unprocessable Entity
- `ConfigurationNotFoundException` → 404 Not Found  
- `ConfigurationKeyNotFoundException` → 404 Not Found
- `ConfigurationException` → 400 Bad Request
- `UnauthorizedAccessException` → 401 Unauthorized
- All other exceptions → 500 Internal Server Error

### Related Classes

- `ErrorResponse` - The standardized error response structure returned by the middleware
- `ValidationException` - Exception thrown when validation fails (from DotnetConfigServer.Exceptions)
- `ConfigurationNotFoundException` - Exception thrown when a configuration is not found
- `ConfigurationKeyNotFoundException` - Exception thrown when a configuration key is not found

## PerformanceMonitoringMiddleware

The `PerformanceMonitoringMiddleware` class monitors HTTP request performance metrics including request duration, memory usage, and CPU time. It helps identify performance bottlenecks by tracking request metrics and logging warnings when requests exceed configured thresholds. The middleware integrates with the `PerformanceMetrics` class to provide aggregated performance statistics and historical data.

### Usage Example

```csharp
using DotnetConfigServer.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register the performance metrics service
builder.Services.AddSingleton<PerformanceMetrics>();

var app = builder.Build();

// Use the performance monitoring middleware
// Typically registered after UseRouting() and before UseEndpoints()
app.UsePerformanceMonitoring();

// Access metrics for monitoring and diagnostics
var metrics = app.Services.GetRequiredService<PerformanceMetrics>();

// Get recent performance metrics
var recentMetrics = metrics.GetRecentMetrics(100);
foreach (var metric in recentMetrics)
{
    Console.WriteLine($"{metric.Timestamp:yyyy-MM-dd HH:mm:ss} | {metric.Method} {metric.Path} | {metric.DurationMs}ms | {metric.MemoryUsedBytes / 1024 / 1024}MB");
}

// Calculate average duration for a specific endpoint
var avgDuration = metrics.GetAverageDurationMs("/api/v1/configurations");
Console.WriteLine($"Average duration for /api/v1/configurations: {avgDuration}ms");

// Log performance summary
metrics.LogSummary();
```

### Public Members

#### PerformanceMonitoringMiddleware

- `PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, PerformanceMetrics metrics)` - Constructor that accepts the next middleware in the pipeline, logger, and metrics service
- `InvokeAsync(HttpContext context)` - Main middleware entry point that processes HTTP requests and records performance metrics

#### PerformanceMetrics

- `PerformanceMetrics(ILogger<PerformanceMetrics> logger)` - Constructor that initializes the metrics service with a logger
- `void RecordRequest(RequestMetric metric)` - Records a new request metric
- `IEnumerable<RequestMetric> GetRecentMetrics(int count = 100)` - Retrieves recent request metrics (default: last 100)
- `double GetAverageDurationMs(string path = null)` - Calculates average request duration in milliseconds, optionally filtered by path
- `void LogSummary()` - Logs a performance summary with average duration, memory usage, and slow request count

#### RequestMetric

- `Path` (string) - The HTTP request path
- `Method` (string) - The HTTP request method (GET, POST, etc.)
- `StatusCode` (int) - The HTTP response status code
- `DurationMs` (long) - Request duration in milliseconds
- `MemoryUsedBytes` (long) - Memory used by the request in bytes
- `Timestamp` (DateTime) - When the request was processed (UTC)

### Metrics Endpoint

To access live metrics collected by the middleware, use the `/metrics` endpoint:

```bash
curl -s http://localhost:5000/metrics | jq .
```

This returns Prometheus-compatible metrics including:
- Request duration histograms
- Memory usage statistics
- Request counts by endpoint
- Error rates

### Related Classes

- `RequestMetric` - Represents a single request performance measurement with properties for path, method, status code, duration, memory usage, and timestamp

## BatchConfigurationImporter

The `BatchConfigurationImporter` class provides utilities for bulk importing, exporting, cloning, and merging configuration keys across different environments. It simplifies common batch operations like migrating configurations from JSON files, setting up new environments, and synchronizing configurations between development, staging, and production environments.



### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Initialize the importer with your server URL
var importer = new BatchConfigurationImporter("https://localhost:5001");

// Example 1: Import configurations from a JSON file
var importResult = await importer.ImportFromJsonAsync("configurations.json");
Console.WriteLine($"Import completed: {importResult.SuccessCount} succeeded, {importResult.FailureCount} failed");

// Example 2: Export configuration to JSON file for backup
await importer.ExportToJsonAsync(
    "550e8400-e29b-41d4-a716-446655440001",
    "backup-configuration.json");

// Example 3: Clone configuration from one environment to another
var cloneResult = await importer.CloneConfigurationAsync(
    "dev-config-id",
    "prod-config-id");
Console.WriteLine($"Clone completed: {cloneResult}");

// Example 4: Merge configurations with overwrite enabled
var mergeResult = await importer.MergeConfigurationsAsync(
    "source-config-id",
    "target-config-id",
    overwrite: true);
Console.WriteLine($"Merge completed: {mergeResult}");
```

### File Format for JSON Import

The JSON file should follow this structure:

```json
{
  "configurationId": "550e8400-e29b-41d4-a716-446655440001",
  "keys": [
    {
      "key": "Database:Host",
      "value": "prod-db.example.com",
      "isEncrypted": false,
      "description": "Database server hostname"
    },
    {
      "key": "Database:Password",
      "value": "SecurePassword123!",
      "isEncrypted": true,
      "description": "Database password"
    },
    {
      "key": "Features:EnableNewCheckout",
      "value": "true",
      "isEncrypted": false,
      "description": "Enable new checkout flow"
    }
  ]
}
```

### Public Members

- `BatchConfigurationImporter(string baseUrl)` - Constructor that initializes the importer with server URL
- `ImportFromJsonAsync(string jsonFilePath)` - Import configurations from a JSON file
- `ImportAsync(BatchImportRequest request)` - Import configurations from an object
- `ExportToJsonAsync(string configurationId, string outputPath)` - Export configuration to JSON file for backup
- `CloneConfigurationAsync(string sourceConfigId, string targetConfigId)` - Clone configuration from one environment to another
- `MergeConfigurationsAsync(string sourceConfigId, string targetConfigId, bool overwrite = true)` - Merge configurations with optional overwrite

### Result Classes

- `BatchImportResult` - Contains import statistics:
  - `SuccessCount` - Number of successfully imported keys
  - `FailureCount` - Number of failed imports
  - `Errors` - List of error messages for failed imports
  - `ToString()` - Returns formatted summary string

- `BatchImportRequest` - Request object for batch import:
  - `ConfigurationId` - Target configuration ID
  - `Keys` - List of configuration keys to import

- `ConfigurationKeyImport` - Individual configuration key:
  - `Key` - Configuration key name
  - `Value` - Configuration value
  - `IsEncrypted` - Whether the value should be encrypted
  - `Description` - Optional description

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

## IApiResponseTransformer

The `IApiResponseTransformer` interface provides methods for transforming and processing JSON API responses. It handles deserialization into typed objects, field mapping, field extraction, and flattening of nested JSON structures.

### Usage Example

```csharp
using DotnetConfigServer.Integration;

// Create transformer instance (typically via dependency injection)
var transformer = new ApiResponseTransformer(logger);

// Example 1: Basic transformation
string jsonResponse = "{\"name\":\"OrderService\",\"status\":\"active\",\"version\":1}";
var serviceConfig = transformer.Transform<ServiceConfiguration>(jsonResponse);
Console.WriteLine(serviceConfig.Name); // "OrderService"

// Example 2: Transform with field mapping
string apiResponse = "{\"serviceName\":\"PaymentGateway\",\"serviceStatus\":\"running\"}";
var mappedConfig = transformer.TransformWithMapping<ServiceStatus>(
    apiResponse,
    new Dictionary<string, string> { ["Name"] = "serviceName", ["Status"] = "serviceStatus" }
);
Console.WriteLine(mappedConfig.Name); // "PaymentGateway"
Console.WriteLine(mappedConfig.Status); // "running"

// Example 3: Extract specific fields
string complexResponse = "{\"database\":{\"host\":\"db.example.com\",\"port\":5432},\"cache\":{\"enabled\":true}}";
var extracted = transformer.ExtractFields(
    complexResponse,
    "database.host",
    "cache.enabled"
);
Console.WriteLine(extracted["database.host"]); // "db.example.com"
Console.WriteLine(extracted["cache.enabled"]); // true

// Example 4: Flatten nested JSON
string nestedJson = "{\"service\":{\"name\":\"ConfigServer\",\"endpoints\":{\"health\":\"/health\",\"config\":\"/api/config\"}}}";
var flattened = transformer.Flatten(nestedJson);
foreach (var kvp in flattened)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
// Output:
// service.name: ConfigServer
// service.endpoints.health: /health
// service.endpoints.config: /api/config
```

### Public Members

- `T Transform<T>(string json)` - Deserializes JSON into a strongly-typed object
- `T TransformWithMapping<T>(string json, Dictionary<string, string> fieldMapping)` - Transforms JSON with custom field name mapping
- `Dictionary<string, object?> ExtractFields(string json, params string[] fields)` - Extracts specific fields from JSON
- `Dictionary<string, object?> Flatten(string json, string separator = ".")` - Flattens nested JSON structure into a flat dictionary

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

## ValidationRule

The `ValidationRule` class represents a reusable configuration validation rule that can be applied to configuration keys matching a specific pattern. It supports various rule types including regex matching, length constraints, allowed values, numeric ranges, URL validation, JSON validation, and cross-key validation. Rules can be scoped to specific configurations or defined globally, and are automatically enforced during configuration validation operations.

### Usage Example

```csharp
using DotnetConfigServer.Models;

// Create a URL validation rule for service endpoints
var urlRule = new ValidationRule
{
    Name = "Service URL Validation",
    Description = "All service URL keys must contain valid absolute URLs",
    ConfigurationId = configurationId, // Optional: null for global rules
    RuleType = ValidationRuleType.Url,
    TargetKeyPattern = ".*ServiceUrl$", // Matches keys ending with "ServiceUrl"
    Parameters = null, // Optional parameters for specific rule types
    IsActive = true,
    CreatedBy = "admin@example.com"
};

// Create a regex validation rule for database connection strings
var regexRule = new ValidationRule
{
    Name = "Database Connection Regex",
    Description = "Database connection strings must match required format",
    RuleType = ValidationRuleType.Regex,
    TargetKeyPattern = "Database:ConnectionString",
    Parameters = @"^Server=[^;]+;Database=[^;]+;.*$",
    IsActive = true,
    CreatedBy = "admin@example.com"
};

// Create a numeric range validation rule for timeout values
var rangeRule = new ValidationRule
{
    Name = "Timeout Range Validation",
    Description = "Timeout values must be between 10 and 300 seconds",
    RuleType = ValidationRuleType.NumericRange,
    TargetKeyPattern = ".*Timeout$",
    Parameters = "10,300", // min,max format
    IsActive = true,
    CreatedBy = "admin@example.com"
};

// Add the rule via API
var ruleResponse = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configurationId}/validation-rules",
    urlRule);

// Validate the configuration against all active rules
var validationResult = await client.PostAsJsonAsync(
    $"/api/v1/configurations/{configurationId}/validation-rules/validate",
    new { });

if (!validationResult.IsValid)
{
    foreach (var violation in validationResult.Violations)
    {
        Console.WriteLine($"{violation.KeyName}: {violation.Message}");
    }
}
```

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

### AuditLog

The `AuditLog` class represents a comprehensive audit trail entry that tracks all changes and actions performed within the configuration server. It maintains a complete history of configuration modifications, user actions, timestamps, and contextual information including IP addresses and user agents. Audit logs are automatically created for all configuration operations and can be queried to provide compliance reports, security investigations, and change tracking.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create an audit log entry for a configuration creation
var createLog = AuditLog.CreateEntry(
    configurationId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    entityType: "Configuration",
    entityId: "123e4567-e89b-12d3-a456-426614174001",
    entityName: "Production Settings",
    userId: "admin@example.com",
    userEmail: "admin@example.com",
    details: "Created new production configuration",
    newValues: "{\"Name\":\"Production Settings\",\"Environment\":\"Production\"}"
);

// Set request context with IP address and user agent
createLog.SetRequestContext("192.168.1.100", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

Console.WriteLine($"Created at: {createLog.Timestamp:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Action: {createLog.ActionType}");
Console.WriteLine($"Status: {createLog.Status}");

// Create an audit log entry for a configuration update
var updateLog = AuditLog.UpdateEntry(
    configurationId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    entityType: "Configuration",
    entityId: "123e4567-e89b-12d3-a456-426614174001",
    entityName: "Production Settings",
    userId: "backend-team@example.com",
    userEmail: "backend-team@example.com",
    oldValues: "{\"Database:Host\":\"localhost\"}",
    newValues: "{\"Database:Host\":\"prod-db.example.com\"}",
    details: "Updated database host for production environment"
);

updateLog.SetRequestContext("10.0.0.50", "curl/8.6.0");

Console.WriteLine($"Updated at: {updateLog.Timestamp:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Changes: {updateLog.OldValues} → {updateLog.NewValues}");

// Create an audit log entry for a configuration deletion
var deleteLog = AuditLog.DeleteEntry(
    configurationId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    entityType: "Configuration",
    entityId: "123e4567-e89b-12d3-a456-426614174002",
    entityName: "Legacy Settings",
    userId: "admin@example.com",
    userEmail: "admin@example.com",
    oldValues: "{\"Name\":\"Legacy Settings\",\"Environment\":\"Staging\"}"
);

deleteLog.SetRequestContext("172.16.0.10", "PostmanRuntime/7.36.0");

Console.WriteLine($"Deleted at: {deleteLog.Timestamp:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Deleted entity: {deleteLog.EntityName}");

// Mark an audit log as failed (e.g., during error handling)
var failedLog = AuditLog.CreateEntry(
    configurationId: Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    entityType: "Configuration",
    entityId: "123e4567-e89b-12d3-a456-426614174003",
    entityName: "Failed Operation",
    userId: "service-account@example.com",
    userEmail: "service-account@example.com",
    details: "Attempted to create configuration with invalid data"
);

failedLog.MarkAsFailed("Validation failed: Configuration name cannot be empty");

Console.WriteLine($"Status: {failedLog.Status}");
Console.WriteLine($"Failure reason: {failedLog.Details}");
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

## DotnetConfigServerOptions

`DotnetConfigServerOptions` is the root configuration class that defines all settings for the Dotnet Config Server application. It uses the `IOptions<DotnetConfigServerOptions>` pattern for type-safe, validated configuration and supports environment variable overrides using double underscores as separators.

The options class includes nested configuration objects for different subsystems:
- **ApplicationSettings**: Core application behavior and API settings
- **Encryption**: Configuration for encrypting sensitive configuration values using AES-256
- **Webhook**: Webhook delivery configuration including retry policies and signature verification
- **RateLimit**: API rate limiting to prevent abuse
- **Cache**: Caching configuration for performance optimization
- **Database**: Database connection and behavior settings
- **Performance**: Performance monitoring and metrics collection
- **Security**: Security-related application settings

### Usage Example

```csharp
// Configure services in Program.cs
builder.Services.Configure<DotnetConfigServerOptions>(options =>
{
    options.ApplicationSettings = new ApplicationSettingsOptions
    {
        ApiVersion = "v2",
        MaxVersionHistory = 200,
        EnableCors = true,
        EnableSwagger = false,
        EnableDetailedErrors = false,
        EnableRequestLogging = true,
        EnablePerformanceMonitoring = true
    };

    options.Encryption = new EncryptionOptions
    {
        KeySize = 256,
        SaltSize = 32,
        Iterations = 20000,
        Algorithm = "AES256"
    };

    options.Webhook = new WebhookOptions
    {
        MaxRetries = 10,
        TimeoutSeconds = 60,
        BatchSize = 200,
        EnableSignatureVerification = true,
        EnableAutoRetry = true
    };

    options.RateLimit = new RateLimitOptions
    {
        RequestsPerMinute = 500,
        RetryAfterSeconds = 30,
        EnableRateLimiting = true,
        RateLimitExemptPaths = new[] { "/health", "/metrics", "/swagger" }
    };

    options.Cache = new CacheOptions
    {
        DefaultDurationSeconds = 600,
        EnableDistributedCache = true,
        DistributedCacheDurationSeconds = 7200
    };

    options.Database = new DatabaseOptions
    {
        EnableConnectionPooling = true,
        ConnectionTimeoutSeconds = 60,
        CommandTimeoutSeconds = 60,
        EnableAutomaticMigration = true
    };

    options.Performance = new PerformanceOptions
    {
        EnableMetrics = true,
        MetricsSampleRate = 0.2,
        EnableRequestTracing = false,
        MaxRequestBodySizeKb = 2048
    };

    options.Security = new SecurityOptions
    {
        EnableHttpsRedirection = true,
        EnableRequestValidation = true,
        EnableCorsPolicy = true,
        TrustedOrigins = new[] { "https://config-ui.example.com", "https://dashboard.example.com" }
    };
});

// Access via dependency injection
var configOptions = serviceProvider.GetRequiredService<IOptions<DotnetConfigServerOptions>>().Value;
Console.WriteLine($"API Version: {configOptions.ApplicationSettings.ApiVersion}");
Console.WriteLine($"Encryption Algorithm: {configOptions.Encryption.Algorithm}");
Console.WriteLine($"Cache Duration: {configOptions.Cache.DefaultDurationSeconds}s");
```

### Public Members

- `ApplicationSettings` - Application-specific settings including API version, CORS, Swagger, and logging options
- `Encryption` - Encryption configuration for sensitive configuration values
- `Webhook` - Webhook delivery configuration
- `RateLimit` - Rate limiting configuration
- `Cache` - Caching configuration
- `Database` - Database settings
- `Performance` - Performance monitoring settings
- `Security` - Security settings

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

## VersioningAndRollback

The `VersioningAndRollback` class provides comprehensive configuration versioning, comparison, and rollback capabilities. It enables teams to track all configuration changes, compare versions, and safely roll back to previous states when issues arise. This class supports both blue-green and canary deployment patterns for zero-downtime configuration updates.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Initialize the versioning manager with your server URL
var versioning = new VersioningAndRollback("https://localhost:5001");
var configurationId = "550e8400-e29b-41d4-a716-446655440001";

// Create a new configuration version
var newVersion = await versioning.CreateVersionAsync(
    configurationId,
    "Q3 2026 release with new features",
    "Added payment gateway integration and updated timeout settings"
);
Console.WriteLine($"Created version {newVersion.Version}: {newVersion.Description}");

// Get the currently active version
var activeVersion = await versioning.GetActiveVersionAsync(configurationId);
Console.WriteLine($"Active version: {activeVersion.Version} ({activeVersion.Status})");

// Compare two versions to see what changed
var diffs = await versioning.CompareVersionsAsync(
    configurationId,
    activeVersion.Id,
    newVersion.Id
);
Console.WriteLine($"Changes: {diffs.Count} keys modified");

// Publish the new version to make it active
var publishedVersion = await versioning.PublishVersionAsync(
    configurationId,
    newVersion.Id,
    "Production release"
);
Console.WriteLine($"Published version {publishedVersion.Version}");

// Archive the old version (optional)
await versioning.ArchiveVersionAsync(configurationId, activeVersion.Id);
Console.WriteLine("Old version archived");

// Display version history
await versioning.DisplayVersionHistoryAsync(configurationId);

// Blue-green deployment pattern
// var result = await versioning.BlueGreenDeploymentAsync(
//     configurationId,
//     "Production release with zero downtime",
//     validateAsync: async (versionId) => {
//         // Run validation tests against the new version
//         return true;
//     }
// );

// Canary deployment pattern
// await versioning.CanaryDeploymentAsync(
//     configurationId,
//     "Feature flag rollout",
//     validateAsync: async (versionId, percentage) => {
//         // Run tests against the canary version at specified traffic percentage
//         return true;
//     }
// );
```

### Public Members

- `Id` - Unique identifier for the configuration version
- `Version` - Version number (sequential integer)
- `Status` - Current status (Draft, Published, Archived)
- `KeyCount` - Number of configuration keys in this version
- `Description` - Human-readable description of the version
- `CreatedAt` - When the version was created
- `PublishedAt` - When the version was published (null if not published)

### Public Methods

- `CreateVersionAsync(configurationId, description, changeNotes)` - Create a new configuration version from current state
- `ListVersionsAsync(configurationId)` - List all versions for a configuration
- `GetActiveVersionAsync(configurationId)` - Get the currently active version
- `PublishVersionAsync(configurationId, versionId, notes)` - Publish a version to make it the active version
- `CompareVersionsAsync(configurationId, fromVersionId, toVersionId)` - Compare two versions and get the differences
- `RollbackVersionAsync(configurationId, targetVersionId, reason)` - Roll back to a previous version
- `ArchiveVersionAsync(configurationId, versionId)` - Archive a version (removes it from active management)
- `DisplayVersionHistoryAsync(configurationId)` - Display version history in a formatted table
- `DisplayDifferencesAsync(configurationId, fromVersionId, toVersionId)` - Display differences between two versions in a formatted table
- `BlueGreenDeploymentAsync(configurationId, changeDescription, validateAsync)` - Automated blue-green deployment pattern
- `CanaryDeploymentAsync(configurationId, changeDescription, validateAsync)` - Canary deployment with gradual rollout

## EnrichedDiff

The `EnrichedDiff` class represents a configuration diff enriched with full version metadata for viewer display. It provides a comprehensive view of changes between two configuration versions with detailed statistics, individual change entries, and version context. This type is used by the diff viewer API to present rich, human-readable diff information with complete version history and change categorization.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create an enriched diff between two configuration versions
var enrichedDiff = new EnrichedDiff
{
    DiffId = Guid.NewGuid(),
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    FromVersion = new ConfigurationVersionSummary
    {
        Id = Guid.Parse("456e4567-e89b-12d3-a456-426614174001"),
        Version = 1,
        Status = "Published",
        KeyCount = 25,
        Description = "Initial production configuration",
        CreatedAt = DateTime.UtcNow.AddDays(-7)
    },
    ToVersion = new ConfigurationVersionSummary
    {
        Id = Guid.Parse("789e4567-e89b-12d3-a456-426614174002"),
        Version = 2,
        Status = "Published",
        KeyCount = 28,
        Description = "Q2 2026 release with new features",
        CreatedAt = DateTime.UtcNow
    },
    Changes = new List<DiffEntry>
    {
        new DiffEntry
        {
            Key = "Database:ConnectionString",
            ChangeType = ChangeType.Modified,
            OldValue = "Server=old-db.example.com;Database=Orders",
            NewValue = "Server=prod-db.example.com;Database=Orders;User=admin;"
        },
        new DiffEntry
        {
            Key = "Feature:EnableNewCheckout",
            ChangeType = ChangeType.Added,
            OldValue = null,
            NewValue = "true"
        },
        new DiffEntry
        {
            Key = "Legacy:OldFeature",
            ChangeType = ChangeType.Deleted,
            OldValue = "true",
            NewValue = null
        },
        new DiffEntry
        {
            Key = "Api:TimeoutSeconds",
            ChangeType = ChangeType.Modified,
            OldValue = "30",
            NewValue = "60"
        }
    },
    AddedCount = 1,
    ModifiedCount = 2,
    DeletedCount = 1,
    GeneratedAt = DateTime.UtcNow
};

// Access diff statistics
Console.WriteLine($"Diff between v{enrichedDiff.FromVersion.Version} → v{enrichedDiff.ToVersion.Version}");
Console.WriteLine($"Generated at: {enrichedDiff.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Total changes: {enrichedDiff.TotalChanges}");
Console.WriteLine($"Added: {enrichedDiff.AddedCount}");
Console.WriteLine($"Modified: {enrichedDiff.ModifiedCount}");
Console.WriteLine($"Deleted: {enrichedDiff.DeletedCount}");

// Access categorized changes
Console.WriteLine($"\nAdded keys:");
foreach (var change in enrichedDiff.AddedKeys)
{
    Console.WriteLine($"  + {change.Key} = {change.NewValue}");
}

Console.WriteLine($"\nModified keys:");
foreach (var change in enrichedDiff.ModifiedKeys)
{
    Console.WriteLine($"  * {change.Key}: '{change.OldValue}' → '{change.NewValue}'");
}

Console.WriteLine($"\nDeleted keys:");
foreach (var change in enrichedDiff.DeletedKeys)
{
    Console.WriteLine($"  - {change.Key}");
}

// Access version context
Console.WriteLine($"\nVersion context:");
Console.WriteLine($"  From: v{enrichedDiff.FromVersion.Version} - {enrichedDiff.FromVersion.Description}");
Console.WriteLine($"  To:   v{enrichedDiff.ToVersion.Version} - {enrichedDiff.ToVersion.Description}");
```

### Public Members

- `DiffId` - Unique identifier for the diff record
- `ConfigurationId` - The configuration being compared
- `FromVersion` - Metadata of the source (from) version
- `ToVersion` - Metadata of the target (to) version
- `Changes` - List of all individual key changes between the two versions
- `AddedCount` - Number of keys added in the target version
- `ModifiedCount` - Number of keys whose values changed
- `DeletedCount` - Number of keys removed in the target version
- `TotalChanges` - Sum of all added, modified, and deleted changes
- `GeneratedAt` - UTC timestamp when the diff was computed or retrieved
- `AddedKeys` - Filtered collection of change entries that represent newly added keys
- `ModifiedKeys` - Filtered collection of change entries that represent keys with updated values
- `DeletedKeys` - Filtered collection of change entries that represent removed keys

## ConfigurationDiff

The `ConfigurationDiff` class represents the difference between two configuration versions, tracking all changes including additions, modifications, and deletions. It provides a comprehensive audit trail of configuration changes with detailed metrics and methods for analyzing and summarizing the differences.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a configuration diff between two versions
var configurationDiff = new ConfigurationDiff
{
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    FromVersionId = Guid.Parse("456e4567-e89b-12d3-a456-426614174001"),
    ToVersionId = Guid.Parse("789e4567-e89b-12d3-a456-426614174002"),
    CreatedBy = "admin@example.com"
};

// Add changes to the diff
configurationDiff.AddChange("Database:ConnectionString", ChangeType.Modified,
    "Server=old-db.example.com;Database=Orders",
    "Server=prod-db.example.com;Database=Orders;User=admin;");

configurationDiff.AddChange("Feature:EnableNewCheckout", ChangeType.Added,
    null, "true");

configurationDiff.AddChange("Legacy:OldFeature", ChangeType.Deleted,
    "true", null);

// Get summary statistics
Console.WriteLine($"Total changes: {configurationDiff.TotalChanges}");
Console.WriteLine($"Added: {configurationDiff.AddedCount}");
Console.WriteLine($"Modified: {configurationDiff.ModifiedCount}");
Console.WriteLine($"Deleted: {configurationDiff.DeletedCount}");

// Get detailed changes
var addedChanges = configurationDiff.GetChangesByType(ChangeType.Added);
var modifiedChanges = configurationDiff.GetChangesByType(ChangeType.Modified);
var deletedChanges = configurationDiff.GetChangesByType(ChangeType.Deleted);

Console.WriteLine($"\nAdded keys:");
foreach (var change in addedChanges)
{
    Console.WriteLine($"  - {change.Key} = {change.NewValue}");
}

Console.WriteLine($"\nModified keys:");
foreach (var change in modifiedChanges)
{
    Console.WriteLine($"  - {change.Key}: '{change.OldValue}' → '{change.NewValue}'");
}

Console.WriteLine($"\nDeleted keys:");
foreach (var change in deletedChanges)
{
    Console.WriteLine($"  - {change.Key}");
}

// Get a structured summary
var summary = configurationDiff.GetSummary();
Console.WriteLine($"\nDiff summary - ID: {summary.Id}");
Console.WriteLine($"Created at: {summary.CreatedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Created by: {summary.CreatedBy}");

// Get human-readable summary
var changesSummary = configurationDiff.GetChangesSummary();
Console.WriteLine($"\n{changesSummary}");
```

### Public Members

- `Id` - Unique identifier for the diff
- `ConfigurationId` - The configuration being compared
- `FromVersionId` - The source version ID
- `ToVersionId` - The target version ID
- `CreatedAt` - When the diff was created
- `CreatedBy` - Who created the diff
- `TotalChanges` - Total number of changes (Added + Modified + Deleted)
- `AddedCount` - Number of added configuration keys
- `ModifiedCount` - Number of modified configuration keys
- `DeletedCount` - Number of deleted configuration keys
- `Changes` - List of all change entries
- `AddChange(key, changeType, oldValue, newValue)` - Add a new change entry
- `GetChangesByType(changeType)` - Filter changes by type
- `GetSummary()` - Get structured summary
- `GetChangesSummary()` - Get human-readable summary

## EncryptionKey

The `EncryptionKey` class represents an encryption key used for securing sensitive configuration values in the Dotnet Config Server. It manages key lifecycle including creation, activation, deactivation, rotation, and expiration tracking. Each encryption key stores the encrypted key material, salt, algorithm specification, and usage statistics to support secure configuration encryption operations.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a new encryption key for production use
var productionKey = new EncryptionKey
{
    Name = "Production Encryption Key 2026",
    KeyId = "prod-key-2026-001",
    Algorithm = EncryptionAlgorithm.AES256,
    EncryptedKey = Convert.FromBase64String("base64-encoded-encrypted-key-data"),
    Salt = Convert.FromBase64String("base64-encoded-salt-data"),
    Description = "Primary encryption key for production configuration encryption",
    ExpiresAt = DateTime.UtcNow.AddYears(1),
    CreatedBy = "security-team@example.com",
    IsPrimary = true
};

// Validate the encryption key
try
{
    productionKey.Validate();
    Console.WriteLine("Encryption key is valid!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// Check key status
Console.WriteLine($"Key is active: {productionKey.IsActive}");
Console.WriteLine($"Key is primary: {productionKey.IsPrimary}");
Console.WriteLine($"Key expires at: {productionKey.ExpiresAt:yyyy-MM-dd}");
Console.WriteLine($"Days until expiration: {(productionKey.ExpiresAt - DateTime.UtcNow).TotalDays:F1}");

// Track usage
productionKey.IncrementUsage();
Console.WriteLine($"Usage count: {productionKey.UsageCount}");

// Check if key needs rotation (within 30 days of expiration)
if (productionKey.IsNearExpiration())
{
    Console.WriteLine("WARNING: Key is near expiration and should be rotated soon");
}

// Deactivate a key when it's compromised or no longer needed
var oldKey = new EncryptionKey
{
    Name = "Old Key 2025",
    KeyId = "old-key-2025",
    Algorithm = EncryptionAlgorithm.AES256,
    EncryptedKey = Convert.FromBase64String("old-encrypted-key"),
    Salt = Convert.FromBase64String("old-salt"),
    ExpiresAt = DateTime.UtcNow.AddDays(-1), // Already expired
    CreatedBy = "security-team@example.com"
};

oldKey.Deactivate();
Console.WriteLine($"Old key active status: {oldKey.IsActive}");

// Get a safe summary view (without sensitive encrypted data)
var summary = productionKey.GetSummary();
Console.WriteLine($"Key summary - {summary.Name} ({summary.KeyId}): {summary.UsageCount} uses");
```

## ConfigurationKey

The `ConfigurationKey` class represents a single key-value pair within a configuration. It provides comprehensive validation, type conversion, and metadata tracking capabilities. Configuration keys support encryption, validation rules, and soft deletion while maintaining a complete audit trail through timestamps and user tracking.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a new configuration key for a database connection string
var databaseKey = new ConfigurationKey
{
    Key = "Database:ConnectionString",
    Value = "Server=prod-db.example.com;Database=Orders;User=admin;Password=Secure123!",
    DefaultValue = "Server=localhost;Database=Orders",
    Description = "Production database connection string for order processing service",
    ValueType = ConfigurationValueType.String,
    IsEncrypted = true,  // Will be automatically encrypted when stored
    IsRequired = true,
    IsSensitive = true,
    IsActive = true,
    CreatedBy = "backend-team@example.com",
    UpdatedBy = "backend-team@example.com",
    MinLength = 10,
    MaxLength = 500,
    ValidationRegex = @"^Server=[^;]+;Database=[^;]+"
};

// Validate the configuration key
try
{
    databaseKey.Validate();
    Console.WriteLine("Configuration key is valid!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  {error.Key}: {error.Value}");
    }
}

// Add additional validation rules
var timeoutKey = new ConfigurationKey
{
    Key = "Api:TimeoutSeconds",
    Value = "30",
    ValueType = ConfigurationValueType.Integer,
    Description = "API request timeout in seconds",
    IsRequired = true,
    MinValue = 5,
    MaxValue = 300,
    CreatedBy = "backend-team@example.com"
};

// Parse typed values from configuration keys
int timeoutValue = (int)timeoutKey.GetTypedValue();
Console.WriteLine($"Timeout value: {timeoutValue} seconds");

bool isValid = bool.Parse(new ConfigurationKey
{
    Key = "Feature:EnableNewCheckout",
    Value = "true",
    ValueType = ConfigurationValueType.Boolean,
    CreatedBy = "product-team@example.com"
}.GetTypedValue()?.ToString() ?? "false");

Console.WriteLine($"New checkout feature enabled: {isValid}");

// Update key metadata
var featureKey = new ConfigurationKey
{
    Key = "Feature:EnableAnalytics",
    Value = "true",
    Description = "Enable user analytics tracking",
    CreatedBy = "product-team@example.com"
};

featureKey.Update("false", "Disable analytics for privacy compliance", "compliance-team@example.com");
Console.WriteLine($"Updated at: {featureKey.UpdatedAt}");
Console.WriteLine($"Updated by: {featureKey.UpdatedBy}");

// Soft delete a configuration key
var deprecatedKey = new ConfigurationKey
{
    Key = "Legacy:OldFeature",
    Value = "true",
    CreatedBy = "legacy-team@example.com"
};

deprecatedKey.Delete();
Console.WriteLine($"Key deleted at: {deprecatedKey.DeletedAt}");
Console.WriteLine($"Is active: {deprecatedKey.IsActive}");

// Get summary representation
var summary = databaseKey.GetSummary();
Console.WriteLine($"Key summary - Id: {summary.Id}, Key: {summary.Key}, Type: {summary.ValueType}");
```

## Configuration

The `Configuration` class represents a configuration profile that contains multiple key-value pairs for a specific application and environment. It manages configuration state including encryption settings, versioning, soft deletion, and audit trails. Configurations support hierarchical inheritance through `ParentConfigurationId`, enabling base configurations to be extended by child configurations.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Common;
using System;

// Create a new configuration for a production order processing service
var productionConfig = new Configuration
{
    Name = "OrderProcessingService-Production",
    Description = "Production configuration for order processing service with payment integration",
    Environment = Environment.Production,
    ApplicationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    IsEncrypted = true,
    EncryptionAlgorithm = EncryptionAlgorithm.AES256,
    EncryptionKeyId = "prod-key-2026-001",
    CreatedBy = "backend-team@example.com",
    IsActive = true
};

// Validate the configuration
try
{
    productionConfig.Validate();
    Console.WriteLine("Configuration validation successful!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value)}");
    }
}

// Create a new version for the next deployment
var newVersion = productionConfig.CreateNewVersion();
newVersion.Description = "Q3 2026 release with new features and optimizations";
newVersion.UpdatedBy = "backend-team@example.com";

Console.WriteLine($"Original config version: {productionConfig.VersionNumber}");
Console.WriteLine($"New version number: {newVersion.VersionNumber}");
Console.WriteLine($"New version ID: {newVersion.Id}");

// Update configuration metadata
productionConfig.Update(
    "OrderProcessingService-Production-v2",
    "Updated production configuration with new payment gateway settings",
    Environment.Production,
    true,
    "backend-team@example.com"
);

Console.WriteLine($"Updated configuration: {productionConfig.Name}");
Console.WriteLine($"Last updated: {productionConfig.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

// Set encryption settings
productionConfig.SetEncryption(EncryptionAlgorithm.AES256, "prod-key-2026-002");
Console.WriteLine($"Encryption enabled: {productionConfig.IsEncrypted}");
Console.WriteLine($"Encryption algorithm: {productionConfig.EncryptionAlgorithm}");

// Soft delete a configuration (mark as inactive)
var oldConfig = new Configuration
{
    Name = "Legacy-Configuration",
    ApplicationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    Environment = Environment.Development,
    CreatedBy = "admin@example.com"
};

oldConfig.Delete("admin@example.com");
Console.WriteLine($"Configuration deleted at: {oldConfig.DeletedAt}");
Console.WriteLine($"Is active: {oldConfig.IsActive}");

// Get summary view (without sensitive data)
var summary = productionConfig.GetSummary();
Console.WriteLine($"Configuration summary - {summary.Name} ({summary.Id}): v{summary.VersionNumber}");
```

## ChangeRequest

The `ChangeRequest` class represents a pending configuration change that requires approval before being applied. It implements a controlled change management workflow with status tracking, reviewer assignment, and audit trails. Change requests support four operations: creating, updating, or deleting configuration keys, or performing configuration-level operations. Once approved, the change can be automatically applied or scheduled for later deployment.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System.Text.Json;

// Create a new change request for updating a configuration key
var changeRequest = new ChangeRequest
{
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    ConfigurationKeyId = Guid.Parse("789e4567-e89b-12d3-a456-426614174001"),
    Operation = ChangeRequestOperation.UpdateKey,
    Payload = JsonSerializer.Serialize(new
    {
        Key = "Database:ConnectionString",
        Value = "Server=prod-db.example.com;Database=Orders;User=admin;Password=secure123;"
    }),
    Summary = "Update production database connection string for performance optimization",
    RequestedBy = "backend-team@example.com"
};

// Requester submits the change request
changeRequest.Status = ChangeRequestStatus.Pending;
changeRequest.RequestedAt = DateTime.UtcNow;

Console.WriteLine($"Change request created: {changeRequest.Id}");
Console.WriteLine($"Status: {changeRequest.Status}");
Console.WriteLine($"Requested by: {changeRequest.RequestedBy}");

// Reviewer approves the change request
changeRequest.Approve("security-team@example.com", "Verified no sensitive data exposure");

Console.WriteLine($"Change approved by: {changeRequest.ReviewedBy}");
Console.WriteLine($"Approval time: {changeRequest.ReviewedAt}");
Console.WriteLine($"Status: {changeRequest.Status}");

// After applying the change, mark it as applied
changeRequest.MarkApplied("deployment-bot");

Console.WriteLine($"Change applied by: {changeRequest.AppliedBy}");
Console.WriteLine($"Applied at: {changeRequest.AppliedAt}");

// Alternative: Reject a change request
// changeRequest.Reject("security-team@example.com", "Contains hardcoded credentials");

// Alternative: Cancel a pending change request
// changeRequest.Cancel();
```

### Public Members

- `Id` - Unique identifier for the change request
- `ConfigurationId` - The configuration being modified
- `ConfigurationKeyId` - The specific configuration key being modified (for key-level operations)
- `Operation` - Type of operation (CreateKey, UpdateKey, DeleteKey, ConfigurationOperation)
- `Payload` - Serialized change payload containing the actual modification
- `Summary` - Brief description of the change
- `Status` - Current status (Pending, Approved, Rejected, Applied, Failed, Cancelled)
- `RequestedBy` - Who requested the change
- `RequestedAt` - When the request was submitted
- `ReviewedBy` - Who reviewed the change
- `ReviewedAt` - When the change was reviewed
- `ApprovedBy` - Who approved the change
- `RejectedBy` - Who rejected the change
- `AppliedBy` - Who applied the change
- `AppliedAt` - When the change was applied
- `RejectedAt` - When the change was rejected
- `CancelledAt` - When the change was cancelled
- `Reason` - Reason for rejection or cancellation
- `Approve(reviewer, comment)` - Approve the change request
- `Reject(reviewer, comment)` - Reject the change request
- `Cancel()` - Cancel a pending change request
- `MarkApplied(appliedBy)` - Mark the change as applied
- `Validate()` - Validate the change request

## AuditLogViewer

The `AuditLogViewer` class provides comprehensive functionality for retrieving, analyzing, and displaying audit logs from the configuration server. It's designed for compliance reporting, debugging, and monitoring configuration changes across applications and environments. The viewer supports filtering by action type, date range, and user, and provides both programmatic access to audit data and formatted display methods for human-readable output.

### Usage Example

```csharp
using DotnetConfigServer.Examples;
using System;
using System.Threading.Tasks;

// Create an audit log viewer instance
var viewer = new AuditLogViewer("https://localhost:5001");
var configId = "550e8400-e29b-41d4-a716-446655440001";

// Display recent audit logs in a formatted table
await viewer.DisplayAuditLogsAsync(configId);

// Get changes made by a specific user
var userChanges = await viewer.GetUserChangesAsync(configId, "admin@example.com");
Console.WriteLine($"User made {userChanges.Count} changes");

// Get all configuration changes within a date range
var fromDate = DateTime.UtcNow.AddDays(-30);
var toDate = DateTime.UtcNow;
var recentChanges = await viewer.GetChangesInDateRangeAsync(configId, fromDate, toDate);
Console.WriteLine($"Recent changes: {recentChanges.Count}");

// Display changes to a specific configuration key
await viewer.DisplayKeyChangeHistoryAsync(configId, "Database:Host");

// Generate an audit report showing activity summary
await viewer.DisplayAuditReportAsync(configId, days: 30);

// Export audit logs to CSV
await viewer.ExportAuditLogsAsync(configId, "audit-logs.csv");

// Detect suspicious activity patterns
await viewer.DetectAnomaliesAsync(configId);
```

### Public Members

- `Id` - Unique identifier for the AuditLogViewer instance
- `GetAuditLogsAsync(configurationId, action, fromDate, toDate, pageSize, pageNumber)` - Retrieve audit logs with optional filtering
- `DisplayAuditLogsAsync(configurationId, action, pageSize)` - Display audit logs in formatted table
- `GetUserChangesAsync(configurationId, user)` - Get changes made by a specific user
- `GetChangesInDateRangeAsync(configurationId, fromDate, toDate)` - Get all configuration changes within a date range
- `DisplayKeyChangeHistoryAsync(configurationId, keyName)` - Display changes to a specific configuration key
- `DisplayAuditReportAsync(configurationId, days)` - Generate an audit report showing activity summary
- `ExportAuditLogsAsync(configurationId, filePath)` - Export audit logs to CSV format
- `DetectAnomaliesAsync(configurationId)` - Monitor for suspicious activity patterns

## WebhookDelivery

The `WebhookDelivery` class represents a webhook delivery attempt that tracks the status and outcome of webhook notifications sent to subscribers when configuration changes occur. It manages retry logic, error tracking, response handling, and provides methods for marking deliveries as successful or failed.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

// Create a webhook delivery for a configuration change event
var webhookDelivery = new WebhookDelivery
{
    WebhookSubscriptionId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    ConfigurationVersionId = Guid.Parse("456e4567-e89b-12d3-a456-426614174001"),
    EventId = Guid.Parse("789e4567-e89b-12d3-a456-426614174002"),
    Payload = "{\"configurationId\":\"123e4567-e89b-12d3-a456-426614174000\",\"eventType\":\"ConfigurationUpdated\",\"versionId\":\"456e4567-e89b-12d3-a456-426614174001\"}",
    EventType = "ConfigurationUpdated",
    Url = "https://order-service.example.com/config/webhook",
    AttemptNumber = 1,
    Status = WebhookDeliveryStatus.Pending
};

// Simulate sending the webhook to the subscriber
try
{
    using var httpClient = new HttpClient();
    var response = await httpClient.PostAsJsonAsync(webhookDelivery.Url, webhookDelivery.Payload);
    
    if (response.IsSuccessStatusCode)
    {
        // Mark as successful with response details
        webhookDelivery.MarkSuccess(
            statusCode: (int)response.StatusCode,
            responseTimeMs: 150,
            responseBody: await response.Content.ReadAsStringAsync()
        );
        
        Console.WriteLine($"Webhook delivered successfully to {webhookDelivery.Url}");
        Console.WriteLine($"Status code: {webhookDelivery.StatusCode}");
        Console.WriteLine($"Response time: {webhookDelivery.ResponseTimeMs}ms");
    }
    else
    {
        // Mark as failed and schedule retry
        webhookDelivery.MarkFailed(
            errorMessage: $"Webhook endpoint returned {response.StatusCode}",
            statusCode: (int)response.StatusCode,
            responseTimeMs: 200
        );
        
        if (webhookDelivery.ShouldRetry(maxRetries: 5))
        {
            webhookDelivery.ScheduleRetry(delaySeconds: 300);
            Console.WriteLine($"Webhook failed, scheduled retry at {webhookDelivery.NextRetryAt}");
        }
        else
        {
            Console.WriteLine("Max retries reached, delivery marked as failed");
        }
    }
}
catch (Exception ex)
{
    // Handle network errors or other exceptions
    webhookDelivery.MarkFailed(
        errorMessage: $"Network error: {ex.Message}",
        statusCode: 0
    );
    
    if (webhookDelivery.ShouldRetry())
    {
        webhookDelivery.ScheduleRetry();
        Console.WriteLine($"Error occurred, scheduled retry at {webhookDelivery.NextRetryAt}");
    }
}

// Get delivery summary
var summary = webhookDelivery.GetSummary();
Console.WriteLine($"Delivery {summary.Id}: Status={summary.Status}, Attempts={summary.AttemptNumber}");
```

### Public Members

- `Id` - Unique identifier for the delivery
- `WebhookSubscriptionId` - The webhook subscription that triggered this delivery
- `ConfigurationVersionId` - The configuration version being delivered
- `Status` - Current delivery status (Pending, Success, Failed, Retry)
- `EventId` - The original domain event ID
- `CreatedAt` - When the delivery was created
- `SentAt` - When the delivery was sent (null if not sent yet)
- `AttemptNumber` - Number of delivery attempts made
- `StatusCode` - HTTP status code from the webhook endpoint
- `ResponseTimeMs` - Response time in milliseconds
- `ResponseBody` - Response body from the webhook endpoint
- `ErrorMessage` - Error message if delivery failed
- `Payload` - The webhook payload content
- `EventType` - Type of event being delivered
- `Url` - The webhook endpoint URL
- `NextRetryAt` - When the next retry is scheduled
- `MarkSuccess(statusCode, responseTimeMs, responseBody)` - Mark delivery as successful
- `MarkFailed(errorMessage, statusCode, responseTimeMs)` - Mark delivery as failed
- `ScheduleRetry(delaySeconds)` - Schedule a retry attempt
- `ShouldRetry(maxRetries)` - Check if delivery should be retried
- `GetSummary()` - Get a summary view of the delivery
- `Validate()` - Validate the delivery

## WebhookSubscription

The `WebhookSubscription` class represents a webhook subscription for configuration change notifications. It manages webhook endpoints that receive real-time notifications when configuration changes occur, supporting event filtering, retry policies, HMAC signature verification, and comprehensive delivery tracking. Webhook subscriptions enable services to react to configuration updates without polling, ensuring immediate response to changes.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;
using System.Collections.Generic;

// Create a new webhook subscription for configuration change notifications
var webhookSubscription = new WebhookSubscription
{
    Name = "OrderService Configuration Webhook",
    Url = "https://order-service.example.com/api/config/webhook",
    Description = "Receive notifications for configuration changes in the OrderService",
    ConfigurationId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    Status = WebhookStatus.Active,
    IsActive = true,
    MaxRetries = 5,
    VerifySignature = true,
    Secret = "your-webhook-secret-key-here",
    CreatedBy = "backend-team@example.com",
    CustomHeaders = new Dictionary<string, string>
    {
        { "X-Custom-Header", "CustomValue" },
        { "User-Agent", "OrderService-Config-Client/1.0" }
    },
    TriggerEvents = new List<string> { "ConfigurationUpdated", "VersionPublished" }
};

// Validate the webhook subscription
try
{
    webhookSubscription.Validate();
    Console.WriteLine("Webhook subscription is valid!");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($" {error.Key}: {string.Join(", ", error.Value)}");
    }
}

// Generate HMAC signature for webhook payload verification
var payload = "{\"configurationId\":\"123e4567-e89b-12d3-a456-426614174000\",\"event\":\"ConfigurationUpdated\"}";
var signature = webhookSubscription.GenerateSignature(payload);
Console.WriteLine($"Generated signature: {signature}");

// Simulate successful delivery
webhookSubscription.ResetRetryCount(200);
Console.WriteLine($"Last delivery at: {webhookSubscription.LastDeliveryAt}");
Console.WriteLine($"Retry count: {webhookSubscription.RetryCount}");
Console.WriteLine($"Status: {webhookSubscription.Status}");

// Deactivate webhook when no longer needed
// webhookSubscription.Deactivate();

// Get summary view (without sensitive data)
var summary = webhookSubscription.GetSummary();
Console.WriteLine($"Webhook summary - {summary.Name} ({summary.Id}): {summary.Status}");
```

## ConfigurationSnapshot

The `ConfigurationSnapshot` class represents a point-in-time capture of a configuration's state, including the serialized configuration data and associated keys. Snapshots are automatically created whenever a configuration is modified, providing a complete audit trail for rollback operations and compliance reporting.

### Usage Example

```csharp
using DotnetConfigServer.Models;
using System;

// Create a new configuration snapshot
var snapshot = new ConfigurationSnapshot
{
    ConfigurationId = Guid.NewGuid(),
    ConfigurationState = "{\"Name\":\"ProductionService\",\"Environment\":\"Production\"}",
    KeysState = "[{\"Key\":\"Database:Host\",\"Value\":\"prod-db.example.com\"},{\"Key\":\"Database:Port\",\"Value\":\"5432\"}]",
    CreatedBy = "admin@example.com",
    Reason = "Initial production deployment"
};

Console.WriteLine($"Snapshot created: {snapshot.Id}");
Console.WriteLine($"Configuration: {snapshot.ConfigurationId}");
Console.WriteLine($"Created at: {snapshot.CreatedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"State: {snapshot.ConfigurationState}");
```

## ExternalApiClient

The `ExternalApiClient` class provides a robust HTTP client for making external API calls with built-in retry logic, timeout handling, and comprehensive error logging. It simplifies making GET, POST, PUT, and DELETE requests while automatically managing connection retries and request timeouts.

### Usage Example

```csharp
using DotnetConfigServer.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Configure services (typically in Program.cs or Startup.cs)
services.AddHttpClient<ExternalApiClient>();
services.Configure<ExternalApiClientOptions>(options =>
{
    options.Timeout = TimeSpan.FromSeconds(60);
    options.MaxRetries = 5;
    options.RetryDelay = 2000;
});

// Resolve the client (via dependency injection)
var externalApiClient = serviceProvider.GetRequiredService<ExternalApiClient>();

// Example 1: GET request
try
{
    var userData = await externalApiClient.GetAsync<User>(
        "https://api.example.com/users/123",
        new Dictionary<string, string> { ["Authorization"] = "Bearer token123" }
    );
    Console.WriteLine($"User: {userData?.Name}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to get user: {ex.Message}");
}

// Example 2: POST request with request/response types
var newOrder = new { ProductId = 456, Quantity = 2 };
var orderResponse = await externalApiClient.PostAsync<
    object,  // Request type
    OrderResponse  // Response type
>(
    "https://api.example.com/orders",
    newOrder,
    new Dictionary<string, string> { ["Authorization"] = "Bearer token123" }
);

// Example 3: PUT request to update a resource
var updateData = new { Status = "completed", Notes = "Shipped on time" };
var updatedOrder = await externalApiClient.PutAsync<
    object,
    OrderResponse
>(
    "https://api.example.com/orders/789",
    updateData
);

// Example 4: DELETE request
await externalApiClient.DeleteAsync(
    "https://api.example.com/orders/999"
);
```

### Public Members

- `GetAsync<T>(string url, Dictionary<string, string>? headers = null)` - Makes a GET request to an external API and deserializes the JSON response into type T
- `PostAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null)` - Makes a POST request with JSON request body and deserializes the JSON response
- `PutAsync<TRequest, TResponse>(string url, TRequest data, Dictionary<string, string>? headers = null)` - Makes a PUT request with JSON request body and deserializes the JSON response
- `DeleteAsync(string url, Dictionary<string, string>? headers = null)` - Makes a DELETE request to an external API
- `Timeout` (property from `ExternalApiClientOptions`) - HTTP request timeout duration
- `MaxRetries` (property from `ExternalApiClientOptions`) - Maximum number of retry attempts for failed requests
- `RetryDelay` (property from `ExternalApiClientOptions`) - Base delay in milliseconds between retry attempts (exponential backoff is applied)

## ConfigurationEventHandlers

The `ConfigurationEventHandlers` class is responsible for executing side effects in response to domain events within the configuration system. It manages essential tasks like invalidating cache entries, triggering webhook notifications, and generating internal system notifications whenever configuration changes occur.

### Usage Example

```csharp
using DotnetConfigServer.Events;

// Typically resolved via dependency injection in your event processing service
public class MyEventProcessor
{
    private readonly ConfigurationEventHandlers _eventHandlers;

    public MyEventProcessor(ConfigurationEventHandlers eventHandlers)
    {
        _eventHandlers = eventHandlers;
    }

    public async Task ProcessEvent(ConfigurationCreatedEvent @event)
    {
        // Execute side effects for a newly created configuration
        await _eventHandlers.HandleConfigurationCreatedAsync(@event);
    }
}
```

## StringExtensionsTests

The `StringExtensionsTests` class provides comprehensive unit tests for the `StringExtensions` static class, which offers a suite of useful string manipulation extension methods. It validates null/whitespace detection, truncation with custom suffixes, case conversion (kebab-case, snake_case, PascalCase), pattern matching, whitespace removal, repetition, email validation, URL encoding/decoding, common prefix extraction, and safe filename generation.

### What It Tests

- **Null/Whitespace Detection**: Validates `IsNullOrWhiteSpace` for null, empty, and whitespace-only strings
- **String Truncation**: Tests truncating strings with default ellipsis or custom suffixes
- **Case Conversion**: Converts between PascalCase, kebab-case, and snake_case formats
- **Pattern Matching**: Validates regex pattern matching for strings
- **Whitespace Handling**: Removes all whitespace characters from strings
- **String Repetition**: Repeats strings a specified number of times
- **Email Validation**: Validates email address formats
- **URL Encoding/Decoding**: Encodes and decodes URL-safe strings
- **Common Prefix**: Extracts common prefix between two strings
- **Safe Filename Generation**: Sanitizes strings to create valid filenames

### Public Members

- `IsNullOrWhiteSpace_NullValue_ReturnsTrue` - Tests null string detection
- `IsNullOrWhiteSpace_EmptyString_ReturnsTrue` - Tests empty string detection
- `IsNullOrWhiteSpace_WhitespaceOnly_ReturnsTrue` - Tests whitespace-only detection
- `IsNullOrWhiteSpace_NonEmptyString_ReturnsFalse` - Tests non-empty string detection
- `Truncate_StringExceedsMaxLength_TruncatesWithSuffix` - Tests truncation with default suffix
- `Truncate_StringWithinMaxLength_ReturnsOriginal` - Tests truncation with strings within limit
- `Truncate_StringEqualToMaxLength_ReturnsOriginal` - Tests truncation with exact length match
- `Truncate_EmptyString_ReturnsEmpty` - Tests truncation with empty strings
- `Truncate_WithCustomSuffix_UsesCustomSuffix` - Tests truncation with custom suffix
- `ToKebabCase_PascalCase_ConvertsToKebab` - Tests PascalCase to kebab-case conversion
- `ToKebabCase_MultiWordPascalCase_ConvertsCorrectly` - Tests multi-word PascalCase conversion
- `ToKebabCase_EmptyString_ReturnsEmpty` - Tests empty string handling for kebab-case
- `ToSnakeCase_PascalCase_ConvertsToSnake` - Tests PascalCase to snake_case conversion
- `ToSnakeCase_EmptyString_ReturnsEmpty` - Tests empty string handling for snake_case
- `ToPascalCase_KebabCase_ConvertsToPascal` - Tests kebab-case to PascalCase conversion
- `ToPascalCase_SnakeCase_ConvertsToPascal` - Tests snake_case to PascalCase conversion
- `ToPascalCase_EmptyString_ReturnsEmpty` - Tests empty string handling for PascalCase
- `MatchesPattern_MatchingPattern_ReturnsTrue` - Tests regex pattern matching
- `MatchesPattern_NonMatchingPattern_ReturnsFalse` - Tests non-matching pattern detection
- `RemoveWhitespace_StringWithSpaces_RemovesAll` - Tests whitespace removal

### Usage Example

```csharp
using DotnetConfigServer.Utilities;

// Test 1: Null/whitespace detection
string? nullValue = null;
bool isNullOrWhiteSpace = nullValue.IsNullOrWhiteSpace(); // true

string emptyValue = "";
isNullOrWhiteSpace = emptyValue.IsNullOrWhiteSpace(); // true

string whitespaceValue = "   ";
isNullOrWhiteSpace = whitespaceValue.IsNullOrWhiteSpace(); // true

string nonEmptyValue = "hello";
isNullOrWhiteSpace = nonEmptyValue.IsNullOrWhiteSpace(); // false

// Test 2: String truncation
string longText = "Hello World";
string truncated = longText.Truncate(7); // "Hell..."
string truncatedCustom = longText.Truncate(8, " [+]"); // "Hell [+]"

// Test 3: Case conversion
string kebabCase = "hello-world";
string pascalCase = kebabCase.ToPascalCase(); // "HelloWorld"

string snakeCase = "hello_world";
pascalCase = snakeCase.ToPascalCase(); // "HelloWorld"

string pascalCaseInput = "MyConfigurationService";
string kebabResult = pascalCaseInput.ToKebabCase(); // "my-configuration-service"
string snakeResult = pascalCaseInput.ToSnakeCase(); // "my_configuration_service"

// Test 4: Pattern matching
bool isNumeric = "12345".MatchesPattern(@"^\d+$"); // true
bool isNotNumeric = "abc".MatchesPattern(@"^\d+$"); // false

// Test 5: Whitespace removal
string spacedText = "Hello World Test";
string noSpaces = spacedText.RemoveWhitespace(); // "HelloWorldTest"

// Test 6: String repetition
string repeated = "ab".Repeat(3); // "ababab"

// Test 7: Email validation
bool isValidEmail = "user@example.com".IsValidEmail(); // true
bool isInvalidEmail = "not-an-email".IsValidEmail(); // false

// Test 8: URL encoding/decoding
string encoded = "hello world".UrlEncode(); // "hello%20world"
string decoded = encoded.UrlDecode(); // "hello world"

// Test 9: Common prefix extraction
string prefix = "config.database.host".CommonPrefix("config.database.port"); // "config.database."

// Test 10: Safe filename generation
string safeFilename = "config/key?value=test".ToSafeFileName(); // "configkeyvalue=test"
```

## ConfigurationServiceTests

The `ConfigurationServiceTests` class provides comprehensive unit tests for the `ConfigurationService` functionality. It validates configuration management operations including creation, update, deletion, and key management with support for both encrypted and non-encrypted configurations.

### What It Tests

- **Configuration Creation**: Validates that configurations can be created with proper audit logging and event publishing
- **Parent Validation**: Ensures parent configuration existence is validated before creating child configurations
- **Configuration Updates**: Tests updating existing configurations with new values
- **Configuration Deletion**: Validates soft deletion by setting DeletedAt timestamp
- **Key Management**: Tests adding configuration keys with automatic encryption for encrypted configurations
- **Error Handling**: Verifies proper exception throwing for invalid operations (e.g., non-existent parent configurations)

### Public Members

- `CreateAsync_ShouldCreateConfiguration_WhenValid` - Tests successful configuration creation with audit logging
- `CreateAsync_ShouldThrowException_WhenParentNotFound` - Tests parent configuration validation
- `UpdateAsync_ShouldUpdateConfiguration_WhenValid` - Tests configuration update functionality
- `DeleteAsync_ShouldDeleteConfiguration` - Tests soft deletion with DeletedAt timestamp
- `AddKeyAsync_ShouldAddKey_WhenValid` - Tests adding configuration keys to non-encrypted configurations
- `AddKeyAsync_ShouldEncryptKey_WhenConfigEncrypted` - Tests automatic encryption for encrypted configurations

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var configRepositoryMock = new Mock<IConfigurationRepository>();
var keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
var encryptionServiceMock = new Mock<IEncryptionService>();
var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
var eventBusMock = new Mock<IEventBus>();
var loggerMock = new Mock<ILogger<ConfigurationService>>();

// Create the service under test
var configurationService = new ConfigurationService(
    configRepositoryMock.Object,
    keyRepositoryMock.Object,
    encryptionServiceMock.Object,
    auditLogRepositoryMock.Object,
    eventBusMock.Object,
    loggerMock.Object
);

// Test 1: Create a new configuration
var newConfig = new Configuration
{
    Name = "OrderProcessingService",
    ApplicationId = Guid.NewGuid()
};

var createdConfig = await configurationService.CreateAsync(newConfig, "admin");
Console.WriteLine($"Configuration created: {createdConfig.Id}");

// Test 2: Update an existing configuration
var updatedConfig = new Configuration
{
    Name = "OrderProcessingService-Updated",
    ApplicationId = createdConfig.ApplicationId
};

var result = await configurationService.UpdateAsync(createdConfig.Id, updatedConfig, "admin");
Console.WriteLine($"Configuration updated: {result.Name}");

// Test 3: Add a configuration key (non-encrypted)
var configKey = new ConfigurationKey
{
    Key = "Database:ConnectionString",
    Value = "Server=localhost;Database=Orders;"
};

var addedKey = await configurationService.AddKeyAsync(createdConfig.Id, configKey, "admin");
Console.WriteLine($"Key added: {addedKey.Key}");

// Test 4: Add a configuration key (encrypted)
var encryptedConfig = new Configuration
{
    Id = Guid.NewGuid(),
    Name = "SecureService",
    ApplicationId = Guid.NewGuid(),
    IsEncrypted = true
};

configRepositoryMock.Setup(r => r.GetByIdAsync(encryptedConfig.Id))
    .ReturnsAsync(encryptedConfig);
encryptionServiceMock.Setup(e => e.EncryptAsync("secret-value", encryptedConfig.Id))
    .ReturnsAsync("encrypted-secret");

var secureKey = new ConfigurationKey
{
    Key = "Api:SecretKey",
    Value = "secret-value"
};

var encryptedKey = await configurationService.AddKeyAsync(encryptedConfig.Id, secureKey, "admin");
Console.WriteLine($"Encrypted key added: {encryptedKey.IsEncrypted}"); // true

// Test 5: Delete a configuration (soft delete)
await configurationService.DeleteAsync(createdConfig.Id, "admin");
Console.WriteLine("Configuration marked as deleted");
```

## ChangeRequestServiceTests

The `ChangeRequestServiceTests` class provides comprehensive unit tests for the `ChangeRequestService` functionality. It validates change request workflows including submission, approval, rejection, and cancellation operations with proper validation, status transitions, and error handling.

### What It Tests

- **Request Submission**: Validates that change requests can be submitted with proper validation of required fields (RequestedBy cannot be empty or whitespace)
- **Status Transitions**: Tests all valid status transitions (Pending → Approved/Rejected/Cancelled → Applied) and prevents invalid transitions
- **Approval Workflow**: Validates approval with immediate application vs delayed application, reviewer assignment, and timestamps
- **Rejection Workflow**: Tests rejection with reviewer assignment and comment recording
- **Cancellation Workflow**: Validates cancellation of pending requests only
- **Error Handling**: Verifies proper exception throwing for non-existent requests and invalid status operations
- **Integration**: Tests interaction with configuration service for applying approved changes
- **Query Operations**: Validates retrieval of pending requests and individual request lookup

### Public Members

- `SubmitAsync_WithValidRequest_SetsStatusToPendingAndSaves` - Tests successful change request submission with status transition
- `SubmitAsync_WithEmptyRequestedBy_ThrowsValidationException` - Tests validation of empty RequestedBy field
- `SubmitAsync_WithWhitespaceRequestedBy_ThrowsValidationException` - Tests validation of whitespace-only RequestedBy field
- `GetByIdAsync_WithValidId_ReturnsRequest` - Tests retrieval of existing change request
- `GetByIdAsync_WithNonExistentId_ReturnsNull` - Tests handling of non-existent request IDs
- `ApproveAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException` - Tests error handling for missing requests
- `ApproveAsync_WithAlreadyApprovedRequest_ThrowsConfigurationException` - Tests prevention of re-approval
- `ApproveAsync_WithPendingRequest_SetsStatusToApproved` - Tests successful approval with immediate application
- `ApproveAsync_WithApplyImmediatelyFalse_DoesNotApplyChange` - Tests delayed application workflow
- `RejectAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException` - Tests error handling for missing requests
- `RejectAsync_WithPendingRequest_SetsStatusToRejected` - Tests successful rejection workflow
- `RejectAsync_WithNonPendingRequest_ThrowsConfigurationException` - Tests prevention of rejection on non-pending requests
- `CancelAsync_WithNonExistentRequest_ThrowsConfigurationNotFoundException` - Tests error handling for missing requests
- `CancelAsync_WithPendingRequest_SetsStatusToCancelled` - Tests successful cancellation workflow
- `CancelAsync_WithNonPendingRequest_ThrowsConfigurationException` - Tests prevention of cancellation on non-pending requests
- `GetPendingAsync_ReturnsPendingRequests` - Tests retrieval of pending change requests
- `ChangeRequest_Approve_SetsReviewerAndTimestamp` - Tests ChangeRequest instance approval method
- `ChangeRequest_Reject_SetsReviewerAndStatus` - Tests ChangeRequest instance rejection method
- `ChangeRequest_MarkApplied_SetsAppliedByAndTimestamp` - Tests ChangeRequest instance application method

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var repositoryMock = new Mock<IChangeRequestRepository>();
var configServiceMock = new Mock<IConfigurationService>();
var loggerMock = new Mock<ILogger<ChangeRequestService>>();

// Create the service under test
var changeRequestService = new ChangeRequestService(
    repositoryMock.Object,
    configServiceMock.Object,
    loggerMock.Object
);

// Test 1: Submit a new change request
var newRequest = new ChangeRequest
{
    ConfigurationId = Guid.NewGuid(),
    ConfigurationKeyId = Guid.NewGuid(),
    RequestedBy = "developer@example.com",
    Operation = ChangeRequestOperation.UpdateKey,
    Payload = "{\"Value\":\"new-database-host\"}",
    Summary = "Update database connection string for production"
};

var submittedRequest = await changeRequestService.SubmitAsync(newRequest);
Console.WriteLine($"Request submitted - Status: {submittedRequest.Status}"); // Status: Pending

// Test 2: Approve the change request (immediate application)
var approvedRequest = await changeRequestService.ApproveAsync(
    submittedRequest.Id,
    "security-team@example.com",
    "Approved after security review",
    applyImmediately: true
);
Console.WriteLine($"Request approved - Status: {approvedRequest.Status}"); // Status: Applied
Console.WriteLine($"Applied by: {approvedRequest.AppliedBy}"); // Applied by: security-team@example.com

// Test 3: Reject a change request
var rejectedRequest = await changeRequestService.RejectAsync(
    submittedRequest.Id,
    "security-team@example.com",
    "Contains sensitive data in payload"
);
Console.WriteLine($"Request rejected - Status: {rejectedRequest.Status}"); // Status: Rejected

// Test 4: Get pending change requests for review
var pendingRequests = await changeRequestService.GetPendingAsync();
Console.WriteLine($"Pending requests count: {pendingRequests.Count}");

// Test 5: Cancel a pending request
var cancelledRequest = await changeRequestService.CancelAsync(
    submittedRequest.Id,
    "developer@example.com"
);
Console.WriteLine($"Request cancelled - Status: {cancelledRequest.Status}"); // Status: Cancelled

// Test 6: Direct ChangeRequest instance methods
var request = new ChangeRequest { RequestedBy = "test-user" };
request.Approve("reviewer@example.com", "Looks good to me");
Console.WriteLine($"Direct approval - Status: {request.Status}"); // Status: Approved

request.Reject("reviewer@example.com", "Does not meet standards");
Console.WriteLine($"Direct rejection - Status: {request.Status}"); // Status: Rejected

request.MarkApplied("deployment-bot");
Console.WriteLine($"Direct application - Status: {request.Status}"); // Status: Applied
```

## VersioningServiceTests

The `VersioningServiceTests` class provides comprehensive unit tests for the `VersioningService` functionality. It validates versioning operations including creating versions from configurations, publishing versions to make them active, retrieving active versions, rolling back to previous versions, and handling error scenarios like non-existent configurations or versions.

### What It Tests

- **Version Creation**: Tests creating new configuration versions with proper version numbering (1.0.0 → 1.0.1), copying keys from previous versions, and setting PreviousVersionId
- **Version Publishing**: Validates that draft versions can be published to become active versions with timestamps
- **Active Version Retrieval**: Tests getting the currently active version or null when none exists
- **Version Retrieval**: Validates retrieving specific versions by ID
- **Rollback Operations**: Tests rolling back configurations to previous versions by creating new versions with restored keys
- **Error Handling**: Verifies proper exception throwing for non-existent configurations or versions

### Public Members

- `CreateVersionAsync_WithNonExistentConfiguration_ThrowsConfigurationNotFoundException` - Tests error handling for missing configurations
- `CreateVersionAsync_FirstVersion_StartsAt1_0_0` - Tests first version numbering starts at 1.0.0
- `CreateVersionAsync_WithPreviousVersion_CopiesKeys` - Tests copying keys from previous active version
- `CreateVersionAsync_SetsPreviousVersionId` - Tests PreviousVersionId is set correctly
- `GetActiveVersionAsync_ReturnsPublishedVersion` - Tests retrieving active published version
- `GetActiveVersionAsync_NoActiveVersion_ReturnsNull` - Tests handling when no active version exists
- `PublishVersionAsync_WithValidVersion_ChangesStatusToPublished` - Tests publishing draft versions to active status
- `PublishVersionAsync_WithNonExistentVersion_ThrowsConfigurationNotFoundException` - Tests error handling for missing versions
- `RollbackAsync_ToSpecificVersion_RestoresKeysFromPreviousVersion` - Tests rollback creates new version with restored keys
- `GetVersionAsync_WithValidId_ReturnsVersion` - Tests retrieving specific version by ID

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var versionRepositoryMock = new Mock<IConfigurationVersionRepository>();
var configRepositoryMock = new Mock<IConfigurationRepository>();
var keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
var loggerMock = new Mock<ILogger<VersioningService>>();

// Create the service under test
var versioningService = new VersioningService(
    versionRepositoryMock.Object,
    configRepositoryMock.Object,
    keyRepositoryMock.Object,
    auditLogRepositoryMock.Object,
    loggerMock.Object
);

// Test 1: Create first version of a configuration
var configId = Guid.NewGuid();
var config = new Configuration
{
    Id = configId,
    Name = "MyService",
    ApplicationId = Guid.NewGuid(),
    CreatedBy = "admin"
};

configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

var newVersion = await versioningService.CreateVersionAsync(configId, "Initial release", "admin");
Console.WriteLine($"Created version {newVersion.VersionNumber}"); // Output: Created version 1.0.0

// Test 2: Create version with previous version (copies keys)
var previousVersionId = Guid.NewGuid();
var previousVersion = new ConfigurationVersion
{
    Id = previousVersionId,
    ConfigurationId = configId,
    VersionNumber = "1.0.0",
    Status = ConfigurationVersionStatus.Active,
    CreatedBy = "admin",
    Keys = new List<ConfigurationKey>
    {
        new ConfigurationKey { Key = "Database:Host", Value = "localhost", ConfigurationId = configId, VersionId = previousVersionId, CreatedBy = "admin" },
        new ConfigurationKey { Key = "Database:Port", Value = "5432", ConfigurationId = configId, VersionId = previousVersionId, CreatedBy = "admin" }
    }
};

configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync(previousVersion);
keyRepositoryMock.Setup(r => r.GetByVersionAsync(previousVersionId)).ReturnsAsync(previousVersion.Keys);

var versionWithPrevious = await versioningService.CreateVersionAsync(configId, "Update for Q2", "admin");
Console.WriteLine($"Created version {versionWithPrevious.VersionNumber} with {versionWithPrevious.Keys.Count} keys"); // Output: Created version 1.0.1 with 2 keys

// Test 3: Publish a draft version to make it active
var draftVersion = new ConfigurationVersion
{
    Id = Guid.NewGuid(),
    ConfigurationId = configId,
    VersionNumber = "1.0.1",
    Status = ConfigurationVersionStatus.Draft,
    CreatedBy = "admin"
};

versionRepositoryMock.Setup(r => r.GetByIdAsync(draftVersion.Id)).ReturnsAsync(draftVersion);
versionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

await versioningService.PublishVersionAsync(draftVersion.Id, "admin");
Console.WriteLine($"Published version - Status: {draftVersion.Status}"); // Output: Published version - Status: Active

// Test 4: Get active version
var activeVersion = await versioningService.GetActiveVersionAsync(configId);
if (activeVersion != null)
{
    Console.WriteLine($"Active version: {activeVersion.VersionNumber}"); // Output: Active version: 1.0.1
}

// Test 5: Rollback to a previous version
var rollbackVersion = await versioningService.RollbackAsync(configId, previousVersionId, "admin");
Console.WriteLine($"Rollback created version {rollbackVersion.VersionNumber}"); // Output: Rollback created version 1.0.2
```

## CacheKeyGeneratorTests

The `CacheKeyGeneratorTests` class provides comprehensive unit tests for the `CacheKeyGenerator` class. It validates that cache key generation methods produce correct and distinct keys for different cache operations throughout the DotnetConfigServer application. The tests ensure proper key formatting, uniqueness, and that related cache keys are properly structured for efficient cache invalidation patterns.

### What It Tests

- **Configuration Keys**: Tests that configuration-specific cache keys contain the configuration ID and follow the "config:" prefix pattern
- **Application Keys**: Validates application-specific cache keys contain the application ID with "app:" prefix
- **Key Distinction**: Ensures different cache operations (configuration vs keys list vs versions) produce distinct keys
- **Version Keys**: Tests version-specific cache keys with "version:" prefix and proper ID inclusion
- **Diff Keys**: Validates diff operation keys contain both version IDs with "diff:" prefix and handle ID order correctly
- **Webhook Keys**: Tests webhook-related cache keys with proper prefixes ("webhooks:", "webhook:")
- **Search Keys**: Validates search operation keys with URL-encoded queries and application ID handling
- **Invalidation Patterns**: Tests that invalidation patterns include all related cache keys for proper cache cleanup
- **Uniqueness**: Ensures different IDs produce different cache keys for both configuration and application operations

### Public Members

- `GetConfigurationKey_ReturnsKeyWithConfigurationId` - Tests configuration key contains configuration ID with "config:" prefix
- `GetApplicationConfigurationsKey_ContainsAppIdAndConfigsSuffix` - Tests application configurations key contains app ID and "configs" suffix
- `GetConfigurationKeysKey_ReturnsDistinctFromConfigurationKey` - Tests keys list key is distinct from configuration key
- `GetConfigurationKeyKey_ContainsKeyId` - Tests configuration key key contains key ID with "key:" prefix
- `GetConfigurationVersionsKey_ContainsVersionsSuffix` - Tests configuration versions key contains ID and "versions" suffix
- `GetConfigurationVersionKey_ContainsVersionId` - Tests configuration version key contains version ID with "version:" prefix
- `GetConfigurationDiffKey_ContainsBothVersionIds` - Tests diff key contains both version IDs with "diff:" prefix
- `GetConfigurationDiffKey_DifferentOrderProducesDifferentKey` - Tests diff key handles version ID order correctly
- `GetApplicationKey_ContainsApplicationId` - Tests application key contains application ID with "app:" prefix
- `GetWebhookSubscriptionsKey_ContainsApplicationId` - Tests webhook subscriptions key contains app ID with "webhooks:" prefix
- `GetWebhookSubscriptionKey_ContainsSubscriptionId` - Tests webhook subscription key contains subscription ID with "webhook:" prefix
- `GetSearchKey_WithQueryOnly_ContainsEncodedQuery` - Tests search key with query-only generates properly encoded key
- `GetSearchKey_WithQueryAndApplicationId_ContainsBoth` - Tests search key with query and app ID contains both
- `GetSearchKey_WithAndWithoutAppId_ProducesDifferentKeys` - Tests search keys differ with/without app ID
- `GetInvalidationPatternsForConfiguration_YieldsExpectedPatterns` - Tests invalidation patterns include all related keys
- `GetInvalidationPatternsForApplication_YieldsExpectedPatterns` - Tests application invalidation patterns include related keys
- `DifferentIds_ProduceDifferentKeys` - Tests different IDs produce different keys

### Usage Example

```csharp
using DotnetConfigServer.Caching;
using System;

// Test 1: Generate configuration cache key
var configId = Guid.NewGuid();
var configKey = CacheKeyGenerator.GetConfigurationKey(configId);
Console.WriteLine($"Configuration key: {configKey}");
// Output: Configuration key: config:550e8400-e29b-41d4-a716-446655440000

// Test 2: Generate application configurations cache key
var appId = Guid.NewGuid();
var appConfigsKey = CacheKeyGenerator.GetApplicationConfigurationsKey(appId);
Console.WriteLine($"Application configs key: {appConfigsKey}");
// Output: Application configs key: app:550e8400-e29b-41d4-a716-446655440000:configs

// Test 3: Generate configuration keys list cache key
var keysListKey = CacheKeyGenerator.GetConfigurationKeysKey(configId);
Console.WriteLine($"Configuration keys list key: {keysListKey}");
// Output: Configuration keys list key: config:550e8400-e29b-41d4-a716-446655440000:keys

// Test 4: Generate configuration version cache key
var versionId = Guid.NewGuid();
var versionKey = CacheKeyGenerator.GetConfigurationVersionKey(versionId);
Console.WriteLine($"Configuration version key: {versionKey}");
// Output: Configuration version key: version:550e8400-e29b-41d4-a716-446655440000

// Test 5: Generate configuration diff cache key
var fromVersionId = Guid.NewGuid();
var toVersionId = Guid.NewGuid();
var diffKey = CacheKeyGenerator.GetConfigurationDiffKey(fromVersionId, toVersionId);
Console.WriteLine($"Configuration diff key: {diffKey}");
// Output: Configuration diff key: diff:550e8400-e29b-41d4-a716-446655440000:550e8400-e29b-41d4-a716-446655440123

// Test 6: Generate webhook-related cache keys
var subscriptionId = Guid.NewGuid();
var webhookSubscriptionsKey = CacheKeyGenerator.GetWebhookSubscriptionsKey(appId);
var webhookSubscriptionKey = CacheKeyGenerator.GetWebhookSubscriptionKey(subscriptionId);
Console.WriteLine($"Webhook subscriptions key: {webhookSubscriptionsKey}");
Console.WriteLine($"Webhook subscription key: {webhookSubscriptionKey}");
// Output: Webhook subscriptions key: webhooks:550e8400-e29b-41d4-a716-446655440000
// Output: Webhook subscription key: webhook:550e8400-e29b-41d4-a716-446655440000

// Test 7: Generate search cache key
var searchKey = CacheKeyGenerator.GetSearchKey("database host");
Console.WriteLine($"Search key: {searchKey}");
// Output: Search key: search:database%20host

// Test 8: Generate search cache key with application ID
var searchKeyWithApp = CacheKeyGenerator.GetSearchKey("timeout", appId);
Console.WriteLine($"Search key with app: {searchKeyWithApp}");
// Output: Search key with app: search:timeout:app:550e8400-e29b-41d4-a716-446655440000

// Test 9: Get invalidation patterns for configuration
var invalidationPatterns = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(configId, appId);
Console.WriteLine("Invalidation patterns:");
foreach (var pattern in invalidationPatterns)
{
    Console.WriteLine($"  - {pattern}");
}
// Output includes: config:550e8400..., config:550e8400...:keys, config:550e8400...:versions, app:550e8400...:configs
```

## CollectionExtensionsTests

The `CollectionExtensionsTests` class provides comprehensive unit tests for the `CollectionExtensions` static class, which offers a suite of useful collection and enumerable extension methods. It validates batch processing, element-wise operations, collection state queries, and partitioning functionality for various collection types.

### What It Tests

- **Batch Processing**: Splits collections into smaller chunks of specified size, handling both divisible and non-divisible cases
- **Element-wise Operations**: Executes actions on each collection element with and without index tracking
- **Collection State Queries**: Determines if collections are null, empty, single-element, or multi-element
- **Partitioning**: Omits the last element or groups consecutive elements by a key selector
- **Distinct Operations**: Returns distinct elements based on a key selector
- **Shuffling**: Randomly reorders collection elements
- **Zipping**: Combines two collections into pairs of elements
- **Dictionary Conversion**: Converts key-value pairs to dictionaries
- **FirstOrDefault**: Gets the first element or returns a default value without throwing

### Public Members

- `Batch_CollectionDivisibleByBatchSize_ProducesFullBatches` - Tests batching when collection size is divisible by batch size
- `Batch_CollectionNotDivisibleByBatchSize_LastBatchIsSmaller` - Tests batching when collection size is not divisible by batch size
- `Batch_EmptyCollection_ReturnsNoBatches` - Tests batching with empty collections
- `Batch_InvalidBatchSize_ThrowsArgumentException` - Tests error handling for invalid batch sizes
- `ForEach_Action_ExecutesForEachElement` - Tests action execution on each element
- `ForEach_ActionWithIndex_PassesCorrectIndices` - Tests action execution with element indices
- `IsNullOrEmpty_NullCollection_ReturnsTrue` - Tests null collection detection
- `IsNullOrEmpty_EmptyCollection_ReturnsTrue` - Tests empty collection detection
- `IsNullOrEmpty_NonEmptyCollection_ReturnsFalse` - Tests non-empty collection detection
- `IsSingle_CollectionWithOneElement_ReturnsTrue` - Tests single-element collection detection
- `IsSingle_CollectionWithMultipleElements_ReturnsFalse` - Tests multi-element collection detection
- `IsSingle_EmptyCollection_ReturnsFalse` - Tests empty collection detection for IsSingle
- `HasMultiple_CollectionWithMultipleElements_ReturnsTrue` - Tests multi-element collection detection
- `HasMultiple_CollectionWithOneElement_ReturnsFalse` - Tests single-element collection detection for HasMultiple
- `HasMultiple_EmptyCollection_ReturnsFalse` - Tests empty collection detection for HasMultiple
- `SkipLast_MultipleElements_OmitsLastElement` - Tests skipping the last element
- `SkipLast_SingleElement_ReturnsEmpty` - Tests SkipLast with single-element collections
- `SkipLast_EmptyCollection_ReturnsEmpty` - Tests SkipLast with empty collections
- `DistinctBy_DuplicateKeys_ReturnsFirstOccurrence` - Tests distinct operation by key selector
- `DistinctBy_AllUniqueKeys_ReturnsAllElements` - Tests distinct operation with all unique keys
- `ZipWith_TwoCollections_CreatesCorrectPairs` - Tests zipping two collections
- `FirstOrDefault_WithDefaultValue_ReturnsDefaultWhenEmpty` - Tests FirstOrDefault with empty collections
- `FirstOrDefault_NonEmptyCollection_ReturnsFirstElement` - Tests FirstOrDefault with non-empty collections
- `Shuffle_ProducesAllOriginalElements` - Tests shuffling preserves all elements
- `GroupConsecutive_ConsecutiveSameValues_GroupsCorrectly` - Tests grouping consecutive elements

### Usage Example

```csharp
using DotnetConfigServer.Utilities;

// Test 1: Batch a collection into smaller chunks
var numbers = Enumerable.Range(1, 10).ToList();
var batches = numbers.Batch(3).ToList();
// batches = [[1, 2, 3], [4, 5, 6], [7, 8, 9], [10]]

// Test 2: Execute action for each element
var results = new List<int>();
numbers.ForEach(x => results.Add(x * 2));
// results = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20]

// Test 3: Check collection state
if (numbers.IsNullOrEmpty())
{
    Console.WriteLine("Collection is null or empty");
}
else if (numbers.IsSingle())
{
    Console.WriteLine("Collection has exactly one element");
}
else if (numbers.HasMultiple())
{
    Console.WriteLine("Collection has multiple elements");
}

// Test 4: Skip the last element
var allButLast = numbers.SkipLast().ToList();
// allButLast = [1, 2, 3, 4, 5, 6, 7, 8, 9]

// Test 5: Get distinct elements by a key
var items = new[]
{
    new { Id = 1, Name = "first" },
    new { Id = 1, Name = "duplicate" },
    new { Id = 2, Name = "unique" }
};
var distinctItems = items.DistinctBy(x => x.Id).ToList();
// distinctItems = [first, unique]

// Test 6: Zip two collections together
var letters = new[] { "a", "b", "c" };
var pairs = numbers.ZipWith(letters).ToList();
// pairs = [(1, "a"), (2, "b"), (3, "c")]

// Test 7: Get first element or default
var firstOrDefault = numbers.FirstOrDefault(0);
// firstOrDefault = 1 (first element)

var emptyList = new List<int>();
var defaultValue = emptyList.FirstOrDefault(-1);
// defaultValue = -1 (default value)

// Test 8: Shuffle collection randomly
var shuffled = numbers.Shuffle(new Random(42)).ToList();
// shuffled contains all elements [1..10] in random order

// Test 9: Group consecutive elements
var consecutiveNumbers = new[] { 1, 1, 2, 2, 2, 1 };
var groups = consecutiveNumbers.GroupConsecutive(x => x).ToList();
// groups = [[1, 1], [2, 2, 2], [1]]
```

## EncryptionServiceTests

The `EncryptionServiceTests` class provides comprehensive unit tests for the `EncryptionService` functionality. It validates encryption and decryption operations, key validation, key generation, and key rotation scenarios. The tests cover both synchronous and asynchronous encryption operations, ensuring proper error handling and edge cases are covered.

### What It Tests

- **Encryption Roundtrip**: Verifies that plain text values can be encrypted and decrypted back to their original form
- **Random IV Generation**: Ensures that identical plaintext values produce different ciphertexts due to random initialization vectors
- **Key Validation**: Tests validation of encryption keys including inactive and expired key scenarios
- **Key Generation**: Validates that newly generated keys have all required cryptographic material populated
- **Asynchronous Encryption**: Tests async encryption operations with proper error handling for missing primary keys
- **Key Rotation**: Validates key rotation functionality including marking keys as non-primary and persisting changes

### Public Members

- `Encrypt_ThenDecrypt_ReturnsOriginalPlainText` - Tests basic encryption/decryption roundtrip
- `Encrypt_SamePlainText_ProducesDistinctCipherTextDueToRandomIv` - Tests random IV generation
- `Encrypt_OutputIsValidBase64` - Tests that encryption output is valid Base64
- `ValidateKey_InactiveKey_ThrowsEncryptionExceptionMentioningKeyId` - Tests validation of inactive keys
- `ValidateKey_ExpiredKey_ThrowsEncryptionException` - Tests validation of expired keys
- `GenerateNewKey_ReturnsKeyWithPopulatedCryptographicMaterial` - Tests key generation
- `EncryptAsync_WhenNoPrimaryKeyExistsForConfiguration_ThrowsConfigurationException` - Tests async encryption with missing primary key
- `RotateKeyAsync_WhenKeyNotFound_ThrowsConfigurationNotFoundException` - Tests key rotation with missing key
- `RotateKeyAsync_WhenKeyExists_MarksItRotatedAndPersistsChange` - Tests successful key rotation

### Usage Example

```csharp
using DotnetConfigServer.Services;
using DotnetConfigServer.Models;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var keyRepositoryMock = new Mock<IEncryptionKeyRepository>();
var loggerMock = new Mock<ILogger<EncryptionService>>();

// Create the encryption service
var encryptionService = new EncryptionService(keyRepositoryMock.Object, loggerMock.Object);

// Test 1: Basic encryption roundtrip
var key = encryptionService.GenerateNewKey("test-key");
const string sensitiveValue = "Server=prod;Database=orders;";
var cipherText = encryptionService.Encrypt(sensitiveValue, key);
var decrypted = encryptionService.Decrypt(cipherText, key);
Console.WriteLine($"Roundtrip successful: {decrypted == sensitiveValue}"); // true

// Test 2: Random IV generation - same plaintext produces different ciphertexts
var cipher1 = encryptionService.Encrypt("static-value", key);
var cipher2 = encryptionService.Encrypt("static-value", key);
Console.WriteLine($"Different ciphertexts: {cipher1 != cipher2}"); // true

// Test 3: Validate key scenarios
try
{
var inactiveKey = new EncryptionKey { IsActive = false };
encryptionService.ValidateKey(inactiveKey);
}
catch (EncryptionException ex)
{
Console.WriteLine($"Inactive key validation failed: {ex.Message.Contains(key.KeyId)}"); // true
}

// Test 4: Generate new key with populated cryptographic material
var newKey = encryptionService.GenerateNewKey("service-key");
Console.WriteLine($"Key has material: {newKey.Salt != null && newKey.EncryptedKey != null}"); // true

// Test 5: Async encryption with missing primary key
var configId = Guid.NewGuid();
keyRepositoryMock.Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
.ReturnsAsync((EncryptionKey?)null);
try
{
await encryptionService.EncryptAsync("secret", configId);
}
catch (ConfigurationException ex)
{
Console.WriteLine($"Async encryption failed as expected: {ex.Message.Contains("primary")}"); // true
}

// Test 6: Key rotation
keyRepositoryMock.Setup(r => r.GetByKeyIdAsync("key-123"))
.ReturnsAsync(newKey);
keyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<EncryptionKey>()))
.Returns(Task.CompletedTask);

var rotatedKey = await encryptionService.RotateKeyAsync("key-123", "admin");
Console.WriteLine($"Key rotated: {rotatedKey.IsPrimary == false}"); // true
Console.WriteLine($"Rotation metadata set: {rotatedKey.RotatedBy == "admin"}"); // true
```

## ConfigurationWorkflowIntegrationTests

The `ConfigurationWorkflowIntegrationTests` class provides comprehensive integration tests that demonstrate end-to-end configuration workflows. It validates the complete lifecycle of configurations including creation, versioning, modification, encryption, validation, and concurrent operations across multiple services (ConfigurationService, VersioningService, DiffService).

These tests ensure that the various services work together correctly and handle real-world scenarios like version progression, diff generation between versions, encryption integration, and concurrent version management.

### What It Tests

- **Complete Workflow**: Validates the full configuration lifecycle from creation through versioning to diff generation
- **Encryption Integration**: Tests encryption/decryption operations within configuration workflows
- **Version Progression**: Validates multi-version configuration management with proper version numbering
- **Key Validation**: Tests configuration key validation constraints and error handling
- **Concurrent Operations**: Validates handling of multiple simultaneous version creation requests

### Public Members

- `FullWorkflow_CreateConfigCreateVersionModifyGenerateDiff` - Demonstrates the complete workflow: create configuration, create versions, modify keys, and generate diff between versions
- `EncryptedConfigurationWorkflow_ConfigurationAndKeysAreEncrypted` - Tests encryption integration with configuration workflow
- `MultiVersionConfiguration_CreatesAndManagesVersionProgression` - Tests configuration with multiple versions and rollback scenario
- `ConfigurationKeyValidation_EnforcesAllConstraints` - Tests configuration key validation in workflow
- `ConcurrentVersionManagement_HandlesMultipleVersionsSimultaneously` - Tests concurrent version creation and diff generation

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var configRepositoryMock = new Mock<IConfigurationRepository>();
var keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
var versionRepositoryMock = new Mock<IConfigurationVersionRepository>();
var diffRepositoryMock = new Mock<IConfigurationDiffRepository>();
var encryptionServiceMock = new Mock<IEncryptionService>();
var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
var eventBusMock = new Mock<IEventBus>();
var configLoggerMock = new Mock<ILogger<ConfigurationService>>();
var versioningLoggerMock = new Mock<ILogger<VersioningService>>();
var diffLoggerMock = new Mock<ILogger<DiffService>>();

// Create services
var configService = new ConfigurationService(
    configRepositoryMock.Object,
    keyRepositoryMock.Object,
    encryptionServiceMock.Object,
    auditLogRepositoryMock.Object,
    eventBusMock.Object,
    configLoggerMock.Object
);

var versioningService = new VersioningService(
    versionRepositoryMock.Object,
    configRepositoryMock.Object,
    keyRepositoryMock.Object,
    auditLogRepositoryMock.Object,
    versioningLoggerMock.Object
);

var diffService = new DiffService(
    diffRepositoryMock.Object,
    versionRepositoryMock.Object,
    keyRepositoryMock.Object,
    diffLoggerMock.Object
);

// Test 1: Full workflow - create config, versions, and generate diff
var appId = Guid.NewGuid();
var configId = Guid.NewGuid();
var userId = "integration-test";

// Create configuration
var config = new Configuration
{
    Id = configId,
    Name = "production-config",
    ApplicationId = appId,
    CreatedBy = userId
};

var createdConfig = await configService.CreateAsync(config, userId);

// Create first version
var version1 = await versioningService.CreateVersionAsync(configId, "Initial release", userId);

// Create second version with modifications
var version2 = await versioningService.CreateVersionAsync(configId, "Add caching", userId);

// Generate diff between versions
var diff = await diffService.GenerateDiffAsync(version1.Id, version2.Id, userId);
Console.WriteLine($"Diff shows {diff.TotalChanges} changes");

// Test 2: Encrypted configuration workflow
var encryptedConfig = new Configuration
{
    Id = Guid.NewGuid(),
    Name = "secrets-config",
    ApplicationId = Guid.NewGuid(),
    CreatedBy = "security-admin"
};

var createdEncryptedConfig = await configService.CreateAsync(encryptedConfig, "security-admin");
createdEncryptedConfig.SetEncryption(EncryptionAlgorithm.AES256, "encryption-key-1");
Console.WriteLine($"Configuration encrypted: {createdEncryptedConfig.IsEncrypted}");

// Test 3: Multi-version progression
var multiConfig = new Configuration
{
    Id = Guid.NewGuid(),
    Name = "deployment-config",
    ApplicationId = Guid.NewGuid(),
    CreatedBy = "deployer"
};

var v1 = await versioningService.CreateVersionAsync(multiConfig.Id, "Initial", "deployer");
var v2 = await versioningService.CreateVersionAsync(multiConfig.Id, "Update 1", "deployer");
var v3 = await versioningService.CreateVersionAsync(multiConfig.Id, "Update 2", "deployer");
Console.WriteLine($"Version progression: {v1.VersionNumber} → {v2.VersionNumber} → {v3.VersionNumber}");

// Test 4: Concurrent version management
var concurrentConfig = new Configuration
{
    Id = Guid.NewGuid(),
    Name = "concurrent-config",
    ApplicationId = Guid.NewGuid(),
    CreatedBy = "concurrent-user"
};

var versions = await Task.WhenAll(
    versioningService.CreateVersionAsync(concurrentConfig.Id, "Version 1", "concurrent-user"),
    versioningService.CreateVersionAsync(concurrentConfig.Id, "Version 2", "concurrent-user"),
    versioningService.CreateVersionAsync(concurrentConfig.Id, "Version 3", "concurrent-user")
);
Console.WriteLine($"Created {versions.Length} versions concurrently");
```

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
| `MemoryCacheServiceTests.cs` | In-memory caching with expiration, complex types, cache-aside pattern, and statistics |
| `WebhookServiceTests.cs` | Webhook subscription management, delivery retry logic, and signature generation |

Individual test project path: `tests/dotnet-config-server.Tests/`

## MemoryCacheServiceTests

The `MemoryCacheServiceTests` class provides comprehensive unit tests for the `MemoryCacheService` functionality. It validates in-memory caching operations including setting and retrieving values, expiration handling, cache-aside pattern usage, complex type storage, and cache statistics tracking. The tests cover various scenarios such as cache hits, cache misses, expiration policies, and concurrent operations.

### Usage Example

```csharp
using DotnetConfigServer.Caching;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock logger
var loggerMock = new Mock<ILogger<MemoryCacheService>>();
var cacheService = new MemoryCacheService(loggerMock.Object);

// Test 1: Basic set and get operations
await cacheService.SetAsync("app:config:timeout", 30);
var timeout = await cacheService.GetAsync<int>("app:config:timeout");
Console.WriteLine($"Timeout value: {timeout}"); // Output: Timeout value: 30

// Test 2: Cache-aside pattern (get or create)
var expensiveValue = await cacheService.GetOrCreateAsync("expensive:data", async () =>
{
    // This factory only runs if key doesn't exist
    var result = await FetchFromDatabaseAsync();
    return result;
});

// Test 3: Expiration handling
await cacheService.SetAsync("temp:key", "temporary-value", TimeSpan.FromSeconds(1));
var beforeExpiry = await cacheService.GetAsync<string>("temp:key"); // Returns "temporary-value"
await Task.Delay(TimeSpan.FromSeconds(2));
var afterExpiry = await cacheService.GetAsync<string>("temp:key"); // Returns null (expired)

// Test 4: Complex type storage
var config = new Dictionary<string, string>
{
    ["Database:Host"] = "prod-db.example.com",
    ["Database:Port"] = "5432",
    ["Database:Name"] = "orders"
};
await cacheService.SetAsync("database:config", config);
var cachedConfig = await cacheService.GetAsync<Dictionary<string, string>>("database:config");

// Test 5: Cache statistics
var stats = await cacheService.GetStatsAsync();
Console.WriteLine($"Cache stats - Sets: {stats.Sets}, Hits: {stats.Hits}, Misses: {stats.Misses}, Deletes: {stats.Deletes}");

// Test 6: Key prefix filtering
await cacheService.SetAsync("service:api:timeout", 30);
await cacheService.SetAsync("service:api:retries", 3);
await cacheService.SetAsync("cache:size", 100);
var serviceKeys = (await cacheService.GetKeysAsync("service")).ToList(); // Returns ["service:api:timeout", "service:api:retries"]

// Cleanup
cacheService.Dispose();
```

## WebhookServiceTests

The `WebhookServiceTests` class provides comprehensive unit tests for the `WebhookService` functionality. It covers subscription management operations (create, read, update, delete), subscription lifecycle management (activate, deactivate), delivery retry logic, and HMAC signature generation for webhook authenticity verification.

### Usage Example

```csharp
using DotnetConfigServer.Tests;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Setup mock dependencies
var subscriptionRepositoryMock = new Mock<IWebhookSubscriptionRepository>();
var deliveryRepositoryMock = new Mock<IWebhookDeliveryRepository>();
var loggerMock = new Mock<ILogger<WebhookService>>();
var httpHandlerMock = new Mock<HttpMessageHandler>();
var httpClient = new HttpClient(httpHandlerMock.Object);

// Create the service under test
var webhookService = new WebhookService(
    subscriptionRepositoryMock.Object,
    deliveryRepositoryMock.Object,
    loggerMock.Object,
    httpClient
);

// Test 1: Create a new webhook subscription
var newSubscription = new WebhookSubscription
{
    Name = "OrderService Webhook",
    Url = "https://order-service.example.com/config/webhook",
    ConfigurationId = Guid.NewGuid(),
    VerifySignature = true,
    IsActive = true
};

var createdSubscription = await webhookService.CreateSubscriptionAsync(newSubscription, "admin");
Assert.NotNull(createdSubscription);
Assert.Equal("admin", createdSubscription.CreatedBy);

// Test 2: Generate HMAC signature for webhook verification
if (createdSubscription.Secret != null)
{
    var payload = "{\"configurationId\":\"...\",\"event\":\"ConfigurationUpdated\"}";
    var signature = createdSubscription.GenerateSignature(payload);
    Assert.NotNull(signature);
    Assert.Matches("^[0-9A-F]+$", signature);
}

// Test 3: Update subscription
var updatedSubscription = new WebhookSubscription
{
    Name = "Updated OrderService Webhook",
    Url = "https://updated-order-service.example.com/config/webhook",
    Description = "Updated description"
};

var result = await webhookService.UpdateSubscriptionAsync(
    createdSubscription.Id,
    updatedSubscription,
    "editor"
);
Assert.Equal("Updated OrderService Webhook", result.Name);

// Test 4: Get subscription by ID
var retrieved = await webhookService.GetSubscriptionAsync(createdSubscription.Id);
Assert.NotNull(retrieved);

// Test 5: Delete subscription (soft delete - sets IsActive to false)
await webhookService.DeleteSubscriptionAsync(createdSubscription.Id, "admin");

// Test 6: Activate and deactivate lifecycle
var activated = await webhookService.ActivateAsync(createdSubscription.Id, "admin");
Assert.True(activated.IsActive);

var deactivated = await webhookService.DeactivateAsync(createdSubscription.Id, "admin");
Assert.False(deactivated.IsActive);

// Test 7: Handle retry logic for failed deliveries
var deliveryCount = await webhookService.RetryFailedDeliveriesAsync(maxRetries: 5);
```

## WebhookServiceTests

The `WebhookServiceTests` class provides comprehensive unit tests for the `WebhookService` functionality. It covers subscription management operations (create, read, update, delete), subscription lifecycle management (activate, deactivate), delivery retry logic, and HMAC signature generation for webhook authenticity verification.

### Usage Example

```csharp
using DotnetConfigServer.Tests;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Setup mock dependencies
var subscriptionRepositoryMock = new Mock<IWebhookSubscriptionRepository>();
var deliveryRepositoryMock = new Mock<IWebhookDeliveryRepository>();
var loggerMock = new Mock<ILogger<WebhookService>>();
var httpHandlerMock = new Mock<HttpMessageHandler>();
var httpClient = new HttpClient(httpHandlerMock.Object);

// Create the service under test
var webhookService = new WebhookService(
    subscriptionRepositoryMock.Object,
    deliveryRepositoryMock.Object,
    loggerMock.Object,
    httpClient
);

// Test 1: Create a new webhook subscription
var newSubscription = new WebhookSubscription
{
    Name = "OrderService Webhook",
    Url = "https://order-service.example.com/config/webhook",
    ConfigurationId = Guid.NewGuid(),
    VerifySignature = true,
    IsActive = true
};

var createdSubscription = await webhookService.CreateSubscriptionAsync(newSubscription, "admin");
Assert.NotNull(createdSubscription);
Assert.Equal("admin", createdSubscription.CreatedBy);

// Test 2: Generate HMAC signature for webhook verification
if (createdSubscription.Secret != null)
{
    var payload = "{\"configurationId\":\"...\",\"event\":\"ConfigurationUpdated\"}";
    var signature = createdSubscription.GenerateSignature(payload);
    Assert.NotNull(signature);
    Assert.Matches("^[0-9A-F]+$", signature);
}

// Test 3: Update subscription
var updatedSubscription = new WebhookSubscription
{
    Name = "Updated OrderService Webhook",
    Url = "https://updated-order-service.example.com/config/webhook",
    Description = "Updated description"
};

var result = await webhookService.UpdateSubscriptionAsync(
    createdSubscription.Id,
    updatedSubscription,
    "editor"
);
Assert.Equal("Updated OrderService Webhook", result.Name);

// Test 4: Get subscription by ID
var retrieved = await webhookService.GetSubscriptionAsync(createdSubscription.Id);
Assert.NotNull(retrieved);

// Test 5: Delete subscription (soft delete - sets IsActive to false)
await webhookService.DeleteSubscriptionAsync(createdSubscription.Id, "admin");

// Test 6: Activate and deactivate lifecycle
var activated = await webhookService.ActivateAsync(createdSubscription.Id, "admin");
Assert.True(activated.IsActive);

var deactivated = await webhookService.DeactivateAsync(createdSubscription.Id, "admin");
Assert.False(deactivated.IsActive);

// Test 7: Handle retry logic for failed deliveries
var deliveryCount = await webhookService.RetryFailedDeliveriesAsync(maxRetries: 5);
```

## BatchOperationServiceTests

The `BatchOperationServiceTests` class provides comprehensive unit tests for the `BatchOperationService` functionality. It validates batch operations for updating and deleting configuration keys, checking operation status, and cancellation behavior. The tests cover various scenarios including null/empty inputs, successful operations, error handling, and status tracking.

### Public Members

- `UpdateKeysAsync_NullInput_ReturnsSuccessWithEmptyOperationId` - Tests null input handling
- `UpdateKeysAsync_EmptyList_ReturnsSuccessWithEmptyOperationId` - Tests empty list handling
- `UpdateKeysAsync_AllKeysFound_UpdatesAllAndReturnsSuccess` - Tests successful batch updates
- `UpdateKeysAsync_SomeKeysNotFound_RecordsErrors` - Tests error handling for missing keys
- `DeleteKeysAsync_NullInput_ReturnsSuccessWithEmptyOperationId` - Tests null input handling
- `DeleteKeysAsync_EmptyList_ReturnsSuccessWithEmptyOperationId` - Tests empty list handling
- `DeleteKeysAsync_AllKeysFound_DeletesAllAndReturnsSuccess` - Tests successful batch deletions
- `DeleteKeysAsync_KeyNotFound_SkipsWithNoError` - Tests error handling for missing keys
- `GetStatusAsync_UnknownOperationId_ReturnsNotFoundStatus` - Tests status retrieval for unknown operations
- `GetStatusAsync_AfterUpdate_ReturnsCompletedStatus` - Tests status tracking after operations
- `CancelAsync_UnknownOperationId_DoesNotThrow` - Tests cancellation safety
- `BatchOperationStatus_Elapsed_CompletedOperation_ReturnsDurationBetweenStartAndCompletion` - Tests duration calculation

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup mock dependencies
var keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
var loggerMock = new Mock<ILogger<BatchOperationService>>();

// Create the service under test
var batchService = new BatchOperationService(keyRepositoryMock.Object, loggerMock.Object);

// Test 1: Batch update configuration keys
var key1Id = Guid.NewGuid();
var key2Id = Guid.NewGuid();

var key1 = new ConfigurationKey
{
    Id = key1Id,
    Key = "Database:Host",
    Value = "localhost",
    ConfigurationId = Guid.NewGuid(),
    VersionId = Guid.NewGuid(),
    CreatedBy = "admin"
};

var key2 = new ConfigurationKey
{
    Id = key2Id,
    Key = "Cache:TTL",
    Value = "300",
    ConfigurationId = Guid.NewGuid(),
    VersionId = Guid.NewGuid(),
    CreatedBy = "admin"
};

keyRepositoryMock.Setup(r => r.GetByIdAsync(key1Id)).ReturnsAsync(key1);
keyRepositoryMock.Setup(r => r.GetByIdAsync(key2Id)).ReturnsAsync(key2);
keyRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
keyRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

// Create batch update requests
var updates = new List<KeyUpdateRequest>
{
    new() { KeyId = key1Id, NewValue = "prod-db.example.com" },
    new() { KeyId = key2Id, NewValue = "600" }
};

// Execute batch update
var updateResult = await batchService.UpdateKeysAsync(updates, "admin");

Console.WriteLine($"Update successful: {updateResult.Success}");
Console.WriteLine($"Operation ID: {updateResult.OperationId}");
Console.WriteLine($"Success count: {updateResult.SuccessCount}");
Console.WriteLine($"Error count: {updateResult.ErrorCount}");

// Verify the updates were applied
Console.WriteLine($"Key1 new value: {key1.Value}"); // "prod-db.example.com"
Console.WriteLine($"Key2 new value: {key2.Value}"); // "600"

// Test 2: Batch delete configuration keys
keyRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);

var deleteResult = await batchService.DeleteKeysAsync(new List<Guid> { key1Id, key2Id }, "admin");

Console.WriteLine($"Delete successful: {deleteResult.Success}");
Console.WriteLine($"Operation ID: {deleteResult.OperationId}");

// Test 3: Check operation status
var status = await batchService.GetStatusAsync(updateResult.OperationId);
Console.WriteLine($"Status: {status.Status}"); // "completed"
Console.WriteLine($"Progress: {status.Progress}"); // 1.0
Console.WriteLine($"Duration: {status.Elapsed}"); // TimeSpan showing operation duration

// Test 4: Handle null/empty inputs gracefully
var nullResult = await batchService.UpdateKeysAsync(null!, "admin");
Console.WriteLine($"Null input handled: {nullResult.Success}"); // true, empty operation ID

var emptyResult = await batchService.UpdateKeysAsync(new List<KeyUpdateRequest>(), "admin");
Console.WriteLine($"Empty list handled: {emptyResult.Success}"); // true, empty operation ID

// Test 5: Handle missing keys without throwing errors
var missingKeyId = Guid.NewGuid();
keyRepositoryMock.Setup(r => r.GetByIdAsync(missingKeyId)).ReturnsAsync((ConfigurationKey?)null);

var partialResult = await batchService.UpdateKeysAsync(
    new List<KeyUpdateRequest> { new() { KeyId = missingKeyId, NewValue = "new-value" } },
    "admin"
);

Console.WriteLine($"Partial success: Success={partialResult.Success}, Errors={partialResult.ErrorCount}");
```

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

## ConfigurationDiffTests

The `ConfigurationDiffTests` class provides comprehensive unit tests for the `ConfigurationDiff` class, which is responsible for tracking and calculating differences between configuration versions. It validates diff calculation functionality including added, modified, and deleted key detection, change tracking, and validation of diff entries.

### Public Members

- `AddChange_SingleAddedEntry_IncrementsOnlyAddedCounter` - Tests single added entry tracking
- `AddChange_MixedChangeTypes_TotalReflectsAllCounters` - Tests mixed change type tracking
- `GetChangesByType_FiltersBySpecifiedType` - Tests filtering changes by type
- `GetSummary_ReflectsAccumulatedCounts` - Tests summary generation
- `DiffEntry_Validate_AddedEntryWithNullNewValue_ThrowsValidationException` - Tests validation of added entries
- `DiffEntry_Validate_DeletedEntryWithNullOldValue_ThrowsValidationException` - Tests validation of deleted entries
- `DiffEntry_Validate_ModifiedEntryWithBothValues_DoesNotThrow` - Tests validation of modified entries
- `DiffEntry_GetChangeDescription_ModifiedEntry_ContainsBothOldAndNewValues` - Tests change description generation for modified entries
- `DiffEntry_GetChangeDescription_AddedEntry_FormatsCorrectly` - Tests change description formatting for added entries

### Usage Example

```csharp
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Tests;
using FluentAssertions;

// Create a diff instance
var diff = new ConfigurationDiff
{
    ConfigurationId = Guid.NewGuid(),
    FromVersionId = Guid.NewGuid(),
    ToVersionId = Guid.NewGuid(),
    CreatedBy = "test-user"
};

// Add changes of different types
diff.AddChange("database.host", ChangeType.Added, null, "localhost");
diff.AddChange("cache.ttl", ChangeType.Modified, "300", "600");
diff.AddChange("legacy.endpoint", ChangeType.Deleted, "old-url");

// Verify counters
Console.WriteLine($"Total changes: {diff.TotalChanges}"); // Output: Total changes: 3
Console.WriteLine($"Added: {diff.AddedCount}"); // Output: Added: 1
Console.WriteLine($"Modified: {diff.ModifiedCount}"); // Output: Modified: 1
Console.WriteLine($"Deleted: {diff.DeletedCount}"); // Output: Deleted: 1

// Get changes by type
var addedChanges = diff.GetChangesByType(ChangeType.Added);
var modifiedChanges = diff.GetChangesByType(ChangeType.Modified);
var deletedChanges = diff.GetChangesByType(ChangeType.Deleted);

// Get summary
var summary = diff.GetSummary();
Console.WriteLine($"Summary created by: {summary.CreatedBy}"); // Output: Summary created by: test-user

// Validate diff entries
var addedEntry = new DiffEntry
{
    Key = "feature.flag",
    ChangeType = ChangeType.Added,
    NewValue = "true"
};

var deletedEntry = new DiffEntry
{
    Key = "old.key",
    ChangeType = ChangeType.Deleted,
    OldValue = "value"
};

var modifiedEntry = new DiffEntry
{
    Key = "db.timeout",
    ChangeType = ChangeType.Modified,
    OldValue = "30",
    NewValue = "60"
};

// Get change descriptions
Console.WriteLine(addedEntry.GetChangeDescription()); // Output: Added: feature.flag = true
Console.WriteLine(modifiedEntry.GetChangeDescription()); // Output: Modified: db.timeout from 30 to 60
```

## CachingBenchmarks

The `CachingBenchmarks` class provides a comprehensive suite of benchmarks to evaluate the performance and behavior of caching strategies in the configuration server. It includes scenarios for cache hits, cache misses, cache-aside implementation efficiency, cache eviction, and cache size management.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new CachingBenchmarks();

// Initialize dependencies and test data (required before running benchmarks)
await benchmarks.GlobalSetup();

// Execute specific benchmarks
await benchmarks.GetConfiguration_CacheMiss();
await benchmarks.GetConfiguration_CacheHit();
await benchmarks.GetConfiguration_WithCacheAside();
await benchmarks.GetKeys_CacheMiss();
await benchmarks.GetKeys_CacheHit();
await benchmarks.GetKeys_WithCacheAside();
await benchmarks.SearchConfigurations_CacheMiss();
await benchmarks.SearchConfigurations_CacheHit();
await benchmarks.GetConfigurationCount_CacheMiss();
await benchmarks.GetConfigurationCount_CacheHit();
await benchmarks.CacheEviction();
await benchmarks.CacheSizeTracking();
await benchmarks.CacheWithEncryptedData();

// Cleanup test data and resources
await benchmarks.GlobalCleanup();
```

## CachingBenchmarksExtensions

The `CachingBenchmarksExtensions` class provides a set of extension methods for `CachingBenchmarks` to facilitate testing of advanced caching scenarios, including batch operations, concurrent access patterns, and specific eviction policy testing. These extensions allow for more complex and realistic benchmarking by simulating high-load scenarios and varied caching conditions.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new CachingBenchmarks();
await benchmarks.GlobalSetup();

// Initialize test data
var configIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

// Execute batch and concurrent benchmarks
await benchmarks.GetConfigurations_CacheHitBatch(configIds);
await benchmarks.ConcurrentCacheAccess(Guid.NewGuid(), concurrencyLevel: 20);

// Run combined scenario benchmarks
await benchmarks.AllCacheHitScenarios();

// Retrieve and analyze cache operation statistics
var stats = benchmarks.GetCacheOperationStats();
foreach (var stat in stats)
{
    Console.WriteLine($"{stat.Name}: {stat.Description}");
}

// Cleanup resources
await benchmarks.GlobalCleanup();
```

## VersioningBenchmarksExtensions

The `VersioningBenchmarksExtensions` class provides a collection of extension methods for `VersioningBenchmarks` that enable advanced benchmarking scenarios for configuration versioning operations. These methods facilitate batch operations, status filtering, and querying version metadata, allowing for comprehensive performance testing of version lifecycle management.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;
using DotnetConfigServer.Models;

// Instantiate the benchmark suite
var benchmarks = new VersioningBenchmarks();
await benchmarks.GlobalSetup();

// Create a configuration for testing
var configId = Guid.NewGuid();

// Create a batch of versions for bulk operations
var versionIds = await benchmarks.CreateVersionBatchAsync(
    configId, 
    versionCount: 5,
    prefix: "Benchmark",
    userName: "test-user"
);

// Get the count of versions for this configuration
var versionCount = await benchmarks.GetVersionCountAsync(configId);
Console.WriteLine($"Created {versionCount} versions");

// Publish all versions in the batch
var publishTasks = await benchmarks.PublishVersionBatchAsync(versionIds, "test-user");
await Task.WhenAll(publishTasks);

// Get versions filtered by status
var publishedVersions = await benchmarks.GetVersionsByStatusAsync(
    configId, 
    ConfigurationVersionStatus.Published
);
Console.WriteLine($"Published versions: {publishedVersions.Count}");

// Get the most recent version
var mostRecent = await benchmarks.GetMostRecentVersionAsync(configId);
if (mostRecent != null)
{
    Console.WriteLine($"Most recent version: {mostRecent.ReleaseNotes}");
}

// Get version by description
var specificVersion = await benchmarks.GetVersionByDescriptionAsync(
    configId, 
    "Benchmark version 2"
);

// Get version counts grouped by status
var statusCounts = await benchmarks.GetVersionCountsByStatusAsync(configId);
foreach (var kvp in statusCounts)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value} versions");
}

// Archive all versions in the batch
var archiveTasks = await benchmarks.ArchiveVersionBatchAsync(versionIds, "test-user");
await Task.WhenAll(archiveTasks);

// Deprecate specific versions
var deprecateTasks = await benchmarks.DeprecateVersionBatchAsync(
    new List<Guid> { versionIds[0], versionIds[1] },
    "test-user"
);
await Task.WhenAll(deprecateTasks);

// Cleanup resources
await benchmarks.GlobalCleanup();
```

## EncryptionBenchmarks

The `EncryptionBenchmarks` class provides a comprehensive suite of benchmarks to evaluate the performance of the `IEncryptionService` implementation. It covers various encryption and decryption scenarios including synchronous and asynchronous operations, key validation, key generation, key rotation, and large payload handling.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new EncryptionBenchmarks();

// Initialize dependencies and test data (required before running benchmarks)
await benchmarks.GlobalSetup();

// Execute encryption/decryption benchmarks
var encrypted = benchmarks.EncryptSync();
var decrypted = benchmarks.DecryptSync();
var encryptedAsync = await benchmarks.EncryptAsync();
var decryptedAsync = await benchmarks.DecryptAsync();

// Execute key management benchmarks
var isValid = benchmarks.ValidateKey();
var newKey = benchmarks.GenerateNewKey();
await benchmarks.RotateKey();

// Execute large text benchmarks
var encryptedLarge = benchmarks.EncryptLargeText();
var decryptedLarge = benchmarks.DecryptLargeText();
var encryptedLargeAsync = await benchmarks.EncryptLargeTextAsync();
var decryptedLargeAsync = await benchmarks.DecryptLargeTextAsync();

// Cleanup test data and resources
await benchmarks.GlobalCleanup();
```

## EncryptionWorkflowIntegrationTests

The `EncryptionWorkflowIntegrationTests` class provides integration tests that cover the complete encryption lifecycle within the Dotnet Config Server system. These tests validate end-to-end scenarios including key generation, encryption/decryption operations, key rotation with fallback support, and automatic re-encryption of configuration values.

### What It Tests

- **Encryption Roundtrip**: Verifies that plain text values can be encrypted and decrypted back to their original form
- **Random IV Generation**: Ensures that identical plaintext values produce different ciphertexts due to random initialization vectors
- **Key Rotation**: Tests that old ciphertexts remain decryptable after key rotation via fallback to previous keys
- **Auto-Encryption**: Validates that configuration keys are automatically encrypted when added to encrypted configurations
- **Bulk Re-encryption**: Confirms that all encrypted keys in a configuration are re-encrypted with the new primary key
- **Error Handling**: Ensures proper exceptions are thrown when encryption keys are unavailable
- **Concurrency Safety**: Verifies that concurrent encryption operations on different configurations produce independent results

### Usage Example

```csharp
using DotnetConfigServer.Tests;
using DotnetConfigServer.Services;
using DotnetConfigServer.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

// Setup dependencies
var keyRepositoryMock = new Mock<IEncryptionKeyRepository>();
var configRepoMock = new Mock<IConfigurationRepository>();
var configKeyRepoMock = new Mock<IConfigurationKeyRepository>();
var auditRepoMock = new Mock<IAuditLogRepository>();
var eventBusMock = new Mock<IEventBus>();
var loggerMock = new Mock<ILogger<EncryptionService>>();

// Create the encryption service
var encryptionService = new EncryptionService(
    keyRepositoryMock.Object,
    loggerMock.Object
);

// Create the configuration service
var configurationService = new ConfigurationService(
    configRepoMock.Object,
    configKeyRepoMock.Object,
    encryptionService,
    auditRepoMock.Object,
    eventBusMock.Object,
    new Mock<ILogger<ConfigurationService>>().Object
);

// Test 1: Basic encryption roundtrip
var key = encryptionService.GenerateNewKey("test-key");
const string sensitiveValue = "Server=prod;Database=orders;";
var cipherText = encryptionService.Encrypt(sensitiveValue, key);
var decrypted = encryptionService.Decrypt(cipherText, key);

// Test 2: Key rotation with fallback
var oldKey = CreateRealKey("old-key");
var newKey = CreateRealKey("new-key");
var cipherWithOldKey = encryptionService.Encrypt("secret", oldKey);

// Simulate repository behavior
keyRepositoryMock
    .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
    .ReturnsAsync(newKey);
keyRepositoryMock
    .Setup(r => r.GetActiveKeysByConfigurationAsync(configId))
    .ReturnsAsync(new List<EncryptionKey> { newKey, oldKey });

// Decrypt using fallback to old key
var decryptedValue = await encryptionService.DecryptAsync(cipherWithOldKey, configId);

// Test 3: Auto-encryption when adding keys
var configId = Guid.NewGuid();
var config = new Configuration { Id = configId, IsEncrypted = true };
configRepoMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);

var keyToAdd = new ConfigurationKey
{
    Key = "api.secret",
    Value = "plain-api-key",
    IsEncrypted = false,
    ConfigurationId = configId
};

var result = await configurationService.AddKeyAsync(configId, keyToAdd, "admin");
// result.Value is now encrypted automatically

// Test 4: Bulk re-encryption
var keys = new List<ConfigurationKey>
{
    new() { Key = "db.conn", Value = "old-encrypted-value", IsEncrypted = true, ConfigurationId = configId },
    new() { Key = "api.token", Value = "another-encrypted-value", IsEncrypted = true, ConfigurationId = configId }
};

await encryptionService.ReEncryptConfigurationAsync(configId, keys, "ops");
// All encrypted keys now use the new primary key
```

## DateTimeExtensionsTests

The `DateTimeExtensionsTests` class provides comprehensive unit tests for the `DateTimeExtensions` static class, which offers a suite of useful date and time manipulation methods. It validates relative time formatting, ISO 8601 conversion, date boundary calculations, and business day counting functionality.

### What It Tests

- **Relative Time Formatting**: Converts date differences into human-readable strings like "just now", "5 minutes ago", "2 days ago"
- **ISO 8601 Conversion**: Ensures dates can be serialized and deserialized while preserving timezone information
- **Date Boundaries**: Validates methods that return the start/end of day, week, month, and year
- **Date Range Queries**: Tests inclusive boundary checking for date ranges
- **Business Day Calculations**: Counts weekdays while excluding weekends
- **Age Calculation**: Computes age from a birth date
- **Leap Year Detection**: Identifies leap years correctly

### Usage Example

```csharp
using DotnetConfigServer.Utilities;

// Format dates relative to now
var now = DateTime.UtcNow;
Console.WriteLine(now.ToRelativeTime()); // "just now"
Console.WriteLine(now.AddMinutes(-5).ToRelativeTime()); // "5 minutes ago"
Console.WriteLine(now.AddHours(-2).ToRelativeTime()); // "2 hours ago"
Console.WriteLine(now.AddDays(-1).ToRelativeTime()); // "1 day ago"
Console.WriteLine(now.AddDays(-7).ToRelativeTime()); // "1 week ago"

// Convert to ISO 8601 format
var isoDate = now.ToIso8601();
Console.WriteLine(isoDate); // "2024-07-19T14:30:45.123Z"

// Date boundaries
var today = DateTime.UtcNow;
var startOfDay = today.StartOfDay(); // 2024-07-19 00:00:00
var endOfDay = today.EndOfDay(); // 2024-07-19 23:59:59.9999999

var startOfWeek = today.StartOfWeek(DayOfWeek.Monday); // Previous Monday
var startOfMonth = today.StartOfMonth(); // First day of month
var startOfYear = today.StartOfYear(); // January 1st

var endOfMonth = today.EndOfMonth(); // Last day of month
var endOfYear = today.EndOfYear(); // December 31st

// Date range queries
var dateRangeStart = new DateTime(2024, 1, 1);
var dateRangeEnd = new DateTime(2024, 12, 31);
var testDate = new DateTime(2024, 6, 15);

if (testDate.IsBetween(dateRangeStart, dateRangeEnd))
{
    Console.WriteLine($"{testDate:yyyy-MM-dd} is within 2024");
}

// Business day calculations
var businessDays = today.GetBusinessDaysBetween(today.AddDays(10));
Console.WriteLine($"Next 10 days contain {businessDays} business days");

// Age calculation
var birthDate = new DateTime(1990, 5, 15);
var age = birthDate.GetAge();
Console.WriteLine($"Age: {age}");

// Leap year detection
if (DateTime.Now.IsLeapYear())
{
    Console.WriteLine("This year is a leap year!");
}
```

## ConfigurationModelTests

The `ConfigurationModelTests` class provides comprehensive unit tests for the `Configuration` model validation and behavior. It covers validation rules for required fields, length limits, versioning operations, encryption state management, deletion tracking, and summary generation.

### Public Members

- `Validate_EmptyName_ThrowsValidationExceptionWithNameError` - Validates that an empty Name throws a ValidationException with a Name error
- `Validate_WhitespaceName_ThrowsValidationException` - Validates that a whitespace Name throws a ValidationException
- `Validate_EmptyApplicationId_ThrowsValidationExceptionWithApplicationIdError` - Validates that an empty ApplicationId throws a ValidationException with an ApplicationId error
- `Validate_NameExceedsMaxLength_ThrowsValidationException` - Validates that a Name exceeding the maximum length throws a ValidationException
- `Validate_ValidConfiguration_DoesNotThrow` - Validates that a valid configuration does not throw any exception
- `CreateNewVersion_IncrementsVersionNumberAndPreservesIdentity` - Validates that creating a new version increments the version number and preserves identity
- `Delete_SetsIsActiveFalseAndRecordsDeletedBy` - Validates that deleting a configuration sets IsActive to false and records the deleter
- `SetEncryption_WithAes256_SetsIsEncryptedTrueAndStoresKeyId` - Validates that setting encryption with AES256 sets the encryption flag and stores the key ID
- `SetEncryption_WithNone_ClearsEncryptionFlag` - Validates that setting encryption to None clears the encryption flag
- `GetSummary_ReturnsSnapshotMatchingCurrentState` - Validates that GetSummary returns a snapshot matching the current state

### Usage Example

```csharp
using DotnetConfigServer.Models;
using DotnetConfigServer.Exceptions;
using FluentAssertions;

// Create a valid configuration
var config = new Configuration
{
    Name = "production-settings",
    ApplicationId = Guid.NewGuid(),
    CreatedBy = "admin"
};

// Test 1: Validate configuration with empty name throws exception
config.Name = string.Empty;
try
{
    config.Validate();
    throw new Exception("Should have thrown ValidationException");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed as expected: {ex.Message}");
    Console.WriteLine($"Errors: {string.Join(", ", ex.Errors.Keys)}");
}

// Test 2: Validate configuration with valid name passes
config.Name = "production-settings";
config.Validate(); // No exception thrown
Console.WriteLine("Valid configuration passed validation");

// Test 3: Create a new version from existing configuration
var originalId = config.Id;
var originalAppId = config.ApplicationId;
var originalVersion = config.VersionNumber;

var newVersion = config.CreateNewVersion();
Console.WriteLine($"New version number: {newVersion.VersionNumber} (expected: {originalVersion + 1})");
Console.WriteLine($"New version ID different from original: {newVersion.Id != originalId}");
Console.WriteLine($"Application ID preserved: {newVersion.ApplicationId == originalAppId}");

// Test 4: Delete configuration (soft delete)
config.Delete("system-admin");
Console.WriteLine($"IsActive after delete: {config.IsActive}"); // false
Console.WriteLine($"Deleted by: {config.DeletedBy}"); // "system-admin"
Console.WriteLine($"Deleted at: {config.DeletedAt}");

// Test 5: Set encryption with AES256
config.SetEncryption(EncryptionAlgorithm.AES256, "key-12345");
Console.WriteLine($"IsEncrypted: {config.IsEncrypted}"); // true
Console.WriteLine($"EncryptionAlgorithm: {config.EncryptionAlgorithm}"); // AES256
Console.WriteLine($"EncryptionKeyId: {config.EncryptionKeyId}"); // "key-12345"

// Test 6: Set encryption to None (disable encryption)
config.SetEncryption(EncryptionAlgorithm.None, null);
Console.WriteLine($"IsEncrypted after None: {config.IsEncrypted}"); // false
Console.WriteLine($"EncryptionKeyId after None: {config.EncryptionKeyId}"); // null

// Test 7: Get configuration summary
var summary = config.GetSummary();
Console.WriteLine($"Summary ID matches: {summary.Id == config.Id}");
Console.WriteLine($"Summary name: {summary.Name}");
Console.WriteLine($"Summary version: {summary.VersionNumber}");
```

## DictionaryExtensionsTests

The `DictionaryExtensionsTests` class provides comprehensive unit tests for the `DictionaryExtensions` class extension methods. It validates dictionary operations including value retrieval with fallbacks, conditional addition and updates, filtering, merging, inversion, projection, flattening, and nested value manipulation through dot-separated paths.

### Public Members

- `GetValueOrDefault_ExistingKey_ReturnsValue` - Returns value when key exists
- `GetValueOrDefault_MissingKey_ReturnsDefaultValue` - Returns explicit default when key missing
- `GetValueOrDefault_MissingKey_NoExplicitDefault_ReturnsTypeDefault` - Returns type default when key missing
- `AddIfNotExists_NewKey_AddsAndReturnsTrue` - Adds new entry and returns true
- `AddIfNotExists_ExistingKey_DoesNotOverwriteAndReturnsFalse` - Preserves existing entry and returns false
- `AddOrUpdate_NewKey_AddsEntry` - Adds new key-value pair
- `AddOrUpdate_ExistingKey_OverwritesValue` - Updates existing key-value pair
- `RemoveWhere_MatchingPredicate_RemovesMatchingEntries` - Removes entries matching predicate
- `RemoveWhere_NothingMatches_RemovesZeroEntries` - Returns zero when no matches
- `Merge_OverwriteTrue_OverwritesExistingKeys` - Overwrites existing keys when merging
- `Merge_OverwriteFalse_DoesNotOverwriteExistingKeys` - Preserves existing keys when merging
- `Invert_UniqueValues_ProducesInvertedDictionary` - Creates inverted dictionary
- `Where_Predicate_ReturnsOnlyMatchingPairs` - Filters dictionary by predicate
- `Select_Selector_TransformsValues` - Projects values using selector function
- `Flatten_FlatDictionary_ReturnsSameKeys` - Returns flat dictionary unchanged
- `Flatten_WithPrefix_PrependsPrefixToKeys` - Adds prefix to all keys
- `GetNestedValue_ExistingDotPath_ReturnsValue` - Retrieves nested value by dot path
- `GetNestedValue_NonExistentPath_ReturnsNull` - Returns null for missing path
- `SetNestedValue_NewPath_CreatesNestedStructure` - Creates nested structure and sets value

### Usage Example

```csharp
using DotnetConfigServer.Utilities;

// Create a configuration dictionary
var config = new Dictionary<string, object?> 
{
    ["Database"] = new Dictionary<string, object?> 
    {
        ["Host"] = "localhost",
        ["Port"] = 5432,
        ["Name"] = "orders"
    },
    ["Cache"] = new Dictionary<string, object?> 
    {
        ["Enabled"] = true,
        ["TTL"] = 300
    },
    ["Api"] = new Dictionary<string, object?> 
    {
        ["Timeout"] = 30,
        ["Retries"] = 3
    }
};

// Get value with fallback - returns null if path doesn't exist
var dbHost = config.GetNestedValue("Database.Host"); // "localhost"
var missingPath = config.GetNestedValue("Database.Missing"); // null

// Get value with type-safe fallback
var port = config.GetNestedValue("Database.Port", 5432); // 5432

// Add new nested value - creates intermediate dictionaries as needed
config.SetNestedValue("Logging.Level", "Debug");

// Merge dictionaries - control whether to overwrite existing keys
var overrides = new Dictionary<string, object?> 
{
    ["Database"] = new Dictionary<string, object?> 
    {
        ["Host"] = "prod-db.example.com", // Will overwrite
        ["PoolSize"] = 20 // Will be added
    },
    ["Cache"] = new Dictionary<string, object?> 
    {
        ["Enabled"] = false // Will NOT overwrite (overwrite=false)
    }
};

config.Merge(overrides, overwrite: true);

// Filter dictionary by predicate
var enabledFeatures = config.Where(kvp => 
    kvp.Value is Dictionary<string, object?> dict && 
    dict.ContainsKey("Enabled") && 
    dict["Enabled"] is true);

// Transform values using Select
var doubledValues = config.Select(v => 
    v is int i ? i * 2 : v);

// Create inverted dictionary (values become keys)
var inverted = config.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString());

// Flatten dictionary with prefix
var flatConfig = config.Flatten(prefix: "app");
// Result: ["app.Database.Host"] = "prod-db.example.com", etc.
```

## DiffBenchmarks

The `DiffBenchmarks` class provides a comprehensive suite of benchmarks to measure the performance of diff and diff-viewer services across a variety of realistic scenarios. It evaluates operations such as comparing configuration versions, retrieving enriched diffs with detailed change information, generating rollback previews, and analyzing version timelines.

The benchmarks use an in-memory SQL Server database that is created and torn down for each run, ensuring isolated and reproducible test conditions.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new DiffBenchmarks();

// Initialize dependencies and test data (required before running benchmarks)
await benchmarks.GlobalSetup();

// Execute diff comparison benchmarks
await benchmarks.CompareConfigurations();
await benchmarks.CompareLargeConfigurations();

// Execute diff viewer service benchmarks
await benchmarks.GetDiff();
await benchmarks.GetDiffWithDetails();
await benchmarks.GetEnrichedDiff();
await benchmarks.GetRollbackPreview();
await benchmarks.GetDiffTimeline();

// Cleanup test data and resources
await benchmarks.GlobalCleanup();
```

## ConfigurationInheritanceIntegrationTests

The `ConfigurationInheritanceIntegrationTests` class provides integration tests that verify the configuration inheritance system works correctly across multiple levels of parent-child relationships. These tests ensure that child configurations properly override parent keys, unique parent keys are inherited, and inheritance chains are resolved correctly even when intermediate configurations are missing.

### What It Tests

- **Key Override**: Child keys override parent keys with the same name while inheriting unique parent keys
- **Multi-Level Inheritance**: Three-level inheritance chains (grandparent → parent → child) are resolved correctly
- **Inheritance Control**: The `resolveInheritance` parameter controls whether inheritance resolution occurs
- **Broken Chains**: Gracefully handles missing parent configurations without throwing exceptions
- **Full Lifecycle**: Tests the complete configuration workflow including creation, key management, updates, and event publishing

### Usage Example

```csharp
using DotnetConfigServer.Tests;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Setup dependencies
var configRepoMock = new Mock<IConfigurationRepository>();
var keyRepoMock = new Mock<IConfigurationKeyRepository>();
var encryptionMock = new Mock<IEncryptionService>();
var auditRepoMock = new Mock<IAuditLogRepository>();
var eventBusMock = new Mock<IEventBus>();
var loggerMock = new Mock<ILogger<ConfigurationService>>();

// Create the service under test
var configService = new ConfigurationService(
    configRepoMock.Object,
    keyRepoMock.Object,
    encryptionMock.Object,
    auditRepoMock.Object,
    eventBusMock.Object,
    loggerMock.Object
);

// Test 1: Child overrides parent key
var parentId = Guid.NewGuid();
var childId = Guid.NewGuid();

var parentConfig = new Configuration { Id = parentId, Name = "ParentConfig", ApplicationId = Guid.NewGuid() };
var childConfig = new Configuration { Id = childId, Name = "ChildConfig", ApplicationId = Guid.NewGuid(), ParentConfigurationId = parentId };

configRepoMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(childConfig);
configRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync(parentConfig);

// Parent has "database.host" = "prod-db.example.com"
// Child has "database.host" = "dev-db.example.com" (overrides parent)
// Child also has "feature.flag" = "true" (unique to child)

var result = await configService.GetKeysAsync(childId);
// Returns both keys with "database.host" = "dev-db.example.com" (child override)

// Test 2: Three-level inheritance chain
var grandparentId = Guid.NewGuid();
var grandparentConfig = new Configuration { Id = grandparentId, Name = "GrandparentConfig", ApplicationId = Guid.NewGuid() };
parentConfig = new Configuration { Id = parentId, Name = "ParentConfig", ApplicationId = Guid.NewGuid(), ParentConfigurationId = grandparentId };
childConfig = new Configuration { Id = childId, Name = "ChildConfig", ApplicationId = Guid.NewGuid(), ParentConfigurationId = parentId };

// Grandparent: "log.level" = "debug"
// Parent overrides: "log.level" = "info"
// Child adds: "feature.enabled" = "true"

var threeLevelResult = await configService.GetKeysAsync(childId);
// Returns all three keys with correct inheritance resolution

// Test 3: Disable inheritance resolution
var directOnlyResult = await configService.GetKeysAsync(childId, resolveInheritance: false);
// Returns only keys directly on the child configuration

// Test 4: Broken inheritance chain (missing parent)
var missingParentId = Guid.NewGuid();
var orphanConfig = new Configuration { Id = childId, Name = "OrphanConfig", ApplicationId = Guid.NewGuid(), ParentConfigurationId = missingParentId };

configRepoMock.Setup(r => r.GetByIdAsync(missingParentId)).ReturnsAsync((Configuration?)null);

var orphanResult = await configService.GetKeysAsync(childId);
// Returns only the child's own keys without throwing exceptions
```

## WebhookBenchmarks

The `WebhookBenchmarks` class provides a set of performance benchmarks for webhook operations, including subscription management (CRUD), event dispatching, and retry queue processing. It helps evaluate the efficiency of the webhook system under different workloads, ensuring reliable delivery and performance for configuration change notifications.

## ConfigurationBenchmarks

The `ConfigurationBenchmarks` class provides a comprehensive suite of benchmarks to evaluate the performance of core configuration operations including setup, teardown, and CRUD operations on configurations and configuration keys. It covers scenarios for creating, retrieving, updating, and deleting configurations, as well as searching and counting operations.

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new ConfigurationBenchmarks();

// Initialize dependencies and test data (required before running benchmarks)
await benchmarks.GlobalSetup();

// Execute configuration lifecycle benchmarks
await benchmarks.CreateConfiguration();
await benchmarks.GetConfigurationById();
await benchmarks.GetConfigurationsByApplication();
await benchmarks.UpdateConfiguration();

// Execute configuration search and count benchmarks
await benchmarks.SearchConfigurations();
await benchmarks.GetConfigurationCount();

// Execute configuration key management benchmarks
await benchmarks.GetKeys();
await benchmarks.SearchKeys();
await benchmarks.AddKey();
await benchmarks.UpdateKey();
await benchmarks.DeleteKey();

// Execute encryption-related benchmarks
await benchmarks.CreateConfigurationWithEncryption();
await benchmarks.GetConfigurationWithEncryption();

// Cleanup test data and resources
await benchmarks.GlobalCleanup();
```

### Usage Example

```csharp
using DotnetConfigServer.Benchmarks;

// Instantiate the benchmark suite
var benchmarks = new WebhookBenchmarks();

// Initialize dependencies and test data (required before running benchmarks)
await benchmarks.GlobalSetup();

// Execute subscription management benchmarks
await benchmarks.CreateWebhook();
await benchmarks.GetWebhook();
await benchmarks.GetWebhooksByConfiguration();
await benchmarks.UpdateWebhook();
await benchmarks.DeleteWebhook();

// Execute delivery and processing benchmarks
await benchmarks.DispatchWebhook();
await benchmarks.GetFailedDeliveries();
await benchmarks.ProcessWebhookRetryQueue();

// Execute comprehensive scenario benchmarks
await benchmarks.CreateWebhookWithManyEvents();

// Cleanup test data and resources
await benchmarks.GlobalCleanup();
```

## ServiceExtensionsConfiguration

The `ServiceExtensionsConfiguration` class is a data transfer object used for serializing and deserializing service extension configurations. It provides a structured way to define which services should be registered in the dependency injection container, including data services, business services, webhook clients, Swagger configurations, and database initialization methods.

This configuration is typically used when bootstrapping the application to register services dynamically based on configuration rather than hard-coding them in the startup sequence.

### Usage Example

```csharp
// Define service extensions configuration
var serviceExtensionsConfig = new ServiceExtensionsConfiguration
{
    DataServices = new[] { "DotnetConfigServer.Data.SqlServerConfigurationRepository" },
    BusinessServices = new[] { 
        "DotnetConfigServer.Services.ConfigurationService",
        "DotnetConfigServer.Services.VersioningService",
        "DotnetConfigServer.Services.DiffService"
    },
    WebhookClient = new[] { "DotnetConfigServer.Services.WebhookService" },
    SwaggerConfiguration = new[] { "DotnetConfigServer.Infrastructure.SwaggerExtensions" },
    DatabaseInitialization = new[] { "DotnetConfigServer.Infrastructure.DatabaseInitializer" }
};

// Serialize to JSON for storage or transmission
var json = serviceExtensionsConfig.ToJson(indented: true);

// Later, deserialize from JSON
var loadedConfig = ServiceExtensionsJsonExtensions.FromJson(json);

// Or use the safe TryFromJson method
if (ServiceExtensionsJsonExtensions.TryFromJson(json, out var safeConfig))
{
    Console.WriteLine($"Successfully loaded configuration with {safeConfig?.DataServices?.Length ?? 0} data services");
}
```

## ServiceExtensionsConfigurationExtensions

The `ServiceExtensionsConfigurationExtensions` class provides extension methods for `ServiceExtensionsConfiguration` that enable fluent configuration patterns and common operations on service extension configurations. These methods allow you to check for the presence of specific service types, get counts, and build new configurations with additional services while preserving existing ones.

This extension class is particularly useful when bootstrapping the application or dynamically configuring services based on runtime conditions, allowing for clean, readable code when working with service extension configurations.

### Usage Example

```csharp
// Create a base configuration with some services
var baseConfig = new ServiceExtensionsConfiguration
{
    DataServices = new[] { "DotnetConfigServer.Data.SqlServerConfigurationRepository" },
    BusinessServices = new[]
    {
        "DotnetConfigServer.Services.ConfigurationService",
        "DotnetConfigServer.Services.VersioningService"
    }
};

// Check if configuration has data services
bool hasDataServices = baseConfig.HasDataServices();
Console.WriteLine($"Has data services: {hasDataServices}"); // true

// Check if configuration has business services
bool hasBusinessServices = baseConfig.HasBusinessServices();
Console.WriteLine($"Has business services: {hasBusinessServices}"); // true

// Get all service types
var allServices = baseConfig.GetAllServiceTypes();
Console.WriteLine($"Total services: {allServices.Count}");

// Check if configuration has Swagger configuration
bool hasSwagger = baseConfig.HasSwaggerConfiguration();
Console.WriteLine($"Has Swagger config: {hasSwagger}"); // false

// Check if configuration has database initialization
bool hasDbInit = baseConfig.HasDatabaseInitialization();
Console.WriteLine($"Has DB initialization: {hasDbInit}"); // false

// Get service count
int serviceCount = baseConfig.GetServiceCount();
Console.WriteLine($"Service count: {serviceCount}"); // 3

// Check if any services are configured
bool hasAnyServices = baseConfig.HasAnyServices();
Console.WriteLine($"Has any services: {hasAnyServices}"); // true

// Add more data services using fluent extension
var extendedConfig = baseConfig.WithAddedDataServices(new[]
{
    "DotnetConfigServer.Data.RedisCacheRepository",
    "DotnetConfigServer.Data.SqlServerConfigurationRepository" // duplicate will be removed
});

Console.WriteLine($"Extended data services count: {extendedConfig.DataServices?.Length}"); // 2

// Add more business services
var extendedConfig2 = extendedConfig.WithAddedBusinessServices(new[]
{
    "DotnetConfigServer.Services.DiffService",
    "DotnetConfigServer.Services.RollbackService"
});

Console.WriteLine($"Extended business services count: {extendedConfig2.BusinessServices?.Length}"); // 4
```

## ServiceExtensionsValidation

The `ServiceExtensionsValidation` class provides validation helpers for service extension configurations and parameters used throughout the Dotnet Config Server application. It includes extension methods for `IConfiguration`, `IServiceCollection`, and `IServiceProvider` that validate parameters before they're used in service registration and configuration scenarios.

These validation methods help ensure that required dependencies are provided and configurations are valid before attempting to use them, preventing null reference exceptions and configuration errors at runtime.

### Usage Example

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Example 1: Validate configuration before using it
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Check if configuration is valid
if (configuration.IsValid())
{
    Console.WriteLine("Configuration is valid and ready to use");
}

// Or get detailed validation errors
var validationErrors = configuration.Validate();
if (validationErrors.Count > 0)
{
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}

// Example 2: Validate service collection before registering services
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IMyService, MyService>();

// Ensure service collection is valid
services.EnsureValid();
Console.WriteLine("Service collection is valid");

// Example 3: Validate service provider after building it
var serviceProvider = services.BuildServiceProvider();

// Check if service provider is valid
if (serviceProvider.IsValid())
{
    Console.WriteLine("Service provider is valid and ready to use");
}

// Or throw exception if invalid
serviceProvider.EnsureValid();

// Example 4: Using with ServiceExtensionsConfiguration
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test;"
    })
    .Build();

if (config.IsValid())
{
    Console.WriteLine("Configuration is valid for service extensions");
}
```

## ServiceExtensions

The `ServiceExtensions` class provides extension methods for registering services and dependencies in the dependency injection container. It includes methods for registering data services, business services, webhook clients, Swagger configuration, and database initialization, following a clean, modular approach to service registration.

These extension methods are used in the application's startup sequence to configure the service container with all required dependencies in a maintainable and testable way.

### Usage Example

```csharp
using DotnetConfigServer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// In your Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Register data services (requires IConfiguration)
builder.Services.AddDataServices(builder.Configuration);

// Register business logic services
builder.Services.AddBusinessServices();

// Configure HTTP client for webhook delivery
builder.Services.AddWebhookClient();

// Configure Swagger/OpenAPI documentation
builder.Services.AddSwaggerConfiguration();

// Build the service provider
var app = builder.Build();

// Initialize the database (apply migrations)
await app.Services.InitializeDatabaseAsync();

// Example: Resolve and use a service
var configService = app.Services.GetRequiredService<IConfigurationService>();
var encryptionService = app.Services.GetRequiredService<IEncryptionService>();
```

### Public Methods

- `AddDataServices(IServiceCollection, IConfiguration)` - Registers database context and repository services
- `AddBusinessServices(IServiceCollection)` - Registers business logic services (services, managers)
- `AddWebhookClient(IServiceCollection)` - Configures HTTP client for webhook delivery
- `AddSwaggerConfiguration(IServiceCollection)` - Configures Swagger/OpenAPI documentation
- `InitializeDatabaseAsync(IServiceProvider)` - Applies pending database migrations asynchronously

## ConcurrencyException

The `ConcurrencyException` class represents exceptions thrown when concurrency conflicts occur in the system, such as optimistic concurrency violations or race conditions. It serves as the base class for more specific concurrency-related exceptions and provides consistent error handling patterns for conflict resolution.

This exception type is commonly thrown when multiple processes attempt to modify the same configuration or entity simultaneously, or when circular dependencies are detected in configuration relationships. It includes constructors for creating exception instances with custom messages, inner exceptions, and error codes.

### Usage Example

```csharp
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Services;
using System;

// Example 1: Basic ConcurrencyException usage
try
{
    // Attempt to update a configuration that might be modified by another process
    await configurationService.UpdateConfigurationAsync(configurationId, updatedConfig);
}
catch (ConcurrencyException ex)
{
    // Log the concurrency error
    logger.LogError(ex, "Concurrency conflict detected while updating configuration");
    
    // Implement retry logic with exponential backoff
    await RetryUpdateWithBackoffAsync(configurationId, updatedConfig, retryCount: 0);
}

// Example 2: Handling OptimisticConcurrencyException specifically
try
{
    await versioningService.PublishVersionAsync(versionId);
}
catch (OptimisticConcurrencyException ex)
{
    // Extract detailed conflict information
    var entityType = ex.GetEntityType(); // Returns the entity type (e.g., "Configuration")
    var entityId = ex.GetEntityId();     // Returns the entity ID
    var expectedVersion = ex.GetExpectedVersion(); // Returns the expected version string
    var actualVersion = ex.GetActualVersion();     // Returns the actual version string
    
    logger.LogWarning("Optimistic concurrency conflict detected for {EntityType} {EntityId}", entityType, entityId);
    logger.LogDebug("Expected version: {ExpectedVersion}, Actual version: {ActualVersion}", expectedVersion, actualVersion);
    
    // Display user-friendly retry message
    Console.WriteLine(ex.ToRetryMessage(retryCount: 2));
}

// Example 3: Handling CircularDependencyException
try
{
    await configurationService.CreateConfigurationAsync(configRequest);
}
catch (CircularDependencyException ex)
{
    logger.LogError(ex, "Circular dependency detected in configuration");
    throw new InvalidOperationException("Configuration contains circular dependencies. Please review your configuration structure.", ex);
}

// Example 4: Using ConcurrencyException with inner exception
try
{
    await diffService.CompareConfigurationsAsync(oldVersionId, newVersionId);
}
catch (ConcurrencyException ex)
{
    // Create a new exception with additional context
    throw new ConcurrencyException(
        $"Failed to compare configurations due to concurrency issues: {ex.Message}",
        ex,
        ex.ErrorCode
    );
}
```

## ConcurrencyExceptionExtensions

The `ConcurrencyExceptionExtensions` class provides extension methods for `ConcurrencyException` and its derived types (`OptimisticConcurrencyException`, `CircularDependencyException`) to simplify common concurrency conflict handling scenarios. These methods help identify exception types, extract detailed conflict information, generate user-friendly messages, and determine appropriate retry strategies.

### Public Members

- `IsOptimisticConcurrency` - Checks if exception is an optimistic concurrency conflict
- `IsCircularDependency` - Checks if exception is a circular dependency conflict
- `GetEntityType` - Extracts the entity type from optimistic concurrency exceptions
- `GetEntityId` - Extracts the entity ID from optimistic concurrency exceptions
- `GetExpectedVersion` - Extracts the expected version string
- `GetActualVersion` - Extracts the actual version string
- `ToRetryMessage` - Creates a user-friendly retry message with formatted details
- `ShouldRetryAutomatically` - Determines if automatic retry is recommended
- `GetAllMessages` - Gets all exception messages in the hierarchy

### Usage Example

```csharp
using DotnetConfigServer.Exceptions;
using System;

// Simulate a concurrency conflict
try
{
    // Attempt to save configuration that was modified by another process
    await configurationService.UpdateConfigurationAsync(configId, newConfig);
}
catch (ConcurrencyException ex) when (ex.ShouldRetryAutomatically())
{
    // Check exception type and extract details
    if (ex.IsOptimisticConcurrency())
    {
        Console.WriteLine($"Optimistic concurrency detected!");
        Console.WriteLine($"Entity Type: {ex.GetEntityType()}");
        Console.WriteLine($"Entity ID: {ex.GetEntityId()}");
        Console.WriteLine($"Expected Version: {ex.GetExpectedVersion()}");
        Console.WriteLine($"Actual Version: {ex.GetActualVersion()}");
    }
    else if (ex.IsCircularDependency())
    {
        Console.WriteLine("Circular dependency detected in configuration dependencies!");
    }

    // Generate a user-friendly retry message
    var retryMessage = ex.ToRetryMessage(retryCount: 2);
    Console.WriteLine(retryMessage);

    // Get all messages in the exception hierarchy
    var allMessages = ex.GetAllMessages();
    foreach (var message in allMessages)
    {
        Console.WriteLine($"- {message}");
    }

    // Automatic retry is recommended for these exception types
    if (ex.ShouldRetryAutomatically())
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount)));
        await RetryUpdateConfigurationAsync(configId, newConfig, retryCount + 1);
    }
}
```

## DotnetConfigServerException

The `DotnetConfigServerException` class is the base exception type for all Dotnet Config Server-specific exceptions. It extends the standard `Exception` class with additional properties for error tracking and debugging, including an `ErrorCode` property for categorizing exceptions and a `Details` property for including additional context about the error. This exception type is commonly thrown when Dotnet Config Server-specific operations fail, such as configuration validation errors, versioning conflicts, or encryption issues.

### Public Members

- `string? ErrorCode` - Gets or sets the error code for categorizing exceptions
- `object? Details` - Gets or sets additional error details as an object
- `DotnetConfigServerException(string message)` - Creates a new exception with a custom message
- `DotnetConfigServerException(string message, Exception innerException)` - Creates a new exception with a custom message and inner exception
- `DotnetConfigServerException(string message, string errorCode, object? details = null)` - Creates a new exception with message, error code, and optional details

### Usage Example

```csharp
using DotnetConfigServer.Exceptions;
using Microsoft.Extensions.Logging;

// Example 1: Basic exception with message
try
{
    await configurationService.ValidateConfigurationAsync(configuration);
}
catch (DotnetConfigServerException ex)
{
    logger.LogError(ex, "Dotnet Config Server operation failed");
    Console.WriteLine($"Error: {ex.Message}");
    if (!string.IsNullOrEmpty(ex.ErrorCode))
    {
        Console.WriteLine($"Error Code: {ex.ErrorCode}");
    }
}

// Example 2: Exception with error code and details
try
{
    await encryptionService.EncryptConfigurationValueAsync(sensitiveValue);
}
catch (DotnetConfigServerException ex) when (ex.ErrorCode == "ENCRYPTION_FAILED")
{
    logger.LogError(ex, "Encryption failed for configuration value");
    Console.WriteLine($"Failed to encrypt value. Details: {ex.Details}");
    
    // Re-throw with additional context
    throw new DotnetConfigServerException(
        "Failed to encrypt configuration value - check encryption service",
        "ENCRYPTION_FAILED",
        new { ValueLength = sensitiveValue.Length, Timestamp = DateTime.UtcNow }
    );
}

// Example 3: Exception with inner exception
try
{
    await versioningService.CreateVersionAsync(configurationId, versionRequest);
}
catch (DotnetConfigServerException ex)
{
    logger.LogError(ex, "Version creation failed");
    throw new InvalidOperationException("Version creation failed - see inner exception for details", ex);
}

// Example 4: Using the three-parameter constructor
try
{
    // Simulate a DotnetConfigServer-specific validation failure
    if (string.IsNullOrEmpty(configurationName))
    {
        throw new DotnetConfigServerException(
            "Configuration name cannot be null or empty",
            "VALIDATION_ERROR",
            new { Field = "Name", MaxLength = 100 }
        );
    }
}
catch (DotnetConfigServerException ex)
{
    logger.LogError(ex, "Configuration validation failed");
    Console.WriteLine($"Validation error {ex.ErrorCode}: {ex.Message}");
    Console.WriteLine($"Additional details: {ex.Details}");
}
```

## ConfigurationException

### Public Members

- `ConfigurationException(string message)` - Creates a configuration exception with a custom message
- `ConfigurationException(string message, Exception innerException)` - Creates a configuration exception with a custom message and inner exception
- `ConfigurationException(string message, string errorCode, object? details = null)` - Creates a configuration exception with message, error code, and optional details

### Derived Exception Types

- `ConfigurationNotFoundException` - Thrown when a requested configuration is not found
- `ConfigurationKeyNotFoundException` - Thrown when a configuration key is not found
- `EncryptionException` - Thrown when encryption or decryption fails
- `ConfigurationSnapshotNotFoundException` - Thrown when a configuration snapshot is not found
- `ConfigurationVersionNotFoundException` - Thrown when a configuration version is not found
- `WebhookException` - Thrown when a webhook operation fails

## ComparisonServiceTests

The `ComparisonServiceTests` class provides comprehensive unit tests for the `ComparisonService` functionality. It validates the comparison engine that detects differences between two objects, tracking which properties changed, their original and modified values, and calculating change statistics. The tests cover various scenarios including identical objects, single field changes, multiple field changes, null value handling, and percentage-based change calculations.

### Public Members

- `Name` (string) - The name of the test class instance
- `Port` (int) - The port number for the test configuration
- `Enabled` (bool) - Whether the test configuration is enabled
- `Compare_IdenticalObjects_ReturnsNoChanges()` - Validates that identical objects produce no changes
- `Compare_SingleFieldChanged_ReturnsSingleChange()` - Tests single property change detection
- `Compare_MultipleFieldsChanged_ReturnsAllChanges()` - Verifies multiple property changes are tracked
- `Compare_PropertyChange_IncludesPropertyType()` - Ensures property type information is included in changes
- `HasDifferences_IdenticalObjects_ReturnsFalse()` - Confirms identical objects are reported as having no differences
- `HasDifferences_DifferentObjects_ReturnsTrue()` - Validates that different objects are detected
- `GetSummary_NoChanges_ReturnsTotalChangesZeroAndEmptyFields()` - Tests summary generation for unchanged objects
- `GetSummary_OneOfThreeFieldsChanged_Returns33PercentChangePercentage()` - Validates 33% change calculation
- `GetSummary_AllFieldsChanged_Returns100PercentChangePercentage()` - Verifies 100% change detection
- `Compare_NullToString_PropertyValue_ReturnsNullLiteral()` - Tests null value handling
- `Label` (string?) - Optional label property for nullable string testing

### Usage Example

```csharp
using DotnetConfigServer.Tests;
using DotnetConfigServer.Services;
using Microsoft.Extensions.Logging;
using Moq;

// Setup test dependencies
var loggerMock = new Mock<ILogger<ComparisonService>>();
var comparisonService = new ComparisonService(loggerMock.Object);

// Create test objects
var originalConfig = new { Name = "old-service", Port = 8080, Enabled = false };
var modifiedConfig = new { Name = "new-service", Port = 443, Enabled = true };

// Compare objects and get detailed changes
var comparisonResult = comparisonService.Compare(originalConfig, modifiedConfig);

Console.WriteLine($"Item type: {comparisonResult.ItemType}");
Console.WriteLine($"Total changes: {comparisonResult.Changes.Count}");

foreach (var change in comparisonResult.Changes)
{
    Console.WriteLine($"Property '{change.PropertyName}' changed from '{change.OriginalValue}' to '{change.ModifiedValue}'");
    Console.WriteLine($"  Type: {change.PropertyType}");
}

// Check if objects have differences
bool hasDifferences = comparisonService.HasDifferences(originalConfig, modifiedConfig);
Console.WriteLine($"Has differences: {hasDifferences}");

// Get summary statistics
var summary = comparisonService.GetSummary(originalConfig, modifiedConfig);
Console.WriteLine($"Change percentage: {summary.ChangePercentage:P1}");
Console.WriteLine($"Changed fields: {string.Join(", ", summary.ChangedFields)}");
```

### Usage Example

```csharp
using DotnetConfigServer.Exceptions;
using Microsoft.Extensions.Logging;

// Example 1: Basic ConfigurationException usage
try
{
    var configuration = await configurationService.GetConfigurationAsync(configurationId);
}
catch (ConfigurationException ex)
{
    logger.LogError(ex, "Configuration operation failed");
    
    // Access error code
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    
    // Access error details if available
    if (ex.Details != null)
    {
        Console.WriteLine($"Details: {ex.Details}");
    }
}

// Example 2: Handling specific configuration exceptions
try
{
    var config = await configurationService.GetConfigurationAsync("non-existent-config");
}
catch (ConfigurationNotFoundException ex)
{
    // ConfigurationNotFoundException(string configId)
    Console.WriteLine($"Configuration not found: {ex.Message}");
    
    // Access the configuration ID that was not found
    if (ex.Details is IDictionary<string, object> details && details.TryGetValue("ConfigurationId", out var configId))
    {
        Console.WriteLine($"Missing configuration ID: {configId}");
    }
}

// Example 3: Handling configuration key not found
try
{
    var value = await configurationService.GetConfigurationValueAsync(configId, "NonExistent:Key");
}
catch (ConfigurationKeyNotFoundException ex)
{
    // ConfigurationKeyNotFoundException(string key)
    Console.WriteLine($"Configuration key not found: {ex.Message}");
    
    // Access the key that was not found
    if (ex.Details is IDictionary<string, object> details && details.TryGetValue("Key", out var key))
    {
        Console.WriteLine($"Missing key: {key}");
    }
}

// Example 4: Handling encryption failures
try
{
    var decryptedValue = await encryptionService.DecryptAsync(encryptedValue);
}
catch (EncryptionException ex)
{
    // EncryptionException(string message)
    logger.LogError(ex, "Failed to decrypt configuration value");
    
    // EncryptionException(string message, Exception innerException)
    throw new InvalidOperationException("Configuration decryption failed - check encryption keys", ex);
}

// Example 5: Using ConfigurationException with error codes
try
{
    await webhookService.RegisterWebhookAsync(webhookRequest);
}
catch (WebhookException ex) when (ex.ErrorCode == "WEBHOOK_ERROR")
{
    Console.WriteLine($"Webhook error occurred: {ex.Message}");
    
    if (ex.Details is IDictionary<string, object> details && details.TryGetValue("WebhookId", out var webhookId))
    {
        Console.WriteLine($"Webhook ID: {webhookId}");
    }
}

// Example 6: Creating custom configuration exceptions with details
try
{
    // Simulate a configuration validation failure
    if (string.IsNullOrEmpty(configValue))
    {
        throw new ConfigurationException(
            "Configuration value cannot be null or empty",
            "VALIDATION_FAILED",
            new { Field = "ConnectionString", MaxLength = 255 }
        );
    }
}
catch (ConfigurationException ex)
{
    logger.LogError(ex, "Configuration validation failed");
    Console.WriteLine($"Validation failed with error code: {ex.ErrorCode}");
    Console.WriteLine($"Additional details: {ex.Details}");
}
```

## ConfigurationExceptionExtensions

The `ConfigurationExceptionExtensions` class provides extension methods for `ConfigurationException` and its derived exception types to simplify common configuration error handling scenarios. These methods help identify specific exception types, extract configuration IDs and keys, convert between exception types, and access detailed error information for better error handling and debugging.

### What It Does

This extension class provides methods to:
- Check if an exception is a specific type of configuration error
- Safely extract configuration IDs and keys from exceptions
- Convert generic configuration exceptions to more specific types
- Access detailed error information and error codes
- Work with exception details objects for additional context

### Public Members

- `IsConfigurationNotFound` - Checks if exception is a configuration not found error
- `IsConfigurationKeyNotFound` - Checks if exception is a configuration key not found error
- `IsEncryptionException` - Checks if exception is an encryption error
- `IsConfigurationSnapshotNotFound` - Checks if exception is a configuration snapshot not found error
- `IsConfigurationVersionNotFound` - Checks if exception is a configuration version not found error
- `IsWebhookException` - Checks if exception is a webhook error
- `TryGetConfigurationId` - Safely extracts configuration ID from exceptions
- `TryGetConfigurationId` (Guid overload) - Extracts configuration ID as GUID
- `TryGetKey` - Safely extracts key from configuration key not found exceptions
- `TryGetKeyId` - Extracts key ID as GUID
- `ToConfigurationNotFound` - Converts to ConfigurationNotFoundException
- `ToConfigurationKeyNotFound` - Converts to ConfigurationKeyNotFoundException
- `ToEncryptionException` - Converts to EncryptionException
- `GetDetails` - Gets the error details object
- `GetDetailsDictionary` - Gets all error details as a dictionary
- `HasErrorCode` - Checks if exception has a specific error code

### Usage Example

```csharp
using DotnetConfigServer.Exceptions;
using System;

try
{
    // Attempt to get a configuration that doesn't exist
    var config = await configurationService.GetConfigurationAsync(configurationId);
}
catch (ConfigurationException ex)
{
    // Check exception type
    if (ex.IsConfigurationNotFound())
    {
        Console.WriteLine("Configuration not found - creating a new one...");
        
        // Safely extract configuration ID
        if (ex.TryGetConfigurationId(out var configId))
        {
            Console.WriteLine($"Configuration ID: {configId}");
        }
        
        // Convert to specific exception type
        throw ex.ToConfigurationNotFound();
    }
    else if (ex.IsConfigurationKeyNotFound())
    {
        Console.WriteLine("Configuration key not found - adding default value...");
        
        // Safely extract key
        if (ex.TryGetKey(out var key))
        {
            Console.WriteLine($"Missing key: {key}");
        }
        
        // Convert to specific exception type
        throw ex.ToConfigurationKeyNotFound();
    }
    else if (ex.IsEncryptionException())
    {
        Console.WriteLine("Encryption error occurred - handling decryption failure...");
        
        // Get error details
        var details = ex.GetDetails();
        var detailsDict = ex.GetDetailsDictionary();
        
        Console.WriteLine($"Error code: {ex.ErrorCode}");
        Console.WriteLine($"Has error code 'CONFIG-ENCRYPT-001': {ex.HasErrorCode("CONFIG-ENCRYPT-001")}");
        
        // Convert to specific exception type
        throw ex.ToEncryptionException();
    }
}

// Example with configuration ID extraction
try
{
    await configurationService.UpdateConfigurationAsync(configurationId, updatedConfig);
}
catch (ConfigurationException ex) when (ex.TryGetConfigurationId(out var configId))
{
    Console.WriteLine($"Failed to update configuration {configId}");
    logger.LogError(ex, "Configuration update failed for {ConfigId}", configId);
    
    // Access all details as dictionary
    var details = ex.GetDetailsDictionary();
    foreach (var kvp in details)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}
```


## VersioningBenchmarks

The `VersioningBenchmarks` class provides a set of performance benchmarks for version management operations within the configuration system. It allows developers to measure the efficiency of critical lifecycle actions such as creating, retrieving, publishing, and rolling back configurations across various versions.

### Usage Example

```csharp
// Assuming an instance of VersioningBenchmarks exists, 
// usually invoked via a benchmark runner framework like BenchmarkDotNet.
var benchmarks = new VersioningBenchmarks();

// Perform setup before running benchmarks
await benchmarks.GlobalSetup();

// Measure performance of key versioning operations
await benchmarks.CreateVersion();
await benchmarks.GetVersion();
await benchmarks.GetVersions();
await benchmarks.GetActiveVersion();
await benchmarks.PublishVersion();
await benchmarks.ArchiveVersion();
await benchmarks.DeprecateVersion();
await benchmarks.Rollback();
await benchmarks.GetVersionHistory();
await benchmarks.CleanupOldVersions();
await benchmarks.CreateVersionWithManyKeys();

// Perform cleanup after running benchmarks
await benchmarks.GlobalCleanup();
```

using System;
using System.Threading.Tasks;

// Create the event bus
var eventBus = new EventBus();

// Define an event type
public record ConfigurationChangedEvent(Guid ConfigurationId, string Environment, DateTime ChangedAt);

// Subscribe to events
var subscription = eventBus.Subscribe<ConfigurationChangedEvent>(async (e) =>
{
    Console.WriteLine($"Configuration changed: {e.ConfigurationId} in {e.Environment} at {e.ChangedAt}");
    // Handle the event (e.g., reload configuration, invalidate cache, etc.)
    await Task.CompletedTask;
});

// Publish an event
var configChangedEvent = new ConfigurationChangedEvent(
    Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    "Production",
    DateTime.UtcNow
);

await eventBus.PublishAsync(configChangedEvent);

// Get all subscribers for a specific event type
var subscribers = eventBus.GetSubscribers<ConfigurationChangedEvent>();
Console.WriteLine($"Number of subscribers: {subscribers.Count()}");

// Unsubscribe when done
subscription.Dispose();

// Clear all subscribers (useful for testing or application shutdown)
eventBus.Clear();
```

## EventBusExtensions

The `EventBusExtensions` class provides extension methods for `EventBus` that offer additional functionality for event subscription management, publishing with diagnostics, and subscriber inspection. These methods simplify common event bus operations like subscribing to multiple event types at once, checking subscriber counts, and publishing events while tracking how many handlers were invoked.

### Usage Example

```csharp
using DotnetConfigServer.Events;
using System;
using System.Threading.Tasks;

// Create the event bus
var eventBus = new EventBus();

// Define event types
public record ConfigurationChangedEvent(Guid ConfigurationId, string Environment);
public record CacheInvalidatedEvent(string CacheKey);

// Subscribe to multiple event types with a single handler
eventBus.Subscribe<ConfigurationChangedEvent, CacheInvalidatedEvent>(async (e) =>
{
    Console.WriteLine($"Event received: {e.GetType().Name}");
    await Task.CompletedTask;
});

// Check if there are any subscribers for a specific event type
bool hasConfigSubscribers = eventBus.HasSubscribers<ConfigurationChangedEvent>();
Console.WriteLine($"Has configuration subscribers: {hasConfigSubscribers}");

// Get the exact count of subscribers for an event type
int configSubscriberCount = eventBus.GetSubscriberCount<ConfigurationChangedEvent>();
Console.WriteLine($"Configuration subscriber count: {configSubscriberCount}");

// Publish an event and get the number of handlers that were invoked
var configEvent = new ConfigurationChangedEvent(
    Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    "Production"
);
int handledCount = await eventBus.PublishAsyncWithCount(configEvent);
Console.WriteLine($"Event handled by {handledCount} subscribers");

// Get a snapshot of all subscriber counts across all event types
var allSubscriberCounts = eventBus.GetAllSubscriberCounts();
Console.WriteLine("All subscriber counts:");
foreach (var kvp in allSubscriberCounts)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
}

// Unsubscribe all handlers of a specific type
eventBus.UnsubscribeAll<ConfigurationChangedEvent>();
```

## NotFoundException

The `NotFoundException` is thrown when the requested resource is not found.

Example usage:
```csharp
var notFoundException = new NotFoundException("Resource not found");

// Application-specific exceptions
var appException = new ApplicationNotFoundException("app-123");
var appGuidException = new ApplicationNotFoundException(Guid.Parse("123e4567-e89b-12d3-a456-426614174000"));

// User-specific exceptions  
var userException = new UserNotFoundException("user-456");
var userGuidException = new UserNotFoundException(Guid.Parse("123e4567-e89b-12d3-a456-426614174001"));

// Webhook delivery exceptions
var deliveryException = new WebhookDeliveryNotFoundException("delivery-789");
var deliveryGuidException = new WebhookDeliveryNotFoundException(Guid.Parse("123e4567-e89b-12d3-a456-426614174002"));

// Change request exceptions
var changeRequestException = new ChangeRequestNotFoundException("change-101");
var changeRequestGuidException = new ChangeRequestNotFoundException(Guid.Parse("123e4567-e89b-12d3-a456-426614174003"));
```

## ValidationResult

`ValidationResult` is a sealed record used to represent the outcome of validation operations throughout the Dotnet Config Server application. It provides a simple way to indicate whether validation succeeded or failed, and includes methods for serializing/deserializing validation results to/from JSON.

The type includes static factory methods for creating validation results and parsing from JSON, making it ideal for API responses, configuration validation, and service validation scenarios.

### Public Members

- `public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Problems)`
- `public static ValidationResult Valid` - Creates a valid result with no problems
- `public static ValidationResult Invalid(IReadOnlyList<string> problems)` - Creates an invalid result with validation problems
- `public static string ToJson(ValidationResult value, bool indented = false)` - Serializes to JSON
- `public static ValidationResult? FromJson(string json)` - Deserializes from JSON
- `public static bool TryFromJson(string json, out ValidationResult? value)` - Safely deserializes from JSON

### Usage Example

```csharp
using DotnetConfigServer.Infrastructure;

// Create a successful validation result
var successResult = ValidationResult.Valid;
Console.WriteLine(successResult.ToJson()); // {"isValid":true,"problems":[]}

// Create an invalid validation result with multiple problems
var invalidResult = ValidationResult.Invalid(new[] {"Database connection string is required", "Timeout value must be positive"});
Console.WriteLine(invalidResult.ToJson());
// {"isValid":false,"problems":["Database connection string is required","Timeout value must be positive"]}

// Parse validation result from JSON
string json = "{\"isValid\":true,\"problems\":[]}";
var parsedResult = ValidationResult.FromJson(json);
Console.WriteLine(parsedResult?.IsValid); // True

// Try to parse validation result safely
if (ValidationResult.TryFromJson(json, out var safeResult))
{
    Console.WriteLine($"Successfully parsed: IsValid={safeResult.IsValid}");
}

// Use in validation scenarios
public ValidationResult ValidateConfiguration(string configValue)
{
    if (string.IsNullOrWhiteSpace(configValue))
    {
        return ValidationResult.Invalid(new[] {"Configuration value cannot be empty"});
    }
    
    return ValidationResult.Valid;
}

// Example usage in a service
var validation = ValidateConfiguration("my-config-value");
if (!validation.IsValid)
{
    Console.WriteLine($"Validation failed with {validation.Problems.Count} problems:");
    foreach (var problem in validation.Problems)
    {
        Console.WriteLine($"- {problem}");
    }
}
```

## DomainEvent

The DomainEvent type represents a change in the application configuration.

Usage example:

```csharp
public class MyDomainEvent : DomainEvent
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Source { get; set; }
    public string? UserId { get; set; }
    public Guid ConfigurationId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, object?> Changes { get; set; } = new();
    public Guid KeyId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool IsEncrypted { get; set; }
}
```

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
