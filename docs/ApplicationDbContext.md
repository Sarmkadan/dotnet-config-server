# ApplicationDbContext

The `ApplicationDbContext` class is the Entity Framework Core database context for the dotnet-config-server application. It manages the database schema and provides access to all persistent entities used by the configuration management system, including applications, configurations, versions, webhooks, audit logs, encryption keys, change requests, and validation rules. It inherits from `DbContext` and is typically registered as a scoped service in the dependency injection container.

## API

### `public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)`

Initializes a new instance of the `ApplicationDbContext` with the specified options.

- **Parameters**  
  `options` – A `DbContextOptions<ApplicationDbContext>` instance that configures the database provider, connection string, and other EF Core settings.
- **Return value**  
  None.
- **Throws**  
  `ArgumentNullException` if `options` is `null`.

### `public DbSet<Application> Applications`

Gets or sets the `DbSet` for the `Application` entity. This property provides access to the `Applications` table in the database.

- **Return value**  
  A `DbSet<Application>` that can be used to query and save instances of `Application`.
- **Throws**  
  Never throws on access; exceptions may occur during query execution or save operations.

### `public DbSet<Configuration> Configurations`

Gets or sets the `DbSet` for the `Configuration` entity. Represents the `Configurations` table.

- **Return value**  
  A `DbSet<Configuration>`.
- **Throws**  
  Never throws on access.

### `public DbSet<ConfigurationKey> ConfigurationKeys`

Gets or sets the `DbSet` for the `ConfigurationKey` entity. Represents the `ConfigurationKeys` table.

- **Return value**  
  A `DbSet<ConfigurationKey>`.
- **Throws**  
  Never throws on access.

### `public DbSet<ConfigurationVersion> ConfigurationVersions`

Gets or sets the `DbSet` for the `ConfigurationVersion` entity. Represents the `ConfigurationVersions` table.

- **Return value**  
  A `DbSet<ConfigurationVersion>`.
- **Throws**  
  Never throws on access.

### `public DbSet<WebhookSubscription> WebhookSubscriptions`

Gets or sets the `DbSet` for the `WebhookSubscription` entity. Represents the `WebhookSubscriptions` table.

- **Return value**  
  A `DbSet<WebhookSubscription>`.
- **Throws**  
  Never throws on access.

### `public DbSet<WebhookDelivery> WebhookDeliveries`

Gets or sets the `DbSet` for the `WebhookDelivery` entity. Represents the `WebhookDeliveries` table.

- **Return value**  
  A `DbSet<WebhookDelivery>`.
- **Throws**  
  Never throws on access.

### `public DbSet<ConfigurationDiff> ConfigurationDiffs`

Gets or sets the `DbSet` for the `ConfigurationDiff` entity. Represents the `ConfigurationDiffs` table.

- **Return value**  
  A `DbSet<ConfigurationDiff>`.
- **Throws**  
  Never throws on access.

### `public DbSet<DiffEntry> DiffEntries`

Gets or sets the `DbSet` for the `DiffEntry` entity. Represents the `DiffEntries` table.

- **Return value**  
  A `DbSet<DiffEntry>`.
- **Throws**  
  Never throws on access.

### `public DbSet<AuditLog> AuditLogs`

Gets or sets the `DbSet` for the `AuditLog` entity. Represents the `AuditLogs` table.

- **Return value**  
  A `DbSet<AuditLog>`.
- **Throws**  
  Never throws on access.

### `public DbSet<EncryptionKey> EncryptionKeys`

Gets or sets the `DbSet` for the `EncryptionKey` entity. Represents the `EncryptionKeys` table.

- **Return value**  
  A `DbSet<EncryptionKey>`.
- **Throws**  
  Never throws on access.

### `public DbSet<ChangeRequest> ChangeRequests`

Gets or sets the `DbSet` for the `ChangeRequest` entity. Represents the `ChangeRequests` table.

- **Return value**  
  A `DbSet<ChangeRequest>`.
- **Throws**  
  Never throws on access.

### `public DbSet<ValidationRule> ValidationRules`

Gets or sets the `DbSet` for the `ValidationRule` entity. Represents the `ValidationRules` table.

- **Return value**  
  A `DbSet<ValidationRule>`.
- **Throws**  
  Never throws on access.

## Usage

### Example 1: Creating a new application with an initial configuration

```csharp
public async Task CreateApplicationWithConfiguration(ApplicationDbContext context)
{
    var app = new Application
    {
        Id = Guid.NewGuid(),
        Name = "MyApp",
        Description = "Sample application"
    };

    var config = new Configuration
    {
        Id = Guid.NewGuid(),
        ApplicationId = app.Id,
        Key = "app.settings",
        Value = "{\"theme\": \"dark\"}",
        Version = 1
    };

    context.Applications.Add(app);
    context.Configurations.Add(config);
    await context.SaveChangesAsync();
}
```

### Example 2: Querying recent audit logs for a specific application

```csharp
public async Task<List<AuditLog>> GetRecentAuditLogs(ApplicationDbContext context, Guid applicationId)
{
    return await context.AuditLogs
        .Where(log => log.ApplicationId == applicationId)
        .OrderByDescending(log => log.Timestamp)
        .Take(10)
        .ToListAsync();
}
```

## Notes

- **Thread safety**: `ApplicationDbContext` is not thread-safe. Do not share a single instance across multiple threads. Each thread or asynchronous operation should obtain its own instance, typically via dependency injection with a scoped lifetime.
- **Disposal**: Always dispose the context after use, either explicitly or by relying on the DI container. Failing to dispose can lead to resource leaks.
- **Tracking**: By default, EF Core tracks entities returned from queries. For read-only operations, consider using `AsNoTracking()` to improve performance.
- **Null options**: The constructor throws `ArgumentNullException` if `options` is `null`. Ensure the options are properly configured before instantiation.
- **Schema migrations**: The `DbSet` properties define the entity sets that are included in database migrations. Adding or removing a `DbSet` property will affect the generated migration code.
- **Lazy loading**: Lazy loading is not enabled by default. If required, it must be configured in the options and the entity classes must be made `virtual`.
