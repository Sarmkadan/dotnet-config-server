# CliArgumentParser

The `CliArgumentParser` class parses `--key value` style command-line arguments into a case-insensitive lookup table and exposes typed accessors for them. It is defined in `Utilities/CliArgumentParser.cs` under the `DotnetConfigServer.Utilities` namespace, alongside the `CliConfig` class that maps parsed arguments onto the server's startup settings. The parser supports retrieving raw and typed values, checking for flags, validating required arguments, and generating a help text listing the supported options.

## API

### Constructor
Creates a parser from the raw argument array and parses it immediately.
*   **Parameters**: `string[] args` – The command-line arguments, typically the `args` passed to `Main` or the top-level program; `ILogger<CliArgumentParser> logger` – The logger used for diagnostics and validation errors.
*   **Returns**: A new `CliArgumentParser` instance.
*   **Throws**: `ArgumentNullException` if either `args` or `logger` is null.

### GetValue (single argument)
Gets the raw string value of an argument.
*   **Parameters**: `string key` – The argument name, with or without leading dashes (`"port"` and `"--port"` are equivalent).
*   **Returns**: `string?` – The value, `"true"` if the key was given without a value, or `null` if the key is absent.
*   **Throws**: `ArgumentNullException` if `key` is null; `ArgumentException` if `key` is empty.

### GetValue (with default)
Gets the raw string value of an argument, falling back to a default when the argument is absent.
*   **Parameters**: `string key` – The argument name; `string defaultValue` – The value returned when the argument is absent. It must not be empty.
*   **Returns**: `string` – The argument value, or `defaultValue` if the argument is absent.
*   **Throws**: `ArgumentNullException` if `key` or `defaultValue` is null; `ArgumentException` if `key` or `defaultValue` is empty.

### HasFlag
Checks whether an argument was supplied at all, whether or not it has a value.
*   **Parameters**: `string flag` – The argument name.
*   **Returns**: `bool` – `true` if the argument is present; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `flag` is null; `ArgumentException` if `flag` is empty.

### GetIntValue
Gets an argument value parsed as a 32-bit integer.
*   **Parameters**: `string key` – The argument name.
*   **Returns**: `int?` – The parsed value, or `null` if the argument is absent or is not a valid integer.
*   **Throws**: `ArgumentNullException` if `key` is null; `ArgumentException` if `key` is empty.

Parsing uses `NumberStyles.Integer` and `CultureInfo.InvariantCulture`, so leading and trailing white space and a leading sign are accepted, and the result does not depend on the current culture.

### GetBoolValue
Gets an argument value parsed as a boolean.
*   **Parameters**: `string key` – The argument name; `bool defaultValue` – The value returned when the argument is absent or unparseable. Defaults to `false`.
*   **Returns**: `bool` – The parsed value, or `defaultValue` if the argument is absent or its value is not a recognized boolean literal.
*   **Throws**: `ArgumentNullException` if `key` is null; `ArgumentException` if `key` is empty.

A flag given without a value (for example `--enable-swagger`) is stored as `"true"` and therefore parses as `true`.

### GetHelpText
Returns a fixed usage message describing the options the server accepts.
*   **Parameters**: None.
*   **Returns**: `string` – The help text, beginning with a usage line and followed by an options list and examples.
*   **Throws**: None.

This method is `static` and does not depend on the parsed arguments, so it can be called before a parser exists, for example to print help on `--help`.

### ValidateRequired
Checks that every named argument is present and logs an error listing any that are missing.
*   **Parameters**: `params string[] requiredArgs` – The argument names that must be supplied. An empty list always validates.
*   **Returns**: `bool` – `true` if all required arguments are present; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `requiredArgs` is null or contains a null element; `ArgumentException` if an element is empty.

When validation fails, the method writes an `Error` log entry of the form `Missing required arguments: port, environment` through the injected logger. It does not throw, so the caller decides how to exit.

### ToString
Returns a summary of the main settings with their defaults applied.
*   **Parameters**: None.
*   **Returns**: `string` – A summary such as `CliArgumentParser { Port = 5000, Environment = Development, ... }`.
*   **Throws**: None.

### CliConfig.FromParser
Builds a `CliConfig` from a parser. This method lives on `CliConfig`, but it is the intended bridge from parsed arguments to typed settings.
*   **Parameters**: `CliArgumentParser parser` – The parser to read from.
*   **Returns**: `CliConfig` – A new configuration object with values taken from the arguments and defaults for any that are missing or invalid.
*   **Throws**: `ArgumentNullException` if `parser` is null.

`CliConfig` exposes the following properties, all settable:

| Property | Type | Argument | Default |
|----------|------|----------|---------|
| `Port` | `int` | `--port` | `5000` |
| `Environment` | `string` | `--environment` | `Development` |
| `DatabaseConnection` | `string?` | `--database` | `null` |
| `LogLevel` | `string` | `--log-level` | `Information` |
| `EnableSwagger` | `bool` | `--enable-swagger` | `true` |
| `InitializeDatabase` | `bool` | `--init-db` | `false` |

## Usage

The following examples show typical startup code. The parser is normally created once in `Program.cs` and the logger comes from the host or a logger factory.

```csharp
using DotnetConfigServer.Utilities;
using Microsoft.Extensions.Logging;

// Example 1: Handling --help before anything else
var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<CliArgumentParser>();

if (args.Contains("--help"))
{
    Console.WriteLine(CliArgumentParser.GetHelpText());
    return;
}

var parser = new CliArgumentParser(args, logger);
```

```csharp
using DotnetConfigServer.Utilities;
using Microsoft.Extensions.Logging;

// Example 2: Reading typed values with defaults
// Command line: --port 8080 --environment Production --enable-swagger
var parser = new CliArgumentParser(args, logger);

int port = parser.GetIntValue("port") ?? 5000;                  // 8080
string environment = parser.GetValue("environment", "Development"); // "Production"
string? database = parser.GetValue("database");                 // null when not supplied
bool swagger = parser.GetBoolValue("enable-swagger", true);     // true
bool initDb = parser.HasFlag("init-db");                        // false
```

```csharp
using DotnetConfigServer.Utilities;
using Microsoft.Extensions.Logging;

// Example 3: Validating required arguments and failing fast
var parser = new CliArgumentParser(args, logger);

if (!parser.ValidateRequired("database"))
{
    // An Error entry has already been logged listing the missing arguments
    Environment.Exit(1);
}
```

```csharp
using DotnetConfigServer.Utilities;
using Microsoft.Extensions.Logging;

// Example 4: Building the startup configuration in one step
var parser = new CliArgumentParser(args, logger);
var cliConfig = CliConfig.FromParser(parser);

Console.WriteLine($"Listening on port {cliConfig.Port} ({cliConfig.Environment})");
```

## Notes

*   **Argument Syntax**: Only tokens beginning with `--` are treated as argument names. A token that follows a name and does not start with `--` becomes that name's value, so `--port 8080` stores `8080` under `port`. Tokens that are not preceded by a name, and single-dash tokens such as `-p` that are not consumed as a value, are ignored.
*   **Equals Form Not Supported**: The `--key=value` form is not parsed. `--port=8080` is stored as the name `port=8080` with the value `"true"`, so use the space-separated form instead.
*   **Values That Look Like Names**: A value cannot begin with `--`, because that token is read as the next name. Values beginning with a single dash, such as negative numbers (`--offset -5`), are accepted.
*   **Flags Without Values**: A name followed by another name, or by nothing, is stored as `"true"`. Use `HasFlag` to test presence and `GetBoolValue` to test truthiness.
*   **Unparseable Values**: A value that is not a valid integer or boolean is not an error. `GetIntValue` returns `null` and `GetBoolValue` returns its `defaultValue`. Check the result if a bad value must be reported.
*   **Duplicate Names**: If a name appears more than once, the last occurrence wins.
*   **Name Matching**: Names are matched case-insensitively, and leading dashes are ignored on lookup, so `--Log-Level`, `log-level`, and `-LOG-LEVEL` all refer to the same argument.
*   **Immutability**: All arguments are parsed in the constructor and the internal table is never modified afterwards. Concurrent reads from multiple threads are therefore safe. The logger is used only for the constructor's information entry and for `ValidateRequired` errors.
*   **Help Text**: `GetHelpText` returns a hard-coded string. Update it by hand when options are added or removed, because it is not generated from the parser's state.
