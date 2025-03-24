using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// AdvancedUsage.cs: Example showing configuration management, 
// error handling, and sensitive data handling.

public class AdvancedUsage
{
    public static async Task RunAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };
        string configId = "your-config-id-here";

        // 1. Add an encrypted configuration key (sensitive data)
        var keyRequest = new
        {
            key = "Database:ConnectionString",
            value = "Server=prod-db.example.com;Database=Orders;...",
            isEncrypted = true, // The server handles the encryption
            description = "Production database connection"
        };

        var response = await client.PostAsJsonAsync($"/api/v1/configurations/{configId}/keys", keyRequest);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Encrypted key added successfully.");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Failed to add key: {error}");
        }
    }
}
