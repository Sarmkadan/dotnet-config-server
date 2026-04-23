# Architecture Overview

This document describes the system architecture and design patterns used in Dotnet Config Server.

## Table of Contents

- [Architectural Principles](#architectural-principles)
- [Layered Architecture](#layered-architecture)
- [Component Diagram](#component-diagram)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)
- [Security Architecture](#security-architecture)
- [Scalability Considerations](#scalability-considerations)

## Architectural Principles

### 1. **Separation of Concerns**

The system is organized into distinct layers:
- **Controllers** (API endpoints)
- **Services** (business logic)
- **Repositories** (data access)
- **Models** (domain objects)

Each layer has a single responsibility and can be tested independently.

### 2. **Dependency Injection**

All dependencies are managed through ASP.NET Core's built-in DI container:

```csharp
// In Program.cs
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
```

Benefits:
- Loose coupling between components
- Easier testing with mock implementations
- Centralized dependency configuration

### 3. **Repository Pattern**

Data access is abstracted through repository interfaces:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

Benefits:
- Database layer is replaceable
- Easier unit testing with mock repositories
- Centralized query logic

### 4. **Event-Driven Architecture**

Configuration changes trigger events that are processed asynchronously:

```csharp
public interface IEventBus
{
    void Subscribe<T>(Func<T, Task> handler) where T : DomainEvent;
    Task PublishAsync<T>(T domainEvent) where T : DomainEvent;
}
```

Events include:
- `ConfigurationCreatedEvent`
- `ConfigurationUpdatedEvent`
- `VersionPublishedEvent`
- `WebhookDeliveredEvent`

### 5. **SOLID Principles**

- **S**ingle Responsibility: Each class has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Implementations can replace interfaces
- **I**nterface Segregation: Clients depend on focused interfaces
- **D**ependency Inversion: Depend on abstractions, not concretions

## Layered Architecture

### Presentation Layer (API Controllers)

**Location**: `Controllers/`

Responsible for:
- Handling HTTP requests/responses
- Input validation and model binding
- Response formatting
- Status code selection

**Controllers**:
- `ConfigurationsController` - Configuration CRUD
- `VersionsController` - Version management
- `WebhooksController` - Webhook subscriptions
- `AuditLogsController` - Audit trail
- `ApplicationsController` - Application management

Example:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _service;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetConfiguration(Guid id)
    {
        var configuration = await _service.GetConfigurationAsync(id);
        if (configuration == null)
            return NotFound();
        
        return Ok(configuration);
    }
}
```

### Business Logic Layer (Services)

**Location**: `Services/`

Responsible for:
- Core business logic
- Cross-cutting concerns (encryption, versioning)
- Orchestrating repository operations
- Publishing domain events

**Key Services**:

| Service | Responsibility |
|---------|-----------------|
| `ConfigurationService` | Configuration CRUD, key management |
| `VersioningService` | Version lifecycle, snapshots |
| `EncryptionService` | Encrypt/decrypt sensitive values |
| `DiffService` | Compare versions, generate diffs |
| `WebhookService` | Manage subscriptions, trigger deliveries |
| `AuditLogService` | Track all changes |
| `HealthCheckService` | System health monitoring |

Example:
```csharp
public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly IEventBus _eventBus;

    public async Task<Configuration> CreateConfigurationAsync(CreateConfigurationRequest request)
    {
        var configuration = new Configuration
        {
            ApplicationId = request.ApplicationId,
            Environment = request.Environment,
            Description = request.Description
        };

        await _repository.AddAsync(configuration);
        await _eventBus.PublishAsync(new ConfigurationCreatedEvent(configuration.Id));

        return configuration;
    }
}
```

### Data Access Layer (Repositories)

**Location**: `Repositories/`

Responsible for:
- Database queries
- Entity mapping
- Change tracking
- Transaction management

**Repository Pattern**:
```csharp
public class ConfigurationRepository : BaseRepository<Configuration>
{
    public ConfigurationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Configuration>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _context.Configurations
            .Where(c => c.ApplicationId == applicationId)
            .Include(c => c.Keys)
            .ToListAsync();
    }
}
```

### Data Layer (Entity Framework)

**Location**: `Data/`

- `ApplicationDbContext` - EF Core DbContext
- Entity configurations and migrations
- Database schema definition

### Cross-Cutting Concerns

#### Middleware Pipeline

**Location**: `Middleware/`

Executes in order:
1. `ErrorHandlingMiddleware` - Global exception handling
2. `RequestLoggingMiddleware` - Request/response logging
3. `PerformanceMonitoringMiddleware` - Request timing
4. `RateLimitingMiddleware` - Request throttling

```csharp
// In Program.cs
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
```

#### Caching

**Location**: `Caching/`

- `ICacheService` - Cache abstraction
- `MemoryCacheService` - In-memory implementation
- Configurable TTL per cache key

```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task RemoveAsync(string key);
}
```

#### Event Bus

**Location**: `Events/`

- `IEventBus` - Event publisher/subscriber
- `EventBus` - In-memory implementation
- Domain event handlers

```csharp
var eventBus = services.GetRequiredService<IEventBus>();
eventBus.Subscribe<ConfigurationUpdatedEvent>(async @event =>
{
    // Handle event
    await webhookService.TriggerWebhooksAsync(@event);
});
```

## Component Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    HTTP Clients                          │
│  (Web Services, Mobile Apps, CLI Tools)                │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTPS REST API
                       ▼
        ┌──────────────────────────────────┐
        │    ASP.NET Core Web Server       │
        │                                  │
        │  ┌────────────────────────────┐  │
        │  │ Middleware Pipeline        │  │
        │  │ - Error Handling           │  │
        │  │ - Logging                  │  │
        │  │ - Rate Limiting            │  │
        │  └────────────────────────────┘  │
        │           ▼                       │
        │  ┌────────────────────────────┐  │
        │  │ Controllers                │  │
        │  │ - ConfigurationsController │  │
        │  │ - VersionsController       │  │
        │  │ - WebhooksController       │  │
        │  └────────┬───────────────────┘  │
        │           ▼                       │
        │  ┌────────────────────────────┐  │
        │  │ Services                   │  │
        │  │ - ConfigurationService     │  │
        │  │ - VersioningService        │  │
        │  │ - EncryptionService        │  │
        │  │ - WebhookService           │  │
        │  │ - DiffService              │  │
        │  └────────┬───────────────────┘  │
        │           ▼                       │
        │  ┌────────────────────────────┐  │
        │  │ Repositories               │  │
        │  │ - ConfigurationRepository  │  │
        │  │ - VersionRepository        │  │
        │  │ - WebhookRepository        │  │
        │  └────────┬───────────────────┘  │
        │           ▼                       │
        │  ┌────────────────────────────┐  │
        │  │ Infrastructure             │  │
        │  │ - Cache                    │  │
        │  │ - Event Bus                │  │
        │  │ - Background Workers       │  │
        │  └────────────────────────────┘  │
        └──────────────────┬───────────────┘
                           │
        ┌──────────────────┴──────────────┐
        ▼                                  ▼
    ┌────────────────┐          ┌──────────────────┐
    │  SQL Server    │          │  Webhook         │
    │  Database      │          │  Endpoints       │
    │                │          │  (Client Services)│
    │ - Config       │          └──────────────────┘
    │ - Versions     │
    │ - Audit Logs   │
    │ - Encryption   │
    │   Keys         │
    └────────────────┘
```

## Data Flow

### Configuration Update Flow

```
1. Client sends: PUT /api/v1/configurations/{id}/keys/{keyId}
                        ▼
2. ConfigurationsController validates request
                        ▼
3. ConfigurationService processes update
   - Validate key exists
   - Encrypt if necessary
   - Save changes
                        ▼
4. ConfigurationUpdatedEvent published
                        ▼
5. Event handlers triggered:
   ├─ AuditLogService: Log the change
   ├─ CacheService: Invalidate cache
   └─ WebhookService: Queue webhook deliveries
                        ▼
6. WebhookRetryWorker processes queue
   - Fetch subscriptions
   - Send HTTP requests to endpoints
   - Retry on failure
                        ▼
7. Client webhooks receive notifications
                        ▼
8. Client services reload configuration
```

### Configuration Retrieval Flow

```
1. Client sends: GET /api/v1/configurations/{id}
                        ▼
2. ConfigurationsController receives request
                        ▼
3. Check cache for configuration
   ├─ HIT: Return cached version
   └─ MISS: Continue
                        ▼
4. ConfigurationRepository queries database
   - Get configuration
   - Get all keys (filtered by encryption)
   - Include relationships
                        ▼
5. EncryptionService decrypts sensitive values
   - If isEncrypted=true, decrypt
   - Handle key rotation
                        ▼
6. Configuration mapped to response model
                        ▼
7. Cache result with TTL
                        ▼
8. Return to client with 200 OK
```

## Design Patterns

### 1. **Repository Pattern**

Abstracts data access, allows easy switching between database implementations.

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
}

public class ConfigurationRepository : BaseRepository<Configuration> { }
```

### 2. **Dependency Injection**

Manages object dependencies, reduces coupling.

```csharp
public class ConfigurationService
{
    public ConfigurationService(IRepository<Configuration> repository)
    {
        _repository = repository; // Injected dependency
    }
}
```

### 3. **Observer/Event-Driven**

Decouples components through event subscriptions.

```csharp
eventBus.Subscribe<ConfigurationUpdatedEvent>(
    async @event => await webhookService.TriggerAsync(@event));
```

### 4. **Decorator Pattern**

Middleware stack adds behavior to requests.

```csharp
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
// Each middleware "decorates" the next one
```

### 5. **Strategy Pattern**

Different implementations of encryption, caching, etc.

```csharp
IEncryptionService encryptionService = new AES256EncryptionService();
// Can be swapped with different implementation
```

### 6. **Unit of Work Pattern**

`DbContext` manages transactions and change tracking.

```csharp
using (var transaction = await _context.Database.BeginTransactionAsync())
{
    await _repository.AddAsync(entity);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
```

## Security Architecture

### Encryption

```
Sensitive Value (in memory)
        ▼
    Encryption Service (AES-256)
        ├─ Generate salt
        ├─ Derive key from master key + salt
        └─ Encrypt value
        ▼
Encrypted Data (in database)
```

Encryption key storage:
- Master key: Secure key management service (Azure Key Vault, AWS Secrets Manager)
- Per-value salt: Stored alongside encrypted data
- Key versioning: Support multiple active keys

### Authentication & Authorization

Currently no built-in authentication. Recommended approach:

```csharp
// Add authentication to Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configure */ });

// Apply to controllers
[Authorize(Roles = "Admin")]
[HttpPost("configurations")]
public async Task<IActionResult> CreateConfiguration() { }
```

### API Security

- **HTTPS Only**: Enforce in production
- **CORS**: Configurable per environment
- **Rate Limiting**: Prevent abuse
- **Input Validation**: All endpoints validate requests
- **HMAC Signatures**: Webhook authenticity verification

```csharp
// Webhook signature verification
var signature = request.Headers["X-Webhook-Signature"];
var computed = ComputeHMAC256(payload, secret);
if (signature != computed)
    return Unauthorized();
```

## Scalability Considerations

### Horizontal Scaling

Current implementation supports multiple instances:

```
┌─────────────────┐
│ Load Balancer   │
└────────┬────────┘
    ┌───┼───┐
    ▼   ▼   ▼
  ┌──┐ ┌──┐ ┌──┐
  │A │ │B │ │C │  Application instances
  └──┘ └──┘ └──┘
    └───┬───┘
        ▼
  ┌──────────────┐
  │ SQL Server   │  Shared database
  └──────────────┘
```

**Considerations**:
- In-memory cache: Not shared between instances (use distributed cache for production)
- Event bus: In-memory (use message queue like RabbitMQ for production)
- Webhook queue: Database-backed (safe for multiple instances)

### Caching Strategy

```
Request
  ▼
┌─────────────────┐
│ Memory Cache    │ (Instance-local, TTL = 5 min)
│ - Fast         │ (L1 Cache)
│ - Not shared   │
└─────────────────┘
  ▼ MISS
┌─────────────────┐
│ Database        │ (Shared, source of truth)
│ - Reliable      │
│ - Slower        │
└─────────────────┘
```

### Database Optimization

- Connection pooling: Configurable via connection string
- Indexes: On frequently queried columns
- Query optimization: Use Entity Framework projections
- Archival: Old versions/logs can be moved to archive storage

### Message Queue Integration

For production, replace in-memory event bus:

```csharp
builder.Services.AddSingleton<IEventBus, RabbitMQEventBus>();
// or
builder.Services.AddSingleton<IEventBus, AzureServiceBusEventBus>();
```

This allows:
- Guaranteed delivery
- Load distribution
- Service decoupling
- Scalable webhooks

## Performance Metrics

The application tracks:

- Request duration
- Database query times
- Cache hit rates
- Webhook delivery times
- Error rates

Accessible via:
```bash
GET /metrics
```

Monitor these metrics for performance optimization opportunities.
