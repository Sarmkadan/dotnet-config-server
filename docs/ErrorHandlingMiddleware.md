# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` is an ASP.NET Core middleware component that intercepts unhandled exceptions thrown during request processing and transforms them into a structured JSON error response. It captures the exception message, a timestamp, a trace identifier, and optionally a dictionary of field‑level validation errors. This middleware ensures that all errors are returned in a consistent format, making it easier for clients to parse and handle failures.

## API

### `public ErrorHandlingMiddleware(RequestDelegate next)`

Initializes a new instance of the middleware with the next delegate in the pipeline.

- **Parameters**  
  `next` – The `RequestDelegate` representing the next middleware component to invoke.

- **Returns**  
  Nothing (constructor).

- **Throws**  
  `ArgumentNullException` if `next` is `null`.

### `public async Task InvokeAsync(HttpContext context)`

Processes an incoming HTTP request by calling the next middleware. If an exception is thrown downstream, it is caught and the middleware writes a JSON error response to the context.

- **Parameters**  
  `context` – The `HttpContext` for the current request.

- **Returns**  
  A `Task` representing the asynchronous operation.

- **Throws**  
  Does not throw; all exceptions are handled internally and converted to an error response.

### `public string Message`

Gets or sets the human‑readable error message. This property is populated by the middleware when an exception is caught. It typically contains the exception’s `Message` or a generic fallback.

### `public DateTime Timestamp`

Gets or sets the UTC timestamp at which the error occurred. Set by the middleware at the time the exception is caught.

### `public string TraceId`

Gets or sets a unique identifier for the request, used for correlating logs and error reports. The middleware usually reads this from `HttpContext.TraceIdentifier`.

### `public Dictionary<string, List<string>>? Errors`

Gets or sets an optional dictionary of field‑level validation errors. The key is the field name, and the value is a list of error messages for that field. This property is `null` when no validation errors are present; otherwise it is populated by the middleware (for example, from a `ValidationException`).

## Usage

### Example 1: Registering the middleware in the pipeline

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Add ErrorHandlingMiddleware early in the pipeline so it catches all downstream exceptions.
        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### Example 2: Throwing an exception that triggers the middleware

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetProduct(int id)
    {
        if (id <= 0)
        {
            // The middleware will catch this exception and return a structured error response.
            throw new ArgumentException("Product ID must be greater than zero.");
        }

        // ... normal processing
        return Ok(new { Id = id, Name = "Sample Product" });
    }
}
```

When the exception is thrown, the middleware writes a JSON response similar to:

```json
{
  "message": "Product ID must be greater than zero.",
  "timestamp": "2025-03-28T14:30:00.0000000Z",
  "traceId": "0HLOF7N6Q1V3S:00000001",
  "errors": null
}
```

## Notes

- **Thread safety** – The `ErrorHandlingMiddleware` instance is typically registered as a singleton in the ASP.NET Core dependency injection container. The properties `Message`, `Timestamp`, `TraceId`, and `Errors` are instance fields that are overwritten during each invocation of `InvokeAsync`. If multiple requests are processed concurrently, these properties will be shared and may contain data from different requests. Therefore, the middleware is **not thread‑safe** for reading these properties outside of the request context. They are intended to be used only within the `InvokeAsync` method (e.g., for serialization) and should not be relied upon after the request completes.

- **Null `Errors`** – When no validation errors are present, the `Errors` property remains `null`. Clients should check for `null` before iterating over the dictionary.

- **Exception types** – The middleware catches all exceptions, including `ArgumentException`, `ValidationException`, and unhandled system exceptions. The `Message` property is set to the exception’s `Message` unless the exception is of a type that the middleware is configured to handle differently (e.g., a custom `DomainException`). The `Errors` dictionary is populated only when the caught exception carries field‑level error information.

- **Response format** – The middleware always returns a JSON response with the HTTP status code set to 500 (Internal Server Error) by default. If a more specific status code is required, the middleware should be extended or combined with a custom exception‑to‑status‑code mapping.
