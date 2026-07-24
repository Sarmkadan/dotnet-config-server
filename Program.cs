#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Serilog;
using DotnetConfigServer.Models;
using DotnetConfigServer.Middleware;
using DotnetConfigServer.Middleware.RateLimiting;
using DotnetConfigServer.Caching;
using DotnetConfigServer.Events;
using DotnetConfigServer.Services;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Integration;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.BackgroundWorkers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/application-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Dotnet Config Server");

    // Add Phase 2 services
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    builder.Services.AddSingleton<IEventBus, EventBus>();
    builder.Services.AddSingleton<INotificationService, NotificationService>();
    builder.Services.AddSingleton<PerformanceMetrics>();

    // Rate-limit counter store: shared across instances when UseDistributedStore is enabled,
    // otherwise process-local. AddDistributedMemoryCache provides the default IDistributedCache
    // backend; point it at Redis/SQL Server in hosting configuration for real multi-instance use.
    var rateLimitOptions = builder.Configuration.GetSection("DotnetConfigServer:RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();
    if (rateLimitOptions.UseDistributedStore)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton<IRateLimitStore, DistributedRateLimitStore>();
    }
    else
    {
        builder.Services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
    }

    builder.Services.AddScoped<IComparisonService, ComparisonService>();
    builder.Services.AddScoped<IConfigurationSnapshotService, ConfigurationSnapshotService>();
    builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
    builder.Services.AddScoped<IConfigurationImportService, ConfigurationImportService>();
    builder.Services.AddScoped<IBatchOperationService>(provider =>
{
    var keyRepository = provider.GetRequiredService<IConfigurationKeyRepository>();
    var validationRuleService = provider.GetRequiredService<IValidationRuleService>();
    var logger = provider.GetRequiredService<ILogger<BatchOperationService>>();
    return new BatchOperationService(keyRepository, validationRuleService, logger);
});
    builder.Services.AddScoped<IApiResponseTransformer, ApiResponseTransformer>();
    builder.Services.AddSingleton(new ExternalApiClientOptions());
    builder.Services.AddScoped<ExternalApiClient>();
    // Add HTTP clients
    builder.Services.AddHttpClient<ExternalApiClient>()
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

    builder.Services.AddDataServices(builder.Configuration);
    builder.Services.AddBusinessServices();
    builder.Services.AddWebhookClient();
    builder.Services.AddSwaggerConfiguration();
    builder.Services.AddScoped<ConfigurationEventHandlers>();
    builder.Services.AddHostedService<ConfigurationSyncWorker>();
    builder.Services.AddHostedService<WebhookRetryWorker>();
builder.Services.AddHostedService<ConfigurationSnapshotWorker>();
    builder.Services.AddHostedService<EncryptionKeyRotationWorker>();

    builder.Services.AddControllers();

    // Response compression for large streamed payloads (e.g. configuration exports).
    // Combined with Utf8JsonWriter-based streaming in ConfigurationExporter, this lets
    // large exports be gzip-compressed on the fly instead of buffering the whole
    // response before compressing it.
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
            .Concat(["application/json", "text/csv", "application/xml", "application/x-yaml"]);
    });
    builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });
    builder.Services.AddOptions<DotnetConfigServerOptions>()
        .Bind(builder.Configuration.GetSection("DotnetConfigServer"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

builder.Services.AddOptions<ConfigurationSnapshotOptions>()
    .Bind(builder.Configuration.GetSection("DotnetConfigServer:Snapshot"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EncryptionKeyRotationOptions>()
    .Bind(builder.Configuration.GetSection("DotnetConfigServer:EncryptionKeyRotation"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddHealthChecks()
        .AddCheck<ProcessLivenessHealthCheck>("process", tags: ["live"])
        .AddCheck<ConfigStoreReadinessHealthCheck>("config-store", tags: ["ready"])
        .AddCheck<EncryptionKeyReadinessHealthCheck>("encryption-keys", tags: ["ready"]);

    var app = builder.Build();

    // Wire domain event handlers to the event bus
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

    eventBus.Subscribe<ConfigurationCreatedEvent>(async e =>
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ConfigurationEventHandlers>()
            .HandleConfigurationCreatedAsync(e);
    });
    eventBus.Subscribe<ConfigurationUpdatedEvent>(async e =>
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ConfigurationEventHandlers>()
            .HandleConfigurationUpdatedAsync(e);
    });
    eventBus.Subscribe<ConfigurationKeyChangedEvent>(async e =>
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ConfigurationEventHandlers>()
            .HandleConfigurationKeyChangedAsync(e);
    });
    eventBus.Subscribe<ConfigurationDeletedEvent>(async e =>
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ConfigurationEventHandlers>()
            .HandleConfigurationDeletedAsync(e);
    });

    // Configure middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<PerformanceMonitoringMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/v1.json", "Dotnet Config Server API v1");
        });
    }

    app.UseResponseCompression();
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    Log.Information("Dotnet Config Server started successfully with Phase 2 features");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

