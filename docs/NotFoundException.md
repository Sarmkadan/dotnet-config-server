# NotFoundException

The `NotFoundException` hierarchy within the `dotnet-config-server` project serves as the standard mechanism for signaling that a requested resource does not exist within the system. These exceptions are specialized for core domain entities, including applications, users, webhook deliveries, and change requests. By providing constructors that accept both string and `Guid` identifiers, the API ensures that error messages are consistently formatted and immediately informative, facilitating rapid diagnosis of missing data issues during configuration retrieval and management operations.

## API

The following members constitute the public surface area of the `NotFoundException` types.

### `public NotFoundException`
The base constructor for the exception hierarchy. It initializes a new instance of the generic `NotFoundException` class. This is typically used by derived classes or when a specific resource type context is not required at the throw site.
*   **Parameters**: None.
*   **Return Value**: A new instance of `NotFoundException`.
*   **Throws**: N/A (Constructor).

### `public ApplicationNotFoundException(string applicationId)`
Initializes a new instance of the `ApplicationNotFoundException` class with a specified application identifier.
*   **Parameters**:
    *   `applicationId` (`string`): The string identifier of the missing application.
*   **Return Value**: A new instance of `ApplicationNotFoundException` with a message formatted as `Application '<applicationId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public ApplicationNotFoundException(Guid applicationId)`
Initializes a new instance of the `ApplicationNotFoundException` class with a specified application GUID.
*   **Parameters**:
    *   `applicationId` (`Guid`): The unique identifier of the missing application.
*   **Return Value**: A new instance of `ApplicationNotFoundException` with a message formatted as `Application '<applicationId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public UserNotFoundException(string userId)`
Initializes a new instance of the `UserNotFoundException` class with a specified user identifier.
*   **Parameters**:
    *   `userId` (`string`): The string identifier of the missing user.
*   **Return Value**: A new instance of `UserNotFoundException` with a message formatted as `User '<userId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public UserNotFoundException(Guid userId)`
Initializes a new instance of the `UserNotFoundException` class with a specified user GUID.
*   **Parameters**:
    *   `userId` (`Guid`): The unique identifier of the missing user.
*   **Return Value**: A new instance of `UserNotFoundException` with a message formatted as `User '<userId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public WebhookDeliveryNotFoundException(string deliveryId)`
Initializes a new instance of the `WebhookDeliveryNotFoundException` class with a specified delivery identifier.
*   **Parameters**:
    *   `deliveryId` (`string`): The string identifier of the missing webhook delivery record.
*   **Return Value**: A new instance of `WebhookDeliveryNotFoundException` with a message formatted as `Webhook delivery '<deliveryId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public WebhookDeliveryNotFoundException(Guid deliveryId)`
Initializes a new instance of the `WebhookDeliveryNotFoundException` class with a specified delivery GUID.
*   **Parameters**:
    *   `deliveryId` (`Guid`): The unique identifier of the missing webhook delivery record.
*   **Return Value**: A new instance of `WebhookDeliveryNotFoundException` with a message formatted as `Webhook delivery '<deliveryId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public ChangeRequestNotFoundException(string changeRequestId)`
Initializes a new instance of the `ChangeRequestNotFoundException` class with a specified change request identifier.
*   **Parameters**:
    *   `changeRequestId` (`string`): The string identifier of the missing change request.
*   **Return Value**: A new instance of `ChangeRequestNotFoundException` with a message formatted as `Change request '<changeRequestId>' not found.`.
*   **Throws**: N/A (Constructor).

### `public ChangeRequestNotFoundException(Guid changeRequestId)`
Initializes a new instance of the `ChangeRequestNotFoundException` class with a specified change request GUID.
*   **Parameters**:
    *   `changeRequestId` (`Guid`): The unique identifier of the missing change request.
*   **Return Value**: A new instance of `ChangeRequestNotFoundException` with a message formatted as `Change request '<changeRequestId>' not found.`.
*   **Throws**: N/A (Constructor).

## Usage

### Example 1: Handling Missing Applications by GUID
When retrieving an application configuration by its unique identifier, throw the specific exception if the repository returns null. This ensures the caller receives a structured error indicating exactly which application was missing.

```csharp
public ApplicationConfig GetApplicationConfig(Guid appId)
{
    var config = _repository.FindById(appId);
    
    if (config == null)
    {
        throw new ApplicationNotFoundException(appId);
    }

    return config;
}
```

### Example 2: Validating Webhook Delivery Status
In scenarios where a webhook delivery status is queried using a string-based correlation ID, use the corresponding string constructor to provide immediate context in the exception message.

```csharp
public DeliveryStatus GetDeliveryStatus(string deliveryId)
{
    if (string.IsNullOrWhiteSpace(deliveryId))
    {
        throw new ArgumentException("Delivery ID cannot be empty", nameof(deliveryId));
    }

    var delivery = _deliveryStore.Get(deliveryId);
    
    if (delivery == null)
    {
        throw new WebhookDeliveryNotFoundException(deliveryId);
    }

    return delivery.Status;
}
```

## Notes

*   **Input Validation**: While the constructors accept any string or GUID, recent updates to the project emphasize guard clauses. Callers should ensure that identifiers are validated (e.g., checking for null or empty strings) before invoking these exceptions to prevent ambiguous error messages like `Application '' not found.`.
*   **Thread Safety**: As these classes are immutable data carriers containing only the exception message derived from the constructor arguments, they are inherently thread-safe. Instances can be safely created and thrown across concurrent threads without synchronization.
*   **Message Formatting**: The error messages are generated internally via string interpolation at the time of instantiation. The format strictly follows the pattern `{Resource Type} '{Identifier}' not found.`. Do not rely on modifying the message post-instantiation; if dynamic context is required beyond the ID, it should be handled by the catching logic or through the exception `Data` dictionary.
*   **Inheritance**: All specific exceptions (e.g., `UserNotFoundException`) inherit from the base `NotFoundException` structure, allowing for catch blocks that target the base type if granular handling is not required.
