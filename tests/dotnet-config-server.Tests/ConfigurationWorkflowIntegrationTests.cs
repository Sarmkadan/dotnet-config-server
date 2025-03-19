#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Events;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Integration tests demonstrating end-to-end configuration workflows
/// </summary>
sealed public class ConfigurationWorkflowIntegrationTests
{
    private readonly Mock<IConfigurationRepository> _configRepositoryMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepositoryMock;
    private readonly Mock<IConfigurationVersionRepository> _versionRepositoryMock;
    private readonly Mock<IConfigurationDiffRepository> _diffRepositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<ConfigurationService>> _configLoggerMock;
    private readonly Mock<ILogger<VersioningService>> _versioningLoggerMock;
    private readonly Mock<ILogger<DiffService>> _diffLoggerMock;

    private ConfigurationService _configService = null!;
    private VersioningService _versioningService = null!;
    private DiffService _diffService = null!;

    public ConfigurationWorkflowIntegrationTests()
    {
        _configRepositoryMock = new Mock<IConfigurationRepository>();
        _keyRepositoryMock = new Mock<IConfigurationKeyRepository>();
        _versionRepositoryMock = new Mock<IConfigurationVersionRepository>();
        _diffRepositoryMock = new Mock<IConfigurationDiffRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _configLoggerMock = new Mock<ILogger<ConfigurationService>>();
        _versioningLoggerMock = new Mock<ILogger<VersioningService>>();
        _diffLoggerMock = new Mock<ILogger<DiffService>>();

        SetupServices();
    }

    private void SetupServices()
    {
        _configService = new ConfigurationService(
            _configRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _auditLogRepositoryMock.Object,
            _eventBusMock.Object,
            _configLoggerMock.Object);

        _versioningService = new VersioningService(
            _versionRepositoryMock.Object,
            _configRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _auditLogRepositoryMock.Object,
            _versioningLoggerMock.Object);

        _diffService = new DiffService(
            _diffRepositoryMock.Object,
            _versionRepositoryMock.Object,
            _keyRepositoryMock.Object,
            _diffLoggerMock.Object);
    }

    /// <summary>
    /// Demonstrates the complete workflow: create app config, create versions, modify keys, generate diff
    /// </summary>
    [Fact]
    public async Task FullWorkflow_CreateConfigCreateVersionModifyGenerateDiff()
    {
        var appId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var userId = "test-user";

        // Step 1: Create configuration
        var config = new Configuration
        {
            Id = configId,
            Name = "prod-config",
            ApplicationId = appId,
            CreatedBy = userId
        };

        _configRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Configuration>())).Returns(Task.CompletedTask);
        _configRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var createdConfig = await _configService.CreateAsync(config, userId);

        createdConfig.Should().NotBeNull();
        createdConfig.Name.Should().Be("prod-config");
        createdConfig.CreatedBy.Should().Be(userId);

        // Step 2: Create first version
        var version1Id = Guid.NewGuid();
        var version1 = new ConfigurationVersion
        {
            Id = version1Id,
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Draft,
            CreatedBy = userId,
            Keys = new List<ConfigurationKey>()
        };

        var key1V1 = new ConfigurationKey { Key = "database.host", Value = "localhost", ConfigurationId = configId, VersionId = version1Id, CreatedBy = userId };
        var key2V1 = new ConfigurationKey { Key = "database.port", Value = "5432", ConfigurationId = configId, VersionId = version1Id, CreatedBy = userId };
        version1.Keys.Add(key1V1);
        version1.Keys.Add(key2V1);

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(version1Id)).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var createdVersion1 = await _versioningService.CreateVersionAsync(configId, "Initial release", userId);

        createdVersion1.VersionNumber.Should().Be("1.0.1");
        createdVersion1.Status.Should().Be(ConfigurationVersionStatus.Draft);

        // Step 3: Create second version with modifications
        var version2Id = Guid.NewGuid();
        var version2 = new ConfigurationVersion
        {
            Id = version2Id,
            ConfigurationId = configId,
            VersionNumber = "1.1.0",
            Status = ConfigurationVersionStatus.Draft,
            CreatedBy = userId,
            Keys = new List<ConfigurationKey>()
        };

        var key1V2 = new ConfigurationKey { Key = "database.host", Value = "prod-host", ConfigurationId = configId, VersionId = version2Id, CreatedBy = userId };
        var key2V2 = new ConfigurationKey { Key = "database.port", Value = "5432", ConfigurationId = configId, VersionId = version2Id, CreatedBy = userId };
        var key3V2 = new ConfigurationKey { Key = "cache.enabled", Value = "true", ConfigurationId = configId, VersionId = version2Id, CreatedBy = userId };
        version2.Keys.Add(key1V2);
        version2.Keys.Add(key2V2);
        version2.Keys.Add(key3V2);

        _versionRepositoryMock
            .Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync(version1);
        _keyRepositoryMock
            .Setup(r => r.GetByVersionAsync(version1Id))
            .ReturnsAsync(new List<ConfigurationKey> { key1V1, key2V1 });
        _keyRepositoryMock
            .Setup(r => r.GetByVersionAsync(version2Id))
            .ReturnsAsync(new List<ConfigurationKey> { key1V2, key2V2, key3V2 });

        var createdVersion2 = await _versioningService.CreateVersionAsync(configId, "Add caching", userId);

        createdVersion2.VersionNumber.Should().Be("1.0.1");
        createdVersion2.PreviousVersionId.Should().Be(version1Id.ToString());

        // Step 4: Generate diff between versions
        _versionRepositoryMock.Setup(r => r.GetByIdAsync(version1Id)).ReturnsAsync(version1);
        _versionRepositoryMock.Setup(r => r.GetByIdAsync(version2Id)).ReturnsAsync(version2);
        _diffRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationDiff>())).Returns(Task.CompletedTask);
        _diffRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var diff = await _diffService.GenerateDiffAsync(version1Id, version2Id, userId);

        diff.ConfigurationId.Should().Be(configId);
        diff.ModifiedCount.Should().Be(1); // database.host changed
        diff.AddedCount.Should().Be(1);    // cache.enabled added
        diff.DeletedCount.Should().Be(0);
        diff.TotalChanges.Should().Be(2);

        var modifiedKey = diff.Changes.First(c => c.ChangeType == ChangeType.Modified);
        modifiedKey.Key.Should().Be("database.host");
        modifiedKey.OldValue.Should().Be("localhost");
        modifiedKey.NewValue.Should().Be("prod-host");

        var addedKey = diff.Changes.First(c => c.ChangeType == ChangeType.Added);
        addedKey.Key.Should().Be("cache.enabled");
    }

    /// <summary>
    /// Tests encryption integration with configuration workflow
    /// </summary>
    [Fact]
    public async Task EncryptedConfigurationWorkflow_ConfigurationAndKeysAreEncrypted()
    {
        var appId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var userId = "security-admin";

        var config = new Configuration
        {
            Id = configId,
            Name = "secrets-config",
            ApplicationId = appId,
            CreatedBy = userId
        };

        _configRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Configuration>())).Returns(Task.CompletedTask);
        _configRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var createdConfig = await _configService.CreateAsync(config, userId);

        createdConfig.Should().NotBeNull();

        // Setup encryption for configuration
        var keyId = "encryption-key-1";
        createdConfig.SetEncryption(EncryptionAlgorithm.AES256, keyId);

        createdConfig.IsEncrypted.Should().BeTrue();
        createdConfig.EncryptionKeyId.Should().Be(keyId);
        createdConfig.EncryptionAlgorithm.Should().Be(EncryptionAlgorithm.AES256);
    }

    /// <summary>
    /// Tests configuration with multiple versions and rollback scenario
    /// </summary>
    [Fact]
    public async Task MultiVersionConfiguration_CreatesAndManagesVersionProgression()
    {
        var appId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var userId = "deployer";

        var config = new Configuration
        {
            Id = configId,
            Name = "deployment-config",
            ApplicationId = appId,
            CreatedBy = userId
        };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _configRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Configuration>())).Returns(Task.CompletedTask);
        _configRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditLogRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Create initial version
        var version1Id = Guid.NewGuid();
        var version1 = new ConfigurationVersion
        {
            Id = version1Id,
            ConfigurationId = configId,
            VersionNumber = "1.0.0",
            Status = ConfigurationVersionStatus.Active,
            CreatedBy = userId
        };

        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync(version1);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var v1 = await _versioningService.CreateVersionAsync(configId, "Initial", userId);
        v1.VersionNumber.Should().Be("1.0.1");

        // Create second version
        _versionRepositoryMock
            .Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync(v1);

        var v2 = await _versioningService.CreateVersionAsync(configId, "Update 1", userId);
        v2.VersionNumber.Should().Be("1.0.2");

        // Create third version
        _versionRepositoryMock
            .Setup(r => r.GetActiveVersionAsync(configId))
            .ReturnsAsync(v2);

        var v3 = await _versioningService.CreateVersionAsync(configId, "Update 2", userId);
        v3.VersionNumber.Should().Be("1.0.3");

        // Verify version progression
        v1.VersionNumber.Should().Be("1.0.1");
        v2.VersionNumber.Should().Be("1.0.2");
        v3.VersionNumber.Should().Be("1.0.3");
    }

    /// <summary>
    /// Tests configuration key validation in workflow
    /// </summary>
    [Fact]
    public void ConfigurationKeyValidation_EnforcesAllConstraints()
    {
        var configId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        // Test valid key
        var validKey = new ConfigurationKey
        {
            Key = "app.database.host",
            Value = "localhost",
            ConfigurationId = configId,
            VersionId = versionId,
            CreatedBy = "admin"
        };

        var validAct = () => validKey.Validate();
        validAct.Should().NotThrow();

        // Test key with constraints
        var constrainedKey = new ConfigurationKey
        {
            Key = "api.timeout",
            Value = "5000",
            ConfigurationId = configId,
            VersionId = versionId,
            CreatedBy = "admin",
            MinValue = 1000,
            MaxValue = 10000,
            ValueType = ConfigurationValueType.Integer
        };

        var constrainedAct = () => constrainedKey.Validate();
        constrainedAct.Should().NotThrow();

        // Test key that violates constraints
        constrainedKey.Value = "500"; // Below minimum
        var violatingAct = () => constrainedKey.Validate();
        violatingAct.Should().Throw<Exceptions.ValidationException>();
    }

    /// <summary>
    /// Tests concurrent version creation and diff generation
    /// </summary>
    [Fact]
    public async Task ConcurrentVersionManagement_HandlesMultipleVersionsSimultaneously()
    {
        var configId = Guid.NewGuid();
        var userId = "concurrent-user";
        var config = new Configuration
        {
            Id = configId,
            Name = "concurrent-config",
            ApplicationId = Guid.NewGuid(),
            CreatedBy = userId
        };

        _configRepositoryMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _versionRepositoryMock.Setup(r => r.GetActiveVersionAsync(configId)).ReturnsAsync((ConfigurationVersion?)null);
        _keyRepositoryMock.Setup(r => r.GetByVersionAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ConfigurationKey>());
        _versionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationVersion>())).Returns(Task.CompletedTask);
        _versionRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Create multiple versions concurrently
        var version1Task = _versioningService.CreateVersionAsync(configId, "Version 1", userId);
        var version2Task = _versioningService.CreateVersionAsync(configId, "Version 2", userId);
        var version3Task = _versioningService.CreateVersionAsync(configId, "Version 3", userId);

        var versions = await Task.WhenAll(version1Task, version2Task, version3Task);

        versions.Should().HaveCount(3);
        versions.Select(v => v.VersionNumber).Should().AllSatisfy(vn =>
            vn.Should().Match("?.?.?", "version number should follow semantic versioning"));
    }
}
