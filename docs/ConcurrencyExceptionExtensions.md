# ConcurrencyExceptionExtensions

A set of static extension methods for `ConcurrencyException` that simplify inspection and handling of optimistic concurrency conflicts. The helpers expose semantic properties such as entity identifiers, expected/actual version values, and retry guidance without requiring callers to parse the exception’s message or inner data manually.

## API

### `public static bool IsOptimisticConcurrency(this ConcurrencyException ex)`

- **Purpose**: Determines whether the exception represents an optimistic concurrency conflict.
- **Parameters**: `ex` – the `ConcurrencyException` to evaluate.
- **Return value**: `true` if the exception indicates a concurrency conflict; otherwise `false`.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static bool IsCircularDependency(this ConcurrencyException ex)`

- **Purpose**: Determines whether the exception stems from a circular dependency detected during transaction processing.
- **Parameters**: `ex` – the `ConcurrencyException` to evaluate.
- **Return value**: `true` if the exception indicates a circular dependency; otherwise `false`.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static string? GetEntityType(this ConcurrencyException ex)`

- **Purpose**: Retrieves the CLR type name of the entity involved in the concurrency conflict, if available.
- **Parameters**: `ex` – the `ConcurrencyException` to inspect.
- **Return value**: The entity type name, or `null` when the information is not present.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static Guid? GetEntityId(this ConcurrencyException ex)`

- **Purpose**: Retrieves the unique identifier of the entity involved in the concurrency conflict, if available.
- **Parameters**: `ex` – the `ConcurrencyException` to inspect.
- **Return value**: The entity ID as a `Guid`, or `null` when the information is not present.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static string? GetExpectedVersion(this ConcurrencyException ex)`

- **Purpose**: Retrieves the version value that was expected for the entity at the time of the update.
- **Parameters**: `ex` – the `ConcurrencyException` to inspect.
- **Return value**: The expected version as a string, or `null` when unavailable.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static string? GetActualVersion(this ConcurrencyException ex)`

- **Purpose**: Retrieves the version value that was actually present in the data store when the conflict occurred.
- **Parameters**: `ex` – the `ConcurrencyException` to inspect.
- **Return value**: The actual version as a string, or `null` when unavailable.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static string ToRetryMessage(this ConcurrencyException ex)`

- **Purpose**: Produces a concise, human‑readable message suitable for logging or user display when deciding whether to retry the operation.
- **Parameters**: `ex` – the `ConcurrencyException` to format.
- **Return value**: A formatted string summarizing the conflict (includes entity type, ID, and version mismatch when available).
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static bool ShouldRetryAutomatically(this ConcurrencyException ex)`

- **Purpose**: Indicates whether the operation that caused the exception should be retried automatically based on the library’s retry policy.
- **Parameters**: `ex` – the `ConcurrencyException` to evaluate.
- **Return value**: `true` if automatic retry is recommended; otherwise `false`.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

### `public static IEnumerable<string> GetAllMessages(this ConcurrencyException ex)`

- **Purpose**: Collects the exception’s message and all messages from any inner exceptions into a single enumerable sequence.
- **Parameters**: `ex` – the `ConcurrencyException` to inspect.
- **Return value**: An `IEnumerable<string>` containing each distinct message in the exception chain.
- **Exceptions**: Throws `ArgumentNullException` if `ex` is `null`.

## Usage

```csharp
try
{
    await repository.UpdateAsync(entity);
}
catch (ConcurrencyException ce)
{
    if (ce.IsOptimisticConcurrency())
    {
        // Log details and decide whether to retry.
        var logger = LoggerFactory.CreateLogger<UpdateHandler>();
        logger.LogWarning(
            "Concurrency conflict for {EntityType} Id={EntityId}. Expected version: {Expected}, Actual: {Actual}",
            ce.GetEntityType() ?? typeof(object).Name,
            ce.GetEntityId() ?? Guid.Empty,
            ce.GetExpectedVersion() ?? "unknown",
            ce.GetActualVersion() ?? "unknown");

        if (ce.ShouldRetryAutomatically())
        {
            // Retry logic (e.g., exponential backoff) can be applied here.
            await RetryPolicy.ExecuteAsync(() => repository.UpdateAsync(entity));
        }
    }
    else if (ce.IsCircularDependency())
    {
        // Handle circular dependency separately.
        Logger.LogError(ce, "Circular dependency detected while processing transaction.");
    }
}
```

```csharp
public void HandleConcurrency(ConcurrencyException ex)
{
    // Gather all messages for diagnostic purposes.
    var allMessages = string.Join(Environment.NewLine, ex.GetAllMessages());
    Debug.WriteLine(allMessages);

    // Produce a user‑friendly retry message.
    var retryMessage = ex.ToRetryMessage();
    Console.WriteLine(retryMessage);

    // Example of extracting strongly‑typed identifiers when available.
    if (ex.GetEntityType() == typeof(Order).Name && ex.GetEntityId() has Value guid)
    {
        var order = orderStore.Load(guid);
        // Further domain‑specific handling…
    }
}
```

## Notes

- All extension methods are pure; they read only from the supplied `ConcurrencyException` instance and do not modify it. Consequently, they are thread‑safe to invoke concurrently on the same or different exception instances.
- If the exception does not contain the expected data (e.g., the entity identifier or version values were not populated by the originating code), the corresponding `Get*` methods return `null`. Callers should guard against `null` when using these values.
- The methods throw `ArgumentNullException` when a `null` reference is passed for `ex`. This behavior aligns with typical .NET extension‑method conventions and helps detect programming errors early.
- The return values of `IsOptimisticConcurrency` and `IsCircularDependency` are mutually exclusive in practice, but the implementation does not enforce exclusivity; callers may evaluate both flags if needed.
- Because the methods rely solely on reading properties, any mutation of the exception instance after a call (e.g., by another thread) could affect subsequent calls. In normal usage, exceptions are immutable after being thrown, so this scenario is unlikely.
