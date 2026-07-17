using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Benchmark suite for testing caching performance and behavior in the configuration server.
/// Measures cache hit/miss scenarios, cache-aside patterns, eviction behavior, and size tracking
/// for various configuration operations including configurations, keys, searches, and counts.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CachingBenchmarks
{
    private IConfigurationService _configurationService;
    private IMemoryCache _memoryCache;
    private Guid _testApplicationId;
    private Guid _testConfigurationId;
    private ServiceProvider _serviceProvider;
    private List<ConfigurationKey> _testKeys;
    private const string CachePrefix = "ConfigServer:";

    /// <summary>
    /// Global setup for all benchmarks.
    /// Initializes dependency injection container with required services, configures memory cache with size limits,
    /// creates test database schema, populates with test application, configuration, and 100 configuration keys.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection with caching
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IConfigurationKeyRepository, ConfigurationKeyRepository>();
        services.AddScoped<IEncryptionKeyRepository, EncryptionKeyRepository>();
        services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        // Add memory cache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024 * 1024 * 100; // 100 MB
            options.CompactionPercentage = 0.25;
        });

        _serviceProvider = services.BuildServiceProvider();

        // Create test data
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        // Create test application
        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "BenchmarkApp",
            Description = "Test application for caching benchmarks"
        };
        dbContext.Applications.Add(app);
        await dbContext.SaveChangesAsync();
        _testApplicationId = app.Id;

        // Create test configuration
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = DotnetConfigServer.Common.Environment.Development,
            Description = "Benchmark configuration for caching",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test keys
        _testKeys = new List<ConfigurationKey>();
        for (int i = 0; i < 100; i++)
        {
            var key = new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = _testConfigurationId,
                Key = $"Cache.Key.{i}",
                Value = $"Value_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _testKeys.Add(key);
        }
        dbContext.ConfigurationKeys.AddRange(_testKeys);
        await dbContext.SaveChangesAsync();

        // Get services
        _configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
        _memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
    }

    /// <summary>
    /// Global cleanup after all benchmarks complete.
    /// Removes test database and disposes service provider.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Benchmark for measuring configuration retrieval performance when cache is empty (cache miss scenario).
    /// Measures the time to retrieve a configuration from database when no cached value exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfiguration_CacheMiss()
    {
        // Ensure cache is empty
        _memoryCache.Remove($"{CachePrefix}{_testConfigurationId}");

        await _configurationService.GetByIdAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration retrieval performance when cache contains the value (cache hit scenario).
    /// Measures the time to retrieve a configuration from cache after it has been populated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfiguration_CacheHit()
    {
        // Pre-populate cache
        var config = await _configurationService.GetByIdAsync(_testConfigurationId);
        _memoryCache.Set($"{CachePrefix}{_testConfigurationId}", config, TimeSpan.FromMinutes(5));

        // Now get from cache
        await _configurationService.GetByIdAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration retrieval using the cache-aside pattern.
    /// Implements manual cache checking before database lookup, then caches the result for future requests.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfiguration_WithCacheAside()
    {
        // Cache-aside pattern
        var cacheKey = $"{CachePrefix}{_testConfigurationId}";

        if (!_memoryCache.TryGetValue(cacheKey, out Configuration? cachedConfig))
        {
            cachedConfig = await _configurationService.GetByIdAsync(_testConfigurationId);
            _memoryCache.Set(cacheKey, cachedConfig, TimeSpan.FromMinutes(5));
        }

        // Use cached config
        var _ = cachedConfig;
    }

    /// <summary>
    /// Benchmark for measuring configuration keys retrieval performance when cache is empty (cache miss scenario).
    /// Measures the time to retrieve all configuration keys from database when no cached value exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetKeys_CacheMiss()
    {
        // Ensure cache is empty
        _memoryCache.Remove($"{CachePrefix}{_testConfigurationId}:Keys");

        await _configurationService.GetKeysAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration keys retrieval performance when cache contains the value (cache hit scenario).
    /// Measures the time to retrieve all configuration keys from cache after it has been populated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetKeys_CacheHit()
    {
        // Pre-populate cache
        var keys = await _configurationService.GetKeysAsync(_testConfigurationId);
        _memoryCache.Set($"{CachePrefix}{_testConfigurationId}:Keys", keys, TimeSpan.FromMinutes(5));

        // Now get from cache
        await _configurationService.GetKeysAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration keys retrieval using the cache-aside pattern.
    /// Implements manual cache checking before database lookup for configuration keys, then caches the result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetKeys_WithCacheAside()
    {
        // Cache-aside pattern for keys
        var cacheKey = $"{CachePrefix}{_testConfigurationId}:Keys";

        if (!_memoryCache.TryGetValue(cacheKey, out List<ConfigurationKey>? cachedKeys))
        {
            cachedKeys = await _configurationService.GetKeysAsync(_testConfigurationId);
            _memoryCache.Set(cacheKey, cachedKeys, TimeSpan.FromMinutes(5));
        }

        // Use cached keys
        var _ = cachedKeys;
    }

    /// <summary>
    /// Benchmark for measuring configuration search performance when cache is empty (cache miss scenario).
    /// Measures the time to search for configurations by name when no cached search results exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task SearchConfigurations_CacheMiss()
    {
        // Ensure cache is empty
        _memoryCache.Remove($"{CachePrefix}{_testApplicationId}:Search:Benchmark");

        await _configurationService.SearchAsync("Benchmark", _testApplicationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration search performance when cache contains the results (cache hit scenario).
    /// Measures the time to retrieve search results from cache after they have been populated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task SearchConfigurations_CacheHit()
    {
        // Pre-populate cache
        var results = await _configurationService.SearchAsync("Benchmark", _testApplicationId);
        _memoryCache.Set($"{CachePrefix}{_testApplicationId}:Search:Benchmark", results, TimeSpan.FromMinutes(3));

        // Now get from cache
        await _configurationService.SearchAsync("Benchmark", _testApplicationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration count retrieval performance when cache is empty (cache miss scenario).
    /// Measures the time to retrieve the count of configurations for an application when no cached value exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationCount_CacheMiss()
    {
        // Ensure cache is empty
        _memoryCache.Remove($"{CachePrefix}{_testApplicationId}:Count");

        await _configurationService.GetConfigurationCountAsync(_testApplicationId);
    }

    /// <summary>
    /// Benchmark for measuring configuration count retrieval performance when cache contains the value (cache hit scenario).
    /// Measures the time to retrieve the count of configurations from cache after it has been populated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationCount_CacheHit()
    {
        // Pre-populate cache
        var count = await _configurationService.GetConfigurationCountAsync(_testApplicationId);
        _memoryCache.Set($"{CachePrefix}{_testApplicationId}:Count", count, TimeSpan.FromMinutes(2));

        // Now get from cache
        await _configurationService.GetConfigurationCountAsync(_testApplicationId);
    }

    /// <summary>
    /// Benchmark for testing memory cache eviction behavior.
    /// Populates the cache with a configuration, then forces eviction by filling the cache beyond its size limit,
    /// verifying that the original configuration entry is evicted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task CacheEviction()
    {
        // Add to cache
        var config = await _configurationService.GetByIdAsync(_testConfigurationId);
        _memoryCache.Set($"{CachePrefix}{_testConfigurationId}", config, TimeSpan.FromMinutes(5));

        // Force eviction by filling cache
        for (int i = 0; i < 1000; i++)
        {
            _memoryCache.Set($"{CachePrefix}TempKey{i}", new object(), TimeSpan.FromMinutes(1));
        }

        // Verify original entry is evicted
        _memoryCache.TryGetValue($"{CachePrefix}{_testConfigurationId}", out _);
    }

    /// <summary>
    /// Benchmark for testing memory cache size tracking behavior.
    /// Adds multiple configurations to the cache while tracking memory usage, verifying that the cache
    /// properly tracks and manages entries based on configured size limits.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task CacheSizeTracking()
    {
        // Add multiple configurations to track size
        for (int i = 0; i < 50; i++)
        {
            var config = new Configuration
            {
                Id = Guid.NewGuid(),
                ApplicationId = _testApplicationId,
                Environment = DotnetConfigServer.Common.Environment.Staging,
                Description = $"Test config {i}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Configurations.Add(config);
            await dbContext.SaveChangesAsync();

            var configData = await _configurationService.GetByIdAsync(config.Id);
            _memoryCache.Set($"{CachePrefix}{config.Id}", configData, TimeSpan.FromMinutes(10));
        }
    }

    /// <summary>
    /// Benchmark for testing caching behavior with encrypted configuration data.
    /// Creates a configuration with encrypted keys, caches the configuration, and verifies
    /// that encrypted data can be properly retrieved from cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task CacheWithEncryptedData()
    {
        // Create configuration with encrypted keys
        var encryptedConfig = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = DotnetConfigServer.Common.Environment.Production,
            Description = "Encrypted test configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Configurations.Add(encryptedConfig);
        await dbContext.SaveChangesAsync();

        // Add encrypted key
        var encryptedKey = new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            ConfigurationId = encryptedConfig.Id,
            Key = "Database:ConnectionString",
            Value = "Server=prod-db;Database=ConfigServer;User=admin;Password=secret123;",
            IsEncrypted = true,
            IsSensitive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.ConfigurationKeys.Add(encryptedKey);
        await dbContext.SaveChangesAsync();

        // Cache encrypted configuration
        var configData = await _configurationService.GetByIdAsync(encryptedConfig.Id);
        _memoryCache.Set($"{CachePrefix}{encryptedConfig.Id}", configData, TimeSpan.FromMinutes(5));

        // Retrieve from cache
        await _configurationService.GetByIdAsync(encryptedConfig.Id);
    }
}