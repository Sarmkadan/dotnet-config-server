#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Creates a new configuration
    /// </summary>
    Task<Configuration> CreateAsync(Configuration configuration, string userId);

    /// <summary>
    /// Gets a configuration by ID
    /// </summary>
    Task<Configuration?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    Task<List<Configuration>> GetByApplicationAsync(Guid applicationId);

    /// <summary>
    /// Updates a configuration
    /// </summary>
    Task<Configuration> UpdateAsync(Guid id, Configuration configuration, string userId);

    /// <summary>
    /// Deletes a configuration (soft delete)
    /// </summary>
    Task DeleteAsync(Guid id, string userId);

    /// <summary>
    /// Adds a key to a configuration version
    /// </summary>
    Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKey key, string userId);

    /// <summary>
    /// Updates a configuration key
    /// </summary>
    Task<ConfigurationKey> UpdateKeyAsync(Guid keyId, string value, string userId);

    /// <summary>
    /// Gets all keys for a configuration version
    /// </summary>
    Task<List<ConfigurationKey>> GetKeysAsync(Guid configurationId, Guid? versionId = null);

    /// <summary>
    /// Gets a specific configuration key
    /// </summary>
    Task<ConfigurationKey?> GetKeyAsync(Guid keyId);

    /// <summary>
    /// Deletes a configuration key
    /// </summary>
    Task DeleteKeyAsync(Guid keyId, string userId);

    /// <summary>
    /// Searches for configurations
    /// </summary>
    Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null);

    /// <summary>
    /// Gets configuration count for an application
    /// </summary>
    Task<int> GetConfigurationCountAsync(Guid applicationId);
}
