#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service interface for configuration version diffing
/// </summary>
public interface IDiffService
{
    /// <summary>
    /// Generates a diff between two configuration versions using exact, case-sensitive comparison.
    /// </summary>
    Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId);

    /// <summary>
    /// Generates a diff between two configuration versions, optionally ignoring leading/trailing
    /// whitespace and treating blank-line-only values as equal.
    /// </summary>
    /// <param name="fromVersionId">The source version identifier.</param>
    /// <param name="toVersionId">The target version identifier.</param>
    /// <param name="userId">Identifier of the user requesting the diff.</param>
    /// <param name="ignoreWhitespaceAndBlankLines">
    /// When <c>true</c>, leading/trailing whitespace is trimmed and values that consist solely of
    /// whitespace (blank lines) are treated as equal.
    /// </param>
    Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId, bool ignoreWhitespaceAndBlankLines);

    /// <summary>
    /// Gets a previously generated diff
    /// </summary>
    Task<ConfigurationDiff?> GetDiffAsync(Guid diffId);

    /// <summary>
    /// Gets all diffs for a configuration
    /// </summary>
    Task<List<ConfigurationDiff>> GetDiffsAsync(Guid configurationId);

    /// <summary>
    /// Gets diff between current and previous version
    /// </summary>
    Task<ConfigurationDiff?> GetLatestDiffAsync(Guid configurationId);

    /// <summary>
    /// Gets changes for a specific key across versions
    /// </summary>
    Task<List<DiffEntry>> GetKeyHistoryAsync(Guid configurationId, string keyName);

    /// <summary>
    /// Compares two specific versions using exact, case-sensitive comparison and returns a summary.
    /// </summary>
    Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id);

    /// <summary>
    /// Compares two specific versions and returns a summary, optionally ignoring leading/trailing
    /// whitespace and treating blank-line-only values as equal.
    /// </summary>
    /// <param name="version1Id">First version identifier.</param>
    /// <param name="version2Id">Second version identifier.</param>
    /// <param name="ignoreWhitespaceAndBlankLines">
    /// When <c>true</c>, leading/trailing whitespace is trimmed and blank-line-only values are
    /// treated as equal.
    /// </param>
    Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id, bool ignoreWhitespaceAndBlankLines);
}
