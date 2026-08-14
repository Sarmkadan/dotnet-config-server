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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userId"/> is <c>null</c> or empty.
    /// </exception>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userId"/> is <c>null</c> or empty.
    /// </exception>
    Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId, bool ignoreWhitespaceAndBlankLines);

    /// <summary>
    /// Gets a previously generated diff
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="diffId"/> is <c>Guid.Empty</c>.
    /// </exception>
    Task<ConfigurationDiff?> GetDiffAsync(Guid diffId);

    /// <summary>
    /// Gets all diffs for a configuration
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c>.
    /// </exception>
    Task<List<ConfigurationDiff>> GetDiffsAsync(Guid configurationId);

    /// <summary>
    /// Gets diff between current and previous version
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c>.
    /// </exception>
    Task<ConfigurationDiff?> GetLatestDiffAsync(Guid configurationId);

    /// <summary>
    /// Gets changes for a specific key across versions
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c> or
    /// <paramref name="keyName"/> is <c>null</c> or empty.
    /// </exception>
    Task<List<DiffEntry>> GetKeyHistoryAsync(Guid configurationId, string keyName);

    /// <summary>
    /// Compares two specific versions using exact, case-sensitive comparison and returns a summary.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when either version identifier is <c>Guid.Empty</c>.
    /// </exception>
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
    /// <exception cref="ArgumentException">
    /// Thrown when either version identifier is <c>Guid.Empty</c>.
    /// </exception>
    Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id, bool ignoreWhitespaceAndBlankLines);
}
