// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Serilog;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.Middleware;
using DotnetConfigServer.Caching;
using DotnetConfigServer.Events;
using DotnetConfigServer.BackgroundWorkers;
using DotnetConfigServer.Services;
using DotnetConfigServer.Integration;

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

    // Add core services
    builder.Services.AddDataServices(builder.Configuration);
    builder.Services.AddBusinessServices();
    builder.Services.AddWebhookClient();
    builder.Services.AddSwaggerConfiguration();

    // Add Phase 2 services
    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
    builder.Services.AddSingleton<IEventBus, EventBus>();
    builder.Services.AddSingleton<PerformanceMetrics>();

    builder.Services.AddScoped<IComparisonService, ComparisonService>();
    builder.Services.AddScoped<IConfigurationSnapshotService, ConfigurationSnapshotService>();
    builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
    builder.Services.AddScoped<IConfigurationImportService, ConfigurationImportService>();
    builder.Services.AddScoped<IBatchOperationService, BatchOperationService>();
    builder.Services.AddScoped<IApiResponseTransformer, ApiResponseTransformer>();
    builder.Services.AddScoped<ExternalApiClient>();
    builder.Services.AddScoped<ConfigurationEventHandlers>();

    // Register event handlers
    builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();

    // Add HTTP clients
    builder.Services.AddHttpClient<ExternalApiClient>()
        .ConfigureHttpClient(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

    // Add background workers
    builder.Services.AddHostedService<ConfigurationSyncWorker>();
    builder.Services.AddHostedService<WebhookRetryWorker>();

    builder.Services.AddControllers();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Initialize database
    await app.Services.InitializeDatabaseAsync();

    // Register event handlers
    var eventBus = app.Services.GetRequiredService<IEventBus>();
    var handlers = app.Services.GetRequiredService<ConfigurationEventHandlers>();

    eventBus.Subscribe<ConfigurationCreatedEvent>(handlers.HandleConfigurationCreatedAsync);
    eventBus.Subscribe<ConfigurationUpdatedEvent>(handlers.HandleConfigurationUpdatedAsync);
    eventBus.Subscribe<ConfigurationKeyChangedEvent>(handlers.HandleConfigurationKeyChangedAsync);
    eventBus.Subscribe<ConfigurationDeletedEvent>(handlers.HandleConfigurationDeletedAsync);

    // Configure middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<PerformanceMonitoringMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>(new RateLimitOptions { RequestsPerMinute = 100 });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/openapi/v1.json", "Dotnet Config Server API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

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

/// <summary>
/// Default notification service implementation.
/// </summary>
public class NotificationServiceImpl : INotificationService
{
    private readonly ILogger<NotificationServiceImpl> _logger;

    public NotificationServiceImpl(ILogger<NotificationServiceImpl> logger)
    {
        _logger = logger;
    }

    public async Task NotifyAsync(Notification notification)
    {
        _logger.LogInformation("Notification: {Type} - {Message}", notification.Type, notification.Message);
        await Task.CompletedTask;
    }

    public async Task NotifyAsync(string type, object payload)
    {
        _logger.LogInformation("Event notification: {Type} - Payload type: {PayloadType}", type, payload.GetType().Name);
        await Task.CompletedTask;
    }
}
