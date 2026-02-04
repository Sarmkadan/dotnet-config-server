# Getting Started with Dotnet Config Server

This guide will help you set up and start using Dotnet Config Server in minutes.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [First Application](#first-application)
- [Your First Configuration](#your-first-configuration)
- [Testing the API](#testing-the-api)
- [Next Steps](#next-steps)

## Prerequisites

Before you begin, ensure you have:

- **.NET 10.0 SDK** installed ([Download](https://dotnet.microsoft.com/download))
- **SQL Server** (any edition)
  - Option 1: Use LocalDB (included with Visual Studio)
  - Option 2: Docker: `docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password123!' -p 1433:1433 mcr.microsoft.com/mssql/server:latest`
- **A code editor** (Visual Studio, VS Code, or Rider)
- **curl or Postman** for testing API endpoints (optional)

## Installation

### Step 1: Clone the Repository

```bash
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

### Step 3: Configure Database Connection

Edit `appsettings.json` and update the connection string if needed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotnetConfigServerDb;Trusted_Connection=true;"
}
```

**Connection String Examples**:

```json
// LocalDB (Windows)
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DotnetConfigServerDb;Trusted_Connection=true;"

// SQL Server (Windows Authentication)
"DefaultConnection": "Server=YOUR_SERVER;Database=DotnetConfigServerDb;Trusted_Connection=true;"

// SQL Server (SQL Authentication)
"DefaultConnection": "Server=YOUR_SERVER;Database=DotnetConfigServerDb;User Id=sa;Password=YourPassword;"

// Docker SQL Server
"DefaultConnection": "Server=localhost,1433;Database=DotnetConfigServerDb;User Id=sa;Password=Password123!;"
```

### Step 4: Create and Seed Database

```bash
# Create database and apply migrations
dotnet ef database update

# Verify database was created
# SQL Server: Object Explorer → Databases → DotnetConfigServerDb
```

### Step 5: Start the Application

```bash
dotnet run
```

You should see output like:
```
info: DotnetConfigServer.Program[0]
      Starting Dotnet Config Server
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[15]
      Application started. Press Ctrl+C to exit.
```

### Step 6: Access the Application

- **Swagger UI**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health
- **API Base**: https://localhost:5001/api/v1

## First Application

Now let's create your first application and configuration.

### Using Swagger UI (Recommended for Beginners)

1. Open https://localhost:5001/swagger
2. Find the "Applications" section
3. Click "POST /api/v1/applications"
4. Click "Try it out"
5. Enter this JSON:
   ```json
   {
     "name": "OrderService",
     "description": "Order processing microservice"
   }
   ```
6. Click "Execute"
7. Copy the `id` from the response - you'll need it next

### Using curl

```bash
curl -X POST https://localhost:5001/api/v1/applications \
  -H "Content-Type: application/json" \
  -d '{
    "name": "OrderService",
    "description": "Order processing microservice"
  }' \
  -k  # -k ignores SSL certificate errors in development
```

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "OrderService",
  "description": "Order processing microservice",
  "createdAt": "2026-05-04T10:30:00Z"
}
```

Keep the `id` for the next steps.

## Your First Configuration

### Create Configuration

Using the Application ID from above, create a configuration:

```bash
curl -X POST https://localhost:5001/api/v1/configurations \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "550e8400-e29b-41d4-a716-446655440000",
    "environment": "Production",
    "description": "Production configuration for Order Service"
  }' \
  -k
```

**Response**:
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "applicationId": "550e8400-e29b-41d4-a716-446655440000",
  "environment": "Production",
  "createdAt": "2026-05-04T10:30:00Z",
  "keyCount": 0
}
```

Keep this `id` as well.

### Add Configuration Keys

Now add some configuration values:

```bash
# Add database connection string
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/keys \
  -H "Content-Type: application/json" \
  -d '{
    "key": "Database:ConnectionString",
    "value": "Server=prod-db.example.com;Database=Orders;User Id=sa;Password=YourPassword;",
    "isEncrypted": true,
    "description": "Production database connection string"
  }' \
  -k

# Add a feature flag
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/keys \
  -H "Content-Type: application/json" \
  -d '{
    "key": "Features:EnableNewCheckout",
    "value": "true",
    "isEncrypted": false,
    "description": "Enable the new checkout flow"
  }' \
  -k

# Add logging level
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/keys \
  -H "Content-Type: application/json" \
  -d '{
    "key": "Logging:LogLevel",
    "value": "Information",
    "isEncrypted": false,
    "description": "Application logging level"
  }' \
  -k
```

### Verify Configuration

```bash
curl -X GET https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001 \
  -H "Accept: application/json" \
  -k
```

You should see all three keys in the response.

## Testing the API

### 1. Create a Version

Versions allow you to track configuration history and rollback if needed:

```bash
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/versions \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Initial production release",
    "changeNotes": "Set up database and feature flags"
  }' \
  -k
```

Keep the returned `id`.

### 2. Publish the Version

Make the version active:

```bash
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/versions/VERSION_ID/publish \
  -H "Content-Type: application/json" \
  -d '{"notes": "Released to production"}' \
  -k
```

Replace `VERSION_ID` with the ID from the previous response.

### 3. Set Up a Webhook

To receive notifications when configurations change:

```bash
# First, set up a simple webhook receiver (you can use webhook.site for testing)
curl -X POST https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/webhooks \
  -H "Content-Type: application/json" \
  -d '{
    "name": "OrderService Webhook",
    "url": "https://webhook.site/your-unique-id",
    "description": "Notify on configuration changes",
    "verifySignature": false
  }' \
  -k
```

Visit [webhook.site](https://webhook.site) to get a unique URL for testing.

### 4. Modify a Key and See Notification

```bash
# Get the key ID first (from the configuration details)
# Then update it:
curl -X PUT https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/keys/KEY_ID \
  -H "Content-Type: application/json" \
  -d '{
    "value": "false",
    "description": "Disabled new checkout for maintenance"
  }' \
  -k
```

Check your webhook.site inbox - you should see a notification!

### 5. View Audit Logs

See all changes made to your configuration:

```bash
curl -X GET https://localhost:5001/api/v1/configurations/660e8400-e29b-41d4-a716-446655440001/audit-logs \
  -H "Accept: application/json" \
  -k | jq .
```

## Next Steps

Now that you've set up the basics, here's what to explore:

### 1. **Integrate with Your Services**

Read about client integration patterns:

```csharp
// Create a configuration client for your service
public class ConfigurationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConfigurationClient> _logger;

    public ConfigurationClient(HttpClient httpClient, ILogger<ConfigurationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Configuration> GetConfigurationAsync(string configId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/configurations/{configId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<Configuration>();
    }

    public async Task ListenForChangesAsync(string webhookUrl)
    {
        // Subscribe webhook to configuration changes
        // Your service receives notifications and reloads config
    }
}
```

### 2. **Set Up Multiple Environments**

Create separate configurations for each environment:

```bash
# Development
curl -X POST https://localhost:5001/api/v1/configurations \
  -d '{"applicationId": "...", "environment": "Development", ...}'

# Staging
curl -X POST https://localhost:5001/api/v1/configurations \
  -d '{"applicationId": "...", "environment": "Staging", ...}'

# Production (already created)
```

### 3. **Implement Hot Reload**

Use webhooks to reload configuration without restarting your service:

```csharp
[HttpPost("/config-webhook")]
public async Task OnConfigurationChanged([FromBody] WebhookPayload payload)
{
    _logger.LogInformation("Configuration changed, reloading...");
    
    // Reload configuration from server
    var config = await _configurationClient.GetConfigurationAsync(payload.ConfigurationId);
    
    // Update in-memory settings
    UpdateApplicationSettings(config);
    
    _logger.LogInformation("Configuration reloaded successfully");
}
```

### 4. **Enable Encryption**

Already enabled by default! Sensitive values like connection strings are automatically encrypted:

```json
{
  "key": "ApiKey",
  "value": "sk-1234567890abcdef",
  "isEncrypted": true,  // This value will be encrypted at rest
  "description": "Third-party API key"
}
```

### 5. **Production Deployment**

See [deployment.md](./deployment.md) for detailed instructions on:
- Docker deployment
- Azure App Service
- Kubernetes
- CI/CD pipeline setup

### 6. **Advanced Features**

Explore:
- Batch operations for importing multiple configs
- Configuration snapshots for rollback
- Performance monitoring and metrics
- Rate limiting configuration
- Custom authentication integration

## Troubleshooting

### Error: "Cannot open database"

```bash
# Verify LocalDB is running
sqllocaldb info

# Start LocalDB if needed
sqllocaldb start mssqllocaldb
```

### Error: "No migrations have been applied"

```bash
# List migrations
dotnet ef migrations list

# Apply all pending migrations
dotnet ef database update
```

### Error: "Unable to connect to the database"

Check your connection string in `appsettings.json`:

```bash
# Test connection with SQL tools
# Windows:
sqlcmd -S "(localdb)\mssqllocaldb" -E

# Or verify connection string format
# Remove extra spaces, verify authentication method
```

### Swagger not loading

Make sure you're in Development environment:

```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Development
dotnet run

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

## Getting Help

- **Documentation**: See [api-reference.md](./api-reference.md) for complete API documentation
- **Architecture**: See [architecture.md](./architecture.md) for system design
- **Deployment**: See [deployment.md](./deployment.md) for production setup
- **FAQ**: See [faq.md](./faq.md) for common questions
- **Issues**: [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)

Happy configuring!
