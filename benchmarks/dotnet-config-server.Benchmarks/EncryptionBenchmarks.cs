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
/// <summary>
/// Provides a set of benchmarks that evaluate the performance of the <see cref="IEncryptionService"/>
/// implementation for various encryption and decryption scenarios, including synchronous and asynchronous
/// operations, key validation, key generation, key rotation, and handling of large payloads.
/// </summary>
public class EncryptionBenchmarks
{
    private IEncryptionService _encryptionService;
    private Guid _testConfigurationId;
    private EncryptionKey _testKey;
    private string _plainText;
    private string _largePlainText;
    private ServiceProvider _serviceProvider;

    /// <summary>
    /// Performs one‑time global setup for the benchmark suite.
    /// It configures a service collection with logging, a SQL Server EF Core context,
    /// registers the encryption service and repository, creates a fresh in‑memory database,
    /// inserts a test <see cref="Configuration"/> and a test <see cref="EncryptionKey"/>,
    /// and prepares sample plain‑text strings for later benchmarks.
    /// </summary>
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

    /// <summary>
    /// Performs one‑time global cleanup after all benchmarks have run.
    /// It deletes the benchmark database and disposes the service provider.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        _serviceProvider.Dispose();
    }

    /// <summary>
    /// Benchmarks synchronous encryption of a typical configuration value.
    /// </summary>
    /// <returns>The encrypted cipher text.</returns>
    [Benchmark]
    public string EncryptSync()
    {
        return _encryptionService.Encrypt(_plainText, _testKey);
    }

    /// <summary>
    /// Benchmarks synchronous decryption of a previously encrypted value.
    /// </summary>
    /// <returns>The decrypted plain text.</returns>
    [Benchmark]
    public string DecryptSync()
    {
        var cipherText = _encryptionService.Encrypt(_plainText, _testKey);
        return _encryptionService.Decrypt(cipherText, _testKey);
    }

    /// <summary>
    /// Benchmarks asynchronous encryption of a typical configuration value.
    /// </summary>
    /// <returns>A task that resolves to the encrypted cipher text.</returns>
    [Benchmark]
    public async Task<string> EncryptAsync()
    {
        return await _encryptionService.EncryptAsync(_plainText, _testConfigurationId);
    }

    /// <summary>
    /// Benchmarks asynchronous decryption of a previously encrypted value.
    /// </summary>
    /// <returns>A task that resolves to the decrypted plain text.</returns>
    [Benchmark]
    public async Task<string> DecryptAsync()
    {
        var cipherText = await _encryptionService.EncryptAsync(_plainText, _testConfigurationId);
        return await _encryptionService.DecryptAsync(cipherText, _testConfigurationId);
    }

    /// <summary>
    /// Benchmarks the validation of an <see cref="EncryptionKey"/>.
    /// </summary>
    /// <returns>True if the key is considered valid; otherwise false.</returns>
    [Benchmark]
    public bool ValidateKey()
    {
        return _encryptionService.ValidateKey(_testKey);
    }

    /// <summary>
    /// Benchmarks generation of a new <see cref="EncryptionKey"/> using the specified name and algorithm.
    /// </summary>
    /// <returns>The newly generated <see cref="EncryptionKey"/> instance.</returns>
    [Benchmark]
    public EncryptionKey GenerateNewKey()
    {
        return _encryptionService.GenerateNewKey("BenchmarkKey", "AES256");
    }

    /// <summary>
    /// Benchmarks asynchronous rotation of an existing encryption key identified by its <c>KeyId</c>.
    /// </summary>
    /// <returns>A task that completes when the rotation operation finishes.</returns>
    [Benchmark]
    public async Task RotateKey()
    {
        await _encryptionService.RotateKeyAsync(_testKey.KeyId, "benchmark-user");
    }

    /// <summary>
    /// Benchmarks synchronous encryption of a larger payload (1 KB of text).
    /// </summary>
    /// <returns>The encrypted cipher text for the large payload.</returns>
    [Benchmark]
    public string EncryptLargeText()
    {
        return _encryptionService.Encrypt(_largePlainText, _testKey);
    }

    /// <summary>
    /// Benchmarks synchronous decryption of a previously encrypted large payload.
    /// </summary>
    /// <returns>The decrypted large plain text.</returns>
    [Benchmark]
    public string DecryptLargeText()
    {
        var cipherText = _encryptionService.Encrypt(_largePlainText, _testKey);
        return _encryptionService.Decrypt(cipherText, _testKey);
    }

    /// <summary>
    /// Benchmarks asynchronous encryption of a larger payload (1 KB of text).
    /// </summary>
    /// <returns>A task that resolves to the encrypted cipher text for the large payload.</returns>
    [Benchmark]
    public async Task<string> EncryptLargeTextAsync()
    {
        return await _encryptionService.EncryptAsync(_largePlainText, _testConfigurationId);
    }

    /// <summary>
    /// Benchmarks asynchronous decryption of a previously encrypted large payload.
    /// </summary>
    /// <returns>A task that resolves to the decrypted large plain text.</returns>
    [Benchmark]
    public async Task<string> DecryptLargeTextAsync()
    {
        var cipherText = await _encryptionService.EncryptAsync(_largePlainText, _testConfigurationId);
        return await _encryptionService.DecryptAsync(cipherText, _testConfigurationId);
    }
}
