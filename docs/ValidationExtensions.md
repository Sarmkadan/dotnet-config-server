# ValidationExtensions

Provides a set of static extension methods for performing common validation checks and combining their results. The type also exposes the `ValidationResult` structure used to represent the outcome of a validation operation.

## API

### ValidateNotEmpty
**Purpose:** Determines whether a supplied string is not null, empty, or consisting only of white‑space characters.  
**Parameters:**  
- `value`: The string to test.  
**Return:** A `ValidationResult` indicating success when the string contains at least one non‑white‑space character; otherwise failure with an appropriate error message.  
**Throws:** `ArgumentNullException` if `value` is `null`.

### ValidateNotEmpty<T>
**Purpose:** Determines whether a supplied reference‑type value is not null.  
**Parameters:**  
- `value`: The value to test.  
**Return:** A `ValidationResult` indicating success when `value` is not `null`; otherwise failure with an error message.  
**Throws:** `ArgumentNullException` if `value` is `null`.

### ValidateMinLength
**Purpose:** Checks that a string meets a minimum length requirement.  
**Parameters:**  
- `value`: The string to examine.  
- `minLength`: The minimum allowed number of characters; must be zero or greater.  
**Return:** A `ValidationResult` indicating success when `value` length is ≥ `minLength`; otherwise failure.  
**Throws:**  
- `ArgumentNullException` if `value` is `null`.  
- `ArgumentOutOfRangeException` if `minLength` is less than zero.

### ValidateMaxLength
**Purpose:** Checks that a string does not exceed a maximum length.  
**Parameters:**  
- `value`: The string to examine.  
- `maxLength`: The maximum allowed number of characters; must be zero or greater.  
**Return:** A `ValidationResult` indicating success when `value` length is ≤ `maxLength`; otherwise failure.  
**Throws:**  
- `ArgumentNullException` if `value` is `null`.  
- `ArgumentOutOfRangeException` if `maxLength` is less than zero.

### ValidateRange<T>
**Purpose:** Verifies that a comparable value falls within an inclusive range.  
**Parameters:**  
- `value`: The value to test.  
- `min`: The lower bound of the range.  
- `max`: The upper bound of the range.  
**Return:** A `ValidationResult` indicating success when `min ≤ value ≤ max`; otherwise failure.  
**Throws:**  
- `ArgumentNullException` if any argument is `null` for reference types.  
- `InvalidOperationException` if `T` does not implement `System.IComparable<T>` (caught at compile time).  
- `ArgumentException` if `min` is greater than `max`.

### ValidatePattern
**Purpose:** Determines whether a string matches a specified regular expression.  
**Parameters:**  
- `value`: The input string.  
- `pattern`: The regular expression pattern to match against.  
**Return:** A `ValidationResult` indicating success when the string satisfies the pattern; otherwise failure.  
**Throws:**  
- `ArgumentNullException` if `value` or `pattern` is `null`.  
- `ArgumentException` if `pattern` is not a valid regular expression.

### ValidateEmail
**Purpose:** Checks that a string looks like a valid e‑mail address.  
**Parameters:**  
- `value`: The string to validate.  
**Return:** A `ValidationResult` indicating success when the string conforms to a typical e‑mail format; otherwise failure.  
**Throws:** `ArgumentNullException` if `value` is `null`.

### ValidateUrl
**Purpose:** Checks that a string looks like a valid URL.  
**Parameters:**  
- `value`: The string to validate.  
**Return:** A `ValidationResult` indicating success when the string conforms to a typical URL format; otherwise failure.  
**Throws:** `ArgumentNullException` if `value` is `null`.

### Combine
**Purpose:** Merges multiple validation results into a single result.  
**Parameters:**  
- `results`: One or more `ValidationResult` instances to combine.  
**Return:** A `ValidationResult` whose `IsValid` property is `true` only if all supplied results are valid; otherwise `false` with an error message that concatenates the individual messages (or returns the first non‑empty message).  
**Throws:** `ArgumentNullException` if `results` is `null` or any element in the array is `null`.

### ThrowIfInvalid
**Purpose:** Throws an exception if a validation result indicates failure.  
**Parameters:**  
- `result`: The `ValidationResult` to inspect.  
**Return:** None.  
**Throws:** `InvalidOperationException` with the message from `result.ErrorMessage` when `result.IsValid` is `false`. Does nothing when the result is valid.

### ValidateCondition
**Purpose:** Creates a validation result based on an arbitrary Boolean condition.  
**Parameters:**  
- `predicate`: A function that returns `true` when the condition is satisfied.  
- `errorMessage`: The message to associate with a failed validation.  
**Return:** A `ValidationResult` indicating success when `predicate()` returns `true`; otherwise failure with `errorMessage`.  
**Throws:** `ArgumentNullException` if `predicate` or `errorMessage` is `null`.

### IsValid
**Purpose:** Gets a value indicating whether the validation operation succeeded.  
**Type:** `bool`  
**Remarks:** Read‑only property set when the `ValidationResult` instance is created.

### ErrorMessage
**Purpose:** Gets the descriptive message explaining why validation failed, or `null` when validation succeeded.  
**Type:** `string?`  
**Remarks:** Read‑only property set when the `ValidationResult` instance is created.

## Usage

```csharp
using DotNetConfigServer.Validation; // namespace containing ValidationExtensions

string email = userInput.Email;
ValidationResult emailResult = ValidationExtensions.ValidateEmail(email);
if (!emailResult.IsValid)
{
    // Handle invalid e‑mail (e.g., show error to user)
    Console.WriteLine(emailResult.ErrorMessage);
}

// Combine several checks for a password
string password = userInput.Password;
var pwdResult = ValidationExtensions.Combine(
    ValidationExtensions.ValidateNotEmpty(password),
    ValidationExtensions.ValidateMinLength(password, 8),
    ValidationExtensions.ValidateMaxLength(password, 64),
    ValidationExtensions.ValidatePattern(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$")
);

ValidationExtensions.ThrowIfInvalid(pwdResult); // throws if any rule fails
```

```csharp
using System;
using DotNetConfigServer.Validation;

int age = GetAgeFromSource();
ValidationResult ageResult = ValidationExtensions.ValidateRange(age, 0, 150);
if (!ageResult.IsValid)
{
    // Log or report the problem
    Logger.Warning("Age validation failed: {Msg}", ageResult.ErrorMessage);
}

// Using ValidateCondition for a custom rule
bool IsPromoCodeValid(string code) => code.Length == 6 && code.All(char.IsLetterOrDigit);
ValidationResult promoResult = ValidationExtensions.ValidateCondition(
    () => IsPromoCodeValid(promoCode),
    "Promo code must be exactly six alphanumeric characters."
);

if (!promoResult.IsValid)
{
    throw new ValidationException(promoResult.ErrorMessage);
}
```

## Notes

- All static validation methods are pure functions; they depend only on their inputs and have no internal state. Consequently, they are thread‑safe and may be called concurrently from multiple threads without synchronization.
- The `ValidationResult` returned by these methods is immutable after creation; its `IsValid` and `ErrorMessage` properties are safe to read from any thread.
- When using `ValidateRange<T>`, the type `T` must implement `System.IComparable<T>`; attempting to use a non‑comparable type will result in a compile‑time error.
- Regular expression validation via `ValidatePattern` does not cache the compiled `Regex` instance; if the same pattern is used repeatedly in high‑frequency scenarios, consider pre‑compiling the pattern outside the validation call.
- The `Combine` method treats a `null` entry in the `results` array as an error and will throw `ArgumentNullException`. To avoid this, filter out null results before calling `Combine`.
- Whitespace‑only strings are considered empty by `ValidateNotEmpty`; if a different notion of emptiness is required (e.g., allowing spaces), preprocess the string accordingly before calling the method.
