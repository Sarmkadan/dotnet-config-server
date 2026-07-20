#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Common;

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
      existingConfig.UpdatedAt = DateTime.UtcNow;
      await _configRepository.UpdateAsync(existingConfig);
    }
    else
    {
      config.Id = snapshot.ConfigurationId; // Ensure ID matches original
      config.CreatedAt = DateTime.UtcNow;
      config.UpdatedAt = DateTime.UtcNow;
      await _configRepository.AddAsync(config);
    }
    await _configRepository.SaveChangesAsync();

    // Deactivate old keys and add new ones
    var currentKeys = await _keyRepository.GetByConfigurationAsync(snapshot.ConfigurationId);
    foreach (var key in currentKeys)
    {
      key.IsActive = false; // Mark as inactive
      key.UpdatedAt = DateTime.UtcNow;
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
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = userId
      });
    }
    await _keyRepository.SaveChangesAsync();

    _logger.LogInformation("Configuration {ConfigId} restored from snapshot {SnapshotId} by {UserId}: {Reason}", snapshot.ConfigurationId, snapshotId, userId, reason);
  }

  private string SerializeConfiguration(Configuration config)
  {
    var configData = new
    {
      config.Name,
      config.Description,
      config.Environment,
      config.IsActive,
      config.IsEncrypted,
      config.ApplicationId,
      config.EncryptionAlgorithm,
      config.EncryptionKeyId,
      config.VersionNumber,
      config.CreatedAt,
      config.UpdatedAt
    };

    return System.Text.Json.JsonSerializer.Serialize(configData);
  }

  private Configuration DeserializeConfiguration(string configState)
  {
    var configData = System.Text.Json.JsonSerializer.Deserialize<ConfigurationData>(configState)
    ?? throw new InvalidOperationException("Failed to deserialize configuration state.");

    return new Configuration
    {
      Name = configData.Name,
      Description = configData.Description,
      Environment = (DotnetConfigServer.Common.Environment)configData.Environment,
      IsActive = configData.IsActive,
      IsEncrypted = configData.IsEncrypted,
      ApplicationId = configData.ApplicationId,
      EncryptionAlgorithm = (DotnetConfigServer.Common.EncryptionAlgorithm)configData.EncryptionAlgorithm,
      EncryptionKeyId = configData.EncryptionKeyId,
      VersionNumber = configData.VersionNumber,
      CreatedAt = configData.CreatedAt,
      UpdatedAt = configData.UpdatedAt
    };
  }

  private string SerializeKeys(List<ConfigurationKey> keys)
  {
    var keyDataList = keys.Select(k => new
    {
      k.Key,
      k.Value,
      k.IsEncrypted,
      k.IsActive,
      k.ValueType,
      k.DefaultValue,
      k.Description,
      k.IsRequired,
      k.IsSensitive,
      k.ValidationRegex,
      k.MinLength,
      k.MaxLength,
      k.AllowedValues,
      k.MinValue,
      k.MaxValue,
      k.ValidateAsUrl,
      k.ValidateAsJson,
      k.CreatedAt,
      k.UpdatedAt
    }).ToList();

    return System.Text.Json.JsonSerializer.Serialize(keyDataList);
  }

  private List<ConfigurationKeyData> DeserializeKeys(string keysState)
  {
    var keyDataList = System.Text.Json.JsonSerializer.Deserialize<List<ConfigurationKeyData>>(keysState)
    ?? new List<ConfigurationKeyData>();

    return keyDataList;
  }

  private class ConfigurationData
  {
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Environment { get; set; }
    public bool IsActive { get; set; }
    public bool IsEncrypted { get; set; }
    public Guid ApplicationId { get; set; }
    public int EncryptionAlgorithm { get; set; }
    public string? EncryptionKeyId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }

  private class ConfigurationKeyData
  {
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public bool IsActive { get; set; }
    public int ValueType { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public string? ValidationRegex { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? AllowedValues { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public bool ValidateAsUrl { get; set; }
    public bool ValidateAsJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}
