# DictionaryExtensions

Provides a set of extension methods for `IDictionary<TKey, TValue>` that simplify common dictionary operations such as safe retrieval, conditional insertion, bulk removal, merging, inversion, filtering, projection, and nested key/value access. These methods are designed to reduce boilerplate code when working with dictionaries in configuration and data mapping scenarios.

## API

### `GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)`

- **Purpose**: Attempts to retrieve the value associated with the specified key. If the key is not found, returns the provided `defaultValue` (or `default(TValue)` if none is supplied).
- **Parameters**:
  - `dictionary` – The source dictionary.
  - `key` – The key to look up.
  - `defaultValue` – Optional fallback value.
- **Returns**: The value if the key exists; otherwise `defaultValue`.
- **Throws**: `ArgumentNullException` if `dictionary` or `key` is `null`.

### `AddIfNotExists<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)`

- **Purpose**: Adds the specified key/value pair only if the key does not already exist in the dictionary.
- **Parameters**:
  - `dictionary` – The target dictionary.
  - `key` – The key to add.
  - `value` – The value to associate with the key.
- **Returns**: `true` if the key was added; `false` if the key already existed (no change).
- **Throws**: `ArgumentNullException` if `dictionary` or `key` is `null`.

### `AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)`

- **Purpose**: Adds the key/value pair if the key is absent, or updates the existing value if the key is present.
- **Parameters**:
  - `dictionary` – The target dictionary.
  - `key` – The key to add or update.
  - `value` – The new value.
- **Returns**: Nothing.
- **Throws**: `ArgumentNullException` if `dictionary` or `key` is `null`.

### `RemoveWhere<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)`

- **Purpose**: Removes all entries that satisfy the given predicate.
- **Parameters**:
  - `dictionary` – The source dictionary.
  - `predicate` – A function that evaluates each key/value pair; entries for which it returns `true` are removed.
- **Returns**: The number of entries removed.
- **Throws**: `ArgumentNullException` if `dictionary` or `predicate` is `null`.

### `Merge<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> other, bool overwriteExisting = true)`

- **Purpose**: Copies all entries from `other` into `dictionary`. When `overwriteExisting` is `true` (default), existing keys are overwritten; when `false`, existing keys are preserved.
- **Parameters**:
  - `dictionary` – The target dictionary.
  - `other` – The source dictionary whose entries are to be merged.
  - `overwriteExisting` – Determines whether to replace values for keys that already exist.
- **Returns**: Nothing.
- **Throws**: `ArgumentNullException` if `dictionary` or `other` is `null`.

### `Invert<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)`

- **Purpose**: Creates a new dictionary where the original values become keys and the original keys become values.
- **Parameters**:
  - `dictionary` – The source dictionary.
- **Returns**: A new `Dictionary<TValue, TKey>` with inverted mapping.
- **Throws**: `ArgumentNullException` if `dictionary` is `null`.  
  `ArgumentException` if the source contains duplicate values (because the resulting keys would not be unique).

### `Where<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)`

- **Purpose**: Filters the dictionary entries based on a predicate and returns a new dictionary containing only the matching entries.
- **Parameters**:
  - `dictionary` – The source dictionary.
  - `predicate` – A function that evaluates each key/value pair; entries for which it returns `true` are included.
- **Returns**: A new `Dictionary<TKey, TValue>` with the filtered entries.
- **Throws**: `ArgumentNullException` if `dictionary` or `predicate` is `null`.

### `Select<TKey, TValue, TResult>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, TResult> selector)`

- **Purpose**: Projects each key/value pair into a new value of type `TResult` and returns a new dictionary with the same keys and the projected values.
- **Parameters**:
  - `dictionary` – The source dictionary.
  - `selector` – A transformation function applied to each key/value pair.
- **Returns**: A new `Dictionary<TKey, TResult>` where each value is the result of `selector`.
- **Throws**: `ArgumentNullException` if `dictionary` or `selector` is `null`.

### `Flatten<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, string separator = ".")`

- **Purpose**: Recursively flattens a nested dictionary structure into a single-level dictionary with dot‑separated keys. Nested `IDictionary` objects (or any object implementing `IDictionary<string, object?>`) are expanded.
- **Parameters**:
  - `dictionary` – The source dictionary. Keys must be of type `string` (or convertible to string).
  - `separator` – The character(s) used to join key segments (default `"."`).
- **Returns**: A `Dictionary<string, object?>` where each key is a dot‑delimited path and each value is a leaf value (non‑dictionary).
- **Throws**: `ArgumentNullException` if `dictionary` is `null`.  
  `InvalidOperationException` if a key is `null` or cannot be converted to a string.

### `GetNestedValue<TKey>(this IDictionary<TKey, object?> dictionary, string keyPath, string separator = ".")`

- **Purpose**: Retrieves a value from a nested dictionary using a dot‑separated key path. Intermediate dictionaries are traversed automatically.
- **Parameters**:
  - `dictionary` – The root dictionary. Keys must be of type `string` (or convertible to string).
  - `keyPath` – A dot‑separated path (e.g., `"database.connection"`).
  - `separator` – The separator used in `keyPath` (default `"."`).
- **Returns**: The value at the specified path, or `null` if any segment of the path is missing.
- **Throws**: `ArgumentNullException` if `dictionary` or `keyPath` is `null`.

### `SetNestedValue<TKey>(this IDictionary<TKey, object?> dictionary, string keyPath, object? value, string separator = ".")`

- **Purpose**: Sets a value at a nested path, creating intermediate dictionaries as needed. If a segment already exists and is not a dictionary, it is overwritten.
- **Parameters**:
  - `dictionary` – The root dictionary. Keys must be of type `string` (or convertible to string).
  - `keyPath` – A dot‑separated path (e.g., `"logging.level"`).
  - `value` – The value to assign at the leaf.
  - `separator` – The separator used in `keyPath` (default `"."`).
- **Returns**: Nothing.
- **Throws**: `ArgumentNullException` if `dictionary` or `keyPath` is `null`.

## Usage

### Example 1: Basic dictionary manipulation

```csharp
using System.Collections.Generic;
using DotNetConfigServer.Extensions; // assumed namespace

var config = new Dictionary<string, string>
{
    ["host"] = "localhost",
    ["port"] = "8080"
};

// Safe retrieval with fallback
string host = config.GetValueOrDefault("host", "default-host");
string timeout = config.GetValueOrDefault("timeout", "30");

// Conditional add
bool added = config.AddIfNotExists("timeout", "60"); // false, key already exists

// Add or update
config.AddOrUpdate("port", "9090");

// Remove entries matching a condition
int removed = config.RemoveWhere(kvp => kvp.Key.StartsWith("temp"));

// Merge with another dictionary
var defaults = new Dictionary<string, string> { ["host"] = "127.0.0.1", ["retry"] = "3" };
config.Merge(defaults, overwriteExisting: false); // "host" stays "localhost", "retry" added

// Invert (values become keys – ensure uniqueness)
var inverted = config.Invert(); // throws if duplicate values exist

// Filter
var filtered = config.Where(kvp => kvp.Value.Length > 3);

// Project values
var intValues = config.Select(kvp => int.TryParse(kvp.Value, out var n) ? n : 0);
```

### Example 2: Nested dictionary operations

```csharp
using System.Collections.Generic;
using DotNetConfigServer.Extensions;

var nested = new Dictionary<string, object?>
{
    ["database"] = new Dictionary<string, object?>
    {
        ["connection"] = "Server=.;Database=app",
        ["pool"] = new Dictionary<string, object?>
        {
            ["min"] = 5,
            ["max"] = 20
        }
    },
    ["logging"] = new Dictionary<string, object?>
    {
        ["level"] = "Info"
    }
};

// Flatten to dot‑notation
var flat = nested.Flatten();
// flat["database.connection"] = "Server=.;Database=app"
// flat["database.pool.min"] = 5
// flat["database.pool.max"] = 20
// flat["logging.level"] = "Info"

// Retrieve a nested value
object? conn = nested.GetNestedValue("database.connection"); // "Server=.;Database=app"
object? missing = nested.GetNestedValue("database.timeout"); // null

// Set a nested value (creates intermediate dictionaries)
nested.SetNestedValue("cache.ttl", 300);
// nested now has: ["cache"] = new Dictionary<string, object?> { ["ttl"] = 300 }
```

## Notes

- **Null keys**: All methods throw `ArgumentNullException` if a key parameter is `null`. For `Flatten`, `GetNestedValue`, and `SetNestedValue`, keys are expected to be non‑null strings; behavior is undefined if a key is `null` or cannot be converted to a string.
- **Null values**: `GetValueOrDefault` and `GetNestedValue` may return `null` if the value is `null` or the key is missing. `AddIfNotExists` and `AddOrUpdate` accept `null` values (if `TValue` is nullable).
- **Duplicate values in `Invert`**: Throws `ArgumentException` because the resulting dictionary cannot contain duplicate keys. Ensure source values are unique before calling.
- **Thread safety**: None of these extension methods perform any synchronization. If the dictionary is accessed or modified concurrently, external locking is required.
- **Performance**: `RemoveWhere` iterates the entire dictionary and removes matching entries; for large dictionaries this may be expensive. `Flatten` recursively traverses nested dictionaries and may cause stack overflow on deeply nested structures.
- **Nested dictionary support**: `Flatten`, `GetNestedValue`, and `SetNestedValue` treat any value that implements `IDictionary<string, object?>` as a nested dictionary. Other dictionary types (e.g., `Dictionary<int, object>`) are not expanded.
