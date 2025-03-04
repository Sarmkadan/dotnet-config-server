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

sealed public class EncryptionServiceTests
{
    private readonly Mock<IEncryptionKeyRepository> _keyRepositoryMock;
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        _keyRepositoryMock = new Mock<IEncryptionKeyRepository>();
        _loggerMock = new Mock<ILogger<EncryptionService>>();
        _sut = new EncryptionService(_keyRepositoryMock.Object, _loggerMock.Object);
    }

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

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlainText()
    {
        var key = CreateValidKey();
        var plainText = "Server=myserver;Database=mydb;User Id=sa;Password=secret;";

        var cipherText = _sut.Encrypt(plainText, key);
        var decrypted = _sut.Decrypt(cipherText, key);

        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_SamePlainText_ProducesDistinctCipherTextDueToRandomIv()
    {
        var key = CreateValidKey();
        const string plainText = "static-value";

        var cipher1 = _sut.Encrypt(plainText, key);
        var cipher2 = _sut.Encrypt(plainText, key);

        cipher1.Should().NotBe(cipher2);
    }

    [Fact]
    public void Encrypt_OutputIsValidBase64()
    {
        var key = CreateValidKey();

        var cipherText = _sut.Encrypt("hello", key);

        var act = () => Convert.FromBase64String(cipherText);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateKey_InactiveKey_ThrowsEncryptionExceptionMentioningKeyId()
    {
        var key = CreateValidKey();
        key.IsActive = false;

        var act = () => _sut.ValidateKey(key);

        act.Should().Throw<EncryptionException>()
           .WithMessage($"*{key.KeyId}*");
    }

    [Fact]
    public void ValidateKey_ExpiredKey_ThrowsEncryptionException()
    {
        var key = CreateValidKey();
        key.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        var act = () => _sut.ValidateKey(key);

        act.Should().Throw<EncryptionException>();
    }

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
}
