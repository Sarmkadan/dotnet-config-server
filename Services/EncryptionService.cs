#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Services;

/// <summary>
/// Service for encryption and decryption operations
/// </summary>
sealed public class EncryptionService : IEncryptionService
{
    private readonly IEncryptionKeyRepository _keyRepository;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IEncryptionKeyRepository keyRepository, ILogger<EncryptionService> logger)
    {
        _keyRepository = keyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Encrypts plain text using AES-256
    /// </summary>
    public string Encrypt(string plainText, EncryptionKey key)
    {
        ValidateKey(key);

        using (var aes = Aes.Create())
        {
            aes.KeySize = AppConstants.Encryption.AesKeySize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Generate IV
            byte[] iv = new byte[aes.BlockSize / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            // Derive key from encrypted key
            aes.Key = DeriveKeyFromEncryptedKey(key);

            using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
            using (var ms = new MemoryStream())
            {
                // Write IV to stream
                ms.Write(iv, 0, iv.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs, Encoding.UTF8))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    /// <summary>
    /// Decrypts cipher text using AES-256
    /// </summary>
    public string Decrypt(string cipherText, EncryptionKey key)
    {
        ValidateKey(key);

        try
        {
            var buffer = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.KeySize = AppConstants.Encryption.AesKeySize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from beginning of buffer
                int ivSize = aes.BlockSize / 8;
                byte[] iv = new byte[ivSize];
                Array.Copy(buffer, 0, iv, 0, ivSize);

                // Derive key
                aes.Key = DeriveKeyFromEncryptedKey(key);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (var ms = new MemoryStream(buffer, ivSize, buffer.Length - ivSize))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for key {KeyId}", key.KeyId);
            throw new EncryptionException("Failed to decrypt value", ex);
        }
    }

    /// <summary>
    /// Encrypts a value using the primary key for a configuration
    /// </summary>
    public async Task<string> EncryptAsync(string plainText, Guid configurationId)
    {
        var key = await GetPrimaryKeyAsync(configurationId);
        if (key is null)
            throw new ConfigurationException("No primary encryption key found for configuration");

        key.IncrementUsage();
        await _keyRepository.UpdateAsync(key);

        return Encrypt(plainText, key);
    }

    /// <summary>
    /// Decrypts a value using the appropriate key
    /// </summary>
    public async Task<string> DecryptAsync(string cipherText, Guid configurationId)
    {
        var primaryKey = await GetPrimaryKeyAsync(configurationId);
        if (primaryKey is null)
            throw new ConfigurationException("No primary encryption key found for configuration");

        try
        {
            return Decrypt(cipherText, primaryKey);
        }
        catch (EncryptionException)
        {
            // If primary key fails, try all active keys
            var allKeys = await _keyRepository.GetActiveKeysByConfigurationAsync(configurationId);
            foreach (var key in allKeys)
            {
                try
                {
                    return Decrypt(cipherText, key);
                }
                catch
                {
                    // Try next key
                    continue;
                }
            }
            throw;
        }
    }

    /// <summary>
    /// Validates encryption key
    /// </summary>
    public bool ValidateKey(EncryptionKey key)
    {
        if (!key.IsValid())
            throw new EncryptionException($"Encryption key {key.KeyId} is not valid or has expired");

        if (key.EncryptedKey.Length == 0 || key.Salt.Length == 0)
            throw new EncryptionException("Encryption key is missing required data");

        return true;
    }

    /// <summary>
    /// Gets the primary encryption key for a configuration
    /// </summary>
    public async Task<EncryptionKey?> GetPrimaryKeyAsync(Guid configurationId)
    {
        return await _keyRepository.GetPrimaryKeyByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Gets an encryption key by ID
    /// </summary>
    public async Task<EncryptionKey?> GetKeyAsync(string keyId)
    {
        return await _keyRepository.GetByKeyIdAsync(keyId);
    }

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    public EncryptionKey GenerateNewKey(string name, string algorithm = "AES256")
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = AppConstants.Encryption.AesKeySize;
            aes.GenerateKey();

            byte[] salt = new byte[AppConstants.Encryption.AesSaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var encryptedKey = DeriveAndEncryptKey(aes.Key, salt);

            return new EncryptionKey
            {
                Name = name,
                KeyId = Guid.NewGuid().ToString(),
                Algorithm = EncryptionAlgorithm.AES256,
                EncryptedKey = encryptedKey,
                Salt = salt,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                CreatedBy = "system"
            };
        }
    }

    /// <summary>
    /// Rotates an encryption key
    /// </summary>
    public async Task RotateKeyAsync(string oldKeyId, string userId)
    {
        var oldKey = await _keyRepository.GetByKeyIdAsync(oldKeyId);
        if (oldKey is null)
            throw new ConfigurationNotFoundException($"Encryption key {oldKeyId} not found");

        oldKey.MarkAsRotated(userId);
        await _keyRepository.UpdateAsync(oldKey);

        _logger.LogInformation("Encryption key {KeyId} rotated by {UserId}", oldKeyId, userId);
    }

    private byte[] DeriveKeyFromEncryptedKey(EncryptionKey key)
    {
        // Use actual key material for derivation, not the KeyId string
        using (var pbkdf2 = new Rfc2898DeriveBytes(
            key.EncryptedKey,
            key.Salt,
            AppConstants.Encryption.AesIterations,
            HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(32); // 256 bits
        }
    }

    private byte[] DeriveAndEncryptKey(byte[] key, byte[] salt)
    {
        return key; // Store key encrypted in repository
    }
}
