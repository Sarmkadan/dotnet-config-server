#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Generic repository interface for data access
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<List<T>> GetAllAsync();
    Task SaveChangesAsync();
}

/// <summary>
/// Configuration repository interface
/// </summary>
public interface IConfigurationRepository : IRepository<Models.Configuration>
{
    Task<List<Models.Configuration>> GetByApplicationIdAsync(Guid applicationId);
    Task<Models.Configuration?> GetByNameAsync(string name, Guid applicationId);
    Task<List<Models.Configuration>> SearchAsync(string query, Guid? applicationId = null);
    Task<int> GetCountByApplicationAsync(Guid applicationId);
}

/// <summary>
/// Configuration key repository interface
/// </summary>
public interface IConfigurationKeyRepository : IRepository<Models.ConfigurationKey>
{
    Task<List<Models.ConfigurationKey>> GetByConfigurationAsync(Guid configurationId);
    Task<List<Models.ConfigurationKey>> GetByVersionAsync(Guid versionId);
    Task<Models.ConfigurationKey?> GetByKeyNameAsync(Guid configurationId, string keyName);
}

/// <summary>
/// Configuration version repository interface
/// </summary>
public interface IConfigurationVersionRepository : IRepository<Models.ConfigurationVersion>
{
    Task<List<Models.ConfigurationVersion>> GetByConfigurationAsync(Guid configurationId);
    Task<Models.ConfigurationVersion?> GetActiveVersionAsync(Guid configurationId);
    Task<Models.ConfigurationVersion?> GetByVersionNumberAsync(Guid configurationId, string versionNumber);
}

/// <summary>
/// Webhook subscription repository interface
/// </summary>
public interface IWebhookSubscriptionRepository : IRepository<Models.WebhookSubscription>
{
    Task<List<Models.WebhookSubscription>> GetByConfigurationAsync(Guid configurationId);
    Task<List<Models.WebhookSubscription>> GetActiveWebhooksAsync();
}

/// <summary>
/// Webhook delivery repository interface
/// </summary>
public interface IWebhookDeliveryRepository : IRepository<Models.WebhookDelivery>
{
    Task<List<Models.WebhookDelivery>> GetBySubscriptionAsync(Guid subscriptionId);
    Task<List<Models.WebhookDelivery>> GetFailedDeliveriesAsync();
    Task<List<Models.WebhookDelivery>> GetPendingDeliveriesAsync();
}

/// <summary>
/// Configuration diff repository interface
/// </summary>
public interface IConfigurationDiffRepository : IRepository<Models.ConfigurationDiff>
{
    Task<List<Models.ConfigurationDiff>> GetByConfigurationAsync(Guid configurationId);
    Task<Models.ConfigurationDiff?> GetByVersionsAsync(Guid fromVersionId, Guid toVersionId);
}

/// <summary>
/// Audit log repository interface
/// </summary>
public interface IAuditLogRepository : IRepository<Models.AuditLog>
{
    Task<List<Models.AuditLog>> GetByConfigurationAsync(Guid configurationId);
    Task<List<Models.AuditLog>> GetByUserAsync(string userId);
    Task<List<Models.AuditLog>> GetByEntityAsync(string entityType, string entityId);
}

/// <summary>
/// Encryption key repository interface
/// </summary>
public interface IEncryptionKeyRepository : IRepository<Models.EncryptionKey>
{
    Task<Models.EncryptionKey?> GetByKeyIdAsync(string keyId);
    Task<Models.EncryptionKey?> GetPrimaryKeyByConfigurationAsync(Guid configurationId);
    Task<List<Models.EncryptionKey>> GetActiveKeysByConfigurationAsync(Guid configurationId);
    Task<List<Models.EncryptionKey>> GetExpiringKeysAsync(int daysUntilExpiration = 30);
}

/// <summary>
/// Application repository interface
/// </summary>
public interface IApplicationRepository : IRepository<Models.Application>
{
    Task<Models.Application?> GetBySlugAsync(string slug);
    Task<Models.Application?> GetByApiKeyAsync(string apiKey);
    Task<List<Models.Application>> GetActiveApplicationsAsync();
}

/// <summary>
/// Configuration snapshot repository interface
/// </summary>
public interface IConfigurationSnapshotRepository : IRepository<Models.ConfigurationSnapshot>
{
    Task<List<Models.ConfigurationSnapshot>> GetByConfigurationAsync(Guid configurationId);
    Task<Models.ConfigurationSnapshot?> GetLatestSnapshotAsync(Guid configurationId);
}
