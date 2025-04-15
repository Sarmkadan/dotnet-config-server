using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ConfigurationBenchmarks
{
    private IConfigurationService _configurationService;
    private IEncryptionService _encryptionService;
    private IVersioningService _versioningService;
private IMemoryCache _memoryCache;
    private Guid _testApplicationId;
    private Guid _testConfigurationId;
    private List<ConfigurationKey> _testKeys;
    private ServiceProvider _serviceProvider;
    private Random _random = new Random(42);

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ConfigDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IConfigurationKeyRepository, ConfigurationKeyRepository>();
        services.AddScoped<IEncryptionKeyRepository, EncryptionKeyRepository>();
        services.AddScoped<IVersionRepository, VersionRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();

        _serviceProvider = services.BuildServiceProvider();

        // Create test data
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
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
            Environment = "Development",
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

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    [Benchmark]
    public async Task CreateConfiguration()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = "BenchmarkTest",
            Description = "Benchmark test configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.CreateAsync(config, "benchmark-user");
    }

    [Benchmark]
    public async Task GetConfigurationById()
    {
        await _configurationService.GetByIdAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task GetConfigurationsByApplication()
    {
        await _configurationService.GetByApplicationAsync(_testApplicationId);
    }

    [Benchmark]
    public async Task UpdateConfiguration()
    {
        var config = new Configuration
        {
            Id = _testConfigurationId,
            ApplicationId = _testApplicationId,
            Environment = "Development",
            Description = "Updated benchmark configuration",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _configurationService.UpdateAsync(_testConfigurationId, config, "benchmark-user");
    }

    [Benchmark]
    public async Task SearchConfigurations()
    {
        await _configurationService.SearchAsync("Benchmark", _testApplicationId);
    }

    [Benchmark]
    public async Task GetKeys()
    {
        await _configurationService.GetKeysAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task SearchKeys()
    {
        await _configurationService.SearchKeysAsync("Benchmark.Key", null, _testConfigurationId);
    }

    [Benchmark]
    public async Task GetConfigurationCount()
    {
        await _configurationService.GetConfigurationCountAsync(_testApplicationId);
    }

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

    [Benchmark]
    public async Task UpdateKey()
    {
        var key = _testKeys.First();
        await _configurationService.UpdateKeyAsync(key.Id, "UpdatedValue", "benchmark-user");
    }

    [Benchmark]
    public async Task DeleteKey()
    {
        var key = _testKeys.First();
        await _configurationService.DeleteKeyAsync(key.Id, "benchmark-user");
    }

    [Benchmark]
    public async Task CreateConfigurationWithEncryption()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = _testApplicationId,
            Environment = "BenchmarkEncrypted",
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

    [Benchmark]
    public async Task GetConfigurationWithEncryption()
    {
        await _configurationService.GetByIdAsync(_testConfigurationId);
    }
}
