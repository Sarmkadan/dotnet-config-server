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
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="releaseNotes">Release notes for the version</param>
    /// <param name="userId">User ID creating the version</param>
    /// <param name="expectedVersionNumber">Optional: Expected current version number for optimistic concurrency check</param>
    /// <returns>The newly created configuration version</returns>
    /// <exception cref="OptimisticConcurrencyException">Thrown when the expected version doesn't match the actual current version</exception>
    Task<ConfigurationVersion> CreateVersionAsync(
        Guid configurationId,
        string releaseNotes,
        string userId,
        string? expectedVersionNumber = null);

    /// <summary>
    /// Gets a configuration version
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <returns>The configuration version, or null if not found</returns>
    Task<ConfigurationVersion?> GetVersionAsync(Guid versionId);

    /// <summary>
    /// Gets all versions of a configuration
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <returns>A list of all versions for the configuration</returns>
    Task<List<ConfigurationVersion>> GetVersionsAsync(Guid configurationId);

    /// <summary>
    /// Gets the active (published) version of a configuration
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <returns>The active configuration version, or null if none exists</returns>
    Task<ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId);

    /// <summary>
    /// Publishes a version (makes it the active version)
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <param name="userId">User ID publishing the version</param>
    /// <returns>The published configuration version</returns>
    Task<ConfigurationVersion> PublishVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Archives a version
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <param name="userId">User ID archiving the version</param>
    /// <returns>The archived configuration version</returns>
    Task<ConfigurationVersion> ArchiveVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Deprecates a version
    /// </summary>
    /// <param name="versionId">The version ID</param>
    /// <param name="userId">User ID deprecating the version</param>
    /// <returns>The deprecated configuration version</returns>
    Task<ConfigurationVersion> DeprecateVersionAsync(Guid versionId, string userId);

    /// <summary>
    /// Rolls back to a previous version
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="previousVersionId">The ID of the version to roll back to</param>
    /// <param name="userId">User ID performing the rollback</param>
    /// <returns>The newly created rollback version</returns>
    Task<ConfigurationVersion> RollbackAsync(Guid configurationId, Guid previousVersionId, string userId);

    /// <summary>
    /// Gets version history for a configuration
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <returns>A list of version summaries ordered by creation date, newest first</returns>
    Task<List<ConfigurationVersionSummary>> GetVersionHistoryAsync(Guid configurationId);

    /// <summary>
    /// Cleans up old versions based on retention policy
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="maxVersions">Maximum number of versions to retain</param>
    /// <returns>The number of versions archived during cleanup</returns>
    Task<int> CleanupOldVersionsAsync(Guid configurationId, int maxVersions);
}
