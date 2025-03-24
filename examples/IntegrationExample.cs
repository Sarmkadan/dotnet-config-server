using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

// IntegrationExample.cs: Example showing how to register the configuration client
// in an ASP.NET Core Dependency Injection container.

public static class IntegrationExample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register HttpClient for the Config Server
        services.AddHttpClient("ConfigServerClient", client =>
        {
            client.BaseAddress = new Uri("https://localhost:5001");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        
        // Register your service that consumes the Config Server
        // services.AddSingleton<IConfigurationService, MyConfigurationService>();
    }
}
