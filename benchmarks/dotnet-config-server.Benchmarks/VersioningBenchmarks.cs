using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class VersioningBenchmarks
{
    private IVersioningService _versioningService;
    private IConfigurationService _configurationService;
    private Guid _testConfigurationId;
    private List<Guid> _createdVersions;
    private ServiceProvider _serviceProvider;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IConfigurationKeyRepository, ConfigurationKeyRepository>();
        services.AddScoped<IEncryptionKeyRepository, EncryptionKeyRepository>();
        services.AddScoped<IChangeRequestRepository, ChangeRequestRepository>();

        _serviceProvider = services.BuildServiceProvider();

        // Create test data
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        // Create test configuration
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Environment = DotnetConfigServer.Common.Environment.Development,
            Description = "Test configuration for versioning benchmarks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test keys
        var keys = new List<ConfigurationKey>();
        for (int i = 0; i < 50; i++)
        {
            keys.Add(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = _testConfigurationId,
                Key = $"VersionTest.Key.{i}",
                Value = $"Value_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        dbContext.ConfigurationKeys.AddRange(keys);
        await dbContext.SaveChangesAsync();

        // Get services
        _versioningService = scope.ServiceProvider.GetRequiredService<IVersioningService>();
        _configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        // Create initial versions
        _createdVersions = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            var version = await _versioningService.CreateVersionAsync(_testConfigurationId, $"Initial version {i}", "benchmark-user");
            _createdVersions.Add(version.Id);
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    [Benchmark]
    public async Task CreateVersion()
    {
        await _versioningService.CreateVersionAsync(_testConfigurationId, "Benchmark version", "benchmark-user");
    }

    [Benchmark]
    public async Task GetVersion()
    {
        await _versioningService.GetVersionAsync(_createdVersions[0]);
    }

    [Benchmark]
    public async Task GetVersions()
    {
        await _versioningService.GetVersionsAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task GetActiveVersion()
    {
        await _versioningService.GetActiveVersionAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task PublishVersion()
    {
        var version = await _versioningService.CreateVersionAsync(_testConfigurationId, "Version to publish", "benchmark-user");
        await _versioningService.PublishVersionAsync(version.Id, "benchmark-user");
    }

    [Benchmark]
    public async Task ArchiveVersion()
    {
        await _versioningService.ArchiveVersionAsync(_createdVersions[0], "benchmark-user");
    }

    [Benchmark]
    public async Task DeprecateVersion()
    {
        await _versioningService.DeprecateVersionAsync(_createdVersions[1], "benchmark-user");
    }

    [Benchmark]
    public async Task Rollback()
    {
        await _versioningService.RollbackAsync(_testConfigurationId, _createdVersions[0], "benchmark-user");
    }

    [Benchmark]
    public async Task GetVersionHistory()
    {
        await _versioningService.GetVersionHistoryAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task CleanupOldVersions()
    {
        await _versioningService.CleanupOldVersionsAsync(_testConfigurationId, 3);
    }

    [Benchmark]
    public async Task CreateVersionWithManyKeys()
    {
        // Create a configuration with many keys
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Environment = DotnetConfigServer.Common.Environment.Development,
            Description = "Configuration with many keys for versioning test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Configurations.Add(config);

        // Add 200 keys
        var keys = new List<ConfigurationKey>();
        for (int i = 0; i < 200; i++)
        {
            keys.Add(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = config.Id,
                Key = $"LargeConfig.Key.{i}",
                Value = $"Value_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        dbContext.ConfigurationKeys.AddRange(keys);
        await dbContext.SaveChangesAsync();

        var versioningService = scope.ServiceProvider.GetRequiredService<IVersioningService>();
        await versioningService.CreateVersionAsync(config.Id, "Large configuration version", "benchmark-user");
    }
}
