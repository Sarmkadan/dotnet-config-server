#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Services;

/// <summary>
/// Service for generating and managing configuration version diffs
/// </summary>
public sealed class DiffService : IDiffService
{
    private readonly IConfigurationDiffRepository _diffRepository;
    private readonly IConfigurationVersionRepository _versionRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly ILogger<DiffService> _logger;

    public DiffService(
        IConfigurationDiffRepository diffRepository,
        IConfigurationVersionRepository versionRepository,
        IConfigurationKeyRepository keyRepository,
        ILogger<DiffService> logger)
    {
        _diffRepository = diffRepository;
        _versionRepository = versionRepository;
        _keyRepository = keyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generates a diff between two versions.
    /// By default performs an exact comparison. To ignore leading/trailing whitespace
    /// and treat blank‑line‑only differences as equal, use the overload with
    /// <c>ignoreWhitespaceAndBlankLines</c> set to <c>true</c>.
    /// </summary>
    public async Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId)
    {
        // Preserve existing behaviour (exact comparison)
        return await GenerateDiffAsync(fromVersionId, toVersionId, userId, ignoreWhitespaceAndBlankLines: false);
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
    public async Task<ConfigurationDiff> GenerateDiffAsync(
        Guid fromVersionId,
        Guid toVersionId,
        string userId,
        bool ignoreWhitespaceAndBlankLines)
    {
        var fromVersion = await _versionRepository.GetByIdAsync(fromVersionId);
        var toVersion = await _versionRepository.GetByIdAsync(toVersionId);

        if (fromVersion is null || toVersion is null)
            throw new ConfigurationNotFoundException("One or both versions not found");

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

        // Find added and modified keys
        foreach (var toKey in toKeys)
        {
            var fromKey = fromKeys.FirstOrDefault(k => k.Key == toKey.Key);
            if (fromKey is null)
            {
                diff.AddChange(toKey.Key, ChangeType.Added, null, toKey.Value);
            }
            else if (ValuesDiffer(fromKey.Value, toKey.Value, ignoreWhitespaceAndBlankLines))
            {
                diff.AddChange(toKey.Key, ChangeType.Modified, fromKey.Value, toKey.Value);
            }
        }

        // Find deleted keys
        foreach (var fromKey in fromKeys)
        {
            if (!toKeys.Any(k => k.Key == fromKey.Key))
            {
                diff.AddChange(fromKey.Key, ChangeType.Deleted, fromKey.Value, null);
            }
        }

        await _diffRepository.AddAsync(diff);
        await _diffRepository.SaveChangesAsync();

        _logger.LogInformation("Diff generated between versions {FromVersion} and {ToVersion} by {UserId}",
            fromVersion.VersionNumber, toVersion.VersionNumber, userId);

        return diff;
    }

    /// <summary>
    /// Gets a diff
    /// </summary>
    public async Task<ConfigurationDiff?> GetDiffAsync(Guid diffId)
    {
        return await _diffRepository.GetByIdAsync(diffId);
    }

    /// <summary>
    /// Gets all diffs for a configuration
    /// </summary>
    public async Task<List<ConfigurationDiff>> GetDiffsAsync(Guid configurationId)
    {
        return await _diffRepository.GetByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Gets the latest diff for a configuration
    /// </summary>
    public async Task<ConfigurationDiff?> GetLatestDiffAsync(Guid configurationId)
    {
        var diffs = await GetDiffsAsync(configurationId);
        return diffs.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Gets the history of changes for a specific key
    /// </summary>
    public async Task<List<DiffEntry>> GetKeyHistoryAsync(Guid configurationId, string keyName)
    {
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
    public async Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id)
    {
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
    public async Task<ConfigurationDiffSummary> ComparVersionsAsync(
        Guid version1Id,
        Guid version2Id,
        bool ignoreWhitespaceAndBlankLines)
    {
        var existingDiff = await _diffRepository.GetByVersionsAsync(version1Id, version2Id);
        if (existingDiff is not null)
            return existingDiff.GetSummary();

        var keys1 = await _keyRepository.GetByVersionAsync(version1Id);
        var keys2 = await _keyRepository.GetByVersionAsync(version2Id);

        int added = keys2.Count(k => !keys1.Any(k1 => k1.Key == k.Key));
        int deleted = keys1.Count(k => !keys2.Any(k2 => k2.Key == k.Key));
        int modified = keys1.Count(k =>
        {
            var matching = keys2.FirstOrDefault(k2 => k2.Key == k.Key);
            return matching != null && ValuesDiffer(k.Value, matching.Value, ignoreWhitespaceAndBlankLines);
        });

        return new ConfigurationDiffSummary
        {
            Id = Guid.NewGuid(),
            TotalChanges = added + deleted + modified,
            AddedCount = added,
            DeletedCount = deleted,
            ModifiedCount = modified,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
    }

    /// <summary>
    /// Determines whether two configuration values differ, taking the optional
    /// whitespace‑ignoring behaviour into account.
    /// </summary>
    private static bool ValuesDiffer(string? a, string? b, bool ignoreWhitespaceAndBlankLines)
    {
        if (!ignoreWhitespaceAndBlankLines)
            return a != b;

        // Normalise both values: trim whitespace and treat pure whitespace as empty.
        var normA = NormalizeValue(a);
        var normB = NormalizeValue(b);
        return normA != normB;
    }

    /// <summary>
    /// Normalises a configuration value for whitespace‑ignoring comparison.
    /// Trims leading/trailing whitespace and converts a value that is null,
    /// empty, or consists only of whitespace to an empty string.
    /// </summary>
    private static string NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }
}
