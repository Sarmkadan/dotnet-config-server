# ConfigurationKey
The `ConfigurationKey` type represents a single configuration key in the dotnet-config-server project. It encapsulates various properties of the key, including its identifier, name, value, and metadata such as creation and update timestamps. This type is essential for managing and storing configuration data in the system.

## API
The `ConfigurationKey` type has the following public members:
* `Id`: A unique identifier of type `Guid` that distinguishes the configuration key.
* `Key`: A string representing the name of the configuration key.
* `Value`: A string containing the value associated with the configuration key.
* `DefaultValue`: A nullable string that stores the default value of the configuration key, if any.
* `Description`: A nullable string providing a brief description of the configuration key.
* `ValueType`: An enumeration of type `ConfigurationValueType` that indicates the data type of the configuration key's value.
* `ConfigurationId`: A `Guid` identifier referencing the configuration that this key belongs to.
* `VersionId`: A `Guid` identifier referencing the version of the configuration that this key is associated with.
* `CreatedAt`: A `DateTime` object representing the timestamp when the configuration key was created.
* `UpdatedAt`: A `DateTime` object representing the timestamp when the configuration key was last updated.
* `DeletedAt`: A nullable `DateTime` object representing the timestamp when the configuration key was deleted, if applicable.
* `CreatedBy`: A string indicating the user or entity that created the configuration key.
* `UpdatedBy`: A nullable string indicating the user or entity that last updated the configuration key, if any.
* `IsActive`: A boolean flag indicating whether the configuration key is currently active.
* `IsEncrypted`: A boolean flag indicating whether the configuration key's value is encrypted.
* `IsRequired`: A boolean flag indicating whether the configuration key is mandatory.
* `IsSensitive`: A boolean flag indicating whether the configuration key's value is sensitive information.
* `ValidationRegex`: A nullable string containing a regular expression pattern for validating the configuration key's value, if any.
* `MinLength` and `MaxLength`: Nullable integers specifying the minimum and maximum allowed lengths for the configuration key's value, if applicable.

## Usage
Here are two examples demonstrating how to use the `ConfigurationKey` type in C#:
```csharp
// Example 1: Creating a new configuration key
var newKey = new ConfigurationKey
{
    Id = Guid.NewGuid(),
    Key = "MyConfigKey",
    Value = "MyConfigValue",
    DefaultValue = "DefaultVal",
    Description = "This is my config key",
    ValueType = ConfigurationValueType.String,
    ConfigurationId = Guid.NewGuid(),
    VersionId = Guid.NewGuid(),
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    CreatedBy = "JohnDoe",
    IsActive = true,
    IsEncrypted = false,
    IsRequired = true,
    IsSensitive = false,
    ValidationRegex = @"^[a-zA-Z0-9]+$",
    MinLength = 5,
    MaxLength = 10
};

// Example 2: Updating an existing configuration key
var existingKey = new ConfigurationKey
{
    Id = Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"),
    Key = "ExistingConfigKey",
    Value = "NewValue",
    UpdatedAt = DateTime.UtcNow,
    UpdatedBy = "JaneDoe"
};
```

## Notes
When working with `ConfigurationKey` instances, consider the following edge cases and thread-safety remarks:
* The `Id` property must be unique across all configuration keys in the system.
* The `ConfigurationId` and `VersionId` properties establish relationships between configuration keys and their respective configurations and versions.
* The `CreatedAt`, `UpdatedAt`, and `DeletedAt` properties are used for auditing and tracking changes to configuration keys.
* The `IsActive` flag determines whether a configuration key is currently in use.
* The `IsEncrypted` and `IsSensitive` flags indicate the security and sensitivity of the configuration key's value.
* The `ValidationRegex`, `MinLength`, and `MaxLength` properties are used to enforce validation rules on the configuration key's value.
* When updating a `ConfigurationKey` instance, ensure that the `UpdatedAt` property is set to the current timestamp and the `UpdatedBy` property is set to the user or entity performing the update.
* In a multi-threaded environment, consider using synchronization mechanisms to ensure thread safety when accessing and modifying `ConfigurationKey` instances.
