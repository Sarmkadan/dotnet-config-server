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
    /// <param name="configuration">The configuration object to create.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The created configuration.</returns>
    Task<Configuration> CreateAsync(Configuration configuration, string userId);

    /// <summary>
    /// Gets a configuration by ID
    /// </summary>
    /// <param name="id">The ID of the configuration to retrieve.</param>
    /// <returns>The configuration if found, otherwise null.</returns>
    Task<Configuration?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <returns>A list of configurations.</returns>
    Task<List<Configuration>> GetByApplicationAsync(Guid applicationId);

    /// <summary>
    /// Updates a configuration
    /// </summary>
    /// <param name="id">The ID of the configuration to update.</param>
    /// <param name="configuration">The configuration data to update.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The updated configuration.</returns>
    Task<Configuration> UpdateAsync(Guid id, Configuration configuration, string userId);

    /// <summary>
    /// Deletes a configuration (soft delete)
    /// </summary>
    /// <param name="id">The ID of the configuration to delete.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    Task DeleteAsync(Guid id, string userId);

    /// <summary>
    /// Adds a key to a configuration version
    /// </summary>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <param name="key">The configuration key to add.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The added configuration key.</returns>
    Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKey key, string userId);

    /// <summary>
    /// Updates a configuration key
    /// </summary>
    /// <param name="keyId">The ID of the key to update.</param>
    /// <param name="value">The new value for the key.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The updated configuration key.</returns>
    Task<ConfigurationKey> UpdateKeyAsync(Guid keyId, string value, string userId);

    /// <summary>
    /// Gets all keys for a configuration version
    /// </summary>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <param name="versionId">The optional ID of the version to retrieve keys for.</param>
    /// <param name="resolveInheritance">Whether to resolve inheritance.</param>
    /// <returns>A list of configuration keys.</returns>
    Task<List<ConfigurationKey>> GetKeysAsync(Guid configurationId, Guid? versionId = null, bool resolveInheritance = true);

    /// <summary>
    /// Gets a specific configuration key
    /// </summary>
    /// <param name="keyId">The ID of the key to retrieve.</param>
    /// <returns>The configuration key if found, otherwise null.</returns>
    Task<ConfigurationKey?> GetKeyAsync(Guid keyId);

    /// <summary>
    /// Deletes a configuration key
    /// </summary>
    /// <param name="keyId">The ID of the key to delete.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    Task DeleteKeyAsync(Guid keyId, string userId);

    /// <summary>
    /// Searches for configurations
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="applicationId">The optional ID of the application.</param>
    /// <returns>A list of configurations matching the query.</returns>
    Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null);

    /// <summary>
    /// Searches for configuration keys by text and/or prefix
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="prefix">The key prefix.</param>
    /// <param name="configurationId">The ID of the configuration.</param>
    /// <returns>A list of configuration keys matching the search criteria.</returns>
    Task<List<ConfigurationKey>> SearchKeysAsync(string? query, string? prefix, Guid? configurationId);

    /// <summary>
    /// Gets configuration count for an application
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <returns>The number of configurations for the application.</returns>
    Task<int> GetConfigurationCountAsync(Guid applicationId);
}
