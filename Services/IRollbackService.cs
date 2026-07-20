#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for executing rollbacks and reading rollback history.
/// </summary>
public interface IRollbackService
{
    /// <summary>
    /// Executes a rollback to a target version and publishes the newly created version.
    /// </summary>
    Task<RollbackResult> ExecuteRollbackAsync(Guid configurationId, Guid targetVersionId, string reason, string userId);

    /// <summary>
    /// Gets rollback history for a configuration.
    /// </summary>
    Task<List<RollbackRecord>> GetRollbackHistoryAsync(Guid configurationId);

    /// <summary>
    /// Generates a preview of changes that would be applied during a rollback to a target version.
    /// Returns the set of changes without applying them.
    /// </summary>
    Task<RollbackPreview> PreviewRollbackAsync(Guid configurationId, Guid targetVersionId, string userId);
}
