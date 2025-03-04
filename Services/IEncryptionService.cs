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
    string Encrypt(string plainText, EncryptionKey key);

    /// <summary>
    /// Decrypts a value using the specified encryption key
    /// </summary>
    string Decrypt(string cipherText, EncryptionKey key);

    /// <summary>
    /// Encrypts a value using the primary encryption key
    /// </summary>
    Task<string> EncryptAsync(string plainText, Guid configurationId);

    /// <summary>
    /// Decrypts a value using the appropriate encryption key
    /// </summary>
    Task<string> DecryptAsync(string cipherText, Guid configurationId);

    /// <summary>
    /// Validates if a key is suitable for encryption
    /// </summary>
    bool ValidateKey(EncryptionKey key);

    /// <summary>
    /// Gets the primary encryption key for a configuration
    /// </summary>
    Task<EncryptionKey?> GetPrimaryKeyAsync(Guid configurationId);

    /// <summary>
    /// Gets an encryption key by ID
    /// </summary>
    Task<EncryptionKey?> GetKeyAsync(string keyId);

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    EncryptionKey GenerateNewKey(string name, string algorithm = "AES256");

    /// <summary>
    /// Rotates an encryption key
    /// </summary>
    Task RotateKeyAsync(string oldKeyId, string userId);
}
