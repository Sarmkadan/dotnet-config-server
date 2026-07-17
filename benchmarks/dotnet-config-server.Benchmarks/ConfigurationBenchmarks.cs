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
/// Benchmark tests for configuration-related operations to measure performance of the configuration service.
/// Tests various CRUD operations, searching, and encryption scenarios to identify performance bottlenecks.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ConfigurationBenchmarks
{
    /// <summary>
    /// Configuration service for managing application configurations and keys.
    /// </summary>
    private IConfigurationService _configurationService;

    /// <summary>
    /// Encryption service for handling encrypted configuration values.
    /// </summary>
    private IEncryptionService _encryptionService;

    /// <summary>
    /// Versioning service for managing configuration versions and history.
    /// </summary>
    private IVersioningService _versioningService;

    /// <summary>
    /// Memory cache for caching frequently accessed configuration data.
    /// </summary>
    private IMemoryCache _memoryCache;

    /// <summary>
    /// Application identifier used for benchmark testing.
    /// </summary>
    private Guid _testApplicationId;

    /// <summary>
    /// Configuration identifier used for benchmark testing.
    /// </summary>
    private Guid _testConfigurationId;

    /// <summary>
    /// Collection of test configuration keys created during setup.
    /// </summary>
    private List<ConfigurationKey> _testKeys;

    /// <summary>
    /// Service provider for dependency injection in benchmark tests.
    /// </summary>
    private ServiceProvider _serviceProvider;

    /// <summary>
    /// Random number generator with fixed seed for reproducible benchmark results.
    /// </summary>
    private Random _random = new Random(42);

    /// <summary>
    /// Sets up the benchmark environment by configuring dependency injection, creating test data,
    /// and initializing services used throughout the benchmark tests.
    /// </summary>
    /// <returns>A Task representing the asynchronous setup operation.</returns>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
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
            Description = "Test application for benchmarks"
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
            Description = "Benchmark configuration",
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
                Key = $"Benchmark.Key.{i}",
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
        _encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        _versioningService = scope.ServiceProvider.GetRequiredService<IVersioningService>();
    }

    /// <summary>
    /// Cleans up the benchmark environment by disposing of the service provider and deleting the test database.
    /// </summary>
    /// <returns>A Task representing the asynchronous cleanup operation.</returns>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Benchmark test for creating a new configuration.
    /// Measures the time to create a new configuration in the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task CreateConfiguration()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = DotnetConfigServer.Common.Environment.Staging,
            Description = "Benchmark test configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.CreateAsync(config, "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for retrieving a configuration by its unique identifier.
    /// Measures the time to fetch a single configuration from the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationById()
    {
        await _configurationService.GetByIdAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark test for retrieving all configurations for a specific application.
    /// Measures the time to fetch multiple configurations from the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationsByApplication()
    {
        await _configurationService.GetByApplicationAsync(_testApplicationId);
    }

    /// <summary>
    /// Benchmark test for updating an existing configuration.
    /// Measures the time to update a configuration in the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task UpdateConfiguration()
    {
        var config = new Configuration
        {
            Id = _testConfigurationId,
            ApplicationId = _testApplicationId,
            Environment = DotnetConfigServer.Common.Environment.Development,
            Description = "Updated benchmark configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.UpdateAsync(_testConfigurationId, config, "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for searching configurations by keyword.
    /// Measures the time to search for configurations containing a specific term.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task SearchConfigurations()
    {
        await _configurationService.SearchAsync("Benchmark", _testApplicationId);
    }

    /// <summary>
    /// Benchmark test for retrieving all configuration keys for a specific configuration.
    /// Measures the time to fetch configuration keys from the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetKeys()
    {
        await _configurationService.GetKeysAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark test for searching configuration keys by keyword.
    /// Measures the time to search for configuration keys containing a specific term.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task SearchKeys()
    {
        await _configurationService.SearchKeysAsync("Benchmark.Key", null, _testConfigurationId);
    }

    /// <summary>
    /// Benchmark test for counting configurations for a specific application.
    /// Measures the time to count configurations in the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationCount()
    {
        await _configurationService.GetConfigurationCountAsync(_testApplicationId);
    }

    /// <summary>
    /// Benchmark test for adding a new configuration key.
    /// Measures the time to create a new configuration key in the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task AddKey()
    {
        var key = new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Key = $"New.Benchmark.Key.{_random.Next(1000)}",
            Value = "NewValue",
            IsEncrypted = false,
            IsSensitive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.AddKeyAsync(_testConfigurationId, key, "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for updating an existing configuration key.
    /// Measures the time to update a configuration key in the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task UpdateKey()
    {
        var key = _testKeys.First();
        await _configurationService.UpdateKeyAsync(key.Id, "UpdatedValue", "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for deleting a configuration key.
    /// Measures the time to remove a configuration key from the database.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task DeleteKey()
    {
        var key = _testKeys.First();
        await _configurationService.DeleteKeyAsync(key.Id, "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for creating a configuration with encrypted keys.
    /// Measures the time to create a new configuration and add encrypted configuration keys.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task CreateConfigurationWithEncryption()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = DotnetConfigServer.Common.Environment.Production,
            Description = "Benchmark encrypted configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.CreateAsync(config, "benchmark-user");

        var key = new ConfigurationKey
        {
            Id = Guid.NewGuid(),
            ConfigurationId = config.Id,
            Key = "Sensitive.Database.Connection",
            Value = "Server=prod-db;Database=ConfigServer;User=admin;Password=secret123;",
            IsEncrypted = true,
            IsSensitive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.AddKeyAsync(config.Id, key, "benchmark-user");
    }

    /// <summary>
    /// Benchmark test for retrieving a configuration with encrypted keys.
    /// Measures the time to fetch a configuration that contains encrypted keys.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task GetConfigurationWithEncryption()
    {
        await _configurationService.GetByIdAsync(_testConfigurationId);
    }
}