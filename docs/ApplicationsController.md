# ApplicationsController

The `ApplicationsController` provides RESTful endpoints for managing application definitions in the configuration server. It supports full CRUD operations as well as retrieving all configurations associated with a specific application. The controller also exposes pagination state properties that are populated after executing a `GetAll` request, allowing callers to inspect the current page, page size, total count, and the returned items.

## API

### `public ApplicationsController()`

Initializes a new instance of the controller. No parameters are required; dependency injection is handled by the framework.

### `public async Task<IActionResult> Create([FromBody] ApplicationCreateDto dto)`

Creates a new application.

- **Parameters**  
  `dto` – An object containing the application data (e.g., name, description). Must be valid according to data annotations.
- **Returns**  
  `201 Created` with the created application resource in the response body.  
  `400 Bad Request` if the model state is invalid.  
  `409 Conflict` if an application with the same unique identifier already exists.
- **Throws**  
  `ArgumentNullException` if `dto` is `null`.

### `public async Task<IActionResult> GetById(Guid id)`

Retrieves a single application by its unique identifier.

- **Parameters**  
  `id` – The GUID of the application.
- **Returns**  
  `200 OK` with the application resource.  
  `404 Not Found` if no application with the given `id` exists.
- **Throws**  
  `ArgumentException` if `id` is `Guid.Empty`.

### `public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)`

Retrieves a paginated list of all applications.

- **Parameters**  
  `page` – The page number (1‑based). Defaults to 1.  
  `pageSize` – The number of items per page. Defaults to 20.
- **Returns**  
  `200 OK` with a paginated result. The response body contains the items for the requested page. After this method completes, the controller’s `Items`, `Page`, `PageSize`, and `TotalCount` properties are updated to reflect the result.
- **Throws**  
  `ArgumentOutOfRangeException` if `page` is less than 1 or `pageSize` is less than 1.

### `public async Task<IActionResult> Update(Guid id, [FromBody] ApplicationUpdateDto dto)`

Updates an existing application.

- **Parameters**  
  `id` – The GUID of the application to update.  
  `dto` – An object containing the updated fields.
- **Returns**  
  `200 OK` with the updated application resource.  
  `400 Bad Request` if the model state is invalid.  
  `404 Not Found` if no application with the given `id` exists.  
  `409 Conflict` if the update would violate a uniqueness constraint.
- **Throws**  
  `ArgumentNullException` if `dto` is `null`.  
  `ArgumentException` if `id` is `Guid.Empty`.

### `public async Task<IActionResult> Delete(Guid id)`

Deletes an application and all its associated configurations.

- **Parameters**  
  `id` – The GUID of the application to delete.
- **Returns**  
  `204 No Content` on success.  
  `404 Not Found` if no application with the given `id` exists.
- **Throws**  
  `ArgumentException` if `id` is `Guid.Empty`.

### `public async Task<IActionResult> GetConfigurations(Guid applicationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)`

Retrieves all configurations belonging to a specific application, with pagination.

- **Parameters**  
  `applicationId` – The GUID of the parent application.  
  `page` – The page number (1‑based). Defaults to 1.  
  `pageSize` – The number of items per page. Defaults to 20.
- **Returns**  
  `200 OK` with a paginated list of configuration resources.  
  `404 Not Found` if no application with the given `applicationId` exists.
- **Throws**  
  `ArgumentOutOfRangeException` if `page` is less than 1 or `pageSize` is less than 1.  
  `ArgumentException` if `applicationId` is `Guid.Empty`.

### `public List<T> Items { get; set; }`

Gets or sets the list of items returned by the most recent paginated query (`GetAll` or `GetConfigurations`). The type parameter `T` is determined by the context (e.g., `ApplicationDto` or `ConfigurationDto`). This property is `null` until a paginated method is called.

### `public int Page { get; set; }`

Gets or sets the current page number (1‑based) of the most recent paginated query. Defaults to 0 before any paginated call.

### `public int PageSize { get; set; }`

Gets or sets the page size used in the most recent paginated query. Defaults to 0 before any paginated call.

### `public int TotalCount { get; set; }`

Gets or sets the total number of items available across all pages from the most recent paginated query. Defaults to 0 before any paginated call.

## Usage

### Example 1: Creating and retrieving an application

```csharp
// Assume controller is injected via constructor
var createDto = new ApplicationCreateDto { Name = "MyApp", Description = "Example application" };
var createResult = await controller.Create(createDto);

if (createResult is CreatedResult created)
{
    var app = created.Value as ApplicationDto;
    Console.WriteLine($"Created application with ID: {app.Id}");

    var getResult = await controller.GetById(app.Id);
    if (getResult is OkObjectResult ok)
    {
        var retrieved = ok.Value as ApplicationDto;
        Console.WriteLine($"Retrieved: {retrieved.Name}");
    }
}
```

### Example 2: Paginating through all applications

```csharp
// First page
var result = await controller.GetAll(page: 1, pageSize: 10);
if (result is OkObjectResult)
{
    Console.WriteLine($"Page {controller.Page} of {Math.Ceiling((double)controller.TotalCount / controller.PageSize)}");
    foreach (var app in controller.Items)
    {
        Console.WriteLine($"- {app.Name}");
    }
}

// Subsequent pages
while (controller.Page * controller.PageSize < controller.TotalCount)
{
    result = await controller.GetAll(page: controller.Page + 1, pageSize: controller.PageSize);
    // Process items...
}
```

## Notes

- The pagination properties (`Items`, `Page`, `PageSize`, `TotalCount`) are **not thread-safe**. They are intended for single‑threaded use within the same request context. Do not rely on their values across concurrent requests or after the controller instance is reused.
- All `Guid` parameters are validated for emptiness; passing `Guid.Empty` will cause an `ArgumentException`.
- The `Create` and `Update` methods enforce model validation via data annotations. Ensure the DTOs are properly decorated.
- Deleting an application cascades to all its configurations. This operation is irreversible.
- The `GetAll` and `GetConfigurations` methods reset the pagination properties on each call. If you need to preserve pagination state across multiple calls, store the values externally.
- The controller does not implement any caching; each request hits the underlying data store.
