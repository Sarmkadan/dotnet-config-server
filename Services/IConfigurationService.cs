#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for centralized configuration management. Provides CRUD operations
/// for configurations, key-value management with versioning, and full audit trail of changes.
/// </summary>
/// <remarks>
/// <para>
/// Configurations are organized hierarchically: Application -> Configuration -> Version -> Keys.
/// Each mutation (create, update, delete) records the <paramref name="userId"/> for audit purposes.
/// Deletions are soft-deletes - data is marked as deleted but not physically removed.
/// </para>
/// <para>
/// Use <see cref="GetKeysAsync"/> with a specific <paramref name="versionId"/> to retrieve
/// keys for a particular version, or omit it to get keys from the latest version.
/// </para>
/// </remarks>
public interface IConfigurationService
{
    /// <summary>Creates a new configuration under an application. Records creation in the audit log.</summary>
    /// <param name="configuration">The configuration to create.</param>
    /// <param name="userId">Identifier of the user performing the action.</param>
    Task<Configuration> CreateAsync(Configuration configuration, string userId);

    /// <summary>Retrieves a configuration by its unique ID, or <c>null</c> if not found.</summary>
    Task<Configuration?> GetByIdAsync(Guid id);

    /// <summary>Returns all configurations belonging to the specified application.</summary>
    Task<List<Configuration>> GetByApplicationAsync(Guid applicationId);

    /// <summary>Updates an existing configuration. Creates a new version if key values changed.</summary>
    Task<Configuration> UpdateAsync(Guid id, Configuration configuration, string userId);

    /// <summary>Soft-deletes a configuration. The data is preserved but excluded from queries.</summary>
    Task DeleteAsync(Guid id, string userId);

    /// <summary>Adds a key-value pair to a configuration's current version.</summary>
    Task<ConfigurationKey> AddKeyAsync(Guid configurationId, ConfigurationKey key, string userId);

    /// <summary>Updates the value of an existing configuration key.</summary>
    Task<ConfigurationKey> UpdateKeyAsync(Guid keyId, string value, string userId);

    /// <summary>Returns all keys for a configuration, optionally filtered to a specific version.</summary>
    /// <param name="configurationId">The parent configuration ID.</param>
    /// <param name="versionId">If specified, returns keys for this version only. Otherwise returns the latest.</param>
    Task<List<ConfigurationKey>> GetKeysAsync(Guid configurationId, Guid? versionId = null);

    /// <summary>Retrieves a specific configuration key by its ID, or <c>null</c>.</summary>
    Task<ConfigurationKey?> GetKeyAsync(Guid keyId);

    /// <summary>Soft-deletes a configuration key.</summary>
    Task DeleteKeyAsync(Guid keyId, string userId);

    /// <summary>Searches configurations by name or description, optionally scoped to an application.</summary>
    Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null);

    /// <summary>Returns the total number of configurations for an application.</summary>
    Task<int> GetConfigurationCountAsync(Guid applicationId);
}
