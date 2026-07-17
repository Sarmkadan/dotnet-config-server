# ServiceExtensionsConfigurationExtensions

The `ServiceExtensionsConfigurationExtensions` static class provides extension methods for the `ServiceExtensionsConfiguration` type. These methods enable inspection of the registered service categories (data services, business services, Swagger configuration, database initialization) and support the creation of new configuration instances with additional service types added. The class is designed to be used during application startup to query and compose service registration metadata before passing it to the dependency injection container.

## API

All members are extension methods on `ServiceExtensionsConfiguration`. Unless otherwise noted, they do not throw exceptions; however, passing a `null` instance will result in an `ArgumentNullException`.

### `HasDataServices`

```csharp
public static bool HasDataServices(this ServiceExtensionsConfiguration configuration)
```

Returns `true` if the configuration contains at least one data service type; otherwise `false`.

### `HasBusinessServices`

```csharp
public static bool HasBusinessServices(this ServiceExtensionsConfiguration configuration)
```

Returns `true` if the configuration contains at least one business service type; otherwise `false`.

### `HasSwaggerConfiguration`

```csharp
public static bool HasSwaggerConfiguration(this ServiceExtensionsConfiguration configuration)
```

Returns `true` if the configuration includes Swagger-related settings; otherwise `false`.

### `HasDatabaseInitialization`

```csharp
public static bool HasDatabaseInitialization(this ServiceExtensionsConfiguration configuration)
```

Returns `true` if the configuration includes database initialization logic; otherwise `false`.

### `HasAnyServices`

```csharp
public static bool HasAnyServices(this ServiceExtensionsConfiguration configuration)
```

Returns `true` if the configuration contains any service type (data, business, or other); otherwise `false`.

### `GetServiceCount`

```csharp
public static int GetServiceCount(this ServiceExtensionsConfiguration configuration)
```

Returns the total number of service types registered in the configuration.

### `GetAllServiceTypes`

```csharp
public static IReadOnlyList<string> GetAllServiceTypes(this ServiceExtensionsConfiguration configuration)
```

Returns a read-only list of all service type names registered in the configuration. The order is not guaranteed.

### `WithAddedDataServices`

```csharp
public static ServiceExtensionsConfiguration WithAddedDataServices(this ServiceExtensionsConfiguration configuration)
```

Returns a new `ServiceExtensionsConfiguration` instance that includes all services from the original configuration plus the default set of data services. The original configuration is not modified.

### `WithAddedBusinessServices`

```csharp
public static ServiceExtensionsConfiguration WithAddedBusinessServices(this ServiceExtensionsConfiguration configuration)
```

Returns a new `ServiceExtensionsConfiguration` instance that includes all services from the original configuration plus the default set of business services. The original configuration is not modified.

## Usage

### Example 1: Inspecting and extending a configuration

```csharp
using ServiceExtensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var config = new ServiceExtensionsConfiguration();

        if (!config.HasAnyServices())
        {
            config = config.WithAddedDataServices();
        }

        if (!config.HasSwaggerConfiguration())
        {
            // Add Swagger configuration manually or via another extension
        }

        int count = config.GetServiceCount();
        Console.WriteLine($"Total services registered: {count}");

        // Pass config to the DI registration pipeline
        services.AddServiceExtensions(config);
    }
}
```

### Example 2: Querying service types and conditional registration

```csharp
using ServiceExtensions;

public class ServiceRegistrar
{
    public void Register(IServiceCollection services, ServiceExtensionsConfiguration config)
    {
        if (config.HasBusinessServices())
        {
            // Business services are present; register additional infrastructure
            services.AddScoped<IBusinessService, BusinessService>();
        }

        if (config.HasDatabaseInitialization())
        {
            // Ensure database initializer is registered
            services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        }

        var allTypes = config.GetAllServiceTypes();
        foreach (var typeName in allTypes)
        {
            Console.WriteLine($"Registering: {typeName}");
        }
    }
}
```

## Notes

- All extension methods are pure: they do not modify the input `ServiceExtensionsConfiguration` instance. The `WithAddedDataServices` and `WithAddedBusinessServices` methods return a new instance, leaving the original unchanged.
- If the input configuration is `null`, every method will throw an `ArgumentNullException`. Ensure the configuration is instantiated before calling these extensions.
- The class is stateless and thread-safe. Multiple threads may safely call these methods on the same or different configuration instances without synchronization.
- The `GetAllServiceTypes` method returns a snapshot of the service type names at the time of the call. The returned list is immutable and will not reflect subsequent modifications to the configuration.
- Edge cases: an empty configuration (no services registered) will cause `HasAnyServices`, `HasDataServices`, and `HasBusinessServices` to return `false`, `GetServiceCount` to return `0`, and `GetAllServiceTypes` to return an empty list. The `WithAdded*` methods will still produce a new configuration with the added services.
