// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for ConfigurationKey entity
/// </summary>
public class ConfigurationKeyRepository : BaseRepository<ConfigurationKey>, IConfigurationKeyRepository
{
    public ConfigurationKeyRepository(ApplicationDbContext context, ILogger<ConfigurationKeyRepository> logger)
        : base(context, logger) { }

    public async Task<List<ConfigurationKey>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(k => k.ConfigurationId == configurationId && k.IsActive)
            .OrderBy(k => k.Key).ToListAsync();
    }

    public async Task<List<ConfigurationKey>> GetByVersionAsync(Guid versionId)
    {
        return await _dbSet.Where(k => k.VersionId == versionId && k.IsActive)
            .OrderBy(k => k.Key).ToListAsync();
    }

    public async Task<ConfigurationKey?> GetByKeyNameAsync(Guid configurationId, string keyName)
    {
        return await _dbSet.FirstOrDefaultAsync(k =>
            k.ConfigurationId == configurationId && k.Key == keyName && k.IsActive);
    }
}

/// <summary>
/// Repository for ConfigurationVersion entity
/// </summary>
public class ConfigurationVersionRepository : BaseRepository<ConfigurationVersion>, IConfigurationVersionRepository
{
    public ConfigurationVersionRepository(ApplicationDbContext context, ILogger<ConfigurationVersionRepository> logger)
        : base(context, logger) { }

    public async Task<List<ConfigurationVersion>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(v => v.ConfigurationId == configurationId)
            .OrderByDescending(v => v.CreatedAt).ToListAsync();
    }

    public async Task<ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId)
    {
        return await _dbSet.FirstOrDefaultAsync(v =>
            v.ConfigurationId == configurationId && v.Status == ConfigurationVersionStatus.Active);
    }

    public async Task<ConfigurationVersion?> GetByVersionNumberAsync(Guid configurationId, string versionNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(v =>
            v.ConfigurationId == configurationId && v.VersionNumber == versionNumber);
    }
}

/// <summary>
/// Repository for WebhookSubscription entity
/// </summary>
public class WebhookSubscriptionRepository : BaseRepository<WebhookSubscription>, IWebhookSubscriptionRepository
{
    public WebhookSubscriptionRepository(ApplicationDbContext context, ILogger<WebhookSubscriptionRepository> logger)
        : base(context, logger) { }

    public async Task<List<WebhookSubscription>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(w => w.ConfigurationId == configurationId && w.IsActive)
            .OrderByDescending(w => w.CreatedAt).ToListAsync();
    }

    public async Task<List<WebhookSubscription>> GetActiveWebhooksAsync()
    {
        return await _dbSet.Where(w => w.IsActive && w.Status == WebhookStatus.Active)
            .ToListAsync();
    }
}

/// <summary>
/// Repository for WebhookDelivery entity
/// </summary>
public class WebhookDeliveryRepository : BaseRepository<WebhookDelivery>, IWebhookDeliveryRepository
{
    public WebhookDeliveryRepository(ApplicationDbContext context, ILogger<WebhookDeliveryRepository> logger)
        : base(context, logger) { }

    public async Task<List<WebhookDelivery>> GetBySubscriptionAsync(Guid subscriptionId)
    {
        return await _dbSet.Where(d => d.WebhookSubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<List<WebhookDelivery>> GetFailedDeliveriesAsync()
    {
        return await _dbSet.Where(d => d.Status == WebhookDeliveryStatus.Failed)
            .OrderBy(d => d.NextRetryAt).ToListAsync();
    }

    public async Task<List<WebhookDelivery>> GetPendingDeliveriesAsync()
    {
        return await _dbSet.Where(d => d.Status == WebhookDeliveryStatus.Pending)
            .OrderBy(d => d.CreatedAt).ToListAsync();
    }
}

/// <summary>
/// Repository for ConfigurationDiff entity
/// </summary>
public class ConfigurationDiffRepository : BaseRepository<ConfigurationDiff>, IConfigurationDiffRepository
{
    public ConfigurationDiffRepository(ApplicationDbContext context, ILogger<ConfigurationDiffRepository> logger)
        : base(context, logger) { }

    public async Task<List<ConfigurationDiff>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(d => d.ConfigurationId == configurationId)
            .OrderByDescending(d => d.CreatedAt).ToListAsync();
    }

    public async Task<ConfigurationDiff?> GetByVersionsAsync(Guid fromVersionId, Guid toVersionId)
    {
        return await _dbSet.FirstOrDefaultAsync(d =>
            (d.FromVersionId == fromVersionId && d.ToVersionId == toVersionId) ||
            (d.FromVersionId == toVersionId && d.ToVersionId == fromVersionId));
    }
}

/// <summary>
/// Repository for AuditLog entity
/// </summary>
public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository> logger)
        : base(context, logger) { }

    public async Task<List<AuditLog>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(a => a.ConfigurationId == configurationId)
            .OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<List<AuditLog>> GetByUserAsync(string userId)
    {
        return await _dbSet.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<List<AuditLog>> GetByEntityAsync(string entityType, string entityId)
    {
        return await _dbSet.Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp).ToListAsync();
    }
}

/// <summary>
/// Repository for EncryptionKey entity
/// </summary>
public class EncryptionKeyRepository : BaseRepository<EncryptionKey>, IEncryptionKeyRepository
{
    public EncryptionKeyRepository(ApplicationDbContext context, ILogger<EncryptionKeyRepository> logger)
        : base(context, logger) { }

    public async Task<EncryptionKey?> GetByKeyIdAsync(string keyId)
    {
        return await _dbSet.FirstOrDefaultAsync(k => k.KeyId == keyId && k.IsActive);
    }

    public async Task<EncryptionKey?> GetPrimaryKeyByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.FirstOrDefaultAsync(k =>
            k.IsPrimary && k.IsActive && k.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<List<EncryptionKey>> GetActiveKeysByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(k => k.IsActive && k.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(k => k.IsPrimary).ToListAsync();
    }

    public async Task<List<EncryptionKey>> GetExpiringKeysAsync(int daysUntilExpiration = 30)
    {
        var expirationDate = DateTime.UtcNow.AddDays(daysUntilExpiration);
        return await _dbSet.Where(k => k.IsActive && k.ExpiresAt <= expirationDate)
            .OrderBy(k => k.ExpiresAt).ToListAsync();
    }
}

/// <summary>
/// Repository for Application entity
/// </summary>
public class ApplicationRepository : BaseRepository<Application>, IApplicationRepository
{
    public ApplicationRepository(ApplicationDbContext context, ILogger<ApplicationRepository> logger)
        : base(context, logger) { }

    public async Task<Application?> GetBySlugAsync(string slug)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Slug == slug && a.IsActive);
    }

    public async Task<Application?> GetByApiKeyAsync(string apiKey)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.ApiKey == apiKey && a.IsActive);
    }

    public async Task<List<Application>> GetActiveApplicationsAsync()
    {
        return await _dbSet.Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();
    }
}
