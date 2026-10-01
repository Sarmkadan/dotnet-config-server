#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using DotnetConfigServer.Models;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Services;

/// <summary>
/// Low-level cryptographic provider that wraps symmetric key operations.
/// Used by <see cref="EncryptionService"/> for the actual AES encrypt/decrypt work
/// and for key generation with configurable key sizes and algorithms.
/// </summary>
public sealed class EncryptionProvider : IDisposable
{
    private readonly ILogger<EncryptionProvider> _logger;
    private readonly EncryptionKeyRotationOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionProvider"/> class.
    /// </summary>
    /// <param name="options">Key rotation and algorithm options.</param>
    /// <param name="logger">The logger instance.</param>
    public EncryptionProvider(
        IOptions<EncryptionKeyRotationOptions> options,
        ILogger<EncryptionProvider> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Encrypts plaintext bytes using AES-256-CBC with PKCS7 padding.
    /// A random IV is prepended to the returned ciphertext.
    /// </summary>
    /// <param name="plainText">The data to encrypt.</param>
    /// <param name="keyMaterial">The raw key bytes (must be 16, 24 or 32 bytes).</param>
    /// <returns>IV + ciphertext as a single byte array.</returns>
    public byte[] Encrypt(byte[] plainText, byte[] keyMaterial)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentNullException.ThrowIfNull(keyMaterial);

        using var aes = Aes.Create();
        aes.Key = keyMaterial;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);

        // Prepend IV so decryption can extract it
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        _logger.LogDebug("Encrypted {PlainLength} bytes into {CipherLength} bytes",
            plainText.Length, result.Length);
        return result;
    }

    /// <summary>
    /// Decrypts ciphertext produced by <see cref="Encrypt"/>.
    /// Expects the IV to be prepended to the ciphertext.
    /// </summary>
    /// <param name="cipherText">IV + ciphertext bytes.</param>
    /// <param name="keyMaterial">The raw key bytes.</param>
    /// <returns>The decrypted plaintext bytes.</returns>
    public byte[] Decrypt(byte[] cipherText, byte[] keyMaterial)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(cipherText);
        ArgumentNullException.ThrowIfNull(keyMaterial);

        using var aes = Aes.Create();
        aes.Key = keyMaterial;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(cipherText, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(cipherText, iv.Length, cipherText.Length - iv.Length);

        _logger.LogDebug("Decrypted {CipherLength} bytes into {PlainLength} bytes",
            cipherText.Length, decrypted.Length);
        return decrypted;
    }

    /// <summary>
    /// Convenience overload: encrypts a UTF-8 string and returns base64.
    /// </summary>
    /// <param name="plainText">The string to encrypt.</param>
    /// <param name="keyMaterial">The raw key bytes.</param>
    /// <returns>Base64-encoded ciphertext (IV-prepended).</returns>
    public string EncryptString(string plainText, byte[] keyMaterial)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = Encrypt(bytes, keyMaterial);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Convenience overload: decrypts a base64 ciphertext back to a UTF-8 string.
    /// </summary>
    /// <param name="base64CipherText">Base64-encoded ciphertext.</param>
    /// <param name="keyMaterial">The raw key bytes.</param>
    /// <returns>The decrypted string.</returns>
    public string DecryptString(string base64CipherText, byte[] keyMaterial)
    {
        var bytes = Convert.FromBase64String(base64CipherText);
        var decrypted = Decrypt(bytes, keyMaterial);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Generates cryptographically secure random key material of the specified size.
    /// </summary>
    /// <param name="keySizeInBits">Key size in bits (128, 192 or 256). Defaults to 256.</param>
    /// <returns>Random key bytes.</returns>
    public byte[] GenerateKeyMaterial(int keySizeInBits = 256)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (keySizeInBits is not (128 or 192 or 256))
            throw new ArgumentOutOfRangeException(nameof(keySizeInBits), "Key size must be 128, 192 or 256 bits.");

        var key = new byte[keySizeInBits / 8];
        RandomNumberGenerator.Fill(key);

        _logger.LogInformation("Generated new {KeySize}-bit key material", keySizeInBits);
        return key;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _logger.LogInformation("EncryptionProvider disposed");
    }
}
