# Dotnet Config Server

![CI](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/ci.yml/badge.svg) ![License](https://img.shields.io/github/license/sarmkadan/dotnet-config-server) ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4) [![Build](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/build.yml) [![Docker](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/docker.yml/badge.svg)](https://github.com/sarmkadan/dotnet-config-server/actions/workflows/docker.yml)

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
# API: https://localhost:5001 | Swagger: https://localhost:5001/swagger
```

Or with Docker Compose (no local SQL Server needed):

```bash
docker-compose up
# API: http://localhost:80 | Swagger: http://localhost:80/swagger
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
        Console.WriteLine($" {error.Key}: {error.Value}");
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

## ApplicationDbContext

The `ApplicationDbContext` is the Entity Framework Core database context that serves as the primary interface between the application and the database. It manages all database operations including CRUD operations for configurations, applications, webhook subscriptions, audit logs, and encryption keys. The context provides access to all entity collections through its `DbSet<T>` properties, enabling comprehensive configuration management with built-in validation, encryption, and versioning capabilities.

**Key Features:**
- Manages 14 different entity types covering the entire configuration lifecycle
- Supports Entity Framework Core migrations for database schema management
- Provides optimized indexing for common query patterns
- Enables change tracking and audit logging automatically
- Integrates with dependency injection for testability and flexibility

### Usage Example

```csharp
// Configure DbContext with SQL Server (appsettings.json)
services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

// Constructor injection in a service
public class ConfigurationService
{
    private readonly ApplicationDbContext _context;

    public ConfigurationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Application> GetApplicationBySlugAsync(string slug)
    {
        return await _context.Applications
            .Include(a => a.Configurations)
            .ThenInclude(c => c.Versions)
            .FirstOrDefaultAsync(a => a.Slug == slug);
    }

    public async Task AddConfigurationAsync(Configuration configuration)
    {
        _context.Configurations.Add(configuration);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateConfigurationAsync(Configuration configuration)
    {
        _context.Configurations.Update(configuration);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Configuration>> GetActiveConfigurationsAsync(int applicationId)
    {
        return await _context.Configurations
            .Where(c => c.ApplicationId == applicationId && c.IsActive)
            .ToListAsync();
    }

    public async Task<List<WebhookDelivery>> GetFailedWebhookDeliveriesAsync(int subscriptionId)
    {
        return await _context.WebhookDeliveries
            .Where(d => d.WebhookSubscriptionId == subscriptionId && d.Status == "failed")
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}

// Usage in a controller
[ApiController]
[Route("api/v1/configurations")]
public class ConfigurationController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ConfigurationController(ApplicationDbContext context)
    {
        _context = context;
    }

## BaseRepository

The `BaseRepository<T>` class is an abstract generic repository implementation that provides common CRUD operations for Entity Framework Core entities. It serves as a foundation for concrete repository implementations, handling database operations such as retrieving entities by ID, adding, updating, and deleting entities, as well as retrieving all entities and saving changes to the database. The repository includes error handling and logging capabilities to ensure robust data access operations.

**Key Features:**
- Abstract base class that can be inherited by any entity-specific repository
- Provides standard CRUD operations with async/await pattern
- Includes comprehensive error handling and logging through `ILogger<T>`
- Supports Entity Framework Core's change tracking and database operations
- Designed for dependency injection with `ApplicationDbContext`

### Usage Example

```csharp
// Create a concrete repository implementation
public class ApplicationRepository : BaseRepository<Application>
{
    public ApplicationRepository(ApplicationDbContext context, ILogger<ApplicationRepository> logger)
        : base(context, logger)
    {
    }

    // Add custom methods specific to Application entity
    public async Task<Application?> GetBySlugAsync(string slug)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Slug == slug);
    }
}

// Usage in a service
public class ApplicationService
{
    private readonly ApplicationRepository _repository;

    public ApplicationService(ApplicationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Application?> GetApplicationAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task AddApplicationAsync(Application application)
    {
        await _repository.AddAsync(application);
        await _repository.SaveChangesAsync();
    }

    public async Task UpdateApplicationAsync(Application application)
    {
        await _repository.UpdateAsync(application);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteApplicationAsync(Application application)
    {
        await _repository.DeleteAsync(application);
        await _repository.SaveChangesAsync();
    }

    public async Task<List<Application>> GetAllApplicationsAsync()
    {
        return await _repository.GetAllAsync();
    }
}

// Register in DI container (Program.cs or Startup.cs)
builder.Services.AddScoped<ApplicationRepository>();
```

## ConfigurationKeyRepository

The `ConfigurationKeyRepository` class provides specialized data access operations for `ConfigurationKey` entities, enabling efficient querying and management of configuration key-value pairs within the Dotnet Config Server. It extends the base repository functionality with methods for retrieving configuration keys by configuration ID, version, or key name, as well as advanced search capabilities that support filtering by configuration, key prefix, and text queries across key names, values, and descriptions.

**Key Features:**
- Retrieve all active configuration keys for a specific configuration
- Find specific configuration keys by name within a configuration
- Search configuration keys with flexible filtering options
- Retrieve all active keys for a specific configuration version
- Filter by configuration ID, key prefix, or text search across multiple fields

### Usage Example

```csharp
// Example: Managing configuration keys for a microservice
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using Microsoft.Extensions.Logging;

// Create repository instance (typically injected via DI)
var repository = new ConfigurationKeyRepository(dbContext, logger);

// Get all active configuration keys for a specific configuration
var keys = await repository.GetByConfigurationAsync(configurationId);

// Find a specific configuration key by name
var connectionStringKey = await repository.GetByKeyNameAsync(configurationId, "ConnectionStrings:Default");

// Search configuration keys with various filters
var searchResults = await repository.SearchAsync(
    query: "timeout",
    prefix: "ConnectionStrings:",
    configurationId: configurationId
);

// Get all active keys for a specific version
var versionKeys = await repository.GetByVersionAsync(versionId);

// Example: Using in a configuration management service
public class ConfigurationKeyService
{
    private readonly ConfigurationKeyRepository _repository;
    private readonly ConfigurationVersionRepository _versionRepository;

    public ConfigurationKeyService(
        ConfigurationKeyRepository repository,
        ConfigurationVersionRepository versionRepository)
    {
        _repository = repository;
        _versionRepository = versionRepository;
    }

    public async Task UpdateConfigurationKeyAsync(
        Guid configurationId,
        string keyName,
        string newValue,
        string updatedBy)
    {
        // Find the existing key
        var existingKey = await _repository.GetByKeyNameAsync(configurationId, keyName);
        
        if (existingKey == null)
        {
            throw new KeyNotFoundException($"Configuration key '{keyName}' not found.");
        }
        
        // Create new version
        var newVersion = new ConfigurationVersion
        {
            ConfigurationId = configurationId,
            VersionNumber = Guid.NewGuid().ToString(),
            Status = ConfigurationVersionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = updatedBy,
            Changes = $"Updated key: {keyName}"
        };
        
        // Update key value
        existingKey.Value = newValue;
        existingKey.UpdatedAt = DateTime.UtcNow;
        existingKey.UpdatedBy = updatedBy;
        
        // Save changes
        await _versionRepository.AddAsync(newVersion);
        await _repository.UpdateAsync(existingKey);
        await _repository.SaveChangesAsync();
    }

    public async Task<List<ConfigurationKey>> SearchConfigurationKeysAsync(
        string searchTerm,
        Guid? configurationId = null)
    {
        return await _repository.SearchAsync(
            query: searchTerm,
            prefix: null,
            configurationId: configurationId
        );
    }
}

// Register in DI container (Program.cs)
builder.Services.AddScoped<ConfigurationKeyRepository>();
```

[HttpGet("{id}")]
public async Task<IActionResult> GetConfiguration(int id)
{
    var config = await _context.Configurations
        .Include(c => c.Keys)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (config == null) return NotFound();

    return Ok(config);
}