#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Services;

/// <summary>
/// Service for creating and managing configuration snapshots.
/// Snapshots capture the complete state of a configuration at a specific point in time.
/// </summary>
public interface IConfigurationSnapshotService
{
    /// <summary>
    /// Creates a snapshot of the current configuration state.
    /// </summary>
    Task<ConfigurationSnapshot> CreateSnapshotAsync(Guid configurationId, string userId, string? reason = null);

    /// <summary>
    /// Gets a snapshot by ID.
    /// </summary>
    Task<ConfigurationSnapshot?> GetSnapshotAsync(Guid snapshotId);

    /// <summary>
    /// Gets all snapshots for a configuration.
    /// </summary>
    Task<List<ConfigurationSnapshot>> GetConfigurationSnapshotsAsync(Guid configurationId);

    /// <summary>
    /// Restores a configuration to a specific snapshot.
    /// </summary>
    Task RestoreFromSnapshotAsync(Guid snapshotId, string userId, string reason);
}

public sealed class ConfigurationSnapshotService : IConfigurationSnapshotService
{
    private readonly IConfigurationRepository _configRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly IConfigurationSnapshotRepository _snapshotRepository;
    private readonly ILogger<ConfigurationSnapshotService> _logger;

    public ConfigurationSnapshotService(
        IConfigurationRepository configRepository,
        IConfigurationKeyRepository keyRepository,
        IConfigurationSnapshotRepository snapshotRepository,
        ILogger<ConfigurationSnapshotService> logger)
    {
        _configRepository = configRepository;
        _keyRepository = keyRepository;
        _snapshotRepository = snapshotRepository;
        _logger = logger;
    }

    public async Task<ConfigurationSnapshot> CreateSnapshotAsync(Guid configurationId, string userId, string? reason = null)
    {
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config is null)
            throw new ConfigurationNotFoundException(configurationId.ToString());

        var keys = await _keyRepository.GetByConfigurationAsync(configurationId);

        var snapshot = new ConfigurationSnapshot
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            ConfigurationState = SerializeConfiguration(config),
            KeysState = SerializeKeys(keys),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Reason = reason
        };

        await _snapshotRepository.AddAsync(snapshot);
        await _snapshotRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration snapshot {SnapshotId} created for {ConfigId}", snapshot.Id, configurationId);
        return snapshot;
    }

    public async Task<ConfigurationSnapshot?> GetSnapshotAsync(Guid snapshotId)
    {
        return await _snapshotRepository.GetByIdAsync(snapshotId);
    }

    public async Task<List<ConfigurationSnapshot>> GetConfigurationSnapshotsAsync(Guid configurationId)
    {
        return await _snapshotRepository.GetByConfigurationAsync(configurationId);
    }

    public async Task RestoreFromSnapshotAsync(Guid snapshotId, string userId, string reason)
    {
        var snapshot = await _snapshotRepository.GetByIdAsync(snapshotId);
        if (snapshot is null)
            throw new ConfigurationSnapshotNotFoundException(snapshotId.ToString());

        var config = DeserializeConfiguration(snapshot.ConfigurationState);
        var keys = DeserializeKeys(snapshot.KeysState);

        // Restore configuration (update existing or create new)
        var existingConfig = await _configRepository.GetByIdAsync(snapshot.ConfigurationId);
        if (existingConfig is not null)
        {
            existingConfig.Name = config.Name;
            existingConfig.Description = config.Description;
            existingConfig.Environment = config.Environment;
            existingConfig.IsActive = config.IsActive;
            existingConfig.IsEncrypted = config.IsEncrypted;
            await _configRepository.UpdateAsync(existingConfig);
        }
        else
        {
            config.Id = snapshot.ConfigurationId; // Ensure ID matches original
            await _configRepository.AddAsync(config);
        }
        await _configRepository.SaveChangesAsync();

        // Deactivate old keys and add new ones
        var currentKeys = await _keyRepository.GetByConfigurationAsync(snapshot.ConfigurationId);
        foreach (var key in currentKeys)
        {
            key.IsActive = false; // Mark as inactive
            await _keyRepository.UpdateAsync(key);
        }
        await _keyRepository.SaveChangesAsync();

        foreach (var keyData in keys)
        {
            await _keyRepository.AddAsync(new ConfigurationKey
            {
                Id = Guid.NewGuid(),
                ConfigurationId = snapshot.ConfigurationId,
                Key = keyData.Key,
                Value = keyData.Value,
                IsEncrypted = keyData.IsEncrypted,
                IsActive = keyData.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }
        await _keyRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration {ConfigId} restored from snapshot {SnapshotId} by {UserId}: {Reason}", snapshot.ConfigurationId, snapshotId, userId, reason);
    }

    private string SerializeConfiguration(Configuration config)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            config.Name,
            config.Description,
            config.Environment,
            config.IsActive,
            config.IsEncrypted
        });
    }

    private Configuration DeserializeConfiguration(string configState)
    {
        var anonymousObject = System.Text.Json.JsonSerializer.Deserialize<dynamic>(configState)
            ?? throw new InvalidOperationException("Failed to deserialize configuration state.");

        return new Configuration
        {
            Name = anonymousObject.Name,
            Description = anonymousObject.Description,
            Environment = anonymousObject.Environment,
            IsActive = anonymousObject.IsActive,
            IsEncrypted = anonymousObject.IsEncrypted
        };
    }

    private string SerializeKeys(List<ConfigurationKey> keys)
    {
        return System.Text.Json.JsonSerializer.Serialize(keys.Select(k => new
        {
            k.Key,
            k.Value,
            k.IsEncrypted,
            k.IsActive
        }));
    }

    private List<ConfigurationKeyData> DeserializeKeys(string keysState)
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<ConfigurationKeyData>>(keysState)
            ?? new List<ConfigurationKeyData>();
    }

    private class ConfigurationKeyData
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public bool IsActive { get; set; }
    }
}



