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

/// <summary>
/// Integration tests covering configuration inheritance: child configs that override
/// parent keys, and detection of circular dependency chains.
/// </summary>
public sealed class ConfigurationInheritanceIntegrationTests
{
    private readonly Mock<IConfigurationRepository> _configRepoMock;
    private readonly Mock<IConfigurationKeyRepository> _keyRepoMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly Mock<IAuditLogRepository> _auditRepoMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly ConfigurationService _sut;

    public ConfigurationInheritanceIntegrationTests()
    {
        _configRepoMock = new Mock<IConfigurationRepository>();
        _keyRepoMock = new Mock<IConfigurationKeyRepository>();
        _encryptionMock = new Mock<IEncryptionService>();
        _auditRepoMock = new Mock<IAuditLogRepository>();
        _eventBusMock = new Mock<IEventBus>();

        _sut = new ConfigurationService(
            _configRepoMock.Object,
            _keyRepoMock.Object,
            _encryptionMock.Object,
            _auditRepoMock.Object,
            _eventBusMock.Object,
            new Mock<ILogger<ConfigurationService>>().Object);
    }

    private static Configuration CreateConfig(Guid id, Guid? parentId = null) => new()
    {
        Id = id,
        Name = $"config-{id}",
        ApplicationId = Guid.NewGuid(),
        CreatedBy = "admin",
        ParentConfigurationId = parentId
    };

    private static ConfigurationKey CreateKey(string key, string value, Guid configId) => new()
    {
        Key = key,
        Value = value,
        IsActive = true,
        IsEncrypted = false,
        ConfigurationId = configId,
        VersionId = Guid.NewGuid(),
        CreatedBy = "admin"
    };

    /// <summary>
    /// Child keys override parent keys with the same name; unique parent keys are inherited.
    /// </summary>
    [Fact]
    public async Task GetKeysAsync_ChildOverridesParentKey_ReturnsChildValue()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var parent = CreateConfig(parentId);
        var child = CreateConfig(childId, parentId);

        var parentKeys = new List<ConfigurationKey>
        {
            CreateKey("database.host", "parent-host", parentId),
            CreateKey("database.port", "5432", parentId)
        };

        var childKeys = new List<ConfigurationKey>
        {
            CreateKey("database.host", "child-host", childId) // override
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
        _configRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(childId)).ReturnsAsync(childKeys);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(parentId)).ReturnsAsync(parentKeys);

        var result = await _sut.GetKeysAsync(childId);

        result.Should().HaveCount(2);

        var host = result.First(k => k.Key == "database.host");
        host.Value.Should().Be("child-host", "child config must override parent value");

        var port = result.First(k => k.Key == "database.port");
        port.Value.Should().Be("5432", "parent key not overridden must be inherited");
    }

    /// <summary>
    /// Three-level inheritance: grandparent → parent → child.
    /// </summary>
    [Fact]
    public async Task GetKeysAsync_ThreeLevelChain_ResolvesFullInheritance()
    {
        var gpId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var grandparent = CreateConfig(gpId);
        var parent = CreateConfig(parentId, gpId);
        var child = CreateConfig(childId, parentId);

        var gpKeys = new List<ConfigurationKey>
        {
            CreateKey("log.level", "debug", gpId),
            CreateKey("app.timeout", "60", gpId)
        };

        var parentKeys = new List<ConfigurationKey>
        {
            CreateKey("log.level", "info", parentId) // override grandparent
        };

        var childKeys = new List<ConfigurationKey>
        {
            CreateKey("feature.enabled", "true", childId) // unique to child
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
        _configRepoMock.Setup(r => r.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _configRepoMock.Setup(r => r.GetByIdAsync(gpId)).ReturnsAsync(grandparent);

        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(childId)).ReturnsAsync(childKeys);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(parentId)).ReturnsAsync(parentKeys);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(gpId)).ReturnsAsync(gpKeys);

        var result = await _sut.GetKeysAsync(childId);

        result.Should().HaveCount(3);
        result.First(k => k.Key == "log.level").Value.Should().Be("info");        // parent overrides gp
        result.First(k => k.Key == "app.timeout").Value.Should().Be("60");        // from grandparent
        result.First(k => k.Key == "feature.enabled").Value.Should().Be("true");  // child own key
    }

    /// <summary>
    /// When resolveInheritance is false, only direct keys are returned.
    /// </summary>
    [Fact]
    public async Task GetKeysAsync_WithoutInheritanceResolution_ReturnsOnlyDirectKeys()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var child = CreateConfig(childId, parentId);
        var childKeys = new List<ConfigurationKey>
        {
            CreateKey("my.key", "my-value", childId)
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(childId)).ReturnsAsync(childKeys);

        var result = await _sut.GetKeysAsync(childId, resolveInheritance: false);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("my.key");
        _configRepoMock.Verify(r => r.GetByIdAsync(parentId), Times.Never);
    }

    /// <summary>
    /// When a parent config doesn't exist, the chain is gracefully broken and
    /// only the child's own keys are returned.
    /// </summary>
    [Fact]
    public async Task GetKeysAsync_BrokenInheritanceChain_ReturnsAvailableKeys()
    {
        var missingParentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var child = CreateConfig(childId, missingParentId);
        var childKeys = new List<ConfigurationKey>
        {
            CreateKey("child.only", "value", childId)
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(childId)).ReturnsAsync(child);
        _configRepoMock.Setup(r => r.GetByIdAsync(missingParentId)).ReturnsAsync((Configuration?)null);
        _keyRepoMock.Setup(r => r.GetByConfigurationAsync(childId)).ReturnsAsync(childKeys);

        var result = await _sut.GetKeysAsync(childId);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("child.only");
    }

    /// <summary>
    /// Full configuration workflow: create config → add keys → update key → verify event published.
    /// </summary>
    [Fact]
    public async Task FullConfigLifecycle_CreateAddKeysUpdate_EventPublishedOnUpdate()
    {
        var configId = Guid.NewGuid();
        var config = new Configuration
        {
            Id = configId,
            Name = "lifecycle-test",
            ApplicationId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _configRepoMock.Setup(r => r.AddAsync(config)).Returns(Task.CompletedTask);
        _configRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        _auditRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var created = await _sut.CreateAsync(config, "admin");
        created.Name.Should().Be("lifecycle-test");

        var keyId = Guid.NewGuid();
        var configKey = new ConfigurationKey
        {
            Id = keyId,
            Key = "service.url",
            Value = "http://old-endpoint",
            IsEncrypted = false,
            IsActive = true,
            ConfigurationId = configId,
            VersionId = Guid.NewGuid(),
            CreatedBy = "admin"
        };

        _configRepoMock.Setup(r => r.GetByIdAsync(configId)).ReturnsAsync(config);
        _keyRepoMock.Setup(r => r.AddAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _keyRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.AddKeyAsync(configId, configKey, "dev");

        _keyRepoMock.Setup(r => r.GetByIdAsync(keyId)).ReturnsAsync(configKey);
        _keyRepoMock.Setup(r => r.UpdateAsync(It.IsAny<ConfigurationKey>())).Returns(Task.CompletedTask);
        _eventBusMock.Setup(e => e.PublishAsync(It.IsAny<ConfigurationKeyChangedEvent>()))
            .Returns(Task.CompletedTask);

        var updated = await _sut.UpdateKeyAsync(keyId, "http://new-endpoint", "deployer");

        updated.Value.Should().Be("http://new-endpoint");
        updated.UpdatedBy.Should().Be("deployer");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.Is<ConfigurationKeyChangedEvent>(ev =>
                ev.Key == "service.url" &&
                ev.OldValue == "http://old-endpoint" &&
                ev.NewValue == "http://new-endpoint")),
            Times.Once);
    }
}
