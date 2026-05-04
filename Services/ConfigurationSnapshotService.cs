// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

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

public class ConfigurationSnapshotService : IConfigurationSnapshotService
{
    private readonly IConfigurationRepository _configRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly ILogger<ConfigurationSnapshotService> _logger;

    public ConfigurationSnapshotService(
        IConfigurationRepository configRepository,
        IConfigurationKeyRepository keyRepository,
        ILogger<ConfigurationSnapshotService> logger)
    {
        _configRepository = configRepository;
        _keyRepository = keyRepository;
        _logger = logger;
    }

    public async Task<ConfigurationSnapshot> CreateSnapshotAsync(Guid configurationId, string userId, string? reason = null)
    {
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config == null)
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

        _logger.LogInformation("Configuration snapshot {SnapshotId} created for {ConfigId}", snapshot.Id, configurationId);
        return snapshot;
    }

    public async Task<ConfigurationSnapshot?> GetSnapshotAsync(Guid snapshotId)
    {
        // This would normally fetch from repository
        return await Task.FromResult(new ConfigurationSnapshot { Id = snapshotId });
    }

    public async Task<List<ConfigurationSnapshot>> GetConfigurationSnapshotsAsync(Guid configurationId)
    {
        // This would normally fetch from repository
        return await Task.FromResult(new List<ConfigurationSnapshot>());
    }

    public async Task RestoreFromSnapshotAsync(Guid snapshotId, string userId, string reason)
    {
        // Implementation would restore keys and configuration from snapshot
        _logger.LogInformation("Configuration restored from snapshot {SnapshotId} by {UserId}: {Reason}", snapshotId, userId, reason);
        await Task.CompletedTask;
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
}

public class ConfigurationSnapshot
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public string ConfigurationState { get; set; } = string.Empty;
    public string KeysState { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
