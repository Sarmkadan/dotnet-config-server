# ValidationRule

The `ValidationRule` class serves as a data transfer object and result container within the `dotnet-config-server` project, encapsulating the definition, execution state, and outcome of a specific configuration validation logic. It bridges the gap between the declarative rule metadata stored in the system (such as name, type, and target patterns) and the runtime evaluation results, providing immediate feedback on validity through a collection of `ValidationViolation` instances and boolean status flags.

## API

The following members constitute the public interface of the `ValidationRule` type.

### `Id`
```csharp
public Guid Id { get; set; }
```
Gets or sets the unique identifier for this validation rule instance. This GUID distinguishes the rule within the global registry of validation logic.

### `Name`
```csharp
public string Name { get; set; }
```
Gets or sets the human-readable name assigned to the rule. This value is typically used for logging, UI display, and administrative identification.

### `Description`
```csharp
public string? Description { get; set; }
```
Gets or sets an optional detailed explanation of the rule's purpose and logic. This property may be `null` if no description was provided during rule creation.

### `ConfigurationId`
```csharp
public Guid? ConfigurationId { get; set; }
```
Gets or sets the optional identifier of the specific configuration entity to which this rule is scoped. If `null`, the rule may apply globally or across multiple configurations depending on the `TargetKeyPattern`.

### `RuleType`
```csharp
public ValidationRuleType RuleType { get; set; }
```
Gets or sets the enumeration value defining the category or execution strategy of the rule (e.g., Regex, Range, Custom Script). This determines how the `Parameters` are interpreted during evaluation.

### `Parameters`
```csharp
public string? Parameters { get; set; }
```
Gets or sets the serialized configuration data required to execute the rule. The format (JSON, XML, or delimited string) depends on the specified `RuleType`. This property may be `null` if the rule requires no external parameters.

### `IsActive`
```csharp
public bool IsActive { get; set; }
```
Gets or sets a flag indicating whether the rule is currently enabled. Inactive rules are typically skipped during automated validation pipelines.

### `CreatedBy`
```csharp
public string CreatedBy { get; set; }
```
Gets or sets the identifier (usually a username or service account name) of the entity that created this rule.

### `CreatedAt`
```csharp
public DateTime CreatedAt { get; set; }
```
Gets or sets the UTC timestamp indicating when the rule was initially persisted.

### `UpdatedAt`
```csharp
public DateTime UpdatedAt { get; set; }
```
Gets or sets the UTC timestamp of the most recent modification to the rule's definition or state.

### `TargetKeyPattern`
```csharp
public string? TargetKeyPattern { get; set; }
```
Gets or sets an optional pattern (often a glob or regex) used to match configuration keys against which this rule should be applied. If `null`, the rule may apply to all keys or rely on explicit binding.

### `IsValid`
```csharp
public bool IsValid { get; set; }
```
Gets or sets the aggregate result of the validation execution. A value of `true` indicates no violations were found; `false` indicates one or more failures.

### `Violations`
```csharp
public List<ValidationViolation> Violations { get; set; }
```
Gets or sets the collection of specific errors encountered during validation. This list is populated when `IsValid` is `false` and provides granular details about why the configuration failed the rule.

### `KeyName`
```csharp
public string KeyName { get; set; }
```
Gets or sets the specific configuration key that was evaluated in this particular instance of the rule execution.

### `RuleName`
```csharp
public string RuleName { get; set; }
```
Gets or sets the name of the rule as it pertains to this specific execution context. This may duplicate the `Name` property or contain a resolved variant if dynamic naming is used.

### `Message`
```csharp
public string Message { get; set; }
```
Gets or sets a summary message describing the overall outcome of the validation. This is often a concatenation of violation messages or a generic success/failure statement.

### `RuleId`
```csharp
public Guid RuleId { get; set; }
```
Gets or sets the reference identifier linking this result object back to the persistent rule definition. This allows correlation between transient validation results and stored rule metadata.

## Usage

### Example 1: Inspecting Validation Results
This example demonstrates how to consume a `ValidationRule` instance returned from a validation service to determine if a configuration key passes compliance checks and to log specific violations if it fails.

```csharp
public void ProcessValidationResult(ValidationRule result)
{
    if (!result.IsActive)
    {
        Console.WriteLine($"Rule '{result.Name}' is inactive. Skipping analysis.");
        return;
    }

    Console.WriteLine($"Evaluating rule: {result.RuleName} against key: {result.KeyName}");

    if (result.IsValid)
    {
        Console.WriteLine($"Success: {result.Message}");
    }
    else
    {
        Console.WriteLine($"Failure: {result.Message}");
        foreach (var violation in result.Violations)
        {
            Console.WriteLine($"  - Violation at '{violation.Path}': {violation.ErrorMessage}");
        }
    }
}
```

### Example 2: Defining a New Rule Definition
This example illustrates the initialization of a `ValidationRule` object with metadata prior to persisting it to the configuration store. Note that result-specific fields like `IsValid` and `Violations` are typically populated by the engine, not manually set during definition.

```csharp
public ValidationRule CreateRegexRule(Guid configId, string targetPattern)
{
    var rule = new ValidationRule
    {
        Id = Guid.NewGuid(),
        Name = "StrictEmailFormat",
        Description = "Ensures all email fields match RFC 5322 standards.",
        ConfigurationId = configId,
        RuleType = ValidationRuleType.Regex,
        Parameters = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        IsActive = true,
        CreatedBy = "admin@system.local",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        TargetKeyPattern = targetPattern,
        // Result fields usually default to false/empty until execution
        IsValid = false,
        Violations = new List<ValidationViolation>(),
        KeyName = string.Empty,
        RuleName = "StrictEmailFormat",
        Message = string.Empty,
        RuleId = Guid.NewGuid()
    };

    return rule;
}
```

## Notes

*   **State Duality**: The `ValidationRule` type conflates two distinct states: the **definition** of a rule (e.g., `Name`, `RuleType`, `Parameters`) and the **result** of an execution (e.g., `IsValid`, `Violations`, `KeyName`). When using this class, care must be taken to distinguish between instances intended for storage (where result fields are irrelevant) and instances returned from a validator (where definition fields serve as context).
*   **Collection Initialization**: The `Violations` property is exposed as a concrete `List<ValidationViolation>`. Callers must ensure this list is instantiated before adding items to avoid `NullReferenceException`, as the default constructor behavior for this specific property depends on the initialization context (manual vs. deserialization).
*   **Thread Safety**: This class is not thread-safe. Properties such as `IsValid`, `Violations`, and `Message` are mutable and intended to be written once during validation and read subsequently. If a single instance is shared across multiple threads for concurrent modification of these result fields, external synchronization is required.
*   **Nullable Handling**: Properties like `Description`, `ConfigurationId`, `Parameters`, and `TargetKeyPattern` are nullable. Consumers should perform null checks before accessing members of these properties, particularly when serializing the object or passing it to logic that assumes strict scoping or parameterization.
*   **Timestamp Precision**: The `CreatedAt` and `UpdatedAt` properties use `DateTime`. In distributed environments, ensure that the system clock is synchronized (UTC) to maintain accurate audit trails, as this class does not enforce timezone normalization internally.
