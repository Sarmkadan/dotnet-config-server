#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Utilities;

/// <summary>
/// Parser for command-line arguments.
/// Provides utilities for parsing and validating CLI arguments.
/// </summary>
public sealed class CliArgumentParser
{
    private readonly Dictionary<string, string> _arguments;
    private readonly ILogger<CliArgumentParser> _logger;

    public CliArgumentParser(string[] args, ILogger<CliArgumentParser> logger)
    {
        _logger = logger;
        _arguments = ParseArguments(args);
    }

    /// <summary>
    /// Gets an argument value by key.
    /// </summary>
    public string? GetValue(string key)
    {
        return _arguments.TryGetValue(NormalizeKey(key), out var value) ? value : null;
    }

    /// <summary>
    /// Gets an argument value by key with a default fallback.
    /// </summary>
    public string GetValue(string key, string defaultValue)
    {
        return GetValue(key) ?? defaultValue;
    }

    /// <summary>
    /// Checks if an argument flag exists.
    /// </summary>
    public bool HasFlag(string flag)
    {
        return _arguments.ContainsKey(NormalizeKey(flag));
    }

    /// <summary>
    /// Gets an argument value as an integer.
    /// </summary>
    public int? GetIntValue(string key)
    {
        var value = GetValue(key);
        return int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>
    /// Gets an argument value as a boolean.
    /// </summary>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        var value = GetValue(key);
        return value is null ? defaultValue : bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets help text with available options.
    /// </summary>
    public static string GetHelpText()
    {
        return @"
Usage: dotnet-config-server [options]

Options:
  --port <port>              Port to run server on (default: 5000)
  --environment <env>        Environment (Development, Production)
  --database <connection>    Database connection string
  --log-level <level>        Logging level (Debug, Information, Warning, Error)
  --enable-swagger          Enable Swagger UI
  --init-db                  Initialize database on startup
  --help                     Show this help text
  --version                  Show version information

Examples:
  dotnet run --port 8080
  dotnet run --environment Production --log-level Warning
  dotnet run --enable-swagger --port 3000
";
    }

    /// <summary>
    /// Validates required arguments.
    /// </summary>
    public bool ValidateRequired(params string[] requiredArgs)
    {
        var missing = requiredArgs.Where(arg => !HasFlag(arg) && GetValue(arg) is null).ToList();

        if (missing.Count > 0)
        {
            _logger.LogError("Missing required arguments: {Args}", string.Join(", ", missing));
            return false;
        }

        return true;
    }

    private Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--"))
            {
                var key = NormalizeKey(arg);

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    result[key] = args[++i];
                }
                else
                {
                    result[key] = "true";
                }
            }
        }

        return result;
    }

    private static string NormalizeKey(string key)
    {
        return key.TrimStart('-').ToLowerInvariant();
    }
}

/// <summary>
/// Represents parsed CLI arguments configuration.
/// </summary>
public sealed class CliConfig
{
    public int Port { get; set; } = 5000;
    public string Environment { get; set; } = "Development";
    public string? DatabaseConnection { get; set; }
    public string LogLevel { get; set; } = "Information";
    public bool EnableSwagger { get; set; } = true;
    public bool InitializeDatabase { get; set; } = false;

    /// <summary>
    /// Creates a config from parsed arguments.
    /// </summary>
    public static CliConfig FromParser(CliArgumentParser parser)
    {
        return new CliConfig
        {
            Port = parser.GetIntValue("port") ?? 5000,
            Environment = parser.GetValue("environment", "Development"),
            DatabaseConnection = parser.GetValue("database"),
            LogLevel = parser.GetValue("log-level", "Information"),
            EnableSwagger = parser.GetBoolValue("enable-swagger", true),
            InitializeDatabase = parser.HasFlag("init-db")
        };
    }
}
