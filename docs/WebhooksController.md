# WebhooksController

Manages the lifecycle and testing of webhook registrations within the configuration server. This controller provides endpoints to create, retrieve, update, and delete webhook definitions, trigger test deliveries, and inspect the history of past delivery attempts. It serves as the primary interface for integrating external systems that need to react to configuration changes.

## API

### public WebhooksController

Default constructor for the controller. Initializes a new instance without any pre-configured dependencies.

### public async Task<IActionResult> Create
Creates a new webhook registration. Accepts a webhook definition payload from the request body, validates it, persists the registration, and returns the created resource.

- **Parameters:** The webhook definition is expected in the HTTP request body as a JSON object conforming to the webhook model.
- **Returns:** An `IActionResult` representing HTTP 201 Created with the newly created webhook object, or a 400 Bad Request if validation fails.
- **Throws:** May throw if the underlying persistence layer encounters an error.

### public async Task<IActionResult> GetById
Retrieves a single webhook registration by its unique identifier.

- **Parameters:** The webhook ID is expected as a route parameter (e.g., `{id}`).
- **Returns:** An `IActionResult` containing the webhook object with HTTP 200 OK, or 404 Not Found if no webhook matches the provided ID.
- **Throws:** No exceptions are thrown for missing resources; returns 404 instead.

### public async Task<IActionResult> GetByApplication
Retrieves all webhook registrations associated with a specific application.

- **Parameters:** The application identifier is expected as a route or query parameter.
- **Returns:** An `IActionResult` containing a collection of webhook objects with HTTP 200 OK. Returns an empty collection if the application has no registered webhooks.
- **Throws:** No exceptions are thrown; an empty list is returned when no results exist.

### public async Task<IActionResult> Update
Replaces an existing webhook registration with updated data. The entire webhook definition is overwritten.

- **Parameters:** The webhook ID is provided as a route parameter, and the updated webhook definition is provided in the request body.
- **Returns:** An `IActionResult` containing the updated webhook object with HTTP 200 OK, or 404 Not Found if the webhook does not exist, or 400 Bad Request if validation fails.
- **Throws:** `ArgumentException` if the provided ID does not match the payload ID, or if the payload is malformed.

### public async Task<IActionResult> Delete
Removes a webhook registration permanently.

- **Parameters:** The webhook ID is provided as a route parameter.
- **Returns:** An `IActionResult` with HTTP 204 No Content on successful deletion, or 404 Not Found if the webhook does not exist.
- **Throws:** No explicit exceptions; returns 404 for non-existent resources.

### public async Task<IActionResult> Test
Triggers a test delivery to the specified webhook's endpoint URL. This sends a sample payload to verify connectivity and payload structure without requiring a real configuration change event.

- **Parameters:** The webhook ID is provided as a route parameter.
- **Returns:** An `IActionResult` with HTTP 200 OK containing the delivery result (including status code and response body from the target), or 404 Not Found if the webhook does not exist, or 502 Bad Gateway if the target endpoint is unreachable.
- **Throws:** `HttpRequestException` may propagate if the outbound HTTP call fails at the transport level.

### public async Task<IActionResult> GetDeliveries
Retrieves the delivery history for a specific webhook, showing past attempts and their outcomes.

- **Parameters:** The webhook ID is provided as a route parameter. Optional query parameters may control pagination or date ranges.
- **Returns:** An `IActionResult` containing a paginated list of delivery records with HTTP 200 OK, or 404 Not Found if the webhook does not exist.
- **Throws:** No explicit exceptions; returns 404 for non-existent webhooks.

### public bool Success
Indicates whether the most recent operation completed successfully. This property is set after actions such as `Test` or `Create` and reflects the outcome of the internal processing.

### public DateTime Timestamp
Records the UTC time when the last operation was performed by this controller instance. Updated after each action method execution.

### public string Message
Contains a human-readable description of the result of the last operation. Populated with error details on failure or a confirmation message on success.

## Usage

### Example 1: Registering and Testing a Webhook
```csharp
var controller = new WebhooksController();

// Create a new webhook for the "MyApp" application
var createResult = await controller.Create();
if (controller.Success)
{
    Console.WriteLine($"Webhook created at {controller.Timestamp}: {controller.Message}");
}

// Retrieve the created webhook by ID and send a test payload
var getResult = await controller.GetById();
var testResult = await controller.Test();

if (!controller.Success)
{
    Console.WriteLine($"Test failed: {controller.Message}");
}
```

### Example 2: Auditing Delivery History
```csharp
var controller = new WebhooksController();

// Fetch all webhooks for an application
var appWebhooks = await controller.GetByApplication();

// For each webhook, retrieve and inspect delivery history
var deliveriesResult = await controller.GetDeliveries();

if (controller.Success)
{
    Console.WriteLine($"Last checked at {controller.Timestamp}");
}
else
{
    Console.WriteLine($"Could not retrieve deliveries: {controller.Message}");
}
```

## Notes

- **Statefulness:** The `Success`, `Timestamp`, and `Message` properties reflect the outcome of the most recent operation on the controller instance. They are overwritten on each subsequent call and are not thread-safe. Do not rely on these properties across concurrent requests without external synchronization.
- **Idempotency:** `Delete` returns 204 even if the resource was already removed; subsequent deletions of the same ID will return 404. `Update` and `Create` are not idempotent—repeated calls with the same payload may produce duplicate registrations or overwrite existing data.
- **Test Delivery:** The `Test` method makes a real outbound HTTP call to the configured endpoint. Ensure network access and timeouts are configured appropriately. A failure in `Test` does not affect the webhook's enabled status.
- **Pagination:** `GetDeliveries` may return large result sets. Always check for pagination headers or query parameters to avoid unintentionally fetching the entire history.
- **Thread Safety:** This controller is not designed to be used as a singleton across threads. The instance properties `Success`, `Timestamp`, and `Message` are mutated on each call and will race if shared. Scoped or transient lifetime is recommended.
