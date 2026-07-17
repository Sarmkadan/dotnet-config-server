using Moq;
using FluentAssertions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using DotnetConfigServer.Events;
using Microsoft.Extensions.Logging;
using DotnetConfigServer.Exceptions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Unit tests for <see cref="ConfigurationService"/> functionality.
/// Tests various operations including configuration creation, update, deletion, and key management.
/// </summary>
public class ConfigurationServiceTests
{
    private readonly Mock<IConfigurationRepository> _configRepositoryMock = new();
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock = new();
    private readonly Mock<IEncryptionService> _encryptionServiceMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<ILogger<ConfigurationService>> _loggerMock = new();
    private readonly ConfigurationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationServiceTests"/> class.
    /// Sets up mock dependencies and creates the <see cref="ConfigurationService"/> instance under test.
    /// </summary>
    public ConfigurationServiceTests()
    {
        _service = new ConfigurationService(
            _configRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _auditLogRepositoryMock.Object,
            _eventBusMock.Object,
            _loggerMock.Object
        );
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.CreateAsync"/> successfully creates a configuration
    /// when provided with valid configuration data.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldCreateConfiguration_WhenValid()
    {
        // Arrange
        var config = new Configuration { Id = Guid.NewGuid(), Name = "TestConfig", ApplicationId = Guid.NewGuid() };
        string userId = "user1";

        // Act
        var result = await _service.CreateAsync(config, userId);

        // Assert
        result.Should().Be(config);
        result.CreatedBy.Should().Be(userId);
        _configRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Configuration>()), Times.Once);
        _configRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        _auditLogRepositoryMock.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Once);
        _auditLogRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.CreateAsync"/> throws a <see cref="ConfigurationNotFoundException"/>
    /// when attempting to create a configuration with a non-existent parent configuration ID.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenParentNotFound()
    {
        // Arrange
        var config = new Configuration { Id = Guid.NewGuid(), Name = "ChildConfig", ApplicationId = Guid.NewGuid(), ParentConfigurationId = Guid.NewGuid() };
        _configRepositoryMock.Setup(x => x.GetByIdAsync(config.ParentConfigurationId.Value)).ReturnsAsync((Configuration?)null);

        // Act
        Func<Task> act = () => _service.CreateAsync(config, "user1");

        // Assert
        await act.Should().ThrowAsync<ConfigurationNotFoundException>();
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.UpdateAsync"/> successfully updates an existing configuration
    /// when provided with valid configuration data.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldUpdateConfiguration_WhenValid()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingConfig = new Configuration { Id = id, Name = "OldName", ApplicationId = Guid.NewGuid() };
        var newConfig = new Configuration { Name = "NewName", ApplicationId = existingConfig.ApplicationId };
        _configRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(existingConfig);

        // Act
        var result = await _service.UpdateAsync(id, newConfig, "user1");

        // Assert
        result.Name.Should().Be("NewName");
        _configRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Configuration>()), Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.DeleteAsync"/> successfully marks a configuration as deleted
    /// by setting its DeletedAt property and persisting the change.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldDeleteConfiguration()
    {
        // Arrange
        var id = Guid.NewGuid();
        var config = new Configuration { Id = id, Name = "ConfigToDelete" };
        _configRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(config);

        // Act
        await _service.DeleteAsync(id, "user1");

        // Assert
        config.DeletedAt.Should().NotBeNull();
        _configRepositoryMock.Verify(x => x.UpdateAsync(config), Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.AddKeyAsync"/> successfully adds a configuration key
    /// when the configuration is not encrypted.
    /// </summary>
    [Fact]
    public async Task AddKeyAsync_ShouldAddKey_WhenValid()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, IsEncrypted = false };
        var key = new ConfigurationKey { Key = "TestKey", Value = "TestValue" };
        _configRepositoryMock.Setup(x => x.GetByIdAsync(configId)).ReturnsAsync(config);

        // Act
        var result = await _service.AddKeyAsync(configId, key, "user1");

        // Assert
        result.Key.Should().Be("TestKey");
        result.Value.Should().Be("TestValue");
        _keyRepositoryMock.Verify(x => x.AddAsync(key), Times.Once);
    }

    /// <summary>
    /// Tests that <see cref="ConfigurationService.AddKeyAsync"/> encrypts the key value
    /// when the configuration has encryption enabled.
    /// </summary>
    [Fact]
    public async Task AddKeyAsync_ShouldEncryptKey_WhenConfigEncrypted()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, IsEncrypted = true };
        var key = new ConfigurationKey { Key = "SecretKey", Value = "PlainText" };
        _configRepositoryMock.Setup(x => x.GetByIdAsync(configId)).ReturnsAsync(config);
        _encryptionServiceMock.Setup(x => x.EncryptAsync("PlainText", configId)).ReturnsAsync("EncryptedValue");

        // Act
        var result = await _service.AddKeyAsync(configId, key, "user1");

        // Assert
        result.IsEncrypted.Should().BeTrue();
        result.Value.Should().Be("EncryptedValue");
    }
}
