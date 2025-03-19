#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

sealed public class ConfigurationServiceTests
{
    private readonly Mock<IConfigurationRepository> _configRepoMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<IAuditLogRepository> _auditRepoMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _configRepoMock = new Mock<IConfigurationRepository>();
        _keyRepoMock = new Mock<IConfigurationKeyRepository>();
        _encryptionMock = new Mock<IEncryptionService>();
        _auditRepoMock = new Mock<IAuditLogRepository>();
        _eventBusMock = new Mock<IEventBus>();

        var loggerMock = new Mock<ILogger<ConfigurationService>>();

        _sut = new ConfigurationService(
            _configRepoMock.Object,
            _keyRepoMock.Object,
            _encryptionMock.Object,
            _auditRepoMock.Object,
            _eventBusMock.Object,
            loggerMock.Object);
    }

    private static Configuration CreateValidConfig(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "test-config",
        ApplicationId = Guid.NewGuid(),
        CreatedBy = "admin"
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidConfiguration_SavesAndReturnsIt()
    {
        var config = CreateValidConfig();
        _configRepoMock.Setup(r => r.AddAsync(config)).Returns(Task.CompletedTask);
        _configRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(config, "deploy-user");

        result.Should().NotBeNull();
        result.CreatedBy.Should().Be("deploy-user");
        _configRepoMock.Verify(r => r.AddAsync(config), Times.Once);
        _configRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidName_ThrowsValidationException()
    {
        var config = CreateValidConfig();
        config.Name = string.Empty;

        var act = () => _sut.CreateAsync(config, "user");

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task CreateAsync_EmptyApplicationId_ThrowsValidationException()
    {
        var config = CreateValidConfig();
        config.ApplicationId = Guid.Empty;

        var act = () => _sut.CreateAsync(config, "user");

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainKey("ApplicationId");
    }

    [Fact]
    public async Task CreateAsync_WithParentConfigThatDoesNotExist_ThrowsConfigurationNotFoundException()
    {
        var parentId = Guid.NewGuid();
        var config = CreateValidConfig();
        config.ParentConfigurationId = parentId;

        _configRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync((Configuration?)null);

        var act = () => _sut.CreateAsync(config, "user");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingNonDeletedConfig_ReturnsIt()
    {
        var config = CreateValidConfig();
        _configRepoMock.Setup(r => r.GetByIdAsync(config.Id)).ReturnsAsync(config);

        var result = await _sut.GetByIdAsync(config.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(config.Id);
    }

    [Fact]
    public async Task GetByIdAsync_DeletedConfig_ReturnsNull()
    {
        var config = CreateValidConfig();
        config.DeletedAt = DateTime.UtcNow;
        _configRepoMock.Setup(r => r.GetByIdAsync(config.Id)).ReturnsAsync(config);

        var result = await _sut.GetByIdAsync(config.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentConfig_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _configRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Configuration?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingConfig_SetsDeletedAtAndSaves()
    {
        var config = CreateValidConfig();
        _configRepoMock.Setup(r => r.GetByIdAsync(config.Id)).ReturnsAsync(config);
        _configRepoMock.Setup(r => r.UpdateAsync(config)).Returns(Task.CompletedTask);
        _configRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        await _sut.DeleteAsync(config.Id, "remover");

        config.DeletedAt.Should().NotBeNull();
        config.DeletedAt!.Value.Should().BeOnOrAfter(before);
        config.DeletedBy.Should().Be("remover");
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentConfig_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _configRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Configuration?)null);

        var act = () => _sut.DeleteAsync(id, "user");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    // ── AddKeyAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddKeyAsync_NonEncryptedConfig_AddsKeyDirectly()
    {
        var config = CreateValidConfig();
        config.IsEncrypted = false;

        var key = new ConfigurationKey
        {
            Key = "feature.enabled",
            Value = "true",
            ConfigurationId = config.Id,
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(config.Id)).ReturnsAsync(config);
        _keyRepoMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _keyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.AddKeyAsync(config.Id, key, "dev-user");

        result.Key.Should().Be("feature.enabled");
        result.CreatedBy.Should().Be("dev-user");
        _encryptionMock.Verify(e => e.EncryptAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddKeyAsync_EncryptedConfig_EncryptsValueBeforeSaving()
    {
        var config = CreateValidConfig();
        config.IsEncrypted = true;

        var key = new ConfigurationKey
        {
            Key = "db.password",
            Value = "plain-password",
            ConfigurationId = config.Id,
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(config.Id)).ReturnsAsync(config);
        _encryptionMock.Setup(e => e.EncryptAsync("plain-password", config.Id))
            .ReturnsAsync("encrypted-ciphertext");
        _keyRepoMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _keyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.AddKeyAsync(config.Id, key, "dev-user");

        result.Value.Should().Be("encrypted-ciphertext");
        result.IsEncrypted.Should().BeTrue();
        _encryptionMock.Verify(e => e.EncryptAsync("plain-password", config.Id), Times.Once);
    }

    [Fact]
    public async Task AddKeyAsync_ConfigNotFound_ThrowsConfigurationNotFoundException()
    {
        var id = Guid.NewGuid();
        _configRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Configuration?)null);

        var key = new ConfigurationKey
        {
            Key = "any.key",
            Value = "val",
            ConfigurationId = id,
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        var act = () => _sut.AddKeyAsync(id, key, "user");

        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    // ── UpdateKeyAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateKeyAsync_NonEncryptedKey_UpdatesValueDirectly()
    {
        var keyId = Guid.NewGuid();
        var key = new ConfigurationKey
        {
            Id = keyId,
            Key = "app.debug",
            Value = "false",
            IsEncrypted = false,
            ConfigurationId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _keyRepoMock.Setup(r => r.GetByIdAsync(keyId)).ReturnsAsync(key);
        _keyRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _keyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigurationKeyChangedEvent>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateKeyAsync(keyId, "true", "toggler");

        result.Value.Should().Be("true");
        result.UpdatedBy.Should().Be("toggler");
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<ConfigurationKeyChangedEvent>()), Times.Once);
    }

    [Fact]
    public async Task UpdateKeyAsync_KeyNotFound_ThrowsConfigurationKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _keyRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ConfigurationKey?)null);

        var act = () => _sut.UpdateKeyAsync(id, "new-value", "user");

        await act.Should().ThrowAsync<ConfigurationKeyNotFoundException>();
    }

    // ── DeleteKeyAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteKeyAsync_ExistingKey_SetsIsActiveFalse()
    {
        var keyId = Guid.NewGuid();
        var key = new ConfigurationKey
        {
            Id = keyId,
            Key = "remove.me",
            Value = "x",
            IsActive = true,
            ConfigurationId = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _keyRepoMock.Setup(r => r.GetByIdAsync(keyId)).ReturnsAsync(key);
        _keyRepoMock.Setup(r => r.UpdateAsync(key)).Returns(Task.CompletedTask);
        _keyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.DeleteKeyAsync(keyId, "admin");

        key.IsActive.Should().BeFalse();
        _keyRepoMock.Verify(r => r.UpdateAsync(key), Times.Once);
    }

    [Fact]
    public async Task DeleteKeyAsync_KeyNotFound_ThrowsConfigurationKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _keyRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ConfigurationKey?)null);

        var act = () => _sut.DeleteKeyAsync(id, "user");

        await act.Should().ThrowAsync<ConfigurationKeyNotFoundException>();
    }

    // ── GetByApplicationAsync / SearchAsync ──────────────────────────────────

    [Fact]
    public async Task GetByApplicationAsync_DelegatesToRepository()
    {
        var appId = Guid.NewGuid();
        var configs = new List<Configuration> { CreateValidConfig() };
        _configRepoMock.Setup(r => r.GetByApplicationIdAsync(appId)).ReturnsAsync(configs);

        var result = await _sut.GetByApplicationAsync(appId);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_DelegatesToRepositoryWithQuery()
    {
        var appId = Guid.NewGuid();
        var configs = new List<Configuration> { CreateValidConfig() };
        _configRepoMock.Setup(r => r.SearchAsync("prod", appId)).ReturnsAsync(configs);

        var result = await _sut.SearchAsync("prod", appId);

        result.Should().HaveCount(1);
    }
}
