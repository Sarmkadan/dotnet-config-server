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
/// Service for managing configuration versions
/// </summary>
public class VersioningService : IVersioningService
{
    private readonly IConfigurationVersionRepository _versionRepository;
    private readonly IConfigurationRepository _configRepository;
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<VersioningService> _logger;

    public VersioningService(
        IConfigurationVersionRepository versionRepository,
        IConfigurationRepository configRepository,
        IConfigurationKeyRepository keyRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<VersioningService> logger)
    {
        _versionRepository = versionRepository;
        _configRepository = configRepository;
        _keyRepository = keyRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new version of a configuration
    /// </summary>
    public async Task<ConfigurationVersion> CreateVersionAsync(Guid configurationId, string releaseNotes, string userId)
    {
        var config = await _configRepository.GetByIdAsync(configurationId);
        if (config == null)
            throw new ConfigurationNotFoundException(configurationId.ToString());

        var previousVersion = await GetActiveVersionAsync(configurationId);
        var newVersionNumber = ConfigurationVersion.IncrementVersion(
            previousVersion?.VersionNumber ?? "1.0.0",
            VersionIncrementType.Patch
        );

        var version = new ConfigurationVersion
        {
            ConfigurationId = configurationId,
            VersionNumber = newVersionNumber,
            Status = ConfigurationVersionStatus.Draft,
            ReleaseNotes = releaseNotes,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            PreviousVersionId = previousVersion?.Id.ToString()
        };

        version.Validate();

        // Copy keys from previous version
        if (previousVersion != null)
        {
            var previousKeys = await _keyRepository.GetByVersionAsync(previousVersion.Id);
            foreach (var key in previousKeys)
            {
                var newKey = new ConfigurationKey
                {
                    Key = key.Key,
                    Value = key.Value,
                    DefaultValue = key.DefaultValue,
                    Description = key.Description,
                    ValueType = key.ValueType,
                    ConfigurationId = configurationId,
                    VersionId = version.Id,
                    CreatedBy = userId,
                    IsEncrypted = key.IsEncrypted,
                    IsRequired = key.IsRequired,
                    IsSensitive = key.IsSensitive,
                    ValidationRegex = key.ValidationRegex,
                    MinLength = key.MinLength,
                    MaxLength = key.MaxLength
                };
                version.Keys.Add(newKey);
            }
        }

        version.KeyCount = version.Keys.Count;
        version.HasEncryptedKeys = version.Keys.Any(k => k.IsEncrypted);

        await _versionRepository.AddAsync(version);
        await _versionRepository.SaveChangesAsync();

        _logger.LogInformation("Version {VersionNumber} created for configuration {ConfigId} by {UserId}",
            newVersionNumber, configurationId, userId);

        return version;
    }

    /// <summary>
    /// Gets a configuration version
    /// </summary>
    public async Task<ConfigurationVersion?> GetVersionAsync(Guid versionId)
    {
        return await _versionRepository.GetByIdAsync(versionId);
    }

    /// <summary>
    /// Gets all versions of a configuration
    /// </summary>
    public async Task<List<ConfigurationVersion>> GetVersionsAsync(Guid configurationId)
    {
        return await _versionRepository.GetByConfigurationAsync(configurationId);
    }

    /// <summary>
    /// Gets the active version
    /// </summary>
    public async Task<ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId)
    {
        return await _versionRepository.GetActiveVersionAsync(configurationId);
    }

    /// <summary>
    /// Publishes a version
    /// </summary>
    public async Task<ConfigurationVersion> PublishVersionAsync(Guid versionId, string userId)
    {
        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version == null)
            throw new ConfigurationNotFoundException(versionId.ToString());

        // Unpublish current active version
        var activeVersion = await GetActiveVersionAsync(version.ConfigurationId);
        if (activeVersion != null && activeVersion.Id != versionId)
        {
            activeVersion.Deprecate();
            await _versionRepository.UpdateAsync(activeVersion);
        }

        version.Publish(userId);
        await _versionRepository.UpdateAsync(version);
        await _versionRepository.SaveChangesAsync();

        // Update configuration's current version
        var config = await _configRepository.GetByIdAsync(version.ConfigurationId);
        if (config != null)
        {
            config.CurrentVersionId = version.Id;
            config.VersionNumber = int.Parse(version.VersionNumber.Split('.')[0]);
            await _configRepository.UpdateAsync(config);
            await _configRepository.SaveChangesAsync();
        }

        // Log audit entry
        var auditEntry = AuditLog.CreateEntry(
            version.ConfigurationId,
            nameof(ConfigurationVersion),
            version.Id.ToString(),
            $"Version {version.VersionNumber}",
            userId,
            null,
            $"Published version {version.VersionNumber}"
        );
        await _auditLogRepository.AddAsync(auditEntry);
        await _auditLogRepository.SaveChangesAsync();

        _logger.LogInformation("Version {VersionId} published by {UserId}", versionId, userId);
        return version;
    }

    /// <summary>
    /// Archives a version
    /// </summary>
    public async Task<ConfigurationVersion> ArchiveVersionAsync(Guid versionId, string userId)
    {
        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version == null)
            throw new ConfigurationNotFoundException(versionId.ToString());

        version.Archive(userId);
        await _versionRepository.UpdateAsync(version);
        await _versionRepository.SaveChangesAsync();

        _logger.LogInformation("Version {VersionId} archived by {UserId}", versionId, userId);
        return version;
    }

    /// <summary>
    /// Deprecates a version
    /// </summary>
    public async Task<ConfigurationVersion> DeprecateVersionAsync(Guid versionId, string userId)
    {
        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version == null)
            throw new ConfigurationNotFoundException(versionId.ToString());

        version.Deprecate();
        await _versionRepository.UpdateAsync(version);
        await _versionRepository.SaveChangesAsync();

        _logger.LogInformation("Version {VersionId} deprecated by {UserId}", versionId, userId);
        return version;
    }

    /// <summary>
    /// Rolls back to a previous version
    /// </summary>
    public async Task<ConfigurationVersion> RollbackAsync(Guid configurationId, Guid previousVersionId, string userId)
    {
        var previousVersion = await _versionRepository.GetByIdAsync(previousVersionId);
        if (previousVersion == null)
            throw new ConfigurationNotFoundException(previousVersionId.ToString());

        // Create new version as rollback
        var newVersion = await CreateVersionAsync(configurationId,
            $"Rollback to version {previousVersion.VersionNumber}", userId);

        // Copy keys from previous version
        newVersion.Keys.Clear();
        var previousKeys = await _keyRepository.GetByVersionAsync(previousVersionId);
        foreach (var key in previousKeys)
        {
            var newKey = new ConfigurationKey
            {
                Key = key.Key,
                Value = key.Value,
                DefaultValue = key.DefaultValue,
                Description = key.Description,
                ValueType = key.ValueType,
                ConfigurationId = configurationId,
                VersionId = newVersion.Id,
                CreatedBy = userId,
                IsEncrypted = key.IsEncrypted,
                IsRequired = key.IsRequired,
                IsSensitive = key.IsSensitive
            };
            newVersion.Keys.Add(newKey);
        }

        newVersion.KeyCount = newVersion.Keys.Count;
        newVersion.HasEncryptedKeys = newVersion.Keys.Any(k => k.IsEncrypted);

        await _versionRepository.UpdateAsync(newVersion);
        await _versionRepository.SaveChangesAsync();

        _logger.LogInformation("Rolled back to version {PreviousVersionId} for config {ConfigId} by {UserId}",
            previousVersionId, configurationId, userId);

        return newVersion;
    }

    /// <summary>
    /// Gets version history
    /// </summary>
    public async Task<List<ConfigurationVersionSummary>> GetVersionHistoryAsync(Guid configurationId)
    {
        var versions = await GetVersionsAsync(configurationId);
        return versions.OrderByDescending(v => v.CreatedAt)
            .Select(v => v.GetSummary())
            .ToList();
    }

    /// <summary>
    /// Cleans up old versions
    /// </summary>
    public async Task<int> CleanupOldVersionsAsync(Guid configurationId, int maxVersions)
    {
        var versions = await GetVersionsAsync(configurationId);
        var sortedVersions = versions.OrderByDescending(v => v.CreatedAt).ToList();

        var versionsToArchive = sortedVersions
            .Skip(maxVersions)
            .Where(v => v.Status != ConfigurationVersionStatus.Active)
            .ToList();

        foreach (var version in versionsToArchive)
        {
            version.Status = ConfigurationVersionStatus.Archived;
            await _versionRepository.UpdateAsync(version);
        }

        await _versionRepository.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old versions for configuration {ConfigId}",
            versionsToArchive.Count, configurationId);

        return versionsToArchive.Count;
    }
}
