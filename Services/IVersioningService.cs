#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for configuration versioning
/// </summary>
public interface IVersioningService
{
    /// <summary>
    /// Creates a new version of a configuration
    /// </summary>
    Task<ConfigurationVersion> CreateVersionAsync(Guid configurationId, string releaseNotes, string userId);

    /// <summary>
    /// Gets a configuration version
    /// </summary>
    Task<ConfigurationVersion?> GetVersionAsync(Guid versionId);

    /// <summary>
    /// Gets all versions of a configuration
    /// </summary>
    Task<List<ConfigurationVersion>> GetVersionsAsync(Guid configurationId);

    /// <summary>
    /// Gets the active (published) version of a configuration
    /// </summary>
    Task<ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId);

    /// <summary>
    /// Publishes a version (makes it the active version)
    /// </summary>
    Task<ConfigurationVersion> PublishVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Archives a version
    /// </summary>
    Task<ConfigurationVersion> ArchiveVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Deprecates a version
    /// </summary>
    Task<ConfigurationVersion> DeprecateVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Rolls back to a previous version
    /// </summary>
    Task<ConfigurationVersion> RollbackAsync(Guid configurationId, Guid previousVersionId, string userId);

    /// <summary>
    /// Gets version history for a configuration
    /// </summary>
    Task<List<ConfigurationVersionSummary>> GetVersionHistoryAsync(Guid configurationId);

    /// <summary>
    /// Cleans up old versions based on retention policy
    /// </summary>
    Task<int> CleanupOldVersionsAsync(Guid configurationId, int maxVersions);
}
