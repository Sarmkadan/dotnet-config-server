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
/// Provides rich diff visualization and non-destructive rollback preview for configuration versions.
/// </summary>
sealed public class DiffViewerService : IDiffViewerService
{
    private readonly IVersioningService _versioningService;
    private readonly IConfigurationDiffRepository _diffRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly ILogger<DiffViewerService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DiffViewerService"/>.
    /// </summary>
    public DiffViewerService(
        IVersioningService versioningService,
        IConfigurationDiffRepository diffRepository,
        IConfigurationKeyRepository keyRepository,
        ILogger<DiffViewerService> logger)
    {
        _versioningService = versioningService;
        _diffRepository = diffRepository;
        _keyRepository = keyRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EnrichedDiff> GetEnrichedDiffAsync(
        Guid fromVersionId,
        Guid toVersionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fromVersion = await _versioningService.GetVersionAsync(fromVersionId);
        var toVersion = await _versioningService.GetVersionAsync(toVersionId);

        if (fromVersion is null || toVersion is null)
            throw new ConfigurationNotFoundException("One or both versions not found");

        var cached = await _diffRepository.GetByVersionsAsync(fromVersionId, toVersionId);

        List<DiffEntry> changes;
        DateTime generatedAt;
        Guid diffId;

        if (cached is not null)
        {
            changes = cached.Changes;
            generatedAt = cached.CreatedAt;
            diffId = cached.Id;
        }
        else
        {
            var fromKeys = await _keyRepository.GetByVersionAsync(fromVersionId);
            var toKeys = await _keyRepository.GetByVersionAsync(toVersionId);
            changes = ComputeChanges(fromKeys, toKeys);
            generatedAt = DateTime.UtcNow;
            diffId = Guid.NewGuid();
        }

        _logger.LogInformation(
            "Enriched diff retrieved for versions {From} → {To} ({ConfigId})",
            fromVersion.VersionNumber, toVersion.VersionNumber, fromVersion.ConfigurationId);

        return new EnrichedDiff
        {
            DiffId = diffId,
            ConfigurationId = fromVersion.ConfigurationId,
            FromVersion = fromVersion.GetSummary(),
            ToVersion = toVersion.GetSummary(),
            Changes = changes,
            AddedCount = changes.Count(c => c.ChangeType == ChangeType.Added),
            ModifiedCount = changes.Count(c => c.ChangeType == ChangeType.Modified),
            DeletedCount = changes.Count(c => c.ChangeType == ChangeType.Deleted),
            GeneratedAt = generatedAt
        };
    }

    /// <inheritdoc />
    public async Task<RollbackPreview> GetRollbackPreviewAsync(
        Guid configurationId,
        Guid targetVersionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var targetVersion = await _versioningService.GetVersionAsync(targetVersionId);
        if (targetVersion is null || targetVersion.ConfigurationId != configurationId)
            throw new ConfigurationNotFoundException(targetVersionId.ToString());

        var activeVersion = await _versioningService.GetActiveVersionAsync(configurationId);

        if (activeVersion is null)
        {
            return new RollbackPreview
            {
                ConfigurationId = configurationId,
                TargetVersion = targetVersion.GetSummary(),
                IsRollbackSafe = true,
                WarningMessages = new List<string>
                {
                    "No active version exists; rollback will establish the initial active state."
                }
            };
        }

        var activeKeys = await _keyRepository.GetByVersionAsync(activeVersion.Id);
        var targetKeys = await _keyRepository.GetByVersionAsync(targetVersionId);
        var changes = ComputeChanges(activeKeys, targetKeys);

        var warnings = activeKeys
            .Where(k => k.IsRequired && !targetKeys.Any(t => t.Key == k.Key))
            .Select(k => $"Required key '{k.Key}' is absent in target version and will be removed.")
            .ToList();

        _logger.LogInformation(
            "Rollback preview generated for configuration {ConfigId} targeting version {VersionNumber} ({VersionId})",
            configurationId, targetVersion.VersionNumber, targetVersionId);

        return new RollbackPreview
        {
            ConfigurationId = configurationId,
            CurrentVersion = activeVersion.GetSummary(),
            TargetVersion = targetVersion.GetSummary(),
            Changes = changes,
            AddedCount = changes.Count(c => c.ChangeType == ChangeType.Added),
            ModifiedCount = changes.Count(c => c.ChangeType == ChangeType.Modified),
            DeletedCount = changes.Count(c => c.ChangeType == ChangeType.Deleted),
            IsRollbackSafe = !warnings.Any(),
            WarningMessages = warnings
        };
    }

    /// <inheritdoc />
    public async Task<List<VersionTimelineEntry>> GetVersionTimelineAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var history = await _versioningService.GetVersionHistoryAsync(configurationId);
        var ordered = history.AsEnumerable().Reverse().ToList();

        var timeline = new List<VersionTimelineEntry>(ordered.Count);

        for (var i = 0; i < ordered.Count; i++)
        {
            ConfigurationDiffSummary? diffSummary = null;
            if (i > 0)
            {
                var persisted = await _diffRepository.GetByVersionsAsync(ordered[i - 1].Id, ordered[i].Id);
                diffSummary = persisted?.GetSummary();
            }

            timeline.Add(new VersionTimelineEntry
            {
                Version = ordered[i],
                DiffFromPrevious = diffSummary,
                IsFirst = i == 0
            });
        }

        return timeline;
    }

    private static List<DiffEntry> ComputeChanges(
        IEnumerable<ConfigurationKey> fromKeys,
        IEnumerable<ConfigurationKey> toKeys)
    {
        var from = fromKeys.ToList();
        var to = toKeys.ToList();
        var changes = new List<DiffEntry>();

        foreach (var toKey in to)
        {
            var match = from.FirstOrDefault(k => k.Key == toKey.Key);
            if (match is null)
                changes.Add(MakeEntry(toKey.Key, ChangeType.Added, null, toKey.Value));
            else if (match.Value != toKey.Value)
                changes.Add(MakeEntry(toKey.Key, ChangeType.Modified, match.Value, toKey.Value));
        }

        foreach (var fromKey in from)
        {
            if (!to.Any(k => k.Key == fromKey.Key))
                changes.Add(MakeEntry(fromKey.Key, ChangeType.Deleted, fromKey.Value, null));
        }

        return changes;
    }

    private static DiffEntry MakeEntry(string key, ChangeType changeType, string? oldValue, string? newValue) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiffId = Guid.Empty,
            Key = key,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        };
}

/// <summary>
/// Extension methods to register diff viewer services in the dependency injection container.
/// </summary>
public static class DiffViewerServiceExtensions
{
    /// <summary>
    /// Adds <see cref="IDiffViewerService"/> and its default implementation to <paramref name="services"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddDiffViewerService(this IServiceCollection services)
    {
        services.AddScoped<IDiffViewerService, DiffViewerService>();
        return services;
    }
}
