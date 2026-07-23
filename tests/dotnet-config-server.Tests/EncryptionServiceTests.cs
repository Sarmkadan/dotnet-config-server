#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Contains unit tests for <see cref="EncryptionService"/> covering encryption,
/// decryption, key validation, key generation, and key rotation scenarios.
/// </summary>
public sealed class EncryptionServiceTests
{
    /// <summary>
    /// Mock repository for testing encryption key persistence.
    /// </summary>
    private readonly Mock<IEncryptionKeyRepository> _keyRepositoryMock;

    /// <summary>
    /// Mock logger for verifying service behavior.
    /// </summary>
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;

    /// <summary>
    /// System under test - the encryption service being validated.
    /// </summary>
    private readonly EncryptionService _sut;

    /// <summary>
    /// Initializes the mocks and the <see cref="EncryptionService"/> instance used in the tests.
    /// </summary>
    public EncryptionServiceTests()
    {
        _keyRepositoryMock = new Mock<IEncryptionKeyRepository>();
        _loggerMock = new Mock<ILogger<EncryptionService>>();
        _sut = new EncryptionService(_keyRepositoryMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Creates a valid encryption key for testing purposes.
    /// </summary>
    /// <returns>A configured <see cref="EncryptionKey"/> with valid cryptographic properties.</returns>
    private static EncryptionKey CreateValidKey() => new()
    {
        KeyId = Guid.NewGuid().ToString(),
        Name = "test-key",
        Algorithm = EncryptionAlgorithm.AES256,
        EncryptedKey = new byte[32],
        Salt = RandomNumberGenerator.GetBytes(AppConstants.Encryption.AesSaltSize),
        IsActive = true,
        IsPrimary = true,
        ExpiresAt = DateTime.UtcNow.AddYears(1),
        CreatedBy = "system"
    };

    /// <summary>
    /// Tests that encryption followed by decryption returns the original plain text.
    /// Verifies the basic symmetric encryption/decryption round‑trip functionality.
    /// </summary>
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlainText()
    {
        var key = CreateValidKey();
        var plainText = "Server=myserver;Database=mydb;User Id=sa;Password=secret;";

        var cipherText = _sut.Encrypt(plainText, key);
        var decrypted = _sut.Decrypt(cipherText, key);

        decrypted.Should().Be(plainText);
    }

    /// <summary>
    /// Tests that encrypting the same plain text multiple times produces different cipher texts.
    /// Verifies that the encryption uses a random initialization vector (IV) for each operation.
    /// </summary>
    [Fact]
    public void Encrypt_SamePlainText_ProducesDistinctCipherTextDueToRandomIv()
    {
        var key = CreateValidKey();
        const string plainText = "static-value";

        var cipher1 = _sut.Encrypt(plainText, key);
        var cipher2 = _sut.Encrypt(plainText, key);

        cipher1.Should().NotBe(cipher2);
    }

    /// <summary>
    /// Tests that the base64 payload portion of the encryption output (after the
    /// "v&lt;version&gt;:&lt;keyId&gt;:" header) is valid Base64 encoded data.
    /// Verifies that the cipher text can be safely transmitted as a string.
    /// </summary>
    [Fact]
    public void Encrypt_PayloadIsValidBase64()
    {
        var key = CreateValidKey();

        var cipherText = _sut.Encrypt("hello", key);
        var payload = cipherText.Split(':', 3)[2];

        var act = () => Convert.FromBase64String(payload);
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that new ciphertext carries the current ciphertext format version tag
    /// and the identifier of the key that produced it.
    /// </summary>
    [Fact]
    public void Encrypt_NewCipherText_CarriesCurrentVersionTagAndKeyId()
    {
        var key = CreateValidKey();

        var cipherText = _sut.Encrypt("hello", key);

        cipherText.Should().StartWith($"v{AppConstants.Encryption.CurrentCiphertextVersion}:{key.KeyId}:");
    }

    /// <summary>
    /// Tests that ciphertext produced before the version-tag prefix was introduced
    /// (a bare Base64 payload with no "v&lt;n&gt;:" header) still decrypts successfully.
    /// </summary>
    [Fact]
    public void Decrypt_LegacyUnversionedCipherText_StillSucceeds()
    {
        var key = CreateValidKey();
        var plainText = "legacy-secret-value";

        var versionedCipherText = _sut.Encrypt(plainText, key);
        var legacyCipherText = versionedCipherText.Split(':', 3)[2];

        var decrypted = _sut.Decrypt(legacyCipherText, key);

        decrypted.Should().Be(plainText);
    }

    /// <summary>
    /// Tests that ciphertext tagged with a version this build does not recognize
    /// is rejected outright with a clear <see cref="EncryptionException"/> instead of
    /// being fed through the AES pipeline and possibly yielding silent garbage.
    /// </summary>
    [Fact]
    public void Decrypt_UnknownVersionTag_ThrowsEncryptionExceptionMentioningVersion()
    {
        var key = CreateValidKey();
        var versionedCipherText = _sut.Encrypt("hello", key);
        var payload = versionedCipherText.Split(':', 3)[2];
        var futureVersionCipherText = $"v99:{key.KeyId}:{payload}";

        var act = () => _sut.Decrypt(futureVersionCipherText, key);

        act.Should().Throw<EncryptionException>()
           .WithMessage("*v99*");
    }

    /// <summary>
    /// Tests that validating an inactive key throws <see cref="EncryptionException"/>
    /// and that the exception message contains the key identifier.
    /// </summary>
    [Fact]
    public void ValidateKey_InactiveKey_ThrowsEncryptionExceptionMentioningKeyId()
    {
        var key = CreateValidKey();
        key.IsActive = false;

        var act = () => _sut.ValidateKey(key);

        act.Should().Throw<EncryptionException>()
           .WithMessage($"*{key.KeyId}*");
    }

    /// <summary>
    /// Tests that validating an expired key throws <see cref="EncryptionException"/>.
    /// </summary>
    [Fact]
    public void ValidateKey_ExpiredKey_ThrowsEncryptionException()
    {
        var key = CreateValidKey();
        key.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        var act = () => _sut.ValidateKey(key);

        act.Should().Throw<EncryptionException>();
    }

    /// <summary>
    /// Tests that <see cref="EncryptionService.GenerateNewKey"/> returns a key with all required cryptographic material populated.
    /// </summary>
    [Fact]
    public void GenerateNewKey_ReturnsKeyWithPopulatedCryptographicMaterial()
    {
        var key = _sut.GenerateNewKey("service-key");

        key.Name.Should().Be("service-key");
        key.Algorithm.Should().Be(EncryptionAlgorithm.AES256);
        key.Salt.Should().NotBeEmpty();
        key.EncryptedKey.Should().NotBeEmpty();
        key.KeyId.Should().NotBeNullOrWhiteSpace();
        key.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        key.CreatedBy.Should().Be("system");
    }

    /// <summary>
    /// Tests that attempting to encrypt asynchronously when no primary key exists for the given configuration
    /// throws a <see cref="ConfigurationException"/> indicating the missing primary key.
    /// </summary>
    /// <returns>A task representing the asynchronous assertion.</returns>
    [Fact]
    public async Task EncryptAsync_WhenNoPrimaryKeyExistsForConfiguration_ThrowsConfigurationException()
    {
        var configId = Guid.NewGuid();
        _keyRepositoryMock
            .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
            .ReturnsAsync((EncryptionKey?)null);

        var act = () => _sut.EncryptAsync("secret-value", configId);

        await act.Should().ThrowAsync<ConfigurationException>()
                 .WithMessage("*primary encryption key*");
    }

    /// <summary>
    /// Tests that rotating a key that cannot be found results in a <see cref="ConfigurationNotFoundException"/>
    /// with a message containing the missing key identifier.
    /// </summary>
    /// <returns>A task representing the asynchronous assertion.</returns>
    [Fact]
    public async Task RotateKeyAsync_WhenKeyNotFound_ThrowsConfigurationNotFoundException()
    {
        const string keyId = "ghost-key-id";
        _keyRepositoryMock
            .Setup(r => r.GetByKeyIdAsync(keyId))
            .ReturnsAsync((EncryptionKey?)null);

        var act = () => _sut.RotateKeyAsync(keyId, "admin");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>()
                 .WithMessage($"*{keyId}*");
    }

    /// <summary>
    /// Tests that rotating an existing primary key marks it as non‑primary, records rotation metadata,
    /// and persists the changes via the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RotateKeyAsync_WhenKeyExists_MarksItRotatedAndPersistsChange()
    {
        var key = CreateValidKey();
        key.KeyId = "active-key";
        key.IsPrimary = true;

        _keyRepositoryMock.Setup(r => r.GetByKeyIdAsync("active-key")).ReturnsAsync(key);
        _keyRepositoryMock.Setup(r => r.UpdateAsync(key)).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        await _sut.RotateKeyAsync("active-key", "ops-user");

        key.IsPrimary.Should().BeFalse();
        key.RotatedBy.Should().Be("ops-user");
        key.RotatedAt.Should().NotBeNull();
        key.RotatedAt!.Value.Should().BeOnOrAfter(before);

        _keyRepositoryMock.Verify(r => r.UpdateAsync(key), Times.Once);
    }

    /// <summary>
    /// Tests that decrypting with a different key than the one used for encryption fails.
    /// Verifies that encryption keys are not interchangeable and wrong keys produce decryption errors.
    /// </summary>
    [Fact]
    public void Decrypt_WithWrongKey_ThrowsEncryptionException()
    {
        var key1 = CreateValidKey();
        var key2 = CreateValidKey();

        var plainText = "secret-password";
        var cipherText = _sut.Encrypt(plainText, key1);

        var act = () => _sut.Decrypt(cipherText, key2);

        act.Should().Throw<EncryptionException>();
    }

    /// <summary>
    /// Tests that encrypting an empty string throws an exception.
    /// Verifies that the service properly validates input and rejects empty values.
    /// </summary>
    [Fact]
    public void Encrypt_EmptyString_ThrowsArgumentException()
    {
        var key = CreateValidKey();
        var act = () => _sut.Encrypt(string.Empty, key);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that encrypting a null string throws an exception.
    /// Verifies that the service properly validates input and rejects null values.
    /// </summary>
    [Fact]
    public void Encrypt_NullString_ThrowsArgumentNullException()
    {
        var key = CreateValidKey();
        var act = () => _sut.Encrypt(null!, key);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that encrypting and decrypting unicode characters preserves the original text.
    /// Verifies that the encryption handles international characters, emojis, and special symbols correctly.
    /// </summary>
    [Fact]
    public void EncryptDecrypt_UnicodeCharacters_ReturnsOriginalText()
    {
        var key = CreateValidKey();
        var unicodeText = "Hello 世界 🌍 Привет مرحبا こんにちは";

        var cipherText = _sut.Encrypt(unicodeText, key);
        var decrypted = _sut.Decrypt(cipherText, key);

        decrypted.Should().Be(unicodeText);
    }

    /// <summary>
    /// Tests that encrypting and decrypting whitespace-only strings fails.
    /// Verifies that the service rejects whitespace-only values which are considered invalid.
    /// </summary>
    [Fact]
    public void Encrypt_WhitespaceString_ThrowsArgumentException()
    {
        var key = CreateValidKey();
        var act = () => _sut.Encrypt("   ", key);

        act.Should().Throw<ArgumentException>();
    }
}
