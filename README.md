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

## MemoryCacheService

The `MemoryCacheService` provides an in-memory caching implementation using `ConcurrentDictionary` for thread-safe operations. It's designed for single-instance deployments and includes automatic expiration cleanup, cache statistics tracking, and async/await support for all operations. The service implements `ICacheService` interface and provides methods for getting, setting, removing, checking existence, and bulk operations.

**Key Features:**
- Thread-safe concurrent dictionary-based implementation
- Automatic cleanup of expired entries via background timer
- Comprehensive cache statistics (hits, misses, sets, deletes, size)
- Async/await pattern for all operations
- Support for custom expiration times on cache entries
- Pattern-based key retrieval with `GetKeysAsync`
- Bulk operations for removing multiple keys
- Memory-efficient implementation with proper disposal

### Usage Example

```csharp
// Register in DI container (Program.cs)
builder.Services.AddSingleton<MemoryCacheService>();

// Usage in a service
public class ConfigurationService
{
    private readonly MemoryCacheService _cache;
    private readonly ConfigurationRepository _repository;

    public ConfigurationService(MemoryCacheService cache, ConfigurationRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<Configuration> GetConfigurationWithCachingAsync(Guid configurationId, string environment)
    {
        // Create a cache key based on configuration ID and environment
        var cacheKey = $"config:{configurationId}:{environment}";
        
        // Try to get from cache first
        var cachedConfig = await _cache.GetAsync<Configuration>(cacheKey);
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        // Cache miss - fetch from database
        var configuration = await _repository.GetByIdAsync(configurationId);
        if (configuration != null)
        {
            // Cache for 5 minutes
            await _cache.SetAsync(cacheKey, configuration, TimeSpan.FromMinutes(5));
        }

        return configuration;
    }

    public async Task UpdateConfigurationWithCacheInvalidationAsync(Guid configurationId, string environment, Configuration updatedConfig)
    {
        // Update in database
        await _repository.UpdateAsync(updatedConfig);
        await _repository.SaveChangesAsync();

        // Invalidate cache for this configuration
        var cacheKey = $"config:{configurationId}:{environment}";
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task<List<Configuration>> GetConfigurationsBatchAsync(IEnumerable<Guid> configurationIds, string environment)
    {
        var results = new List<Configuration>();
        var missingIds = new List<Guid>();
        
        // Try to get from cache first
        foreach (var id in configurationIds)
        {
            var cacheKey = $"config:{id}:{environment}";
            var cachedConfig = await _cache.GetAsync<Configuration>(cacheKey);
            if (cachedConfig != null)
            {
                results.Add(cachedConfig);
            }
            else
            {
                missingIds.Add(id);
            }
        }

        // Fetch missing configurations from database
        if (missingIds.Any())
        {
            var missingConfigs = await _repository.GetByIdsAsync(missingIds);
            foreach (var config in missingConfigs)
            {
                var cacheKey = $"config:{config.Id}:{environment}";
                await _cache.SetAsync(cacheKey, config, TimeSpan.FromMinutes(5));
                results.Add(config);
            }
        }

        return results;
    }

    public async Task CheckCacheStatisticsAsync()
    {
        var stats = await _cache.GetStatsAsync();
        Console.WriteLine($"Cache Stats - Hits: {stats.Hits}, Misses: {stats.Misses}, Sets: {stats.Sets}, Deletes: {stats.Deletes}, Size: {stats.Size}");
    }

    public async Task ClearConfigurationCacheAsync(Guid configurationId)
    {
        // Remove all cache entries related to this configuration
        var pattern = $"config:{configurationId}:*";
        var keys = await _cache.GetKeysAsync(pattern);
        await _cache.RemoveAsync(keys);
    }
}

// Example with GetOrCreateAsync for computed values
public async Task<TimeSpan> GetServiceTimeoutAsync(string serviceName)
{
    var cacheKey = $"service-timeout:{serviceName}";
    
    return await _cache.GetOrCreateAsync(
        cacheKey,
        async () => 
        {
            // Simulate expensive computation or database lookup
            var timeoutConfig = await _repository.GetByNameAsync("Timeouts", serviceName);
            return timeoutConfig != null && TimeSpan.TryParse(timeoutConfig.Value, out var timeout)
                ? timeout
                : TimeSpan.FromSeconds(30);
        },
        TimeSpan.FromHours(1) // Cache for 1 hour
    );
}
```

## ICacheService

The `ICacheService` interface defines the contract for distributed caching operations in the Dotnet Config Server. It abstracts over different cache implementations (in-memory, Redis, etc.) and provides essential caching operations including get, set, remove, existence checks, and bulk operations. The interface also exposes comprehensive cache statistics through `GetStatsAsync()` method, which tracks hits, misses, sets, deletes, and current cache size for monitoring and debugging purposes.

**Key Features:**
- Thread-safe asynchronous operations with async/await pattern
- Support for custom expiration times on cache entries
- Bulk operations for removing multiple keys
- Pattern-based key retrieval with `GetKeysAsync`
- Cache statistics tracking for monitoring and debugging
- Generic methods for type-safe caching operations

### Usage Example

```csharp
// Register in DI container (Program.cs)
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Usage in a service
public class ConfigurationService
{
    private readonly ICacheService _cache;
    private readonly ConfigurationRepository _repository;

    public ConfigurationService(ICacheService cache, ConfigurationRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<Configuration> GetConfigurationWithCachingAsync(Guid configurationId, string environment)
    {
        // Create a cache key based on configuration ID and environment
        var cacheKey = $"config:{configurationId}:{environment}";

        // Try to get from cache first
        var cachedConfig = await _cache.GetAsync<Configuration>(cacheKey);
        if (cachedConfig != null)
        {
            return cachedConfig;
        }

        // Cache miss - fetch from database
        var configuration = await _repository.GetByIdAsync(configurationId);
        if (configuration != null)
        {
            // Cache for 5 minutes
            await _cache.SetAsync(cacheKey, configuration, TimeSpan.FromMinutes(5));
        }

        return configuration;
    }

    public async Task UpdateConfigurationWithCacheInvalidationAsync(Guid configurationId, string environment, Configuration updatedConfig)
    {
        // Update in database
        await _repository.UpdateAsync(updatedConfig);
        await _repository.SaveChangesAsync();

        // Invalidate cache for this configuration
        var cacheKey = $"config:{configurationId}:{environment}";
        await _cache.RemoveAsync(cacheKey);
    }

    public async Task CheckCacheStatisticsAsync()
    {
        var stats = await _cache.GetStatsAsync();
        Console.WriteLine($"Cache Stats - Hits: {stats.Hits}, Misses: {stats.Misses}, Sets: {stats.Sets}, Deletes: {stats.Deletes}, Size: {stats.Size}");
    }

    public async Task ClearAllCacheAsync()
    {
        // Clear all entries from cache
        await _cache.ClearAsync();
    }

    public async Task RemoveMultipleKeysAsync(IEnumerable<string> keys)
    {
        // Remove multiple cache entries at once
        await _cache.RemoveAsync(keys);
    }

    public async Task<bool> CheckKeyExistsAsync(string key)
    {
        // Check if a key exists in cache
        return await _cache.ExistsAsync(key);
    }
}
```

## CollectionExtensions

The `CollectionExtensions` static class provides a comprehensive set of utility methods for working with collections and enumerables in .NET. It includes methods for batching collections, checking collection state, performing LINQ-like operations, and manipulating sequences with additional functionality beyond the standard .NET collection APIs. These extensions are particularly useful for processing configuration data, managing collections of configuration keys, and handling various collection-based scenarios in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

// Create sample configuration data
var configKeys = new List<ConfigurationKey>
{
    new ConfigurationKey { KeyName = "Database:ConnectionString", Value = "Server=localhost;Database=ConfigDb" },
    new ConfigurationKey { KeyName = "Database:Timeout", Value = "30" },
    new ConfigurationKey { KeyName = "Cache:Enabled", Value = "true" },
    new ConfigurationKey { KeyName = "Cache:Size", Value = "100" },
    new ConfigurationKey { KeyName = "Logging:Level", Value = "Information" }
};

// Batch configuration keys into groups of 2
var batchedKeys = configKeys.Batch(2);
foreach (var batch in batchedKeys)
{
    Console.WriteLine($"Batch with {batch.Count()} keys:");
    foreach (var key in batch)
    {
        Console.WriteLine($"  - {key.KeyName} = {key.Value}");
    }
    Console.WriteLine();
}

// Check if collection is null or empty
if (configKeys.IsNullOrEmpty())
{
    Console.WriteLine("No configuration keys found.");
}
else
{
    Console.WriteLine($"Found {configKeys.Count} configuration keys.");
}

// Check if collection has only one item
if (configKeys.IsSingle())
{
    Console.WriteLine("Only one configuration key exists.");
}
else
{
    Console.WriteLine("Multiple configuration keys exist.");
}

// Skip the last configuration key
var allButLast = configKeys.SkipLast(1);
Console.WriteLine($"All keys except last: {allButLast.Count()}");

// Get distinct configuration key prefixes using DistinctBy
var prefixes = configKeys
    .Select(k => k.KeyName.Split(':')[0])
    .DistinctBy(prefix => prefix)
    .ToList();

Console.WriteLine("Unique prefixes:");
prefixes.ForEach(p => Console.WriteLine($"  - {p}"));

// Shuffle configuration keys for random processing order
var shuffledKeys = configKeys.Shuffle().ToList();
Console.WriteLine("Processing configuration keys in random order:");
shuffledKeys.ForEach(k => Console.WriteLine($"  - {k.KeyName}"));

// Zip configuration keys with their values as tuples
var zipped = configKeys.ZipWith(k => k.KeyName, k => k.Value);
Console.WriteLine("Configuration key-value pairs:");
zipped.ForEach(t => Console.WriteLine($"  - {t.Item1} = {t.Item2}"));

// Convert to dictionary for quick lookup
var keyDictionary = configKeys.ToDictionary(k => k.KeyName, k => k.Value);
Console.WriteLine($"Dictionary contains {keyDictionary.Count} entries.");

// Process each configuration key using ForEach extension
Console.WriteLine("Processing all configuration keys:");
configKeys.ForEach(key =>
{
    Console.WriteLine($"  Processing: {key.KeyName}");
    // Additional processing logic here
});

// Check if there are multiple database-related keys
var databaseKeys = configKeys.Where(k => k.KeyName.StartsWith("Database:"));
if (databaseKeys.HasMultiple())
{
    Console.WriteLine("Multiple database configuration keys found.");
}
```

## CacheKeyGenerator

The `CacheKeyGenerator` static class provides a centralized way to generate consistent and predictable cache keys for various entities and scenarios within the Dotnet Config Server. It ensures cache keys follow a standardized naming convention using colon-separated segments, making cache management more maintainable and reducing the risk of key collisions. The generator provides methods for creating keys for configurations, applications, versions, webhook subscriptions, and search operations, along with helper methods to generate invalidation patterns for cache cleanup.

**Key Features:**
- Static utility class with consistent naming conventions
- Generates keys for all major entity types (configurations, applications, versions, keys, webhooks)
- Provides invalidation patterns for cache cleanup when entities change
- Supports search operations with query-based keys
- Thread-safe and deterministic key generation

### Usage Example

```csharp
using DotnetConfigServer.Caching;

// Generate cache keys for configuration operations
var configId = Guid.NewGuid();
var applicationId = Guid.NewGuid();
var keyId = Guid.NewGuid();
var versionId = Guid.NewGuid();

// Single entity keys
var configKey = CacheKeyGenerator.GetConfigurationKey(configId);
var appKey = CacheKeyGenerator.GetApplicationKey(applicationId);
var keyKey = CacheKeyGenerator.GetConfigurationKeyKey(keyId);
var versionKey = CacheKeyGenerator.GetConfigurationVersionKey(versionId);

// Collection/list keys
var appConfigsKey = CacheKeyGenerator.GetApplicationConfigurationsKey(applicationId);
var configKeysKey = CacheKeyGenerator.GetConfigurationKeysKey(configId);
var configVersionsKey = CacheKeyGenerator.GetConfigurationVersionsKey(configId);

// Relationship-based keys
var diffKey = CacheKeyGenerator.GetConfigurationDiffKey(versionId, Guid.NewGuid());
var webhookSubsKey = CacheKeyGenerator.GetWebhookSubscriptionsKey(applicationId);
var allAppsKey = CacheKeyGenerator.GetAllApplicationsKey();

// Search key with optional application filter
var searchKey = CacheKeyGenerator.GetSearchKey("timeout settings", applicationId);

// Get invalidation patterns when entities change
var configPatterns = CacheKeyGenerator.GetInvalidationPatternsForConfiguration(configId, applicationId);
foreach (var pattern in configPatterns)
{
    Console.WriteLine($"Invalidate: {pattern}");
}

var appPatterns = CacheKeyGenerator.GetInvalidationPatternsForApplication(applicationId);
foreach (var pattern in appPatterns)
{
    Console.WriteLine($"Invalidate: {pattern}");
}
```

## ConfigurationRepository

The `ConfigurationRepository` class provides specialized data access operations for `Configuration` entities, enabling efficient querying and management of configurations within the Dotnet Config Server. It extends the base repository functionality with methods for retrieving configurations by application ID, configuration name, and advanced search capabilities that support filtering by application and text queries across configuration names and descriptions. The repository also includes methods for counting configurations by application and retrieving deleted configurations before a specific cutoff date.

**Key Features:**
- Retrieve all active configurations for a specific application
- Find specific configurations by name within an application
- Search configurations with flexible filtering options
- Count configurations by application ID
- Retrieve deleted configurations before a specific date

### Usage Example

```csharp
// Example: Managing configurations for microservices
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using Microsoft.Extensions.Logging;

// Create repository instance (typically injected via DI)
var repository = new ConfigurationRepository(dbContext, logger);

// Get all active configurations for a specific application
var appConfigurations = await repository.GetByApplicationIdAsync(applicationId);

// Find a specific configuration by name within an application
var devConfig = await repository.GetByNameAsync("Development", applicationId);

// Search configurations with various filters
var searchResults = await repository.SearchAsync(
    query: "production",
    applicationId: applicationId
);

// Get count of configurations for an application
var configCount = await repository.GetCountByApplicationAsync(applicationId);

// Get deleted configurations before a specific date
var deletedConfigs = await repository.GetDeletedBeforeAsync(DateTime.UtcNow.AddDays(-30));

// Example: Using in a configuration management service
public class ConfigurationService
{
    private readonly ConfigurationRepository _repository;
    private readonly ApplicationRepository _appRepository;

    public ConfigurationService(
        ConfigurationRepository repository,
        ApplicationRepository appRepository)
    {
        _repository = repository;
        _appRepository = appRepository;
    }

    public async Task<Configuration> CreateConfigurationAsync(
        Guid applicationId,
        string name,
        string environment,
        string description,
        string createdBy)
    {
        var application = await _appRepository.GetByIdAsync(applicationId);
        
        if (application == null)
        {
            throw new ArgumentException("Application not found.");
        }

        var configuration = new Configuration
        {
            ApplicationId = applicationId,
            Name = name,
            Environment = environment,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsActive = true
        };

        await _repository.AddAsync(configuration);
        await _repository.SaveChangesAsync();
        
        return configuration;
    }

    public async Task<List<Configuration>> GetConfigurationsByApplicationAsync(Guid applicationId)
    {
        return await _repository.GetByApplicationIdAsync(applicationId);
    }

    public async Task<Configuration?> FindConfigurationByNameAsync(
        Guid applicationId,
        string configurationName)
    {
        return await _repository.GetByNameAsync(configurationName, applicationId);
    }

    public async Task<List<Configuration>> SearchConfigurationsAsync(
        string searchTerm,
        Guid? applicationId = null)
    {
        return await _repository.SearchAsync(searchTerm, applicationId);
    }

    public async Task<int> GetConfigurationCountAsync(Guid applicationId)
    {
        return await _repository.GetCountByApplicationAsync(applicationId);
    }
}

// Register in DI container (Program.cs)
builder.Services.AddScoped<ConfigurationRepository>();
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