#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetConfigServer.Models;

namespace DotnetConfigServer.Services;

/// <summary>
/// In-memory tracker that caches the latest known version number per configuration,
/// enabling fast "has it changed?" checks without hitting the database on every poll.
/// Works alongside <see cref="VersioningService"/> which owns persistence.
/// </summary>
public sealed class VersionTracker
{
    private readonly IVersioningService _versioningService;
    private readonly ILogger<VersionTracker> _logger;
    private readonly ConcurrentDictionary<Guid, string> _versionCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionTracker"/> class.
    /// </summary>
    /// <param name="versioningService">The versioning service for database lookups.</param>
    /// <param name="logger">The logger instance.</param>
    public VersionTracker(
        IVersioningService versioningService,
        ILogger<VersionTracker> logger)
    {
        _versioningService = versioningService ?? throw new ArgumentNullException(nameof(versioningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the cached version string for a configuration, loading from the database if absent.
    /// </summary>
    /// <param name="configurationId">The configuration ID.</param>
    /// <returns>The version string, or <c>null</c> if the configuration has no versions.</returns>
    public async Task<string?> GetCurrentVersionAsync(Guid configurationId)
    {
        if (_versionCache.TryGetValue(configurationId, out var cached))
            return cached;

        var active = await _versioningService.GetActiveVersionAsync(configurationId).ConfigureAwait(false);
        if (active?.VersionNumber is null)
            return null;

        _versionCache[configurationId] = active.VersionNumber;
        _logger.LogDebug("Cached version {Version} for configuration {ConfigurationId}",
            active.VersionNumber, configurationId);
        return active.VersionNumber;
    }

    /// <summary>
    /// Checks whether the configuration has changed since the client's known version.
    /// </summary>
    /// <param name="configurationId">The configuration ID.</param>
    /// <param name="clientVersion">The version the client last saw.</param>
    /// <returns><c>true</c> if the current version differs from <paramref name="clientVersion"/>.</returns>
    public async Task<bool> HasChangedAsync(Guid configurationId, string clientVersion)
    {
        var current = await GetCurrentVersionAsync(configurationId).ConfigureAwait(false);
        return !string.Equals(current, clientVersion, StringComparison.Ordinal);
    }

    /// <summary>
    /// Updates the cached version after a publish or rollback, avoiding a DB round-trip.
    /// </summary>
    /// <param name="configurationId">The configuration ID.</param>
    /// <param name="newVersion">The new version string.</param>
    public void UpdateCache(Guid configurationId, string newVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);
        _versionCache[configurationId] = newVersion;
        _logger.LogInformation("Version cache updated: {ConfigurationId} -> {Version}",
            configurationId, newVersion);
    }

    /// <summary>
    /// Invalidates the cached version for a configuration, forcing the next
    /// <see cref="GetCurrentVersionAsync"/> call to reload from the database.
    /// </summary>
    /// <param name="configurationId">The configuration ID.</param>
    public void Invalidate(Guid configurationId)
    {
        _versionCache.TryRemove(configurationId, out _);
        _logger.LogDebug("Version cache invalidated for configuration {ConfigurationId}", configurationId);
    }

    /// <summary>
    /// Invalidates all cached versions.
    /// </summary>
    public void InvalidateAll()
    {
        var count = _versionCache.Count;
        _versionCache.Clear();
        _logger.LogInformation("Version cache cleared, {Count} entries removed", count);
    }
}
