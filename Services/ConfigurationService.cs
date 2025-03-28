#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
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
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IConfigurationRepository configRepository,
        IConfigurationKeyRepository keyRepository,
        IEncryptionService encryptionService,
        IAuditLogRepository auditLogRepository,
        ILogger<ConfigurationService> logger)
    {
        _configRepository = configRepository;
        _keyRepository = keyRepository;
        _encryptionService = encryptionService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new configuration
    /// </summary>
    public async Task<Configuration> CreateAsync(Configuration configuration, string userId) { configuration.Validate(); configuration.CreatedBy = userId; configuration.UpdatedAt = DateTime.UtcNow; await _configRepository.AddAsync(configuration); await _configRepository.SaveChangesAsync(); var auditEntry = AuditLog.CreateEntry(configuration.Id, nameof(Configuration), configuration.Id.ToString(), configuration.Name, userId, null, $"Created configuration '{configuration.Name}' for application {configuration.ApplicationId}"); await _auditLogRepository.AddAsync(auditEntry); await _auditLogRepository.SaveChangesAsync(); _logger.LogInformation("Configuration {ConfigId} created by {UserId}", configuration.Id, userId); return configuration; }
    {
        configuration.Validate();
        configuration.CreatedBy = userId;
        configuration.UpdatedAt = DateTime.UtcNow;

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
        var existing = await _configRepository.GetByIdAsync(id);
        if (existing is null)
            throw new ConfigurationNotFoundException(id.ToString());

        // Hotfix: Fixed update method to properly trigger hot reload when nested config values change
        existing.Update(configuration.Name, configuration.Description, userId);
        existing.Validate();

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
            oldValues: $"Name={configuration.Name}",
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
        var key = await _keyRepository.GetByIdAsync(keyId);
        if (key is null)
            throw new ConfigurationKeyNotFoundException(keyId.ToString());

        var oldValue = key.Value;
        key.Update(value, key.Description, userId);
        key.Validate();

        await _keyRepository.UpdateAsync(key);
        await _keyRepository.SaveChangesAsync();

        _logger.LogInformation("Configuration key {KeyId} updated by {UserId}", keyId, userId);
        return key;
    }

    /// <summary>
    /// Gets all keys for a configuration version
    /// </summary>
    public async Task<List<ConfigurationKey>> GetKeysAsync(Guid configurationId, Guid? versionId = null)
    {
        var keys = await _keyRepository.GetByConfigurationAsync(configurationId);
        return keys.Where(k => !k.IsActive || (versionId.HasValue && k.VersionId == versionId.Value)).ToList();
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
        return await _configRepository.SearchAsync(query, applicationId);
    }

    /// <summary>
    /// Gets configuration count for an application
    /// </summary>
    public async Task<int> GetConfigurationCountAsync(Guid applicationId)
    {
        return await _configRepository.GetCountByApplicationAsync(applicationId);
    }
}
