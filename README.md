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

## StringExtensions

The `StringExtensions` static class provides a comprehensive set of utility methods for string manipulation, validation, and transformation. It includes methods for checking null/empty strings, truncating text, sanitizing filenames, converting between different string cases, validating email addresses, and working with URLs. These extensions are particularly useful for processing configuration keys, environment names, application slugs, and other string-based data in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using System;

// Example configuration data processing
var configName = "Database:ConnectionString";
var environment = "Development";
var fileName = "My Application Configuration";

// Check if strings are null or whitespace
if (configName.IsNullOrWhiteSpace() || environment.IsNullOrWhiteSpace())
{
    Console.WriteLine("Configuration name or environment is empty!");
}

// Truncate long configuration names for display
var displayName = configName.Truncate(20); // "Database:Connecti..."

// Create safe filenames from configuration names
var safeFileName = fileName.ToSafeFileName(); // Removes invalid characters

// Convert between different string cases for API endpoints and URLs
var kebabCase = "DatabaseConnectionString".ToKebabCase(); // "database-connection-string"
var pascalCase = "database-connection-string".ToPascalCase(); // "DatabaseConnectionString"
var snakeCase = "DatabaseConnectionString".ToSnakeCase(); // "database_connection_string"

// Validate email addresses
var adminEmail = "admin@example.com";
if (adminEmail.IsValidEmail())
{
    Console.WriteLine("Email is valid!");
}

// Remove whitespace from configuration keys
var compactKey = "Database : Connection : String".RemoveWhitespace(); // "Database:Connection:String"

// Repeat strings for patterns or separators
var separator = "=".Repeat(30); // "=============================="

// URL encoding for API endpoints
var encodedKey = configName.UrlEncode(); // "Database%3AConnectionString"
var decodedKey = encodedKey.UrlDecode(); // "Database:ConnectionString"

// Find common prefix between configuration keys
var prefix1 = "Database:ConnectionString";
var prefix2 = "Database:Timeout";
var common = prefix1.CommonPrefix(prefix2); // "Database:"

// Check if a string matches a pattern
if (environment.MatchesPattern("^[A-Za-z]+"))
{
    Console.WriteLine("Environment name is valid!");
}
```

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

## ValidationExtensions

The `ValidationExtensions` static class provides a comprehensive set of validation extension methods for common validation scenarios. It includes fluent API methods for validating strings, collections, GUIDs, and custom conditions with meaningful error messages. These extensions are particularly useful for validating configuration data, user inputs, and API parameters in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using DotnetConfigServer.Exceptions;
using System;
using System.Collections.Generic;

// Validate configuration settings before creating a new application
var appName = "OrderProcessingService";
var description = "";
var maxConnections = 150;
var minConnections = 50;
var email = "admin@example.com";
var url = "https://api.example.com/webhook";
var configId = Guid.NewGuid();
var apiKeys = new List<string>();

// Validate application name is not empty
var nameValidation = appName.ValidateNotEmpty("Application Name");
nameValidation.ThrowIfInvalid();

// Validate description is not empty
var descValidation = description.ValidateNotEmpty("Description");
if (!descValidation.IsValid)
{
    Console.WriteLine($"Validation error: {descValidation.ErrorMessage}");
}

// Validate minimum and maximum length
var lengthValidation = appName.ValidateMinLength(5, "Application Name").Combine(
    new[] { appName.ValidateMaxLength(50, "Application Name") }
);
lengthValidation.ThrowIfInvalid();

// Validate range for connection pool size
var poolSizeValidation = maxConnections.ValidateRange(minConnections, 200, "Connection Pool Size");
poolSizeValidation.ThrowIfInvalid();

// Validate email format
var emailValidation = email.ValidateEmail();
emailValidation.ThrowIfInvalid();

// Validate URL format
var urlValidation = url.ValidateUrl();
urlValidation.ThrowIfInvalid();

// Validate GUID is not empty
var guidValidation = configId.ValidateNotEmpty("Configuration ID");
guidValidation.ThrowIfInvalid();

// Validate collection is not empty
var keysValidation = apiKeys.ValidateNotEmpty("API Keys");
if (!keysValidation.IsValid)
{
    Console.WriteLine($"Error: {keysValidation.ErrorMessage}");
}

// Validate custom condition
var conditionValidation = ValidationExtensions.ValidateCondition(
    maxConnections > minConnections,
    "Maximum connections must be greater than minimum connections",
    "Connection Settings"
);
conditionValidation.ThrowIfInvalid();

// Combine multiple validations
var allValidations = new[]
{
    appName.ValidateNotEmpty("Application Name"),
    appName.ValidateMinLength(3, "Application Name"),
    appName.ValidateMaxLength(50, "Application Name"),
    email.ValidateEmail(),
    url.ValidateUrl(),
    configId.ValidateNotEmpty("Configuration ID"),
    apiKeys.ValidateNotEmpty("API Keys")
};

var combinedValidation = allValidations.Combine();
if (!combinedValidation.IsValid)
{
    Console.WriteLine("Validation failed:");
    Console.WriteLine(combinedValidation.ErrorMessage);
}
```

## DateTimeExtensions

The `DateTimeExtensions` static class provides a comprehensive set of extension methods for `DateTime` manipulation and formatting. It includes utilities for converting dates to human-readable relative time strings, ISO 8601 formatting, and calculating start/end boundaries for days, weeks, months, and years. These extensions are particularly useful for logging, audit trails, scheduling, and date-based filtering in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using System;

// Create sample dates for testing
var now = DateTime.UtcNow;
var yesterday = now.AddDays(-1);
var lastWeek = now.AddDays(-7);
var lastMonth = now.AddMonths(-1);
var birthDate = new DateTime(1990, 5, 15);

// Convert to human-readable relative time
Console.WriteLine($"Now: {now.ToRelativeTime()}");      // "just now"
Console.WriteLine($"Yesterday: {yesterday.ToRelativeTime()}"); // "1 day ago"
Console.WriteLine($"Last week: {lastWeek.ToRelativeTime()}");   // "1 week ago"
Console.WriteLine($"Last month: {lastMonth.ToRelativeTime()}"); // "1 month ago"

// Convert to ISO 8601 format
var isoDate = now.ToIso8601();
Console.WriteLine($"ISO 8601: {isoDate}");

// Get start/end of day
var startOfDay = now.StartOfDay();
var endOfDay = now.EndOfDay();
Console.WriteLine($"Start of day: {startOfDay:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"End of day: {endOfDay:yyyy-MM-dd HH:mm:ss}");

// Get start/end of week (default Monday)
var startOfWeek = now.StartOfWeek();
var endOfWeek = now.EndOfWeek();
Console.WriteLine($"Start of week: {startOfWeek:yyyy-MM-dd}");
Console.WriteLine($"End of week: {endOfWeek:yyyy-MM-dd}");

// Get start/end of week starting on Sunday
var startOfWeekSunday = now.StartOfWeek(DayOfWeek.Sunday);
var endOfWeekSunday = now.EndOfWeek(DayOfWeek.Sunday);
Console.WriteLine($"Start of week (Sunday): {startOfWeekSunday:yyyy-MM-dd}");
Console.WriteLine($"End of week (Sunday): {endOfWeekSunday:yyyy-MM-dd}");

// Get start/end of month
var startOfMonth = now.StartOfMonth();
var endOfMonth = now.EndOfMonth();
Console.WriteLine($"Start of month: {startOfMonth:yyyy-MM}");
Console.WriteLine($"End of month: {endOfMonth:yyyy-MM-dd}");

// Get start/end of year
var startOfYear = now.StartOfYear();
var endOfYear = now.EndOfYear();
Console.WriteLine($"Start of year: {startOfYear:yyyy}");
Console.WriteLine($"End of year: {endOfYear:yyyy-MM-dd}");

// Check if date is between two dates
var testDate = new DateTime(2024, 6, 15);
var rangeStart = new DateTime(2024, 1, 1);
var rangeEnd = new DateTime(2024, 12, 31);
Console.WriteLine($"Is {testDate:yyyy-MM-dd} between {rangeStart:yyyy-MM-dd} and {rangeEnd:yyyy-MM-dd}? {testDate.IsBetween(rangeStart, rangeEnd)}");

// Get number of business days between two dates
var businessDays = new DateTime(2024, 6, 1).GetBusinessDaysBetween(new DateTime(2024, 6, 15));
Console.WriteLine($"Business days between June 1-15, 2024: {businessDays}");

// Check if year is leap year
Console.WriteLine($"Is 2024 a leap year? {new DateTime(2024, 1, 1).IsLeapYear()}"); // True
Console.WriteLine($"Is 2023 a leap year? {new DateTime(2023, 1, 1).IsLeapYear()}"); // False

// Calculate age from birth date
var age = birthDate.GetAge();
Console.WriteLine($"Age from birth date {birthDate:yyyy-MM-dd}: {age} years");
```

## CliArgumentParser

The `CliArgumentParser` class provides utilities for parsing and validating command-line arguments in the Dotnet Config Server. It supports retrieving argument values by key, checking for flag presence, parsing integers and booleans, and validating required arguments. The parser automatically normalizes argument keys (case-insensitive, strips leading dashes) and provides a static method to generate help text for available options.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using Microsoft.Extensions.Logging;
using System;

// Example command-line arguments
var args = new[] { "--port", "8080", "--environment", "Production", "--log-level", "Warning", "--enable-swagger" };

// Create logger (typically injected via DI in real applications)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<CliArgumentParser>();

// Parse arguments
var parser = new CliArgumentParser(args, logger);

// Get argument values
var port = parser.GetIntValue("port"); // 8080
var environment = parser.GetValue("environment"); // "Production"
var database = parser.GetValue("database"); // null
var logLevel = parser.GetValue("log-level"); // "Warning"
var enableSwagger = parser.GetBoolValue("enable-swagger"); // true
var initDb = parser.HasFlag("init-db"); // false

// Get values with defaults
var timeout = parser.GetIntValue("timeout", 30); // 30 (default)
var enableFeature = parser.GetBoolValue("enable-feature", true); // true (default)

// Check if required arguments are present
var isValid = parser.ValidateRequired("port", "environment"); // true if both are present

// Get help text
var helpText = CliArgumentParser.GetHelpText();
Console.WriteLine(helpText);

// Create configuration object from parsed arguments
var config = CliConfig.FromParser(parser);
Console.WriteLine($"Server will run on port: {config.Port}");
Console.WriteLine($"Environment: {config.Environment}");
Console.WriteLine($"Enable Swagger: {config.EnableSwagger}");
```

## JsonExtensions

The `JsonExtensions` static class provides extension methods for JSON serialization, deserialization, and manipulation. It includes utilities for converting objects to/from JSON strings, validating JSON, merging JSON objects, extracting values by path, and cleaning JSON data by removing null values. These extensions are particularly useful for configuration management, API communication, and data transformation in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using System;
using System.Text.Json.Nodes;

// Create a sample configuration object
var config = new
{
    Database = new
    {
        ConnectionString = "Server=localhost;Database=ConfigServer",
        Timeout = 30,
        PoolSize = 100
    },
    Cache = new
    {
        Enabled = true,
        Size = 500
    },
    Logging = new
    {
        Level = "Information",
        FilePath = "/var/log/config-server.log"
    }
};

// Serialize an object to JSON string
var jsonString = config.ToJson();
Console.WriteLine("Serialized JSON:");
Console.WriteLine(jsonString);

// Serialize with pretty printing
var prettyJson = config.ToJson(pretty: true);
Console.WriteLine("\nPretty JSON:");
Console.WriteLine(prettyJson);

// Deserialize JSON back to object
var deserializedConfig = jsonString.FromJson<object>();
Console.WriteLine($"\nDeserialized successfully: {deserializedConfig != null}");

// Safe deserialization that returns null on failure
var safeConfig = invalidJson.TryFromJson<object>();
Console.WriteLine($"Safe deserialization result: {safeConfig == null}");

// Create a JsonObject for manipulation
var jsonObject = new JsonObject
{
    ["name"] = "OrderProcessingService",
    ["environment"] = "Development",
    ["enabled"] = true,
    ["timeout"] = 30
};

// Convert dictionary to JsonObject
var settingsDict = new Dictionary<string, object?>
{
    ["Database:ConnectionString"] = "Server=localhost;Database=Orders",
    ["Database:Timeout"] = 30,
    ["Cache:Enabled"] = true
};
var dictAsJson = settingsDict.ToJsonObject();
Console.WriteLine($"\nDictionary as JsonObject: {dictAsJson}");

// Get value by path (dot notation)
var connectionString = dictAsJson.GetValueByPath("Database:ConnectionString");
Console.WriteLine($"Connection string: {connectionString?.GetValue<string>()}");

// Merge two JSON objects
var baseConfig = new JsonObject
{
    ["name"] = "ConfigServer",
    ["version"] = "1.0"
};

var overrideConfig = new JsonObject
{
    ["version"] = "2.0",
    ["environment"] = "Production"
};

baseConfig.Merge(overrideConfig);
Console.WriteLine($"\nMerged config: {baseConfig}");

// Check if JSON is valid
var isValid = jsonString.IsValidJson();
Console.WriteLine($"\nIs JSON valid: {isValid}");

// Pretty print and minify JSON
var minified = jsonString.Minify();
Console.WriteLine($"\nMinified length: {minified.Length}");

var pretty = jsonString.PrettyPrint();
Console.WriteLine($"Pretty print length: {pretty.Length}");

// Convert JSON to dictionary
var jsonDict = jsonString.ToJsonDictionary();
Console.WriteLine($"\nJSON as dictionary: {jsonDict?.Count} entries");

// Extract values by paths
var extracted = dictAsJson.Extract("Database:ConnectionString", "Cache:Enabled");
Console.WriteLine($"\nExtracted values: {extracted.Count} paths");

// Remove null values from JSON object
var jsonWithNulls = new JsonObject
{
    ["name"] = "TestService",
    ["timeout"] = null,
    ["config"] = new JsonObject
    {
        ["enabled"] = true,
        ["disabled"] = null
    }
};

jsonWithNulls.RemoveNulls();
Console.WriteLine($"\nJSON after removing nulls: {jsonWithNulls}");
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

## DictionaryExtensions

The `DictionaryExtensions` static class provides extension methods for safe dictionary operations including safe value retrieval, conditional addition, merging, filtering, and nested value manipulation. These utilities help prevent common dictionary-related exceptions and simplify working with configuration data structures in the configuration server.

### Usage Example

```csharp
using DotnetConfigServer.Utilities;
using System;
using System.Collections.Generic;

// Create sample configuration data as nested dictionaries
var appConfig = new Dictionary<string, object?>
{
    ["Name"] = "OrderProcessingService",
    ["Environment"] = "Development",
    ["Settings"] = new Dictionary<string, object?>
    {
        ["Database"] = new Dictionary<string, object?>
        {
            ["ConnectionString"] = "Server=localhost;Database=Orders",
            ["Timeout"] = 30,
            ["PoolSize"] = 100
        },
        ["Cache"] = new Dictionary<string, object?>
        {
            ["Enabled"] = true,
            ["Size"] = 500
        },
        ["Logging"] = new Dictionary<string, object?>
        {
            ["Level"] = "Information",
            ["FilePath"] = "/var/log/order-service.log"
        }
    }
};

// Safe value retrieval with default
var timeout = appConfig.GetValueOrDefault("Settings:Database:Timeout", 15);
Console.WriteLine($"Timeout: {timeout}"); // 30

var missingValue = appConfig.GetValueOrDefault("Settings:NonExistent", "default");
Console.WriteLine($"Missing value: {missingValue}"); // "default"

// Conditional addition
var added = appConfig.AddIfNotExists("Settings:NewFeature", true);
Console.WriteLine($"Added new feature: {added}"); // True

// Try to add again - should return false
var alreadyExists = appConfig.AddIfNotExists("Settings:NewFeature", false);
Console.WriteLine($"Already exists: {alreadyExists}"); // False

// Add or update
appConfig.AddOrUpdate("Settings:Database:Timeout", 60);
Console.WriteLine($"Updated timeout: {appConfig["Settings:Database:Timeout"]}"); // 60

// Merge dictionaries
var additionalSettings = new Dictionary<string, object?>
{
    ["Settings:Database:MaxConnections"] = 50,
    ["Settings:RateLimiting:RequestsPerSecond"] = 100
};

appConfig.Merge(additionalSettings);
Console.WriteLine($"Has MaxConnections: {appConfig.ContainsKey("Settings:Database:MaxConnections")}"); // True

// Filter dictionary
var databaseSettings = appConfig.Where(kvp => 
    kvp.Key.StartsWith("Settings:Database:") && 
    kvp.Value is int intValue && intValue > 0
);
Console.WriteLine($"Database settings count: {databaseSettings.Count}"); // 3

// Transform values
var stringValues = appConfig.Select(kvp => kvp.Value?.ToString() ?? "null");
Console.WriteLine($"String values count: {stringValues.Count}"); // Count of all values

// Flatten nested dictionary
var flattened = appConfig.Flatten();
Console.WriteLine($"Flattened keys: {flattened.Count}");
foreach (var kvp in flattened)
{
    Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
}

// Get nested value
var connectionString = appConfig.GetNestedValue("Settings:Database:ConnectionString");
Console.WriteLine($"Connection string: {connectionString}"); // "Server=localhost;Database=Orders"

// Set nested value
appConfig.SetNestedValue("Settings:Database:ReadOnly", true);
var readOnly = appConfig.GetNestedValue("Settings:Database:ReadOnly");
Console.WriteLine($"ReadOnly: {readOnly}"); // True

// Remove items matching predicate
var countBefore = appConfig.Count;
appConfig.RemoveWhere(kvp => kvp.Key.Contains("Cache"));
var countAfter = appConfig.Count;
Console.WriteLine($"Removed {countBefore - countAfter} items containing 'Cache'");

// Invert dictionary (swap keys and values)
var inverted = appConfig.Invert();
Console.WriteLine($"Inverted dictionary has {inverted.Count} entries");
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
## ConfigurationsController

The `ConfigurationsController` provides RESTful API endpoints for managing configurations in the Dotnet Config Server. It handles CRUD operations for configuration entities, key management, and search functionality, enabling microservices to retrieve, update, and manage their configuration data programmatically. The controller supports both configuration-level operations and granular key management with proper versioning and audit trail integration.

**Key Features:**
- Create, read, update, and delete configurations
- Retrieve all configurations for a specific application
- Manage configuration keys (add, retrieve)
- Search configurations by query and application
- Search configuration keys by text and/or key prefix
- RESTful API design with proper HTTP status codes
- Comprehensive error handling and logging
- Integration with versioning and webhook services

### Usage Example

```csharp
using DotnetConfigServer.Controllers;
using DotnetConfigServer.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Example: Using ConfigurationsController in a service
public class ConfigurationManagementService
{
    private readonly ConfigurationsController _controller;
    private readonly ILogger<ConfigurationManagementService> _logger;

    public ConfigurationManagementService(
        ConfigurationsController controller,
        ILogger<ConfigurationManagementService> logger)
    {
        _controller = controller;
        _logger = logger;
    }

    public async Task<Configuration> CreateConfigurationAsync(
        Guid applicationId,
        string name,
        string environment,
        string description)
    {
        var configuration = new Configuration
        {
            ApplicationId = applicationId,
            Name = name,
            Environment = environment,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        var result = await _controller.Create(configuration);
        
        if (result is CreatedAtActionResult createdResult && createdResult.Value is Configuration createdConfig)
        {
            _logger.LogInformation("Created configuration: {ConfigId}", createdConfig.Id);
            return createdConfig;
        }

        throw new Exception("Failed to create configuration");
    }

    public async Task<Configuration> GetConfigurationByIdAsync(Guid configurationId)
    {
        var result = await _controller.GetById(configurationId);
        
        if (result is OkObjectResult okResult && okResult.Value is Configuration config)
        {
            return config;
        }

        throw new Exception("Configuration not found");
    }

    public async Task<List<Configuration>> GetConfigurationsByApplicationAsync(Guid applicationId)
    {
        var result = await _controller.GetByApplication(applicationId);
        
        if (result is OkObjectResult okResult && okResult.Value is List<Configuration> configs)
        {
            return configs;
        }

        return new List<Configuration>();
    }

    public async Task<Configuration> UpdateConfigurationAsync(
        Guid configurationId,
        string newName,
        string newDescription)
    {
        var existingConfig = await GetConfigurationByIdAsync(configurationId);
        
        existingConfig.Name = newName;
        existingConfig.Description = newDescription;
        existingConfig.UpdatedAt = DateTime.UtcNow;
        existingConfig.UpdatedBy = "system";

        var result = await _controller.Update(configurationId, existingConfig);
        
        if (result is OkObjectResult okResult && okResult.Value is Configuration updatedConfig)
        {
            _logger.LogInformation("Updated configuration: {ConfigId}", configurationId);
            return updatedConfig;
        }

        throw new Exception("Failed to update configuration");
    }

    public async Task DeleteConfigurationAsync(Guid configurationId)
    {
        var result = await _controller.Delete(configurationId);
        
        if (result is NoContentResult)
        {
            _logger.LogInformation("Deleted configuration: {ConfigId}", configurationId);
        }
        else
        {
            throw new Exception("Failed to delete configuration");
        }
    }

    public async Task<List<ConfigurationKey>> GetConfigurationKeysAsync(Guid configurationId)
    {
        var result = await _controller.GetKeys(configurationId);
        
        if (result is OkObjectResult okResult && okResult.Value is List<ConfigurationKey> keys)
        {
            return keys;
        }

        return new List<ConfigurationKey>();
    }

    public async Task<ConfigurationKey> AddConfigurationKeyAsync(
        Guid configurationId,
        string keyName,
        string keyValue)
    {
        var key = new ConfigurationKey
        {
            KeyName = keyName,
            Value = keyValue,
            Description = $"Added via API at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        var result = await _controller.AddKey(configurationId, key);
        
        if (result is CreatedAtActionResult createdResult && createdResult.Value is ConfigurationKey createdKey)
        {
            _logger.LogInformation("Added key {KeyName} to configuration {ConfigId}", keyName, configurationId);
            return createdKey;
        }

        throw new Exception("Failed to add configuration key");
    }

    public async Task<List<Configuration>> SearchConfigurationsAsync(
        string searchQuery,
        Guid? applicationId = null)
    {
        var result = await _controller.Search(searchQuery, applicationId);
        
        if (result is OkObjectResult okResult && okResult.Value is List<Configuration> configs)
        {
            return configs;
        }

        return new List<Configuration>();
    }

    public async Task<List<ConfigurationKey>> SearchConfigurationKeysAsync(
        string searchQuery,
        string? keyPrefix = null,
        Guid? configurationId = null)
    {
        var result = await _controller.SearchKeys(searchQuery, keyPrefix, configurationId);
        
        if (result is OkObjectResult okResult && okResult.Value is List<ConfigurationKey> keys)
        {
            return keys;
        }

        return new List<ConfigurationKey>();
    }
}

// Example: Calling endpoints via HTTP client
public class ConfigurationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://localhost:5001/api/v1/configurations";

    public ConfigurationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Configuration> CreateConfigurationAsync(Configuration configuration)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}", configuration);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Configuration>();
    }

    public async Task<Configuration> GetConfigurationAsync(Guid configurationId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{configurationId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Configuration>();
    }

    public async Task<List<Configuration>> GetConfigurationsByApplicationAsync(Guid applicationId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/application/{applicationId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Configuration>>();
    }

    public async Task<Configuration> UpdateConfigurationAsync(Guid configurationId, Configuration configuration)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{configurationId}", configuration);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Configuration>();
    }

    public async Task DeleteConfigurationAsync(Guid configurationId)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/{configurationId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ConfigurationKey>> GetConfigurationKeysAsync(Guid configurationId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{configurationId}/keys");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ConfigurationKey>>();
    }

    public async Task<ConfigurationKey> AddConfigurationKeyAsync(Guid configurationId, ConfigurationKey key)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/{configurationId}/keys", key);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConfigurationKey>();
    }

    public async Task<List<Configuration>> SearchConfigurationsAsync(string query, Guid? applicationId = null)
    {
        var url = $"{_baseUrl}/search?q={Uri.EscapeDataString(query)}";
        if (applicationId.HasValue)
        {
            url += $"&applicationId={applicationId.Value}";
        }
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Configuration>>();
    }

    public async Task<List<ConfigurationKey>> SearchConfigurationKeysAsync(
        string query,
        string? prefix = null,
        Guid? configurationId = null)
    {
        var url = $"{_baseUrl}/keys/search?q={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrEmpty(prefix))
        {
            url += $"&prefix={Uri.EscapeDataString(prefix)}";
        }
        if (configurationId.HasValue)
        {
            url += $"&configurationId={configurationId.Value}";
        }
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ConfigurationKey>>();
    }
}
```

## AuditLogsController

The `AuditLogsController` provides API endpoints for retrieving and analyzing audit logs within the Dotnet Config Server. It serves as a compliance and debugging interface, allowing users to query audit trails by entity ID, user ID, date ranges, or action types. The controller returns paginated results for efficient data retrieval and includes a summary endpoint that provides aggregated statistics about recent changes.

**Key Features:**
- Retrieve audit logs filtered by entity ID, user ID, date range, or action type
- Paginated results for efficient data retrieval
- Summary endpoint with aggregated statistics (total changes, create/update/delete counts, unique users)
- RESTful API design with proper HTTP status codes
- Comprehensive error handling and logging

### Usage Example

```csharp
using DotnetConfigServer.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// Example: Using AuditLogsController in a service
public class AuditService
{
    private readonly AuditLogsController _controller;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger, IAuditLogRepository repository)
    {
        _logger = logger;
        _controller = new AuditLogsController(repository, logger);
    }

    public async Task LogAuditSummaryAsync()
    {
        // Get summary of changes in the last 7 days
        var summaryResult = await _controller.GetSummary(days: 7);
        
        if (summaryResult is OkObjectResult okResult)
        {
            var summary = okResult.Value as AuditLogsController.AuditSummary;
            
            _logger.LogInformation("Audit Summary - Total: {Total}, Creates: {Creates}, Updates: {Updates}, Deletes: {Deletes}, Users: {Users}",
                summary?.TotalChanges, summary?.CreateCount, summary?.UpdateCount, summary?.DeleteCount, summary?.UniqueUsers);
            
            if (summary?.LastChange.HasValue == true)
            {
                _logger.LogInformation("Last change occurred at: {LastChange}", summary.LastChange.Value);
            }
        }
    }

    public async Task QueryAuditLogsAsync()
    {
        // Get audit logs for a specific entity
        var entityLogsResult = await _controller.GetByEntity("config-12345", page: 1, pageSize: 50);
        
        // Get audit logs for a specific user
        var userLogsResult = await _controller.GetByUser("user@example.com", page: 1, pageSize: 50);
        
        // Get all audit logs with date filtering
        var allLogsResult = await _controller.GetAll(
            from: DateTime.UtcNow.AddDays(-30),
            to: DateTime.UtcNow,
            action: "Created",
            page: 1,
            pageSize: 100
        );
        
        // Get a specific audit log by ID
        var logId = Guid.NewGuid();
        var singleLogResult = await _controller.GetById(logId);
    }
}

// Example: Calling endpoints via HTTP client
public class AuditLogClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://localhost:5001/api/v1/auditlogs";

    public AuditLogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task GetEntityAuditLogsAsync(string entityId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/entity/{entityId}?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLog>>();
    }

    public async Task GetUserAuditLogsAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/user/{userId}?page=1&pageSize=50");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLog>>();
    }

    public async Task GetAllAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? action = null)
    {
        var url = $"{_baseUrl}?page=1&pageSize=50";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue) url += $"&to={to.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(action)) url += $"&action={Uri.EscapeDataString(action)}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<PaginatedResult<AuditLog>>();
    }

    public async Task<AuditSummary> GetAuditSummaryAsync(int days = 7)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/summary?days={days}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuditSummary>();
    }

    public async Task<AuditLog> GetAuditLogByIdAsync(Guid logId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{logId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuditLog>();
    }
}
```
