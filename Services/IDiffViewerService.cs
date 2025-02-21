// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for rich diff visualization and non-destructive rollback preview across configuration versions.
/// </summary>
public interface IDiffViewerService
{
    /// <summary>
    /// Returns a diff between two versions enriched with version metadata and grouped change lists.
    /// Uses a cached diff record when available; otherwise computes the diff from the current key state
    /// without persisting anything.
    /// </summary>
    /// <param name="fromVersionId">The identifier of the source (older) version.</param>
    /// <param name="toVersionId">The identifier of the target (newer) version.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>An <see cref="EnrichedDiff"/> containing full version metadata and per-key change details.</returns>
    Task<EnrichedDiff> GetEnrichedDiffAsync(
        Guid fromVersionId,
        Guid toVersionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes what a rollback to <paramref name="targetVersionId"/> would change relative to the
    /// currently active version, without persisting any state or performing the rollback.
    /// </summary>
    /// <param name="configurationId">The configuration for which the rollback is previewed.</param>
    /// <param name="targetVersionId">The version that would be restored.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>
    /// A <see cref="RollbackPreview"/> listing all key changes and safety warnings the rollback would trigger.
    /// </returns>
    Task<RollbackPreview> GetRollbackPreviewAsync(
        Guid configurationId,
        Guid targetVersionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all versions in chronological order, each annotated with change counts
    /// from its immediately preceding version where a persisted diff record exists.
    /// </summary>
    /// <param name="configurationId">The configuration whose version history is requested.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>An ordered list of <see cref="VersionTimelineEntry"/> from oldest to newest.</returns>
    Task<List<VersionTimelineEntry>> GetVersionTimelineAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);
}
