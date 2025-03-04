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
sealed public class DiffService : IDiffService
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
    /// Generates a diff between two versions
    /// </summary>
    public async Task<ConfigurationDiff> GenerateDiffAsync(Guid fromVersionId, Guid toVersionId, string userId)
    {
        var fromVersion = await _versionRepository.GetByIdAsync(fromVersionId);
        var toVersion = await _versionRepository.GetByIdAsync(toVersionId);

        if (fromVersion == null || toVersion == null)
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
            if (fromKey == null)
            {
                diff.AddChange(toKey.Key, ChangeType.Added, null, toKey.Value);
            }
            else if (fromKey.Value != toKey.Value)
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
    /// Compares two versions and returns a summary
    /// </summary>
    public async Task<ConfigurationDiffSummary> ComparVersionsAsync(Guid version1Id, Guid version2Id)
    {
        var existingDiff = await _diffRepository.GetByVersionsAsync(version1Id, version2Id);
        if (existingDiff != null)
            return existingDiff.GetSummary();

        var keys1 = await _keyRepository.GetByVersionAsync(version1Id);
        var keys2 = await _keyRepository.GetByVersionAsync(version2Id);

        int added = keys2.Count(k => !keys1.Any(k1 => k1.Key == k.Key));
        int deleted = keys1.Count(k => !keys2.Any(k2 => k2.Key == k.Key));
        int modified = keys1.Count(k => keys2.FirstOrDefault(k2 => k2.Key == k.Key)?.Value != k.Value);

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
}
