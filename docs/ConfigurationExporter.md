# ConfigurationExporter

The `ConfigurationExporter` class provides static utility methods for exporting configuration data from `dotnet-config-server` into various standardized formats. These methods facilitate serialization of configuration key-value pairs into JSON, CSV, XML, and environment-variable formats, enabling interoperability with external systems, debugging, or backup purposes.

## API

### `public static string ExportAsJson`
Exports the entire configuration as a JSON-formatted string. The output includes all key-value pairs in a hierarchical structure if applicable.

- **Returns**: A JSON string representing the configuration.
- **Throws**:
  - `InvalidOperationException` if the configuration cannot be serialized to JSON (e.g., due to circular references or unsupported types).

---

### `public static string ExportKeysAsJson`
Exports only the configuration keys (without values) as a JSON-formatted array of strings.

- **Returns**: A JSON array string containing configuration keys.
- **Throws**:
  - `InvalidOperationException` if serialization fails.

---

### `public static string ExportAsCsv`
Exports the configuration as a CSV-formatted string. Each line represents a key-value pair, with keys and values separated by a comma. Values containing commas or newlines are escaped according to RFC 4180.

- **Returns**: A CSV string with key-value pairs.
- **Throws**:
  - `InvalidOperationException` if the configuration cannot be converted to CSV (e.g., due to malformed data).

---

### `public static string ExportKeysAsCsv`
Exports only the configuration keys as a CSV-formatted string. Each line contains a single key.

- **Returns**: A CSV string with configuration keys.
- **Throws**:
  - `InvalidOperationException` if serialization fails.

---

### `public static string ExportAsXml`
Exports the configuration as an XML-formatted string. Each key-value pair is represented as an element with attributes or nested elements, depending on the configuration structure.

- **Returns**: An XML string representing the configuration.
- **Throws**:
  - `InvalidOperationException` if the configuration cannot be serialized to XML (e.g., due to invalid characters in keys or unsupported value types).

---

### `public static string ExportKeysAsXml`
Exports only the configuration keys as an XML-formatted string. Keys are listed as elements within a root node.

- **Returns**: An XML string containing configuration keys.
- **Throws**:
  - `InvalidOperationException` if serialization fails.

---

### `public static string ExportAsEnvFormat`
Exports the configuration in a format compatible with environment variable files (e.g., `.env`). Each line follows the pattern `KEY=VALUE`, with values escaped if they contain special characters.

- **Returns**: A string formatted for environment variable files.
- **Throws**:
  - `InvalidOperationException` if the configuration cannot be converted to the environment format (e.g., keys contain invalid characters).

## Usage

### Example 1: Exporting Configuration to JSON
