# StringExtensions

The `StringExtensions` class provides a collection of static utility methods designed to extend the functionality of the .NET `string` type within the `dotnet-config-server` project. These helpers address common string manipulation requirements such as case conversion, sanitization for file systems and URLs, pattern matching, and validation, ensuring consistent data formatting and safety across configuration handling operations.

## API

### IsNullOrWhiteSpace
Determines whether a specified string is null, empty, or consists only of white-space characters.
*   **Parameters**: `string value` – The string to test.
*   **Returns**: `bool` – `true` if the value is null, empty, or white-space; otherwise, `false`.
*   **Throws**: None.

### Truncate
Shortens a string to a specified maximum length, appending an ellipsis if truncation occurs.
*   **Parameters**: `string value` – The source string; `int maxLength` – The maximum allowed length including the ellipsis.
*   **Returns**: `string` – The truncated string or the original string if it fits within the limit.
*   **Throws**: `ArgumentOutOfRangeException` if `maxLength` is less than the length of the ellipsis or negative.

### ToSafeFileName
Converts a string into a format safe for use as a file name by removing or replacing invalid characters.
*   **Parameters**: `string value` – The source string.
*   **Returns**: `string` – A sanitized string containing only valid file name characters.
*   **Throws**: `ArgumentNullException` if the input is null.

### ToKebabCase
Converts a string from PascalCase, camelCase, or snake_case into kebab-case (lowercase with hyphens).
*   **Parameters**: `string value` – The source string.
*   **Returns**: `string` – The converted kebab-case string.
*   **Throws**: `ArgumentNullException` if the input is null.

### ToPascalCase
Converts a string from kebab-case, snake_case, or camelCase into PascalCase.
*   **Parameters**: `string value` – The source string.
*   **Returns**: `string` – The converted PascalCase string.
*   **Throws**: `ArgumentNullException` if the input is null.

### ToSnakeCase
Converts a string from PascalCase, camelCase, or kebab-case into snake_case (lowercase with underscores).
*   **Parameters**: `string value` – The source string.
*   **Returns**: `string` – The converted snake_case string.
*   **Throws**: `ArgumentNullException` if the input is null.

### MatchesPattern
Evaluates whether a string matches a specific wildcard pattern (supporting `*` and `?`).
*   **Parameters**: `string value` – The string to test; `string pattern` – The wildcard pattern.
*   **Returns**: `bool` – `true` if the string matches the pattern; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if either argument is null.

### RemoveWhitespace
Removes all whitespace characters from a string.
*   **Parameters**: `string value` – The source string.
*   **Returns**: `string` – A new string with all whitespace removed.
*   **Throws**: `ArgumentNullException` if the input is null.

### Repeat
Generates a new string by repeating the input value a specified number of times.
*   **Parameters**: `string value` – The string to repeat; `int count` – The number of repetitions.
*   **Returns**: `string` – The concatenated result.
*   **Throws**: `ArgumentNullException` if `value` is null; `ArgumentOutOfRangeException` if `count` is negative.

### IsValidEmail
Validates whether a string conforms to a standard email address format.
*   **Parameters**: `string value` – The string to validate.
*   **Returns**: `bool` – `true` if the format is valid; otherwise, `false`.
*   **Throws**: None (returns `false` for null or empty inputs).

### UrlEncode
Converts a string into its URL-encoded representation.
*   **Parameters**: `string value` – The string to encode.
*   **Returns**: `string` – The URL-encoded string.
*   **Throws**: `ArgumentNullException` if the input is null.

### UrlDecode
Converts a URL-encoded string back to its decoded representation.
*   **Parameters**: `string value` – The encoded string.
*   **Returns**: `string` – The decoded string.
*   **Throws**: `ArgumentNullException` if the input is null.

### CommonPrefix
Identifies the longest common prefix shared among a set of strings.
*   **Parameters**: `IEnumerable<string> values` – The collection of strings to compare.
*   **Returns**: `string` – The common prefix, or an empty string if none exists.
*   **Throws**: `ArgumentNullException` if the collection or any item within it is null.

## Usage

The following examples demonstrate typical usage scenarios within the configuration server context, such as sanitizing keys for storage and formatting configuration names.

```csharp
using DotNetConfigServer.Extensions;

// Example 1: Sanitizing a user-provided configuration key for file storage
public void SaveConfigSnapshot(string configName, string content)
{
    if (string.IsNullOrWhiteSpace(configName))
    {
        throw new ArgumentException("Config name cannot be empty.");
    }

    // Ensure the filename is safe for the underlying OS
    string safeFileName = configName.ToSafeFileName();
    
    // Truncate if the name exceeds filesystem limits (e.g., 200 chars)
    string finalFileName = safeFileName.Truncate(200);

    File.WriteAllText($"./snapshots/{finalFileName}.json", content);
}
```

```csharp
using DotNetConfigServer.Extensions;
using System.Collections.Generic;

// Example 2: Normalizing environment variable names and validating contact emails
public void ProcessEnvironmentSettings(Dictionary<string, string> settings)
{
    foreach (var kvp in settings)
    {
        // Convert arbitrary keys to snake_case for consistent database mapping
        string normalizedKey = kvp.Key.ToSnakeCase();
        
        // Validate admin email if present
        if (normalizedKey == "admin_email" && !kvp.Value.IsValidEmail())
        {
            Console.WriteLine($"Warning: Invalid email format for key '{kvp.Key}'.");
        }
        
        // Encode value if it needs to be passed in a query string later
        string encodedValue = kvp.Value.UrlEncode();
        
        // Logic to store normalizedKey and encodedValue...
    }
}
```

## Notes

*   **Null Handling**: Most transformation methods (`ToKebabCase`, `UrlEncode`, `RemoveWhitespace`, etc.) explicitly throw `ArgumentNullException` if the input string is null. Validation methods like `IsNullOrWhiteSpace` and `IsValidEmail` handle null inputs gracefully by returning a boolean result.
*   **Thread Safety**: As this class consists entirely of static methods that operate on immutable string instances and do not maintain internal state, all members are inherently thread-safe.
*   **Edge Cases**:
    *   `Truncate` will return the original string if the input length is less than or equal to `maxLength`; it only appends an ellipsis if truncation actually occurs.
    *   `CommonPrefix` returns an empty string if the input collection contains items with no shared starting characters or if the collection contains an empty string.
    *   Case conversion methods assume standard Latin alphabet rules; behavior with non-Latin unicode characters depends on the underlying .NET `char` handling.
