using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// BasicUsage.cs: A minimal example showing how to connect to the 
// Dotnet Config Server and fetch a configuration.

public class BasicUsage
{
    public static async Task RunAsync()
    {
        // 1. Setup HttpClient with the Config Server base URL
        using var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

        // 2. Fetch a configuration by its ID
        string configId = "your-config-id-here";
        
        try
        {
            var response = await client.GetFromJsonAsync<dynamic>($"/api/v1/configurations/{configId}");
            Console.WriteLine($"Fetched configuration: {response}");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error fetching configuration: {e.Message}");
        }
    }
}
