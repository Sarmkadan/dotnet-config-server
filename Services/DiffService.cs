#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;
using DotnetConfigServer.Exceptions;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for generating and managing configuration version diffs. Implements core diff generation and retrieval operations.
/// </summary>
public sealed class DiffService : IDiffService
{
    private readonly IConfigurationDiffRepository _diffRepository;
    private readonly IConfigurationVersionRepository _versionRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly IConfigDiffer _differ;
    private readonly ILogger<DiffService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DiffService"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any dependency is <c>null</c>.
    /// </exception>
    public DiffService(
        IConfigurationDiffRepository diffRepository,
        IConfigurationVersionRepository versionRepository,
        IConfigurationKeyRepository keyRepository,
        IConfigDiffer differ,
        ILogger<DiffService> logger)
    {
        ArgumentNullException.ThrowIfNull(diffRepository);
        ArgumentNullException.ThrowIfNull(versionRepository);
        ArgumentNullException.ThrowIfNull(keyRepository);
        ArgumentNullException.ThrowIfNull(differ);
        ArgumentNullException.ThrowIfNull(logger);

        _diffRepository = diffRepository;
        _versionRepository = versionRepository;
        _keyRepository = keyRepository;
        _differ = differ;
        _logger = logger;
    }

    /// <summary>
    /// Generates a diff between two versions.
    /// By default performs an exact comparison. To ignore leading/trailing whitespace
    /// and treat blank‑line‑only differences as equal, use the overload with
    /// <c>ignoreWhitespaceAndBlankLines</c> set to <c>true</c>.
    /// </summary>
    /// <param name="fromVersionId">The source version identifier.</param>
    /// <param name="toVersionId">The target version identifier.</param>
    /// <param name="userId">Identifier of the user requesting the diff.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="ConfigurationNotFoundException">
    /// Thrown when one or both versions cannot be found.
    /// </exception>
    public async Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId)
    {
        try
        {
            _logger.LogInformation("Entering {MethodName} with {FromVersionId}={FromVersionId}, {ToVersionId}={ToVersionId}, {UserId}={UserId}",
                nameof(GenerateDiffAsync), fromVersionId, toVersionId, userId);
            ArgumentException.ThrowIfNullOrEmpty(userId);
            // Preserve existing behaviour (exact comparison)
            return await GenerateDiffAsync(fromVersionId, toVersionId, userId, ignoreWhitespaceAndBlankLines: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}: {Message}", nameof(GenerateDiffAsync), ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Generates a diff between two versions with optional whitespace‑ignoring behaviour.
    /// </summary>
    /// <param name="fromVersionId">The source version identifier.</param>
    /// <param name="toVersionId">The target version identifier.</param>
    /// <param name="userId">Identifier of the user requesting the diff.</param>
    /// <param name="ignoreWhitespaceAndBlankLines">
    /// When <c>true</c>, leading/trailing whitespace is trimmed and values that consist
    /// solely of whitespace (blank lines) are treated as equal.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userId"/> is <c>null</c> or empty.
    /// </exception>
    /// <exception cref="ConfigurationNotFoundException">
    /// Thrown when one or both versions cannot be found.
    /// </exception>
    public async Task<ConfigurationDiff> GenerateDiffAsync(
        Guid fromVersionId,
        Guid toVersionId,
        string userId,
        bool ignoreWhitespaceAndBlankLines)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var fromVersion = await _versionRepository.GetByIdAsync(fromVersionId);
        var toVersion = await _versionRepository.GetByIdAsync(toVersionId);

        if (fromVersion is null || toVersion is null)
            throw new ConfigurationNotFoundException("One or both versions not found");

        // If the two version identifiers are the same, return an empty diff early.
        if (fromVersionId == toVersionId)
        {
            var emptyDiff = new ConfigurationDiff
            {
                ConfigurationId = fromVersion.ConfigurationId,
                FromVersionId = fromVersionId,
                ToVersionId = toVersionId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Requested diff between identical versions {VersionId}; returning empty diff.",
                fromVersionId);

            return emptyDiff;
        }

        var fromKeys = await _keyRepository.GetByVersionAsync(fromVersionId);
        var toKeys = await _keyRepository.GetByVersionAsync(toVersionId);

        var diff = new ConfigurationDiff
        {
            ConfigurationId = fromVersion.ConfigurationId,
            FromVersionId = fromVersionId,
            ToVersionId = toVersionId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        var options = new ConfigDiffOptions(IgnoreWhitespaceAndBlankLines: ignoreWhitespaceAndBlankLines);
        var changes = _differ.Diff(ToKeyMap(fromKeys), ToKeyMap(toKeys), options);

        // If there are no changes, avoid persisting an empty diff.
        if (changes.Count == 0)
        {
            _logger.LogInformation(
                "No differences found between versions {FromVersion} and {ToVersion} for user {UserId}.",
                fromVersion.VersionNumber, toVersion.VersionNumber, userId);
            return diff;
        }

        foreach (var change in changes)
        {
            diff.AddChange(change.Key, change.ChangeType, change.OldValue, change.NewValue);
        }

        await _diffRepository.AddAsync(diff);
        await _diffRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Diff generated between versions {FromVersion} and {ToVersion} by {UserId}",
            fromVersion.VersionNumber, toVersion.VersionNumber, userId);

        return diff;
    }

    /// <summary>
    /// Gets a diff.
    /// </summary>
    /// <param name="diffId">The identifier of the diff to retrieve.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="diffId"/> is <c>Guid.Empty</c>.
    /// </exception>
    public async Task<ConfigurationDiff?> GetDiffAsync(Guid diffId)
    {
        if (diffId == Guid.Empty)
            throw new ArgumentException("Diff identifier cannot be empty.", nameof(diffId));

        return await _diffRepository.GetByIdAsync(diffId);
    }

    /// <summary>
    /// Gets all diffs for a configuration.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c>.
    /// </exception>
    public async Task<List<ConfigurationDiff>> GetDiffsAsync(Guid configurationId)
    {
        if (configurationId == Guid.Empty)
            throw new ArgumentException("Configuration identifier cannot be empty.", nameof(configurationId));

        return await _diffRepository.GetByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Gets the latest diff for a configuration.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c>.
    /// </exception>
    public async Task<ConfigurationDiff?> GetLatestDiffAsync(Guid configurationId)
    {
        if (configurationId == Guid.Empty)
            throw new ArgumentException("Configuration identifier cannot be empty.", nameof(configurationId));

        var diffs = await GetDiffsAsync(configurationId);
        return diffs.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Gets the history of changes for a specific key.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <param name="keyName">The key name to retrieve history for.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configurationId"/> is <c>Guid.Empty</c> or
    /// <paramref name="keyName"/> is <c>null</c> or empty.
    /// </exception>
    public async Task<List<DiffEntry>> GetKeyHistoryAsync(Guid configurationId, string keyName)
    {
        if (configurationId == Guid.Empty)
            throw new ArgumentException("Configuration identifier cannot be empty.", nameof(configurationId));
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        var diffs = await GetDiffsAsync(configurationId);
        return diffs
            .SelectMany(d => d.Changes)
            .Where(c => c.Key == keyName)
            .OrderBy(c => c.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Compares two versions and returns a summary.
    /// By default performs an exact comparison. Use the overload with
    /// <c>ignoreWhitespaceAndBlankLines</c> to change the behaviour.
    /// </summary>
    /// <param name="version1Id">First version identifier.</param>
    /// <param name="version2Id">Second version identifier.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when either identifier is <c>Guid.Empty</c>.
    /// </exception>
    public async Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id)
    {
        if (version1Id == Guid.Empty)
            throw new ArgumentException("Version identifier cannot be empty.", nameof(version1Id));
        if (version2Id == Guid.Empty)
            throw new ArgumentException("Version identifier cannot be empty.", nameof(version2Id));

        // Preserve existing exact‑comparison behaviour
        return await ComparVersionsAsync(version1Id, version2Id, ignoreWhitespaceAndBlankLines: false);
    }

    /// <summary>
    /// Compares two versions and returns a summary with optional whitespace‑ignoring.
    /// </summary>
    /// <param name="version1Id">First version identifier.</param>
    /// <param name="version2Id">Second version identifier.</param>
    /// <param name="ignoreWhitespaceAndBlankLines">
    /// When <c>true</c>, leading/trailing whitespace is trimmed and blank‑line‑only values
    /// are treated as equal.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when either identifier is <c>Guid.Empty</c>.
    /// </exception>
    public async Task<ConfigurationDiffSummary> ComparVersionsAsync(
        Guid version1Id,
        Guid version2Id,
        bool ignoreWhitespaceAndBlankLines)
    {
        if (version1Id == Guid.Empty)
            throw new ArgumentException("Version identifier cannot be empty.", nameof(version1Id));
        if (version2Id == Guid.Empty)
            throw new ArgumentException("Version identifier cannot be empty.", nameof(version2Id));

        var existingDiff = await _diffRepository.GetByVersionsAsync(version1Id, version2Id);
        if (existingDiff is not null)
            return existingDiff.GetSummary();

        var keys1 = await _keyRepository.GetByVersionAsync(version1Id);
        var keys2 = await _keyRepository.GetByVersionAsync(version2Id);

        var options = new ConfigDiffOptions(IgnoreWhitespaceAndBlankLines: ignoreWhitespaceAndBlankLines);
        var changes = _differ.Diff(ToKeyMap(keys1), ToKeyMap(keys2), options);

        var summary = new ConfigurationDiffSummary
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        foreach (var change in changes)
        {
            switch (change.ChangeType)
            {
                case ChangeType.Added:
                    summary.AddedCount++;
                    break;
                case ChangeType.Deleted:
                    summary.DeletedCount++;
                    break;
                case ChangeType.Modified:
                    summary.ModifiedCount++;
                    break;
            }
        }

        summary.TotalChanges = summary.AddedCount + summary.DeletedCount + summary.ModifiedCount;

        return summary;
    }

    /// <summary>
    /// Projects a list of configuration keys into the key/value map shape expected by
    /// <see cref="IConfigDiffer.Diff"/>. When the same key name appears more than once,
    /// the last occurrence in enumeration order wins.
    /// </summary>
    private static Dictionary<string, string?> ToKeyMap(IEnumerable<Models.ConfigurationKey> keys)
    {
        var map = new Dictionary<string, string?>();
        foreach (var key in keys)
        {
            map[key.Key] = key.Value;
        }

        return map;
    }
}
