# Frequently Asked Questions

## General Questions

### Q: What is Dotnet Config Server used for?

**A:** Dotnet Config Server is a centralized configuration management system for microservices. It provides:
- Single source of truth for application configurations
- Encryption for sensitive values
- Version control and rollback capabilities
- Hot reload support
- Audit logging and change tracking
- Webhook notifications for configuration changes

### Q: Do I need a database to use Dotnet Config Server?

**A:** Yes, SQL Server is required. You can use:
- **LocalDB** (development)
- **SQL Server Express** (free, limited)
- **Azure SQL** (managed cloud)
- **Self-hosted SQL Server** (any edition)

### Q: Can multiple services use the same Dotnet Config Server instance?

**A:** Yes, that's the whole point! Each service can have its own application and configurations. You create separate Application entries for each service.

```csharp
// OrderService
POST /api/v1/applications { "name": "OrderService" }

// PaymentService  
POST /api/v1/applications { "name": "PaymentService" }

// InventoryService
POST /api/v1/applications { "name": "InventoryService" }
```

## Installation & Setup

### Q: Which versions of .NET are supported?

**A:** .NET 10.0 and later. Earlier versions (.NET 6, 7, 8, 9) are not supported.

### Q: I get "Cannot open database" error, what should I do?

**A:** 
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. For LocalDB: `sqllocaldb start mssqllocaldb`
4. Test connection: `sqlcmd -S (localdb)\mssqllocaldb -E`

### Q: How do I apply database migrations?

**A:**
```bash
dotnet ef database update
```

If you modified the schema:
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Q: Can I use Entity Framework Core migrations with an existing database?

**A:** Yes, use the "Database First" approach or scaffold existing schema:
```bash
dotnet ef dbcontext scaffold "connection-string" Microsoft.EntityFrameworkCore.SqlServer
```

## Configuration Management

### Q: Can I have multiple environments (Dev, Staging, Prod)?

**A:** Yes, create separate Configuration entries with different environments:

```csharp
POST /api/v1/configurations { "applicationId": "...", "environment": "Development" }
POST /api/v1/configurations { "applicationId": "...", "environment": "Staging" }
POST /api/v1/configurations { "applicationId": "...", "environment": "Production" }
```

### Q: How do I share common configurations across multiple services?

**A:** Create a "Shared" or "Common" application and have services reference it:

```csharp
// Shared application for common configs
var sharedAppId = await GetOrCreateApplication("Shared");

// Service-specific configs reference shared configs
GET /api/v1/configurations/{sharedConfigId}/keys
```

### Q: Can I import configurations from a JSON file?

**A:** Not yet, but you can batch-create keys using the batch API:

```csharp
var batch = new {
    configurations = new[] {
        new { key = "Setting1", value = "Value1" },
        new { key = "Setting2", value = "Value2" }
    }
};

POST /api/v1/batch/import/{configurationId} body: batch
```

### Q: How are configuration keys organized?

**A:** Keys use dot notation:
```
Database:ConnectionString
Database:PoolSize
Feature:NewCheckout
Feature:BetaUI
Logging:Level
Logging:Format
```

## Encryption & Security

### Q: What encryption algorithm is used?

**A:** AES-256 (Advanced Encryption Standard with 256-bit keys).

### Q: How do I enable encryption for sensitive values?

**A:** Set `isEncrypted: true` when creating a key:

```csharp
{
  "key": "ApiKey:ThirdParty",
  "value": "sk-1234567890abcdef",
  "isEncrypted": true,
  "description": "Third-party API key"
}
```

### Q: Where is the encryption key stored?

**A:** 
- **Development**: In memory (not suitable for production)
- **Production**: Use Azure Key Vault, AWS Secrets Manager, or other secure key management service

Modify `EncryptionService` to integrate with your key vault:

```csharp
public class KeyVaultEncryptionService : IEncryptionService
{
    private readonly IKeyVaultClient _keyVault;
    
    public async Task<string> EncryptAsync(string plaintext)
    {
        var key = await _keyVault.GetKeyAsync("config-server-key");
        return CryptoUtils.Encrypt(plaintext, key);
    }
}
```

### Q: Can I rotate encryption keys?

**A:** Yes, the service supports key versioning. Create a new key and the service will use it for new encryptions:

```csharp
await _encryptionService.GenerateNewKeyAsync();
// Existing encrypted values remain valid with old keys
// New values use the new key
```

### Q: Is authentication required?

**A:** Not by default. For production, add authentication:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

[Authorize]
[HttpPost("configurations")]
public async Task<IActionResult> CreateConfiguration() { }
```

## Versioning & Rollback

### Q: What's the difference between versions and snapshots?

**A:** 
- **Snapshot**: Automatic copy of configuration at a point in time
- **Version**: Explicitly created and published snapshot with metadata

### Q: Can I rollback to any previous version?

**A:** Yes, any version not deleted can be rolled back to:

```csharp
POST /api/v1/configurations/{configId}/versions/{versionId}/rollback
```

### Q: How long are versions retained?

**A:** Configurable in `appsettings.json`:

```json
"ApplicationSettings": {
  "MaxVersionHistory": 100
}
```

Archive or delete old versions to manage storage.

### Q: What happens when I archive a version?

**A:** Archived versions:
- Cannot be published as active
- Are not deleted (retained for audit trail)
- Can be viewed but not modified
- Save database space compared to active versions

### Q: Can I see what changed between versions?

**A:** Yes, use the diff endpoint:

```csharp
GET /api/v1/configurations/{configId}/versions/{fromId}/diff/{toId}
```

Returns list of changes:
```json
[
  {
    "key": "Database:Host",
    "changeType": "Modified",
    "oldValue": "localhost",
    "newValue": "prod-db.example.com"
  },
  {
    "key": "Feature:Beta",
    "changeType": "Added",
    "newValue": "true"
  }
]
```

## Webhooks & Notifications

### Q: How do webhooks work?

**A:** 
1. Service subscribes to configuration changes
2. When configuration is updated, server sends HTTP POST to webhook URL
3. Payload includes change details and HMAC signature
4. Service receives notification and reloads configuration

### Q: Do webhooks retry if delivery fails?

**A:** Yes, automatic retry with exponential backoff:

```json
"Webhook": {
  "MaxRetries": 5,
  "TimeoutSeconds": 30
}
```

Failed deliveries are queued for retry.

### Q: How do I verify webhook authenticity?

**A:** Each webhook includes HMAC-SHA256 signature:

```csharp
[HttpPost("/config-webhook")]
public async Task OnConfigurationChanged([FromBody] WebhookPayload payload)
{
    var signature = Request.Headers["X-Webhook-Signature"];
    var computed = ComputeHMACSHA256(payload, webhookSecret);
    
    if (signature != computed)
        return Unauthorized();
    
    // Process configuration change
}

private string ComputeHMACSHA256(object payload, string secret)
{
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
    {
        var json = JsonConvert.SerializeObject(payload);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}
```

### Q: Can I test webhooks without having a live endpoint?

**A:** Yes, use [webhook.site](https://webhook.site):

1. Visit webhook.site and copy the unique URL
2. Create webhook subscription with that URL
3. Make configuration changes
4. View notifications in webhook.site dashboard

### Q: What data is included in webhook payloads?

**A:**
```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "ConfigurationUpdated",
  "configurationId": "660e8400-e29b-41d4-a716-446655440001",
  "applicationId": "770e8400-e29b-41d4-a716-446655440002",
  "timestamp": "2026-05-04T10:30:00Z",
  "changes": [
    {
      "key": "Database:Host",
      "oldValue": "localhost",
      "newValue": "prod-db.example.com"
    }
  ]
}
```

## Performance & Scaling

### Q: Can I run multiple instances?

**A:** Yes, behind a load balancer:

```
┌──────────────┐
│ Load Balancer│
└──────┬───────┘
    ┌──┴──┬──┬──┐
   ▼  ▼  ▼  ▼
  Instance 1, 2, 3, 4
       ↓↓↓↓
    SQL Server
```

**Note**: In-memory cache and event bus are not shared. For production, use:
- **Cache**: Redis, Azure Cache for Redis
- **Event Bus**: RabbitMQ, Azure Service Bus

### Q: How do I improve performance?

**A:**
1. **Use caching**: Results are cached in memory (TTL configurable)
2. **Database indexing**: Ensure important columns are indexed
3. **Connection pooling**: Configure in connection string
4. **Async operations**: All I/O operations are async
5. **Compression**: Enable HTTP compression in IIS/reverse proxy

### Q: What's the maximum number of configurations?

**A:** Limited only by database capacity. Tested with 1000+ configurations and 100K+ keys.

### Q: How do I monitor performance?

**A:**
```csharp
GET /metrics
```

Response includes:
- Request counts by endpoint
- Average response times
- Error rates
- Cache hit rates
- Database query times

## Troubleshooting

### Q: Webhook is not being delivered

**A:** Check:
1. Webhook URL is publicly accessible
2. Service accepts POST requests
3. Firewall allows outbound HTTPS
4. Check webhook delivery status: `GET /webhooks/{id}/deliveries`
5. Verify no signature verification errors

### Q: Configuration changes not immediately reflected

**A:** 
1. Verify version is published (not in Draft status)
2. Check client is calling correct endpoint
3. Webhook may be delayed - check retry queue
4. Clear local caches if using client-side caching

### Q: Encryption key errors after deployment

**A:** 
1. Ensure encryption key is available in production environment
2. Check key vault access permissions
3. Verify key hasn't expired
4. Regenerate key if necessary

### Q: Database size growing too large

**A:**
1. Archive old versions: `POST /versions/{id}/archive`
2. Delete archived versions after retention period
3. Archive old audit logs
4. Run database maintenance tasks

### Q: Slow response times

**A:**
1. Check database query performance
2. Review logs for slow queries
3. Add database indexes on frequently filtered columns
4. Monitor cache hit rates
5. Consider scaling up database server

## API & Development

### Q: How do I add custom configuration properties?

**A:** Extend the Configuration model:

```csharp
public class Configuration : BaseEntity
{
    // Existing properties
    
    // Add custom properties
    public string Team { get; set; }
    public string Owner { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

Then run migrations:
```bash
dotnet ef migrations add AddCustomProperties
dotnet ef database update
```

### Q: Can I use Dotnet Config Server with non-.NET services?

**A:** Yes, it's language-agnostic. Services in any language can:
- Call REST API to retrieve configurations
- Use webhooks to receive notifications
- Parse JSON responses

Example Python client:
```python
import requests

def get_config(config_id):
    response = requests.get(
        f'https://config-server/api/v1/configurations/{config_id}'
    )
    return response.json()

def on_webhook():
    # Reload configuration
    config = get_config(config_id)
    update_app_config(config)
```

### Q: How do I contribute?

**A:** 
1. Fork repository
2. Create feature branch
3. Make changes
4. Write/update tests
5. Submit pull request

See README for detailed guidelines.

### Q: Where can I report bugs?

**A:** [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)

### Q: Is there commercial support available?

**A:** This is an open-source project. Support available through GitHub issues and discussions.

---

**Still have questions?** Open a discussion on [GitHub](https://github.com/sarmkadan/dotnet-config-server) or contact the maintainer.
