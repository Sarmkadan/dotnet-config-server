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
/// Benchmark tests for webhook operations to measure performance and throughput.
/// </summary>
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

    /// <summary>
    /// Sets up the benchmark environment by configuring dependency injection and creating test data.
    /// Initializes a test configuration and webhook subscription that will be used across all benchmarks.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Setup dependency injection
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ConfigServerBenchmarks;Trusted_Connection=True;MultipleActiveResultSets=true"));

        services.AddHttpClient<IWebhookService, WebhookService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();
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
            Description = "Test configuration for webhook benchmarks",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Configurations.Add(config);
        await dbContext.SaveChangesAsync();
        _testConfigurationId = config.Id;

        // Create test webhook
        var webhook = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Test Webhook",
            Url = "https://example.com/webhook",
            Description = "Test webhook for benchmarks",
            TriggerEvents = new List<string> { "ConfigurationUpdated", "VersionPublished" },
            VerifySignature = true,
            IsActive = true,
            MaxRetries = 3,
            CreatedBy = "benchmark-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.WebhookSubscriptions.Add(webhook);
        await dbContext.SaveChangesAsync();
        _testWebhookId = webhook.Id;

        // Get services
        _webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        _configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
    }

    /// <summary>
    /// Cleans up the benchmark environment by removing test data and disposing of the service provider.
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
    /// Benchmark for creating a new webhook subscription.
    /// Measures the performance of creating a webhook with basic configuration.
    /// </summary>
    [Benchmark]
    public async Task CreateWebhook()
    {
        var webhook = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Benchmark Webhook",
            Url = "https://benchmark.example.com/webhook",
            Description = "Benchmark webhook",
            TriggerEvents = new List<string> { "ConfigurationUpdated" },
            VerifySignature = true,
            IsActive = true,
            MaxRetries = 5,
            CreatedBy = "benchmark-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.CreateSubscriptionAsync(webhook, "benchmark-user");
    }

    /// <summary>
    /// Benchmark for retrieving a single webhook subscription by its ID.
    /// Measures the performance of fetching an existing webhook from the database.
    /// </summary>
    [Benchmark]
    public async Task GetWebhook()
    {
        await _webhookService.GetSubscriptionAsync(_testWebhookId);
    }

    /// <summary>
    /// Benchmark for retrieving all webhook subscriptions for a specific configuration.
    /// Measures the performance of fetching multiple webhook subscriptions filtered by configuration ID.
    /// </summary>
    [Benchmark]
    public async Task GetWebhooksByConfiguration()
    {
        await _webhookService.GetSubscriptionsAsync(_testConfigurationId);
    }

    /// <summary>
    /// Benchmark for updating an existing webhook subscription.
    /// Measures the performance of updating webhook properties including trigger events and retry configuration.
    /// </summary>
    [Benchmark]
    public async Task UpdateWebhook()
    {
        var webhook = new WebhookSubscription
        {
            Id = _testWebhookId,
            ConfigurationId = _testConfigurationId,
            Name = "Updated Benchmark Webhook",
            Url = "https://updated-benchmark.example.com/webhook",
            Description = "Updated benchmark webhook",
            TriggerEvents = new List<string> { "ConfigurationUpdated", "VersionPublished", "KeyAdded" },
            VerifySignature = true,
            IsActive = true,
            MaxRetries = 10,
            CreatedBy = "benchmark-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.UpdateSubscriptionAsync(_testWebhookId, webhook, "benchmark-user");
    }

    /// <summary>
    /// Benchmark for deleting a webhook subscription.
    /// Measures the performance of removing a webhook from the database.
    /// </summary>
    [Benchmark]
    public async Task DeleteWebhook()
    {
        await _webhookService.DeleteSubscriptionAsync(_testWebhookId, "benchmark-user");
    }

    /// <summary>
    /// Benchmark for dispatching a webhook event to a registered subscription.
    /// Measures the performance of delivering a webhook payload to the configured URL.
    /// </summary>
    [Benchmark]
    public async Task DispatchWebhook()
    {
        await _webhookService.DeliverAsync(_testWebhookId, "{\"event\":\"ConfigurationUpdated\"}", Guid.NewGuid());
    }

    /// <summary>
    /// Benchmark for retrieving failed webhook delivery attempts.
    /// Measures the performance of fetching delivery records that resulted in errors.
    /// </summary>
    [Benchmark]
    public async Task GetFailedDeliveries()
    {
        await _webhookService.GetDeliveriesAsync(_testWebhookId);
    }

    /// <summary>
    /// Benchmark for processing the webhook retry queue.
    /// Measures the performance of retrying failed webhook deliveries automatically.
    /// </summary>
    [Benchmark]
    public async Task ProcessWebhookRetryQueue()
    {
        await _webhookService.RetryFailedDeliveriesAsync();
    }

    /// <summary>
    /// Benchmark for creating a webhook subscription with multiple trigger events.
    /// Measures the performance of creating a webhook with an extensive list of event types to monitor.
    /// </summary>
    [Benchmark]
    public async Task CreateWebhookWithManyEvents()
    {
        var webhook = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            ConfigurationId = _testConfigurationId,
            Name = "Comprehensive Webhook",
            Url = "https://comprehensive.example.com/webhook",
            Description = "Webhook with many events",
            TriggerEvents = new List<string> { "ConfigurationUpdated", "VersionPublished", "KeyAdded", "KeyUpdated", "KeyDeleted", "RollbackExecuted" },
            VerifySignature = true,
            IsActive = true,
            MaxRetries = 5,
            CreatedBy = "benchmark-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _webhookService.CreateSubscriptionAsync(webhook, "benchmark-user");
    }
}
