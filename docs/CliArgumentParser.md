# CliArgumentParser
The `CliArgumentParser` class is designed to parse command-line arguments and provide a simple way to access their values. It allows for easy retrieval of argument values, flags, and other configuration settings, making it a useful tool for building command-line interfaces.

## API
* `public CliArgumentParser`: The constructor for the `CliArgumentParser` class, used to create a new instance.
* `public string? GetValue`: Retrieves the value of a command-line argument. Returns `null` if the argument is not present.
* `public string GetValue`: Retrieves the value of a required command-line argument. Throws an exception if the argument is not present.
* `public bool HasFlag`: Checks if a command-line flag is present.
* `public int? GetIntValue`: Retrieves the integer value of a command-line argument. Returns `null` if the argument is not present or cannot be parsed as an integer.
* `public bool GetBoolValue`: Retrieves the boolean value of a command-line argument.
* `public static string GetHelpText`: Returns a help text string for the command-line arguments.
* `public bool ValidateRequired`: Validates that all required command-line arguments are present.
* `public int Port`: Gets the port number from the command-line arguments.
* `public string Environment`: Gets the environment from the command-line arguments.
* `public string? DatabaseConnection`: Gets the database connection string from the command-line arguments.
* `public string LogLevel`: Gets the log level from the command-line arguments.
* `public bool EnableSwagger`: Gets a value indicating whether Swagger is enabled.
* `public bool InitializeDatabase`: Gets a value indicating whether the database should be initialized.
* `public static CliConfig FromParser`: Creates a new `CliConfig` instance from a `CliArgumentParser` instance.

## Usage
```csharp
// Example 1: Parsing command-line arguments
var parser = new CliArgumentParser();
if (parser.ValidateRequired)
{
    Console.WriteLine($"Port: {parser.Port}");
    Console.WriteLine($"Environment: {parser.Environment}");
    Console.WriteLine($"Database Connection: {parser.DatabaseConnection}");
}
else
{
    Console.WriteLine("Required arguments are missing.");
}

// Example 2: Using the CliArgumentParser to configure a service
var parser = new CliArgumentParser();
var config = CliConfig.FromParser(parser);
var service = new MyService(config);
service.Start();
```

## Notes
The `CliArgumentParser` class is designed to be used in a single-threaded environment, as it does not provide any thread-safety guarantees. When using the `GetValue` method to retrieve a required argument, be prepared to handle exceptions if the argument is not present. The `GetIntValue` method will return `null` if the argument is not present or cannot be parsed as an integer, so be sure to check for `null` before attempting to use the value. The `GetHelpText` method can be used to generate a help text string for the command-line arguments, which can be useful for displaying usage information to the user.
