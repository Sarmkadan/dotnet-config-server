# ConfigurationExceptionExtensions
The `ConfigurationExceptionExtensions` class provides a set of extension methods and properties for working with `ConfigurationException` instances. It allows developers to easily determine the type of configuration exception that occurred, extract relevant information, and convert exceptions to more specific types. This enables more robust and informative error handling in configuration-related code.

## API
* `public static bool IsConfigurationNotFound`: Returns `true` if the exception is a `ConfigurationNotFoundException`, indicating that the requested configuration was not found.
* `public static bool IsConfigurationKeyNotFound`: Returns `true` if the exception is a `ConfigurationKeyNotFoundException`, indicating that the requested configuration key was not found.
* `public static bool IsEncryptionException`: Returns `true` if the exception is an `EncryptionException`, indicating an issue with encryption or decryption.
* `public static bool IsConfigurationSnapshotNotFound`: Returns `true` if the exception is a `ConfigurationSnapshotNotFoundException`, indicating that the requested configuration snapshot was not found.
* `public static bool IsConfigurationVersionNotFound`: Returns `true` if the exception is a `ConfigurationVersionNotFoundException`, indicating that the requested configuration version was not found.
* `public static bool IsWebhookException`: Returns `true` if the exception is a `WebhookException`, indicating an issue with a webhook.
* `public static bool TryGetConfigurationId(this ConfigurationException exception, [NotNullWhen(true)] out string? configurationId)`: Attempts to extract the configuration ID from the exception. Returns `true` if successful, and sets the `configurationId` parameter to the extracted value.
* `public static bool TryGetConfigurationId`: Overload without the `configurationId` parameter.
* `public static bool TryGetKey(this ConfigurationException exception, [NotNullWhen(true)] out string? key)`: Attempts to extract the key from the exception. Returns `true` if successful, and sets the `key` parameter to the extracted value.
* `public static bool TryGetKeyId`: Overload without the `key` parameter.
* `public static ConfigurationNotFoundException ToConfigurationNotFound`: Converts the exception to a `ConfigurationNotFoundException`.
* `public static ConfigurationKeyNotFoundException ToConfigurationKeyNotFound`: Converts the exception to a `ConfigurationKeyNotFoundException`.
* `public static EncryptionException ToEncryptionException`: Converts the exception to an `EncryptionException`.
* `public static object? GetDetails`: Returns an object containing additional details about the exception.
* `public static IReadOnlyDictionary<string, object?> GetDetailsDictionary`: Returns a dictionary containing additional details about the exception.
* `public static bool HasErrorCode`: Returns `true` if the exception has an error code.

## Usage
```csharp
try
{
    // Attempt to retrieve a configuration
    var configuration = await configServer.GetConfigurationAsync("example-config");
}
catch (ConfigurationException ex)
{
    if (ex.IsConfigurationNotFound)
    {
        Console.WriteLine("Configuration not found.");
    }
    else if (ex.IsConfigurationKeyNotFound)
    {
        Console.WriteLine("Configuration key not found.");
    }
    else
    {
        Console.WriteLine("An error occurred: " + ex.Message);
    }
}

try
{
    // Attempt to retrieve a configuration ID
    if (ConfigurationExceptionExtensions.TryGetConfigurationId(ex, out var configurationId))
    {
        Console.WriteLine("Configuration ID: " + configurationId);
    }
    else
    {
        Console.WriteLine("Failed to retrieve configuration ID.");
    }
}
```

## Notes
When using the `TryGetConfigurationId` and `TryGetKey` methods, be aware that they may return `false` if the exception does not contain the requested information. Additionally, the `GetDetails` and `GetDetailsDictionary` methods may return `null` or an empty dictionary if no additional details are available. The `HasErrorCode` property can be used to determine if an error code is present in the exception. These extension methods are thread-safe, as they only operate on the exception instance and do not modify any shared state. However, the underlying exception instance may not be thread-safe, so caution should be exercised when accessing its properties or methods from multiple threads.
