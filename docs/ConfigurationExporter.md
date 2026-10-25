# ConfigurationExporter

The `ConfigurationExporter` class (`DotnetConfigServer.Formatters`) is a static utility that serializes `Configuration` and `ConfigurationKey` objects into JSON, CSV, XML, YAML, and `.env` text. Use it for interoperability with external systems, debugging, and backups.

Every format has two kinds of entry point:

- **`Export*` methods** return a `string`. They take an `IEnumerable<T>` and materialize the whole payload in memory.
- **`Write*` methods** write to a `TextWriter` (CSV, XML, YAML, ENV) or a `Stream` (JSON, async). They avoid building one large string. The JSON write methods are the only ones that are async.

## Supported Formats

| Format | Configurations | Keys | Notes |
|--------|----------------|------|-------|
| JSON | `ExportAsJson`, `WriteAsJsonAsync` | `ExportKeysAsJson`, `WriteKeysAsJsonAsync` | camelCase properties. `WriteAsJsonAsync` is the streaming variant. |
| CSV | `ExportAsCsv`, `WriteAsCsv` | `ExportKeysAsCsv`, `WriteKeysAsCsv` | Header row followed by one row per item. |
| XML | `ExportAsXml`, `WriteAsXml` | `ExportKeysAsXml`, `WriteKeysAsXml` | Element-based; values are escaped. |
| YAML | `ExportAsYaml`, `WriteAsYaml` | `ExportKeysAsYaml`, `WriteKeysAsYaml` | A top-level sequence of `- Id:` mappings. |
| ENV | not supported | `ExportAsEnvFormat`, `WriteAsEnvFormat` | `KEY=VALUE` lines. `Configuration` has no value, so only keys can be exported. |

Helper: `ToAsyncEnumerable<T>` adapts an `IEnumerable<T>` to `IAsyncEnumerable<T>` for the streaming JSON methods.

## Fields

The field sets differ between formats. Check the table before parsing an export.

**Configuration** (JSON, XML, YAML):
`Id`, `ApplicationId`, `Name`, `Description`, `Environment`, `IsActive`, `IsEncrypted`, `CreatedAt`, `UpdatedAt`, `CreatedBy`

**Configuration** (CSV): the same fields without `UpdatedAt`.

**ConfigurationKey** (JSON): `id`, `key`, `value`, `description`, `isEncrypted`, `isActive`, `createdAt`, `updatedAt`

**ConfigurationKey** (CSV, XML, YAML): `Id`, `ConfigurationId`, `Key` (`KeyName` in XML), `Value`, `Description`, `IsEncrypted`, `IsActive`, `CreatedAt`

**ConfigurationKey** (ENV): `KEY=VALUE` only. Metadata is not exported.

## API

### JSON

#### `public static string ExportAsJson(IEnumerable<Configuration> configurations, bool pretty = true)`
Exports configurations as a JSON array, using camelCase property names.

- **Parameters**: `pretty` enables indented output.
- **Returns**: A JSON array string. An empty input produces `[]`.
- **Note**: `environment` is written as the enum's numeric value (default `System.Text.Json` enum handling). `WriteAsJsonAsync` writes the name instead.
- **Throws**: `ArgumentNullException` if `configurations` is null.

---

#### `public static string ExportKeysAsJson(IEnumerable<ConfigurationKey> keys, bool pretty = true)`
Exports keys, including their values, as a JSON array.

- **Returns**: A JSON array string.
- **Throws**: `ArgumentNullException` if `keys` is null.

---

#### `public static Task WriteAsJsonAsync(Stream destination, IAsyncEnumerable<Configuration> configurations, bool pretty = true, CancellationToken cancellationToken = default)`
Streams configurations as a JSON array to `destination` through a `Utf8JsonWriter`. The writer flushes after each item, so the full output is never held in memory.

- **Parameters**: `destination` is the target stream. `pretty` enables indented output. `cancellationToken` is checked while iterating.
- **Returns**: A `Task` that completes when the array is written.
- **Throws**: `ArgumentNullException` (on the returned `Task`) if `destination` or `configurations` is null. `OperationCanceledException` if cancellation is requested.

---

#### `public static Task WriteKeysAsJsonAsync(Stream destination, IAsyncEnumerable<ConfigurationKey> keys, bool pretty = true, CancellationToken cancellationToken = default)`
Streaming counterpart of `ExportKeysAsJson`. Same parameters and exceptions as `WriteAsJsonAsync`.

---

#### `public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source, CancellationToken cancellationToken = default)`
Adapts a synchronous sequence for the streaming methods. It yields each item and calls `Task.Yield()` between items.

- **Throws**: `ArgumentNullException` when enumeration starts (the method is an iterator, so the check does not run when it is called). `OperationCanceledException` if cancellation is requested between items.

### CSV

#### `public static string ExportAsCsv(IEnumerable<Configuration> configurations)`
Returns a header row (`Id,ApplicationId,Name,Description,Environment,IsActive,IsEncrypted,CreatedAt,CreatedBy`) followed by one row per configuration. Null `Description` becomes an empty field. `CreatedAt` is written in round-trip (`O`) format.

- **Escaping**: A field is wrapped in double quotes when it contains a comma, a double quote, or a line feed. Embedded quotes are doubled. Carriage returns are not quoted, so this is a subset of RFC 4180.
- **Throws**: `ArgumentNullException` if `configurations` is null.

---

#### `public static void WriteAsCsv(TextWriter writer, IEnumerable<Configuration> configurations)`
Same output as `ExportAsCsv`, written line by line to `writer`.

- **Throws**: `ArgumentNullException` if either argument is null.

---

#### `public static string ExportKeysAsCsv(IEnumerable<ConfigurationKey> keys)`
Returns a header row (`Id,ConfigurationId,Key,Value,Description,IsEncrypted,IsActive,CreatedAt`) followed by one row per key. Escaping follows the same rules as `ExportAsCsv`.

- **Throws**: `ArgumentNullException` if `keys` is null.

---

#### `public static void WriteKeysAsCsv(TextWriter writer, IEnumerable<ConfigurationKey> keys)`
Streaming form of `ExportKeysAsCsv`.

- **Throws**: `ArgumentNullException` if either argument is null.

### XML

#### `public static string ExportAsXml(IEnumerable<Configuration> configurations)`
Returns a `<Configurations>` root with one `<Configuration>` child per item. Each field is a child element; the output has no attributes. Text values are passed through `SecurityElement.Escape`. Ids, booleans, and dates are written as-is.

- **Empty input**: returns `<Configurations />`.
- **Throws**: `ArgumentNullException` if `configurations` is null.

---

#### `public static void WriteAsXml(TextWriter writer, IEnumerable<Configuration> configurations)`
Streaming form of `ExportAsXml`. The sequence is enumerated twice (once for an emptiness check, once for output). Pass a materialized collection such as a `List<T>` when the source is lazy.

- **Throws**: `ArgumentNullException` if either argument is null.

---

#### `public static string ExportKeysAsXml(IEnumerable<ConfigurationKey> keys)`
Returns a `<ConfigurationKeys>` root with one `<Key>` child per key. Child elements are `Id`, `ConfigurationId`, `KeyName`, `Value`, `Description`, `IsEncrypted`, `IsActive`, and `CreatedAt`. Empty input produces an empty `<ConfigurationKeys>` root.

- **Throws**: `ArgumentNullException` if `keys` is null.

---

#### `public static void WriteKeysAsXml(TextWriter writer, IEnumerable<ConfigurationKey> keys)`
Streaming form of `ExportKeysAsXml`.

- **Throws**: `ArgumentNullException` if either argument is null.

### YAML

#### `public static string ExportAsYaml(IEnumerable<Configuration> configurations)`
Returns a YAML sequence. Each item starts with `- Id:`, and the remaining fields are indented by two spaces. Booleans are written in lowercase. `CreatedAt` and `UpdatedAt` use round-trip (`O`) format. Null values are written as `null`, and empty strings as `""`.

- **Escaping**: Values containing YAML-significant characters (`:`, `#`, `{`, `}`, `[`, `]`, `,`, `&`, `*`, `?`, `|`, `>`, quotes, line breaks, or leading/trailing spaces) are wrapped in double quotes with `"` escaped. Backslashes and line breaks are not escaped.
- **Throws**: `ArgumentNullException` if `configurations` is null.

---

#### `public static void WriteAsYaml(TextWriter writer, IEnumerable<Configuration> configurations)`
Streaming form of `ExportAsYaml`.

- **Throws**: `ArgumentNullException` if either argument is null.

---

#### `public static string ExportKeysAsYaml(IEnumerable<ConfigurationKey> keys)`
Returns a YAML sequence of keys with `Id`, `ConfigurationId`, `Key`, `Value`, `Description`, `IsEncrypted`, `IsActive`, and `CreatedAt`. Escaping follows the same rules as `ExportAsYaml`.

- **Throws**: `ArgumentNullException` if `keys` is null.

---

#### `public static void WriteKeysAsYaml(TextWriter writer, IEnumerable<ConfigurationKey> keys)`
Streaming form of `ExportKeysAsYaml`.

- **Throws**: `ArgumentNullException` if either argument is null.

### ENV

#### `public static string ExportAsEnvFormat(IEnumerable<ConfigurationKey> keys)`
Returns one `KEY=VALUE` line per key, suitable for a `.env` file.

- **Key mapping**: The `:` hierarchy separator is replaced with `__`, so `Database:ConnectionString` becomes `Database__ConnectionString`. This matches the ASP.NET environment variable configuration provider.
- **Value quoting**: A value is wrapped in double quotes only when it has leading or trailing whitespace, or contains a space, tab, CR, LF, `"`, `'`, `\`, `#`, `$`, or a backtick. Inside quotes, `\`, `"`, `$`, CR, and LF are escaped. Single quotes and backticks are not escaped inside quotes.
- **Null or empty values**: written as `KEY=`.
- **Throws**: `ArgumentNullException` if `keys` is null.

---

#### `public static void WriteAsEnvFormat(TextWriter writer, IEnumerable<ConfigurationKey> keys)`
Streaming form of `ExportAsEnvFormat`.

- **Throws**: `ArgumentNullException` if either argument is null.

## Usage

### Example 1: Exporting configurations to JSON

```csharp
using DotnetConfigServer.Formatters;

// Compact JSON string for an HTTP response or a log entry
string json = ConfigurationExporter.ExportAsJson(configurations, pretty: false);
```

### Example 2: Streaming keys to a file

```csharp
using DotnetConfigServer.Formatters;

await using var stream = File.Create("keys.json");
await ConfigurationExporter.WriteKeysAsJsonAsync(
    stream,
    ConfigurationExporter.ToAsyncEnumerable(keys),
    cancellationToken: cancellationToken);
```

### Example 3: Writing CSV without building a string

```csharp
using DotnetConfigServer.Formatters;

using var writer = new StreamWriter("configurations.csv");
ConfigurationExporter.WriteAsCsv(writer, configurations);
```

### Example 4: Generating a `.env` file from keys

```csharp
using DotnetConfigServer.Formatters;

File.WriteAllText(".env", ConfigurationExporter.ExportAsEnvFormat(keys));
```
