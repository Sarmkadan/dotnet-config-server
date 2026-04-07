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
}
