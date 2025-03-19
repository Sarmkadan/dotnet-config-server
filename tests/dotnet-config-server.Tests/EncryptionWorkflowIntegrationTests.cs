#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DotnetConfigServer.Common;
using DotnetConfigServer.Events;
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
/// Integration tests covering the encryption lifecycle: key generation, encrypt/decrypt,
/// key rotation, and re-encryption of stored values.
/// </summary>
sealed public class EncryptionWorkflowIntegrationTests
{
    private readonly Mock<IEncryptionKeyRepository> _keyRepositoryMock;
    private readonly Mock<IConfigurationRepository> _configRepoMock;
    private readonly Mock<IConfigurationKeyRepository> _configKeyRepoMock;
    private readonly Mock<IAuditLogRepository> _auditRepoMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly EncryptionService _encryptionService;
    private readonly ConfigurationService _configurationService;

    public EncryptionWorkflowIntegrationTests()
    {
        _keyRepositoryMock = new Mock<IEncryptionKeyRepository>();
        _configRepoMock = new Mock<IConfigurationRepository>();
        _configKeyRepoMock = new Mock<IConfigurationKeyRepository>();
        _auditRepoMock = new Mock<IAuditLogRepository>();
        _eventBusMock = new Mock<IEventBus>();

        _encryptionService = new EncryptionService(
            _keyRepositoryMock.Object,
            new Mock<ILogger<EncryptionService>>().Object);

        _configurationService = new ConfigurationService(
            _configRepoMock.Object,
            _configKeyRepoMock.Object,
            _encryptionService,
            _auditRepoMock.Object,
            _eventBusMock.Object,
            new Mock<ILogger<ConfigurationService>>().Object);
    }

    private static EncryptionKey CreateRealKey(string name = "primary-key") => new()
    {
        Name = name,
        KeyId = Guid.NewGuid().ToString(),
        Algorithm = EncryptionAlgorithm.AES256,
        EncryptedKey = RandomNumberGenerator.GetBytes(32),
        Salt = RandomNumberGenerator.GetBytes(AppConstants.Encryption.AesSaltSize),
        IsActive = true,
        IsPrimary = true,
        ExpiresAt = DateTime.UtcNow.AddYears(1),
        CreatedBy = "system"
    };

    /// <summary>
    /// End-to-end: generate key → encrypt value → decrypt value with same key
    /// </summary>
    [Fact]
    public void EncryptionRoundtrip_PlainTextSurvivesEncryptAndDecrypt()
    {
        var key = _encryptionService.GenerateNewKey("roundtrip-key");
        const string sensitiveValue = "Server=prod;Database=orders;User Id=sa;Password=TopSecret!";

        var cipherText = _encryptionService.Encrypt(sensitiveValue, key);
        var decrypted = _encryptionService.Decrypt(cipherText, key);

        cipherText.Should().NotBe(sensitiveValue);
        decrypted.Should().Be(sensitiveValue);
    }

    /// <summary>
    /// Demonstrates that two encryptions of the same plaintext produce different ciphertext
    /// (random IV per encryption).
    /// </summary>
    [Fact]
    public void Encrypt_RandomIV_NeverProducesIdenticalCiphertext()
    {
        var key = _encryptionService.GenerateNewKey("iv-key");
        const string value = "same-value-each-time";

        var results = Enumerable.Range(0, 10)
            .Select(_ => _encryptionService.Encrypt(value, key))
            .ToHashSet();

        results.Should().HaveCount(10, "every encryption must use a unique IV");
    }

    /// <summary>
    /// Key rotation: marks old key as non-primary and updates repository.
    /// After rotation, keys encrypted with the old key are still decryptable via fallback.
    /// </summary>
    [Fact]
    public async Task KeyRotation_AfterRotation_OldCiphertextStillDecryptableViaFallback()
    {
        var oldKey = CreateRealKey("old-key");
        var newKey = CreateRealKey("new-key");
        var configId = Guid.NewGuid();

        const string secret = "my-secret-password";
        var cipherWithOldKey = _encryptionService.Encrypt(secret, oldKey);

        // Simulate repository: primary key returns new key, fallback returns both
        _keyRepositoryMock
            .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
            .ReturnsAsync(newKey);
        _keyRepositoryMock
            .Setup(r => r.GetActiveKeysByConfigurationAsync(configId))
            .ReturnsAsync(new List<EncryptionKey> { newKey, oldKey });

        // DecryptAsync tries new key first (fails), then falls back to old key
        var decrypted = await _encryptionService.DecryptAsync(cipherWithOldKey, configId);

        decrypted.Should().Be(secret);
    }

    /// <summary>
    /// When adding an encrypted key to an encrypted config, the service should
    /// auto-encrypt the value and mark IsEncrypted = true.
    /// </summary>
    [Fact]
    public async Task AddKeyAsync_EncryptedConfiguration_AutoEncryptsKeyValue()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration
        {
            Id = configId,
            Name = "secrets",
            ApplicationId = Guid.NewGuid(),
            CreatedBy = "admin",
            IsEncrypted = true
        };

        var primaryKey = CreateRealKey();
        _configRepoMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _keyRepositoryMock
            .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
            .ReturnsAsync(primaryKey);
        _keyRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<EncryptionKey>()))
            .Returns(Task.CompletedTask);
        _configKeyRepoMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _configKeyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var keyToAdd = new ConfigurationKey
        {
            Key = "api.secret",
            Value = "plain-api-key",
            IsEncrypted = false,
            ConfigurationId = configId,
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        var result = await _configurationService.AddKeyAsync(configId, keyToAdd, "admin");

        result.IsEncrypted.Should().BeTrue();
        result.Value.Should().NotBe("plain-api-key");

        // Verify we can decrypt the stored value back to the original
        var decrypted = _encryptionService.Decrypt(result.Value, primaryKey);
        decrypted.Should().Be("plain-api-key");
    }

    /// <summary>
    /// ReEncryptConfigurationAsync re-encrypts all encrypted keys using the current primary key.
    /// </summary>
    [Fact]
    public async Task ReEncryptConfiguration_AllEncryptedKeys_AreReEncryptedWithNewKey()
    {
        var configId = Guid.NewGuid();
        var oldKey = CreateRealKey("old");
        var newKey = CreateRealKey("new");

        const string plainText1 = "connection-string-value";
        const string plainText2 = "api-token-value";

        var encryptedWithOld1 = _encryptionService.Encrypt(plainText1, oldKey);
        var encryptedWithOld2 = _encryptionService.Encrypt(plainText2, oldKey);

        var keys = new List<ConfigurationKey>
        {
            new() { Key = "db.conn", Value = encryptedWithOld1, IsEncrypted = true, ConfigurationId = configId, VersionId = Guid.NewGuid(), CreatedBy = "admin" },
            new() { Key = "api.token", Value = encryptedWithOld2, IsEncrypted = true, ConfigurationId = configId, VersionId = Guid.NewGuid(), CreatedBy = "admin" },
            new() { Key = "app.name", Value = "myapp", IsEncrypted = false, ConfigurationId = configId, VersionId = Guid.NewGuid(), CreatedBy = "admin" }
        };

        // After rotation: primary is new key; fallback includes old key
        _keyRepositoryMock
            .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
            .ReturnsAsync(newKey);
        _keyRepositoryMock
            .Setup(r => r.GetActiveKeysByConfigurationAsync(configId))
            .ReturnsAsync(new List<EncryptionKey> { newKey, oldKey });

        await _encryptionService.ReEncryptConfigurationAsync(configId, keys, "ops");

        // Non-encrypted key unchanged
        keys[2].Value.Should().Be("myapp");

        // Encrypted keys have new ciphertext
        keys[0].Value.Should().NotBe(encryptedWithOld1);
        keys[1].Value.Should().NotBe(encryptedWithOld2);

        // New ciphertext decryptable with new key
        _encryptionService.Decrypt(keys[0].Value, newKey).Should().Be(plainText1);
        _encryptionService.Decrypt(keys[1].Value, newKey).Should().Be(plainText2);
    }

    /// <summary>
    /// EncryptAsync with no primary key throws a clear error message.
    /// </summary>
    [Fact]
    public async Task EncryptAsync_NoPrimaryKey_ThrowsDescriptiveConfigurationException()
    {
        var configId = Guid.NewGuid();
        _keyRepositoryMock
            .Setup(r => r.GetPrimaryKeyByConfigurationAsync(configId))
            .ReturnsAsync((EncryptionKey?)null);

        var act = () => _encryptionService.EncryptAsync("value", configId);

        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*primary encryption key*");
    }

    /// <summary>
    /// Concurrent encryption operations on different configurations do not interfere.
    /// </summary>
    [Fact]
    public async Task ConcurrentEncryption_MultipleConfigurations_ProduceIndependentResults()
    {
        var configs = Enumerable.Range(0, 5)
            .Select(_ =>
            {
                var id = Guid.NewGuid();
                var key = CreateRealKey();
                _keyRepositoryMock
                    .Setup(r => r.GetPrimaryKeyByConfigurationAsync(id))
                    .ReturnsAsync(key);
                _keyRepositoryMock
                    .Setup(r => r.UpdateAsync(It.IsAny<EncryptionKey>()))
                    .Returns(Task.CompletedTask);
                return (id, key, secret: $"secret-for-{id}");
            })
            .ToList();

        var tasks = configs.Select(c => _encryptionService.EncryptAsync(c.secret, c.id));
        var ciphertexts = await Task.WhenAll(tasks);

        ciphertexts.Should().HaveCount(5);
        ciphertexts.Should().OnlyHaveUniqueItems();

        for (int i = 0; i < configs.Count; i++)
        {
            _encryptionService.Decrypt(ciphertexts[i], configs[i].key)
                .Should().Be(configs[i].secret);
        }
    }
}
