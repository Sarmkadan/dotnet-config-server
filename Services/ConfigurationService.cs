#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Events;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Services;

/// <summary>
/// Service for managing configurations
/// </summary>
sealed public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _configRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IConfigurationRepository configRepository,
        IConfigurationKeyRepository keyRepository,
        IEncryptionService encryptionService,
        IAuditLogRepository auditLogRepository,
        IEventBus eventBus,
        ILogger<ConfigurationService> logger)
    {
        _configRepository = configRepository;
        _keyRepository = keyRepository;
        _encryptionService = encryptionService;
        _auditLogRepository = auditLogRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    public async Task<Configuration> CreateAsync(Configuration configuration, string userId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        
        configuration.Validate();
        configuration.CreatedBy = userId;
        configuration.UpdatedAt = DateTime.UtcNow;

        if (configuration.ParentConfigurationId.HasValue)
        {
            var parentConfig = await _configRepository.GetByIdAsync(configuration.ParentConfigurationId.Value);
            if (parentConfig is null)
            {
                throw new ConfigurationNotFoundException($"Parent configuration {configuration.ParentConfigurationId} not found.");
            }
        }

        await _configRepository.AddAsync(configuration);
        await _configRepository.SaveChangesAsync();

        // Log creation
        var auditEntry = AuditLog.CreateEntry(
            configuration.Id,
            nameof(Configuration),
            configuration.Id.ToString(),
            configuration.Name,
            userId,
            null,
            $"Created configuration '{configuration.Name}' for application {configuration.ApplicationId}"
        );
        await _auditLogRepository.AddAsync(auditEntry);
        await _auditLogRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration {ConfigId} created by {UserId}", configuration.Id, userId);
        return configuration;
    }

    /// <summary>
    /// Gets a configuration by ID
    /// </summary>
    public async Task<Configuration?> GetByIdAsync(Guid id)
    {
        var config = await _configRepository.GetByIdAsync(id);
        if (config?.DeletedAt is not null)
            return null;
        return config;
    }

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    public async Task<List<Configuration>> GetByApplicationAsync(Guid applicationId)
    {
        return await _configRepository.GetByApplicationIdAsync(applicationId);
    }

    /// <summary>
    /// Updates a configuration
    /// </summary>
    public async Task<Configuration> UpdateAsync(Guid id, Configuration configuration, string userId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
    
        var existing = await _configRepository.GetByIdAsync(id);
        if (existing is null)
            throw new ConfigurationNotFoundException(id.ToString());

        // Hotfix: Fixed update method to properly trigger hot reload when nested config values change
        existing.Update(configuration.Name, configuration.Description, configuration.Environment, configuration.IsActive, userId);
        existing.ParentConfigurationId = configuration.ParentConfigurationId; // Update parent ID

        existing.Validate();

        // Validate parent configuration exists and no circular dependency
        if (existing.ParentConfigurationId.HasValue)
        {
            var parentConfig = await _configRepository.GetByIdAsync(existing.ParentConfigurationId.Value);
            if (parentConfig is null)
            {
                throw new ConfigurationNotFoundException($"Parent configuration {existing.ParentConfigurationId} not found.");
            }
            // Check for circular dependency by attempting to resolve keys (without actual data processing)
            // A simple way to check for circularity is to call GetKeysInternalAsync, which will throw
            // a ConfigurationException if a circular dependency is detected.
            try
            {
                await GetKeysInternalAsync(existing.Id, null, false, new HashSet<Guid>()); // false to avoid resolving parent keys for this check
            }
            catch (ConfigurationException ex) when (ex.Message.Contains("Circular dependency detected"))
            {
                throw new ConfigurationException($"Update creates a circular dependency in configuration inheritance: {ex.Message}");
            }
        }

        await _configRepository.UpdateAsync(existing);
        await _configRepository.SaveChangesAsync();

        // Log update
        var auditEntry = AuditLog.UpdateEntry(
            existing.Id,
            nameof(Configuration),
            existing.Id.ToString(),
            existing.Name,
            userId,
            null,
            oldValues: $"Name={configuration.Name}", // This part of logging needs to be improved to reflect all changes
            newValues: $"Name={existing.Name}"
        );
        await _auditLogRepository.AddAsync(auditEntry);
        await _auditLogRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration {ConfigId} updated by {UserId}", id, userId);
        return existing;
    }

    /// <summary>
    /// Deletes a configuration (soft delete)
    /// </summary>
    public async Task DeleteAsync(Guid id, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    
        var config = await _configRepository.GetByIdAsync(id);
        if (config is null)
            throw new ConfigurationNotFoundException(id.ToString());

        config.Delete(userId);
        await _configRepository.UpdateAsync(config);
        await _configRepository.SaveChangesAsync();

        // Log deletion
        var auditEntry = AuditLog.DeleteEntry(
            config.Id,
            nameof(Configuration),
            config.Id.ToString(),
            config.Name,
            userId,
            null,
            oldValues: config.Name
        );
        await _auditLogRepository.AddAsync(auditEntry);
        await _auditLogRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration {ConfigId} deleted by {UserId}", id, userId);
    }

    /// <summary>
    /// Adds a key to a configuration version
    /// </summary>
    public async Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKey key, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(configurationId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config is null)
            throw new ConfigurationNotFoundException(configurationId.ToString());

        key.Validate();
        key.ConfigurationId = configurationId;
        key.CreatedBy = userId;

        // Encrypt if needed
        if (config.IsEncrypted && !key.IsEncrypted)
        {
            key.Value = await _encryptionService.EncryptAsync(key.Value, configurationId);
            key.IsEncrypted = true;
        }

        await _keyRepository.AddAsync(key);
        await _keyRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration key {KeyName} added to config {ConfigId} by {UserId}", key.Key, configurationId, userId);
        return key;
    }

    /// <summary>
    /// Updates a configuration key value
    /// </summary>
    public async Task<ConfigurationKey> UpdateKeyAsync(Guid keyId, string value, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(keyId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    
        var key = await _keyRepository.GetByIdAsync(keyId);
        if (key is null)
            throw new ConfigurationKeyNotFoundException(keyId.ToString());

        var oldValue = key.Value;
        
        // If the key is encrypted, encrypt the new value before updating
        if (key.IsEncrypted)
        {
            value = await _encryptionService.EncryptAsync(value, key.ConfigurationId);
        }

        key.Update(value, key.Description, userId);
        key.Validate();

        await _keyRepository.UpdateAsync(key);
        await _keyRepository.SaveChangesAsync();

        // Publish event so cache is invalidated and hot-reload subscribers are notified
        await _eventBus.PublishAsync(new ConfigurationKeyChangedEvent
        {
            KeyId = key.Id,
            ConfigurationId = key.ConfigurationId,
            Key = key.Key,
            OldValue = oldValue,
            NewValue = key.Value,
            IsEncrypted = key.IsEncrypted,
            UserId = userId
        });

        _logger.LogInformation("Configuration key {KeyId} updated by {UserId}", keyId, userId);
        return key;
    }

    /// <summary>
    /// Gets all keys for a configuration version, optionally resolving inheritance.
    /// </summary>
    public async Task<List<ConfigurationKey>> GetKeysAsync(Guid configurationId, Guid? versionId = null, bool resolveInheritance = true)
    {
        return await GetKeysInternalAsync(configurationId, versionId, resolveInheritance, new HashSet<Guid>());
    }

    /// <summary>
    /// Internal method to get keys with inheritance resolution and circular dependency detection.
    /// </summary>
    private async Task<List<ConfigurationKey>> GetKeysInternalAsync(Guid configurationId, Guid? versionId, bool resolveInheritance, HashSet<Guid> visitedConfigs)
    {
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config is null)
        {
            throw new ConfigurationNotFoundException(configurationId.ToString());
        }

        // Add current config to visited set to detect circular dependencies
        if (!visitedConfigs.Add(configurationId))
        {
            throw new ConfigurationException($"Circular dependency detected for configuration {configurationId}");
        }

        var currentConfigKeys = await GetRawKeysAsync(configurationId, versionId);
        var resolvedKeys = new Dictionary<string, ConfigurationKey>();

        // Add current config's keys, these take precedence
        foreach (var key in currentConfigKeys)
        {
            resolvedKeys[key.Key] = key;
        }

        // If inheritance is enabled and a parent exists, resolve parent keys
        if (resolveInheritance && config.ParentConfigurationId.HasValue)
        {
            try
            {
                var parentKeys = await GetKeysInternalAsync(config.ParentConfigurationId.Value, null, true, visitedConfigs); // Always resolve parent fully
                foreach (var parentKey in parentKeys)
                {
                    if (!resolvedKeys.ContainsKey(parentKey.Key))
                    {
                        resolvedKeys[parentKey.Key] = parentKey;
                    }
                }
            }
            catch (ConfigurationNotFoundException ex)
            {
                _logger.LogWarning(ex, "Parent configuration {ParentId} not found for configuration {ConfigId}. Inheritance chain broken.", config.ParentConfigurationId, configurationId);
            }
        }

        // Apply decryption to the final merged and resolved list
        var finalKeys = new List<ConfigurationKey>();
        foreach (var key in resolvedKeys.Values)
        {
            var clonedKey = new ConfigurationKey
            {
                Id = key.Id,
                Key = key.Key,
                Value = key.Value, // This will be replaced if encrypted
                DefaultValue = key.DefaultValue,
                Description = key.Description,
                ValueType = key.ValueType,
                ConfigurationId = key.ConfigurationId,
                VersionId = key.VersionId,
                CreatedAt = key.CreatedAt,
                UpdatedAt = key.UpdatedAt,
                CreatedBy = key.CreatedBy,
                UpdatedBy = key.UpdatedBy,
                IsActive = key.IsActive,
                IsEncrypted = key.IsEncrypted,
                IsRequired = key.IsRequired,
                IsSensitive = key.IsSensitive,
                ValidationRegex = key.ValidationRegex,
                MinLength = key.MinLength,
                MaxLength = key.MaxLength,
                AllowedValues = key.AllowedValues,
                MinValue = key.MinValue,
                MaxValue = key.MaxValue,
                ValidateAsUrl = key.ValidateAsUrl,
                ValidateAsJson = key.ValidateAsJson
            };

            if (clonedKey.IsEncrypted)
            {
                try
                {
                    clonedKey.Value = await _encryptionService.DecryptAsync(clonedKey.Value, configurationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decrypt key {KeyId} for configuration {ConfigId}. Returning [DECRYPTION_FAILED].", clonedKey.Id, configurationId);
                    clonedKey.Value = "[DECRYPTION_FAILED]";
                }
            }
            finalKeys.Add(clonedKey);
        }

        visitedConfigs.Remove(configurationId); // Remove from visited when returning from recursion
        return finalKeys;
    }

    /// <summary>
    /// Gets keys directly associated with a configuration and version, without inheritance or decryption.
    /// </summary>
    private async Task<List<ConfigurationKey>> GetRawKeysAsync(Guid configurationId, Guid? versionId)
    {
        return (await _keyRepository.GetByConfigurationAsync(configurationId))
               .Where(k => k.IsActive && (!versionId.HasValue || k.VersionId == versionId.Value))
               .ToList();
    }

    /// <summary>
    /// Gets a specific configuration key
    /// </summary>
    public async Task<ConfigurationKey?> GetKeyAsync(Guid keyId)
    {
        return await _keyRepository.GetByIdAsync(keyId);
    }

    /// <summary>
    /// Deletes a configuration key
    /// </summary>
    public async Task DeleteKeyAsync(Guid keyId, string userId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(keyId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
    
        var key = await _keyRepository.GetByIdAsync(keyId);
        if (key is null)
            throw new ConfigurationKeyNotFoundException(keyId.ToString());

        key.Delete();
        await _keyRepository.UpdateAsync(key);
        await _keyRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration key {KeyId} deleted by {UserId}", keyId, userId);
    }

    /// <summary>
    /// Searches for configurations
    /// </summary>
    public async Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
    
        return await _configRepository.SearchAsync(query, applicationId);
    }

    /// <summary>
    /// Searches for configuration keys by text and/or prefix
    /// </summary>
    public async Task<List<ConfigurationKey>> SearchKeysAsync(string? query, string? prefix, Guid? configurationId)
    {
        return await _keyRepository.SearchAsync(query, prefix, configurationId);
    }

    /// <summary>
    /// Gets configuration count for an application
    /// </summary>
    public async Task<int> GetConfigurationCountAsync(Guid applicationId)
    {
        return await _configRepository.GetCountByApplicationAsync(applicationId);
    }
}
