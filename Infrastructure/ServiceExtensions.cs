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
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
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

        return services;
    }

    /// <summary>
    /// Registers all business logic services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IVersioningService, VersioningService>();
        services.AddScoped<IDiffService, DiffService>();
        services.AddScoped<IWebhookService, WebhookService>();

        return services;
    }

    /// <summary>
    /// Configures HTTP client for webhook delivery
    /// </summary>
    public static IServiceCollection AddWebhookClient(this IServiceCollection services)
    {
        services.AddHttpClient<IWebhookService, WebhookService>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DotnetConfigServer/1.0");
            });

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Dotnet Config Server API",
                Version = "v1",
                Description = "Centralized configuration server for .NET microservices",
                Contact = new()
                {
                    Name = "Vladyslav Zaiets",
                    Url = new Uri("https://sarmkadan.com")
                },
                License = new()
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Initializes the database
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply migrations and create database if needed
        await dbContext.Database.MigrateAsync();
    }
}
