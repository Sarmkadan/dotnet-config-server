#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Storage abstraction for configurations and their versions.
/// Decouples business services (diffing, versioning, exporting) from the
/// concrete persistence technology, so the backing store can later be
/// swapped for a distributed cache or replicated store without touching
/// consumers.
/// </summary>
public interface IConfigStore
{
    /// <summary>
    /// Retrieves a configuration by its identifier.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The configuration if found, otherwise <see langword="null"/>.</returns>
    Task<Configuration?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a configuration.
    /// </summary>
    /// <param name="configuration">The configuration to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The persisted configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <see langword="null"/>.</exception>
    Task<Configuration> SetAsync(Configuration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all versions recorded for a configuration, most recent first.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>The list of known versions for the configuration.</returns>
    Task<IReadOnlyList<ConfigurationVersion>> ListVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a configuration by its identifier.
    /// </summary>
    /// <param name="id">The configuration identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><see langword="true"/> if the configuration existed and was deleted, otherwise <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a lightweight round-trip against the backing store to verify it is reachable.
    /// Used by readiness health checks.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns><see langword="true"/> if the store responded successfully.</returns>
    Task<bool> IsReachableAsync(CancellationToken cancellationToken = default);
}
