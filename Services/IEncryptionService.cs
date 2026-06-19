#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for encryption operations
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a value using the specified encryption key
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted text.</returns>
    string Encrypt(string plainText, EncryptionKey key);

    /// <summary>
    /// Decrypts a value using the specified encryption key
    /// </summary>
    /// <param name="cipherText">The encrypted text.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The decrypted text.</returns>
    string Decrypt(string cipherText, EncryptionKey key);

    /// <summary>
    /// Encrypts a value using the primary encryption key
    /// </summary>
    /// <param name="plainText">The text to encrypt.</param>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <returns>The encrypted text.</returns>
    Task<string> EncryptAsync(string plainText, Guid configurationId);

    /// <summary>
    /// Decrypts a value using the appropriate encryption key
    /// </summary>
    /// <param name="cipherText">The encrypted text.</param>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <returns>The decrypted text.</returns>
    Task<string> DecryptAsync(string cipherText, Guid configurationId);

    /// <summary>
    /// Validates if a key is suitable for encryption
    /// </summary>
    /// <param name="key">The encryption key to validate.</param>
    /// <returns>True if the key is valid, otherwise false.</returns>
    bool ValidateKey(EncryptionKey key);

    /// <summary>
    /// Gets the primary encryption key for a configuration
    /// </summary>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <returns>The primary encryption key if found, otherwise null.</returns>
    Task<EncryptionKey?> GetPrimaryKeyAsync(Guid configurationId);

    /// <summary>
    /// Gets an encryption key by ID
    /// </summary>
    /// <param name="keyId">The ID of the key.</param>
    /// <returns>The encryption key if found, otherwise null.</returns>
    Task<EncryptionKey?> GetKeyAsync(string keyId);

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="algorithm">The encryption algorithm.</param>
    /// <returns>The generated encryption key.</returns>
    EncryptionKey GenerateNewKey(string name, string algorithm = "AES256");

    /// <summary>
    /// Rotates an encryption key
    /// </summary>
    /// <param name="oldKeyId">The ID of the key to rotate.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    Task RotateKeyAsync(string oldKeyId, string userId);

    /// <summary>
    /// Re-encrypts all configuration key values with the current primary key.
    /// Call after a key rotation to migrate stored ciphertext to the new key.
    /// </summary>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <param name="keys">The configuration keys to re-encrypt.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    Task ReEncryptConfigurationAsync(Guid configurationId, IEnumerable<ConfigurationKey> keys, string userId);
}
