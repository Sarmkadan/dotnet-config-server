#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Extension methods for registering services and dependencies
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers all database and repository services
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to configure</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The configured <see cref="IServiceCollection"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null</exception>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection not found in configuration");

            options.UseSqlServer(connectionString);
        });

        // Register repositories
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IConfigurationKeyRepository, ConfigurationKeyRepository>();
        services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<IConfigurationDiffRepository, ConfigurationDiffRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IEncryptionKeyRepository, EncryptionKeyRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IConfigurationSnapshotRepository, ConfigurationSnapshotRepository>();
        services.AddScoped<IChangeRequestRepository, ChangeRequestRepository>();
        services.AddScoped<IValidationRuleRepository, ValidationRuleRepository>();
        services.AddScoped<IConfigStore, ConfigStore>();

        return services;
    }

    /// <summary>
    /// Registers all business logic services with dependency injection
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to configure</param>
    /// <returns>The configured <see cref="IServiceCollection"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddSingleton<IConfigDiffer, KeyValueConfigDiffer>();
        services.AddScoped<IDiffService, DiffService>();
        services.AddScoped<IDiffViewerService, DiffViewerService>();
        services.AddScoped<IRollbackService, RollbackService>();
        services.AddScoped<IValidationRuleService, ValidationRuleService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IChangeRequestService, ChangeRequestService>();

        return services;
    }

    /// <summary>
    /// Configures HTTP client for webhook delivery with default timeout and headers
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to configure</param>
    /// <returns>The configured <see cref="IServiceCollection"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddWebhookClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<IWebhookService, WebhookService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DotnetConfigServer/1.0");
            });

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI documentation with standardized metadata
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to configure</param>
    /// <returns>The configured <see cref="IServiceCollection"/></returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null</exception>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "Dotnet Config Server API";
                document.Info.Version = "v1";
                document.Info.Description = "Centralized configuration server for .NET microservices";
                document.Info.Contact = new()
                {
                    Name = "Vladyslav Zaiets",
                    Url = new Uri("https://sarmkadan.com")
                };
                document.Info.License = new()
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Initializes the database by applying pending migrations
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null</exception>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply migrations and create database if needed
        await dbContext.Database.MigrateAsync();
    }
}
