using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class WebhookBenchmarks
{
    private IWebhookService _webhookService;
    private IConfigurationService _configurationService;
    private Guid _testConfigurationId;
    private Guid _testWebhookId;
    private ServiceProvider _serviceProvider;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ConfigDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IVersionRepository, VersionRepository>();
        services.AddScoped<IChangeRequestRepository, ChangeRequestRepository>();

        _serviceProvider = services.BuildServiceProvider();

        // Create test data
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        // Create test configuration
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            Environment = "Development",
            Description = "Test configuration for webhook benchmarks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test webhook
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Test Webhook",
            Url = "https://example.com/webhook",
            Description = "Test webhook for benchmarks",
            Events = new List<string> { "ConfigurationUpdated", "VersionPublished" },
            VerifySignature = true,
            IsActive = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 3,
                DelaySeconds = 10
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Webhooks.Add(webhook);
        await dbContext.SaveChangesAsync();
        _testWebhookId = webhook.Id;

        // Get services
        _webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        _configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
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
    public async Task CreateWebhook()
    {
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Benchmark Webhook",
            Url = "https://benchmark.example.com/webhook",
            Description = "Benchmark webhook",
            Events = new List<string> { "ConfigurationUpdated" },
            VerifySignature = true,
            IsActive = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 5,
                DelaySeconds = 30
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.CreateWebhookAsync(webhook, "benchmark-user");
    }

    [Benchmark]
    public async Task GetWebhook()
    {
        await _webhookService.GetWebhookAsync(_testWebhookId);
    }

    [Benchmark]
    public async Task GetWebhooksByConfiguration()
    {
        await _webhookService.GetWebhooksByConfigurationAsync(_testConfigurationId);
    }

    [Benchmark]
    public async Task UpdateWebhook()
    {
        var webhook = new Webhook
        {
            Id = _testWebhookId,
            ConfigurationId = _testConfigurationId,
            Name = "Updated Benchmark Webhook",
            Url = "https://updated-benchmark.example.com/webhook",
            Description = "Updated benchmark webhook",
            Events = new List<string> { "ConfigurationUpdated", "VersionPublished", "KeyAdded" },
            VerifySignature = true,
            IsActive = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 10,
                DelaySeconds = 60
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.UpdateWebhookAsync(_testWebhookId, webhook, "benchmark-user");
    }

    [Benchmark]
    public async Task DeleteWebhook()
    {
        await _webhookService.DeleteWebhookAsync(_testWebhookId, "benchmark-user");
    }

    [Benchmark]
    public async Task DispatchWebhook()
    {
        await _webhookService.DispatchWebhookAsync(_testWebhookId, "ConfigurationUpdated", new Dictionary<string, object>(), "benchmark-user");
    }

    [Benchmark]
    public async Task GetFailedDeliveries()
    {
        await _webhookService.GetFailedDeliveriesAsync(_testWebhookId, 10);
    }

    [Benchmark]
    public async Task ProcessWebhookRetryQueue()
    {
        await _webhookService.ProcessRetryQueueAsync();
    }

    [Benchmark]
    public async Task CreateWebhookWithManyEvents()
    {
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Comprehensive Webhook",
            Url = "https://comprehensive.example.com/webhook",
            Description = "Webhook with many events",
            Events = new List<string> { "ConfigurationUpdated", "VersionPublished", "KeyAdded", "KeyUpdated", "KeyDeleted", "RollbackExecuted" },
            VerifySignature = true,
            IsActive = true,
            RetryPolicy = new RetryPolicy
            {
                MaxRetries = 5,
                DelaySeconds = 30
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.CreateWebhookAsync(webhook, "benchmark-user");
    }
}
