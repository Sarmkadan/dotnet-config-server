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

/// <summary>
/// Benchmark class that measures the performance of diff and diff‑viewer services
/// across a variety of realistic scenarios, such as comparing small and large
/// configuration versions, retrieving enriched diffs, and generating rollback
/// previews. The benchmarks use an in‑memory SQL Server database that is created
/// and torn down for each run.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DiffBenchmarks
{
    private IDiffService _diffService;
    private IDiffViewerService _diffViewerService;
    private Guid _testConfigurationId;
    private ConfigurationVersion _version1;
    private ConfigurationVersion _version2;
    private ConfigurationVersion _version3;
    private ServiceProvider _serviceProvider;

    /// <summary>
    /// Performs one‑time global setup for the benchmarks.
    /// <para>
    /// This method configures a <see cref="ServiceCollection"/> with logging,
    /// a SQL Server <see cref="ApplicationDbContext"/>, and all required
    /// repository and service registrations. It then creates a test database,
    /// inserts a test configuration together with three configuration versions
    /// (each containing a set of <see cref="ConfigurationKey"/> records), and
    /// resolves the <see cref="IDiffService"/> and <see cref="IDiffViewerService"/>
    /// instances that will be used by the benchmark methods.
    /// </para>
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the setup is finished.</returns>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IDiffService, DiffService>();
        services.AddScoped<IDiffViewerService, DiffViewerService>();
        services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IConfigurationKeyRepository, ConfigurationKeyRepository>();
        services.AddScoped<IChangeRequestRepository, ChangeRequestRepository>();
        services.AddScoped<IRollbackService, RollbackService>();

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
            Description = "Test configuration for diff benchmarks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test keys for version 1 (50 keys)
        var keysV1 = new List<ConfigurationKey>();
        for (int i = 0; i < 50; i++)
        {
            keysV1.Add(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = _testConfigurationId,
                Key = $"Service.Key.{i}",
                Value = i % 2 == 0 ? $"Value_{i}" : $"OldValue_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        dbContext.ConfigurationKeys.AddRange(keysV1);
        await dbContext.SaveChangesAsync();

        // Create version 1
        _version1 = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            VersionNumber = "1.0.0",
            ReleaseNotes = "Initial version",
            Status = DotnetConfigServer.Common.ConfigurationVersionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "benchmark-user"
        };
        dbContext.ConfigurationVersions.Add(_version1);
        await dbContext.SaveChangesAsync();

        // Update some keys for version 2 (50 keys, 20% modified, 10 new)
        var keysV2 = new List<ConfigurationKey>();
        foreach (var key in keysV1.Take(50))
        {
            keysV2.Add(new ConfigurationKey
            {
                Id = key.Id,
                ConfigurationId = key.ConfigurationId,
                Key = key.Key,
                Value = $"UpdatedValue_{key.Value.Split('_')[1]}",
                IsEncrypted = key.IsEncrypted,
                IsSensitive = key.IsSensitive,
                CreatedAt = key.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add some new keys
        for (int i = 50; i < 60; i++)
        {
            keysV2.Add(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = _testConfigurationId,
                Key = $"NewService.Key.{i}",
                Value = $"NewValue_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        dbContext.ConfigurationKeys.UpdateRange(keysV2);
        await dbContext.SaveChangesAsync();

        // Create version 2
        _version2 = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            VersionNumber = "2.0.0",
            ReleaseNotes = "Updated version with new keys",
            Status = DotnetConfigServer.Common.ConfigurationVersionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "benchmark-user"
        };
        dbContext.ConfigurationVersions.Add(_version2);
        await dbContext.SaveChangesAsync();

        // Create version 3 with more changes (100 keys, 30% modified, 20 new, 5 deleted)
        var keysV3 = new List<ConfigurationKey>();

        // Update existing keys
        foreach (var key in keysV1.Take(50))
        {
            keysV3.Add(new ConfigurationKey
            {
                Id = key.Id,
                ConfigurationId = key.ConfigurationId,
                Key = key.Key,
                Value = $"FinalValue_{key.Value.Split('_')[1]}",
                IsEncrypted = key.IsEncrypted,
                IsSensitive = key.IsSensitive,
                CreatedAt = key.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Add new keys
        for (int i = 60; i < 80; i++)
        {
            keysV3.Add(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = _testConfigurationId,
                Key = $"NewService.Key.{i}",
                Value = $"NewValue_{i}",
                IsEncrypted = false,
                IsSensitive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Delete some keys (simulate by not including them)
        dbContext.ConfigurationKeys.RemoveRange(keysV1.Skip(45).Take(5));

        dbContext.ConfigurationKeys.UpdateRange(keysV3);
        await dbContext.SaveChangesAsync();

        // Create version 3
        _version3 = new ConfigurationVersion
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            VersionNumber = "3.0.0",
            ReleaseNotes = "Final version with significant changes",
            Status = DotnetConfigServer.Common.ConfigurationVersionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "benchmark-user"
        };
        dbContext.ConfigurationVersions.Add(_version3);
        await dbContext.SaveChangesAsync();

        // Get services
        _diffService = scope.ServiceProvider.GetRequiredService<IDiffService>();
        _diffViewerService = scope.ServiceProvider.GetRequiredService<IDiffViewerService>();
    }

    /// <summary>
    /// Performs global cleanup after all benchmarks have run.
    /// <para>
    /// The method deletes the temporary benchmark database and disposes the
    /// <see cref="ServiceProvider"/> that was built during <see cref="GlobalSetup"/>.
    /// </para>
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when cleanup is finished.</returns>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Benchmarks the comparison of two configuration versions using <see cref="IDiffService.ComparVersionsAsync"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous comparison operation.</returns>
    [Benchmark]
    public async Task CompareConfigurations()
    {
        await _diffService.ComparVersionsAsync(_version1.Id, _version2.Id);
    }

    /// <summary>
    /// Benchmarks retrieving an enriched diff between two versions via <see cref="IDiffViewerService.GetEnrichedDiffAsync"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous diff retrieval.</returns>
    [Benchmark]
    public async Task GetDiff()
    {
        await _diffViewerService.GetEnrichedDiffAsync(_version1.Id, _version2.Id);
    }

    /// <summary>
    /// Benchmarks retrieving an enriched diff with details (currently the same call as <see cref="GetDiff"/>).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous diff retrieval.</returns>
    [Benchmark]
    public async Task GetDiffWithDetails()
    {
        await _diffViewerService.GetEnrichedDiffAsync(_version1.Id, _version2.Id);
    }

    /// <summary>
    /// Benchmarks generating a rollback preview for a specific configuration version using <see cref="IDiffViewerService.GetRollbackPreviewAsync"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous preview generation.</returns>
    [Benchmark]
    public async Task GetRollbackPreview()
    {
        await _diffViewerService.GetRollbackPreviewAsync(_testConfigurationId, _version1.Id);
    }

    /// <summary>
    /// Benchmarks the comparison of a small configuration version with a larger one.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous comparison operation.</returns>
    [Benchmark]
    public async Task CompareLargeConfigurations()
    {
        await _diffService.ComparVersionsAsync(_version1.Id, _version3.Id);
    }

    /// <summary>
    /// Benchmarks retrieving the version timeline for a configuration via <see cref="IDiffViewerService.GetVersionTimelineAsync"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous timeline retrieval.</returns>
    [Benchmark]
    public async Task GetDiffTimeline()
    {
        await _diffViewerService.GetVersionTimelineAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmarks retrieving an enriched diff between the second and third versions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous diff retrieval.</returns>
    [Benchmark]
    public async Task GetEnrichedDiff()
    {
        await _diffViewerService.GetEnrichedDiffAsync(_version2.Id, _version3.Id);
    }
}
