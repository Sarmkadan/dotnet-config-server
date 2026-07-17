# EncryptionKey
The `EncryptionKey` type represents a key used for encryption purposes in the dotnet-config-server project. It encapsulates various properties and behaviors related to the key, such as its identifier, name, algorithm, and usage count. This type is designed to provide a structured way of managing encryption keys, including their creation, activation, deactivation, and usage tracking.

## API
The `EncryptionKey` type has the following public members:
* `Id`: A unique identifier for the encryption key, represented as a `Guid`.
* `Name`: The name of the encryption key, represented as a `string`.
* `KeyId`: The key identifier, represented as a `string`.
* `Algorithm`: The encryption algorithm used by the key, represented as an `EncryptionAlgorithm`.
* `EncryptedKey`: The encrypted key, represented as a `byte[]`.
* `Salt`: The salt used for encryption, represented as a `byte[]`.
* `Description`: An optional description of the encryption key, represented as a `string?`.
* `CreatedAt`: The date and time when the encryption key was created, represented as a `DateTime`.
* `RotatedAt`: The date and time when the encryption key was last rotated, represented as a `DateTime?`.
* `ExpiresAt`: The date and time when the encryption key expires, represented as a `DateTime`.
* `IsActive`: A boolean indicating whether the encryption key is active.
* `IsPrimary`: A boolean indicating whether the encryption key is primary.
* `CreatedBy`: The user who created the encryption key, represented as a `string`.
* `RotatedBy`: The user who last rotated the encryption key, represented as a `string?`.
* `UsageCount`: The number of times the encryption key has been used, represented as an `int`.
* `IsValid`: A boolean indicating whether the encryption key is valid.
* `IsNearExpiration`: A boolean indicating whether the encryption key is near expiration.
* `Deactivate`: A method that deactivates the encryption key.
* `Activate`: A method that activates the encryption key.
* `IncrementUsage`: A method that increments the usage count of the encryption key.

## Usage
Here are two examples of using the `EncryptionKey` type:
```csharp
// Example 1: Creating and activating an encryption key
var encryptionKey = new EncryptionKey
{
    Name = "MyEncryptionKey",
    Algorithm = EncryptionAlgorithm.AES,
    EncryptedKey = new byte[] { /* encrypted key bytes */ },
    Salt = new byte[] { /* salt bytes */ },
    Description = "This is my encryption key"
};
encryptionKey.Activate();

// Example 2: Rotating and deactivating an encryption key
var existingEncryptionKey = GetExistingEncryptionKey();
existingEncryptionKey.RotatedBy = "JohnDoe";
existingEncryptionKey.RotatedAt = DateTime.UtcNow;
existingEncryptionKey.Deactivate();
```

## Notes
When working with `EncryptionKey` instances, consider the following edge cases and thread-safety remarks:
* The `Deactivate` and `Activate` methods modify the `IsActive` property and may throw exceptions if the key is already in the desired state.
* The `IncrementUsage` method increments the `UsageCount` property and may throw exceptions if the key is not active.
* The `IsValid` and `IsNearExpiration` properties are calculated based on the key's properties and may change over time.
* The `EncryptionKey` type is not thread-safe by default, and concurrent access to its properties and methods may result in unexpected behavior. To ensure thread safety, consider using synchronization mechanisms or thread-safe collections.
