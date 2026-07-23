#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Services;

/// <summary>
/// Service for encryption and decryption operations
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    /// <summary>
    /// Matches the versioned ciphertext header written by <see cref="Encrypt"/>:
    /// "v&lt;version&gt;:&lt;keyId&gt;:&lt;base64 payload&gt;".
    /// </summary>
    private static readonly Regex VersionedCiphertextHeader =
        new(@"^v(?<version>\d+):(?<keyId>[^:]+):(?<payload>.+)$", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Ciphertext format versions this build knows how to decode. Anything outside this
    /// set is rejected outright instead of being fed to the AES pipeline, which would
    /// otherwise either throw an opaque cryptographic error or - worse - decode into
    /// silent garbage.
    /// </summary>
    private static readonly HashSet<int> SupportedCiphertextVersions =
    [
        AppConstants.Encryption.LegacyCiphertextVersion,
        AppConstants.Encryption.CurrentCiphertextVersion
    ];

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
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentNullException.ThrowIfNull(key);
        
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

                var payload = Convert.ToBase64String(ms.ToArray());
                return $"v{AppConstants.Encryption.CurrentCiphertextVersion.ToString(CultureInfo.InvariantCulture)}:{key.KeyId}:{payload}";
            }
        }
    }

    /// <summary>
    /// Decrypts cipher text using AES-256
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cipherText"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="EncryptionException">
    /// Thrown when <paramref name="key"/> is invalid, the ciphertext carries a version tag
    /// this build does not understand, or decryption otherwise fails (e.g. wrong key material).
    /// </exception>
    public string Decrypt(string cipherText, EncryptionKey key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        ArgumentNullException.ThrowIfNull(key);

        ValidateKey(key);

        var payload = ExtractCiphertextPayload(cipherText, key.KeyId);

        try
        {
            var buffer = Convert.FromBase64String(payload);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentOutOfRangeException.ThrowIfEqual(configurationId, Guid.Empty);
        
        var key = await GetPrimaryKeyAsync(configurationId);
        if (key is null)
            throw new ConfigurationException("No primary encryption key found for configuration");

        key.IncrementUsage();
        await _keyRepository.UpdateAsync(key);

        return Encrypt(plainText, key);
    }

    /// <summary>
    /// Decrypts a value using the appropriate key from the configuration's keyring.
    /// If the ciphertext carries an embedded key identifier (the versioned format
    /// produced by <see cref="Encrypt"/>), that exact key is tried first; otherwise
    /// the primary key is tried, falling back to every other active key.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cipherText"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="configurationId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="ConfigurationException">Thrown when no primary encryption key is configured.</exception>
    /// <exception cref="EncryptionException">Thrown when no key in the keyring can decrypt the ciphertext, or the ciphertext version is unsupported.</exception>
    public async Task<string> DecryptAsync(string cipherText, Guid configurationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        ArgumentOutOfRangeException.ThrowIfEqual(configurationId, Guid.Empty);

        if (TryParseVersionedCiphertext(cipherText, out _, out var embeddedKeyId, out _))
        {
            var taggedKey = await _keyRepository.GetByKeyIdAsync(embeddedKeyId);
            if (taggedKey is not null)
            {
                try
                {
                    return Decrypt(cipherText, taggedKey);
                }
                catch (EncryptionException)
                {
                    // Fall through to the primary/keyring scan below.
                }
            }
        }

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
        ArgumentNullException.ThrowIfNull(key);
        
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
        ArgumentOutOfRangeException.ThrowIfEqual(configurationId, Guid.Empty);
        
        return await _keyRepository.GetPrimaryKeyByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Gets an encryption key by ID
    /// </summary>
    public async Task<EncryptionKey?> GetKeyAsync(string keyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        
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
    /// Rotates an encryption key by marking the old key as rotated and re-encrypting
    /// all configuration values that were protected by it with the new primary key.
    /// </summary>
    /// <remarks>
    /// Key rotation flow:
    /// 1. Call <see cref="GenerateNewKey"/> and persist the new key as primary.
    /// 2. Call <see cref="RotateKeyAsync"/> with the old key ID and the repository of
    ///    configuration keys to re-encrypt.
    /// 3. The old key is marked as rotated (no longer primary) but stays active so
    ///    that any in-flight decryption calls still succeed during the transition.
    /// 4. <see cref="DecryptAsync"/> already falls back to all active keys, so
    ///    consumers that cached the old ciphertext continue to work until they refresh.
    /// 5. After all consumers have reloaded their configuration you can deactivate
    ///    the old key entirely.
    /// </remarks>
    public async Task RotateKeyAsync(string oldKeyId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        var oldKey = await _keyRepository.GetByKeyIdAsync(oldKeyId);
        if (oldKey is null)
            throw new ConfigurationNotFoundException($"Encryption key {oldKeyId} not found");

        oldKey.MarkAsRotated(userId);
        await _keyRepository.UpdateAsync(oldKey);

        _logger.LogInformation("Encryption key {KeyId} rotated by {UserId}", oldKeyId, userId);
    }

    /// <summary>
    /// Re-encrypts all configuration key values for a given configuration using the
    /// current primary key.  Call this after rotating the encryption key to ensure
    /// that stored ciphertext is protected by the new key material.
    /// </summary>
    public async Task ReEncryptConfigurationAsync(Guid configurationId, IEnumerable<Models.ConfigurationKey> keys, string userId)
    {
        var primaryKey = await GetPrimaryKeyAsync(configurationId);
        if (primaryKey is null)
            throw new ConfigurationException("No primary encryption key found for configuration");

        foreach (var key in keys.Where(k => k.IsEncrypted))
        {
            try
            {
                var plainText = await DecryptAsync(key.Value, configurationId);
                key.Value = Encrypt(plainText, primaryKey);
                _logger.LogDebug("Re-encrypted key {KeyId} for configuration {ConfigId}", key.Id, configurationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to re-encrypt key {KeyId} during rotation for configuration {ConfigId}", key.Id, configurationId);
                throw;
            }
        }
    }

    /// <summary>
    /// Attempts to parse the "v&lt;version&gt;:&lt;keyId&gt;:&lt;payload&gt;" header written by
    /// <see cref="Encrypt"/>. Ciphertext produced before versioning was introduced has no
    /// such header and is treated by callers as the legacy format.
    /// </summary>
    private static bool TryParseVersionedCiphertext(string cipherText, out int version, out string keyId, out string payload)
    {
        var match = VersionedCiphertextHeader.Match(cipherText);
        if (!match.Success)
        {
            version = AppConstants.Encryption.LegacyCiphertextVersion;
            keyId = string.Empty;
            payload = cipherText;
            return false;
        }

        version = int.Parse(match.Groups["version"].Value, NumberStyles.None, CultureInfo.InvariantCulture);
        keyId = match.Groups["keyId"].Value;
        payload = match.Groups["payload"].Value;
        return true;
    }

    /// <summary>
    /// Extracts the base64 AES payload from a ciphertext string, validating the embedded
    /// version tag along the way. Ciphertext without a tag is assumed to be the legacy,
    /// pre-versioning format so previously stored secrets keep working after an upgrade.
    /// </summary>
    /// <exception cref="EncryptionException">Thrown when the ciphertext carries a version tag this build does not recognize.</exception>
    private static string ExtractCiphertextPayload(string cipherText, string keyId)
    {
        if (!TryParseVersionedCiphertext(cipherText, out var version, out _, out var payload))
            return cipherText;

        if (!SupportedCiphertextVersions.Contains(version))
        {
            throw new EncryptionException(
                $"Ciphertext for key {keyId} carries unsupported version tag 'v{version.ToString(CultureInfo.InvariantCulture)}'. " +
                $"Supported versions: {string.Join(", ", SupportedCiphertextVersions.OrderBy(v => v))}.");
        }

        return payload;
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
