#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Serilog;
using DotnetConfigServer.Models;
using DotnetConfigServer.Middleware;
using DotnetConfigServer.Caching;
using DotnetConfigServer.Events;
using DotnetConfigServer.Services;
using DotnetConfigServer.Integration;
using DotnetConfigServer.Infrastructure;
using DotnetConfigServer.BackgroundWorkers;

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

    builder.Services.AddScoped<IComparisonService, ComparisonService>();
    builder.Services.AddScoped<IConfigurationSnapshotService, ConfigurationSnapshotService>();
    builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
    builder.Services.AddScoped<IConfigurationImportService, ConfigurationImportService>();
    builder.Services.AddScoped<IBatchOperationService, BatchOperationService>();
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

    builder.Services.AddControllers();
    builder.Services.AddOptions<DotnetConfigServerOptions>()
        .Bind(builder.Configuration.GetSection("DotnetConfigServer"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

builder.Services.AddOptions<ConfigurationSnapshotOptions>()
    .Bind(builder.Configuration.GetSection("DotnetConfigServer:Snapshot"))
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

    builder.Services.AddHealthChecks();

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

