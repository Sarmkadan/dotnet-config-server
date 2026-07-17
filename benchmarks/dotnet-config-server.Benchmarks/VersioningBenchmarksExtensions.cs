using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotnetConfigServer.Common;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Extension methods for <see cref="VersioningBenchmarks"/> that provide additional benchmarking utilities
/// for versioning operations.
/// </summary>
public static class VersioningBenchmarksExtensions
{
    /// <summary>
    /// Creates a batch of versions with the specified count, returning their IDs.
    /// Useful for testing bulk version operations.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <param name="versionCount">Number of versions to create.</param>
    /// <param name="prefix">Optional prefix for version descriptions.</param>
    /// <param name="userName">The user creating the versions.</param>
    /// <returns>List of created version IDs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId is empty.</exception>
    public static async Task<IReadOnlyList<Guid>> CreateVersionBatchAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId,
        int versionCount,
        string prefix = "Batch",
        string userName = "benchmark-user")
    {
        ArgumentNullException.ThrowIfNull(configurationId);
        ArgumentException.ThrowIfNullOrEmpty(userName);

        var versionIds = new List<Guid>(versionCount);

        for (int i = 0; i < versionCount; i++)
        {
            var version = await benchmarks._versioningService.CreateVersionAsync(
                configurationId,
                $"{prefix} version {i.ToString(CultureInfo.InvariantCulture)}",
                userName);
            versionIds.Add(version.Id);
        }

        return versionIds.AsReadOnly();
    }

    /// <summary>
    /// Gets the count of versions for the specified configuration.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <returns>Count of versions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId is empty.</exception>
    public static async Task<int> GetVersionCountAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId)
    {
        ArgumentNullException.ThrowIfNull(configurationId);

        var versions = await benchmarks._versioningService.GetVersionsAsync(configurationId);
        return versions.Count;
    }

    /// <summary>
    /// Publishes all versions in the specified batch.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="versionIds">List of version IDs to publish.</param>
    /// <param name="userName">The user publishing the versions.</param>
    /// <returns>List of publish tasks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when versionIds is null.</exception>
    public static async Task<IReadOnlyList<Task>> PublishVersionBatchAsync(
        this VersioningBenchmarks benchmarks,
        IReadOnlyList<Guid> versionIds,
        string userName = "benchmark-user")
    {
        ArgumentNullException.ThrowIfNull(versionIds);
        ArgumentException.ThrowIfNullOrEmpty(userName);

        var tasks = new List<Task>(versionIds.Count);

        foreach (var versionId in versionIds)
        {
            tasks.Add(benchmarks._versioningService.PublishVersionAsync(versionId, userName));
        }

        return tasks.AsReadOnly();
    }

    /// <summary>
    /// Gets versions filtered by status.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <param name="status">The version status to filter by.</param>
    /// <returns>Filtered list of versions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId is empty.</exception>
    public static async Task<IReadOnlyList<ConfigurationVersion>> GetVersionsByStatusAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId,
        ConfigurationVersionStatus status)
    {
        ArgumentNullException.ThrowIfNull(configurationId);

        var allVersions = await benchmarks._versioningService.GetVersionsAsync(configurationId);
        return allVersions.Where(v => v.Status == status).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the most recent version by creation date.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <returns>The most recent version, or null if none exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId is empty.</exception>
    public static async Task<ConfigurationVersion?> GetMostRecentVersionAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId)
    {
        ArgumentNullException.ThrowIfNull(configurationId);

        var versions = await benchmarks._versioningService.GetVersionsAsync(configurationId);
        return versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Gets the version with the specified description.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <param name="description">The description to search for.</param>
    /// <returns>The matching version, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId or description is null.</exception>
    /// <exception cref="ArgumentException">Thrown when description is empty.</exception>
    public static async Task<ConfigurationVersion?> GetVersionByDescriptionAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId,
        string description)
    {
        ArgumentNullException.ThrowIfNull(configurationId);
        ArgumentException.ThrowIfNullOrEmpty(description);

        var versions = await benchmarks._versioningService.GetVersionsAsync(configurationId);
        return versions.FirstOrDefault(v => string.Equals(v.ReleaseNotes, description, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the count of versions by status.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="configurationId">The configuration ID.</param>
    /// <returns>Dictionary mapping version status to count.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configurationId is empty.</exception>
    public static async Task<Dictionary<ConfigurationVersionStatus, int>> GetVersionCountsByStatusAsync(
        this VersioningBenchmarks benchmarks,
        Guid configurationId)
    {
        ArgumentNullException.ThrowIfNull(configurationId);

        var allVersions = await benchmarks._versioningService.GetVersionsAsync(configurationId);
        return allVersions
            .GroupBy(v => v.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Archives all versions in the specified batch.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="versionIds">List of version IDs to archive.</param>
    /// <param name="userName">The user archiving the versions.</param>
    /// <returns>List of archive tasks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when versionIds is null.</exception>
    public static async Task<IReadOnlyList<Task>> ArchiveVersionBatchAsync(
        this VersioningBenchmarks benchmarks,
        IReadOnlyList<Guid> versionIds,
        string userName = "benchmark-user")
    {
        ArgumentNullException.ThrowIfNull(versionIds);
        ArgumentException.ThrowIfNullOrEmpty(userName);

        var tasks = new List<Task>(versionIds.Count);

        foreach (var versionId in versionIds)
        {
            tasks.Add(benchmarks._versioningService.ArchiveVersionAsync(versionId, userName));
        }

        return tasks.AsReadOnly();
    }

    /// <summary>
    /// Deprecates all versions in the specified batch.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="versionIds">List of version IDs to deprecate.</param>
    /// <param name="userName">The user deprecating the versions.</param>
    /// <returns>List of deprecate tasks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when versionIds is null.</exception>
    public static async Task<IReadOnlyList<Task>> DeprecateVersionBatchAsync(
        this VersioningBenchmarks benchmarks,
        IReadOnlyList<Guid> versionIds,
        string userName = "benchmark-user")
    {
        ArgumentNullException.ThrowIfNull(versionIds);
        ArgumentException.ThrowIfNullOrEmpty(userName);

        var tasks = new List<Task>(versionIds.Count);

        foreach (var versionId in versionIds)
        {
            tasks.Add(benchmarks._versioningService.DeprecateVersionAsync(versionId, userName));
        }

        return tasks.AsReadOnly();
    }
}