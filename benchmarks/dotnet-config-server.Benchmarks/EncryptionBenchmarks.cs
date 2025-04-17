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
public class EncryptionBenchmarks
{
    private IEncryptionService _encryptionService;
    private Guid _testConfigurationId;
    private EncryptionKey _testKey;
    private string _plainText;
    private string _largePlainText;
    private ServiceProvider _serviceProvider;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IEncryptionKeyRepository, EncryptionKeyRepository>();

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
            Description = "Test configuration for encryption benchmarks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test encryption key
        _testKey = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyId = Guid.NewGuid().ToString(),
            Name = "BenchmarkKey",
            Algorithm = DotnetConfigServer.Common.EncryptionAlgorithm.AES256,
            EncryptedKey = System.Text.Encoding.UTF8.GetBytes("ThisIsATestKey12345678901234567890123"), // 32 bytes for AES256
            Salt = System.Text.Encoding.UTF8.GetBytes("BenchmarkSaltValue"),
            IsPrimary = true,
            IsActive = true,
            CreatedBy = "benchmark-user",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };
        dbContext.EncryptionKeys.Add(_testKey);
        await dbContext.SaveChangesAsync();

        // Generate test plain text
        _plainText = "This is a sensitive configuration value that needs to be encrypted for security purposes. It contains API keys, connection strings, and other confidential information.";

        // Generate large plain text (1KB)
        _largePlainText = new string('A', 1024);

        // Get service
        _encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
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
    public string EncryptSync()
    {
        return _encryptionService.Encrypt(_plainText, _testKey);
    }

    [Benchmark]
    public string DecryptSync()
    {
        var cipherText = _encryptionService.Encrypt(_plainText, _testKey);
        return _encryptionService.Decrypt(cipherText, _testKey);
    }

    [Benchmark]
    public async Task<string> EncryptAsync()
    {
        return await _encryptionService.EncryptAsync(_plainText, _testConfigurationId);
    }

    [Benchmark]
    public async Task<string> DecryptAsync()
    {
        var cipherText = await _encryptionService.EncryptAsync(_plainText, _testConfigurationId);
        return await _encryptionService.DecryptAsync(cipherText, _testConfigurationId);
    }

    [Benchmark]
    public bool ValidateKey()
    {
        return _encryptionService.ValidateKey(_testKey);
    }

    [Benchmark]
    public EncryptionKey GenerateNewKey()
    {
        return _encryptionService.GenerateNewKey("BenchmarkKey", "AES256");
    }

    [Benchmark]
    public async Task RotateKey()
    {
        await _encryptionService.RotateKeyAsync(_testKey.KeyId, "benchmark-user");
    }

    [Benchmark]
    public string EncryptLargeText()
    {
        return _encryptionService.Encrypt(_largePlainText, _testKey);
    }

    [Benchmark]
    public string DecryptLargeText()
    {
        var cipherText = _encryptionService.Encrypt(_largePlainText, _testKey);
        return _encryptionService.Decrypt(cipherText, _testKey);
    }

    [Benchmark]
    public async Task<string> EncryptLargeTextAsync()
    {
        return await _encryptionService.EncryptAsync(_largePlainText, _testConfigurationId);
    }

    [Benchmark]
    public async Task<string> DecryptLargeTextAsync()
    {
        var cipherText = await _encryptionService.EncryptAsync(_largePlainText, _testConfigurationId);
        return await _encryptionService.DecryptAsync(cipherText, _testConfigurationId);
    }
}
