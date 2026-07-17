# JsonExtensions

Provides a set of utility methods for serializing objects to JSON, deserializing JSON strings to objects, and manipulating JSON data structures using `System.Text.Json` and `JsonNode`.

## API

### `ToJson<T>(T? obj)`
Serializes the given object to a JSON string.

- **Parameters**
  - `obj` – The object to serialize. May be `null`.
- **Returns**
  - A JSON string, or `null` if `obj` is `null`.
- **Exceptions**
  - `System.Text.Json.JsonException` – Thrown if serialization fails.

---

### `FromJson<T>(string json)`
Deserializes a JSON string into an instance of type `T`.

- **Parameters**
  - `json` – The JSON string to deserialize.
- **Returns**
  - An instance of `T`, or `default` if `json` is `null` or empty.
- **Exceptions**
  - `System.Text.Json.JsonException` – Thrown if deserialization fails.

---

### `TryFromJson<T>(string json, out T? result)`
Attempts to deserialize a JSON string into an instance of type `T`. Populates `result` with the deserialized value if successful.

- **Parameters**
  - `json` – The JSON string to deserialize.
  - `result` – Receives the deserialized value or `default` on failure.
- **Returns**
  - `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**
  - None.

---
### `Merge(JsonObject left, JsonObject right)`
Merges two `JsonObject` instances, combining properties from both. Properties in `right` overwrite those in `left` with the same key.

- **Parameters**
  - `left` – The base JSON object.
  - `right` – The JSON object whose properties override those in `left`.
- **Returns**
  - A new `JsonObject` containing the merged result.
- **Exceptions**
  - None.

---
### `ToJsonObject(object? obj)`
Converts an arbitrary object into a `JsonObject` representation.

- **Parameters**
  - `obj` – The object to convert. May be `null`.
- **Returns**
  - A `JsonObject` if `obj` is non-null; otherwise, `null`.
- **Exceptions**
  - `System.Text.Json.JsonException` – Thrown if conversion fails.

---
### `GetValueByPath(JsonNode root, string path)`
Retrieves a nested value from a `JsonNode` using a dot-separated path (e.g., `"a.b.c"`).

- **Parameters**
  - `root` – The root `JsonNode` to traverse.
  - `path` – The dot-separated path to the desired value.
- **Returns**
  - The value at the specified path, or `null` if the path is invalid or the value does not exist.
- **Exceptions**
  - None.

---
### `IsValidJson(string json)`
Determines whether the provided string is valid JSON.

- **Parameters**
  - `json` – The string to validate.
- **Returns**
  - `true` if the string is valid JSON; otherwise, `false`.
- **Exceptions**
  - None.

---
### `PrettyPrint(string json)`
Formats a JSON string with indentation and line breaks for readability.

- **Parameters**
  - `json` – The JSON string to format.
- **Returns**
  - A pretty-printed JSON string, or `null` if `json` is `null` or empty.
- **Exceptions**
  - `System.Text.Json.JsonException` – Thrown if `json` is malformed.

---
### `Minify(string json)`
Removes all whitespace and formatting from a JSON string to produce a compact representation.

- **Parameters**
  - `json` – The JSON string to minify.
- **Returns**
  - A minified JSON string, or `null` if `json` is `null` or empty.
- **Exceptions**
  - `System.Text.Json.JsonException` – Thrown if `json` is malformed.

---
### `ToJsonDictionary(JsonNode? node)`
Converts a `JsonNode` into a `Dictionary<string, object?>` representation.

- **Parameters**
  - `node` – The `JsonNode` to convert. May be `null`.
- **Returns**
  - A dictionary containing the node’s key-value pairs, or `null` if `node` is `null`.
- **Exceptions**
  - None.

---
### `Extract(JsonObject source, params string[] keys)`
Extracts a subset of properties from a `JsonObject` into a new `Dictionary<string, object?>`.

- **Parameters**
  - `source` – The source `JsonObject`.
  - `keys` – The property names to extract.
- **Returns**
  - A dictionary containing the requested properties, or `null` if `source` is `null`.
- **Exceptions**
  - None.

---
### `RemoveNulls(JsonObject obj)`
Removes all properties with `null` values from a `JsonObject`.

- **Parameters**
  - `obj` – The `JsonObject` to process.
- **Returns**
  - The same `JsonObject` instance with `null` properties removed.
- **Exceptions**
  - None.

## Usage

```csharp
// Example 1: Serialization and deserialization
var settings = new { Theme = "Dark", Notifications = true };
string json = JsonExtensions.ToJson(settings);
var deserialized = JsonExtensions.FromJson<dynamic>(json);
Console.WriteLine(deserialized.Theme); // Output: Dark

// Example 2: Merging JSON objects
var baseConfig = JsonNode.Parse("""{ "host": "localhost", "port": 8080 }""")!.AsObject();
var overrides = JsonNode.Parse("""{ "port": 9000, "ssl": true }""")!.AsObject();
var merged = JsonExtensions.Merge(baseConfig, overrides);
Console.WriteLine(merged["port"]); // Output: 9000
```

## Notes

- **Thread safety**: All methods are stateless and thread-safe when used with immutable inputs. Methods that mutate `JsonObject` instances (e.g., `RemoveNulls`) modify the input directly; callers should ensure exclusive access if the object is shared.
- **Null handling**: Methods that accept `null` inputs generally return `null` without throwing, except where JSON parsing explicitly fails (e.g., `PrettyPrint` on malformed input).
- **Performance**: `PrettyPrint` and `Minify` allocate new strings; prefer caching results when the same JSON is processed repeatedly.
- **Path traversal**: `GetValueByPath` does not validate paths beyond existence; malformed paths return `null` without error.
