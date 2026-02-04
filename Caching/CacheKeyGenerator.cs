// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Caching;

/// <summary>
/// Generates consistent cache keys for different entities and scenarios.
/// Ensures cache keys follow a predictable naming convention.
/// </summary>
public static class CacheKeyGenerator
{
    private const string Separator = ":";

    /// <summary>
    /// Generates cache key for a configuration.
    /// </summary>
    public static string GetConfigurationKey(Guid configurationId)
    {
        return $"config{Separator}{configurationId}";
    }

    /// <summary>
    /// Generates cache key for all configurations of an application.
    /// </summary>
    public static string GetApplicationConfigurationsKey(Guid applicationId)
    {
        return $"app{Separator}{applicationId}{Separator}configs";
    }

    /// <summary>
    /// Generates cache key for configuration keys.
    /// </summary>
    public static string GetConfigurationKeysKey(Guid configurationId)
    {
        return $"config{Separator}{configurationId}{Separator}keys";
    }

    /// <summary>
    /// Generates cache key for a specific configuration key.
    /// </summary>
    public static string GetConfigurationKeyKey(Guid keyId)
    {
        return $"key{Separator}{keyId}";
    }

    /// <summary>
    /// Generates cache key for configuration versions.
    /// </summary>
    public static string GetConfigurationVersionsKey(Guid configurationId)
    {
        return $"config{Separator}{configurationId}{Separator}versions";
    }

    /// <summary>
    /// Generates cache key for a specific version.
    /// </summary>
    public static string GetConfigurationVersionKey(Guid versionId)
    {
        return $"version{Separator}{versionId}";
    }

    /// <summary>
    /// Generates cache key for configuration diff.
    /// </summary>
    public static string GetConfigurationDiffKey(Guid fromVersionId, Guid toVersionId)
    {
        return $"diff{Separator}{fromVersionId}{Separator}{toVersionId}";
    }

    /// <summary>
    /// Generates cache key for an application.
    /// </summary>
    public static string GetApplicationKey(Guid applicationId)
    {
        return $"app{Separator}{applicationId}";
    }

    /// <summary>
    /// Generates cache key for all applications.
    /// </summary>
    public static string GetAllApplicationsKey()
    {
        return "apps{Separator}all";
    }

    /// <summary>
    /// Generates cache key for webhook subscriptions.
    /// </summary>
    public static string GetWebhookSubscriptionsKey(Guid applicationId)
    {
        return $"webhooks{Separator}{applicationId}";
    }

    /// <summary>
    /// Generates cache key for a single webhook subscription.
    /// </summary>
    public static string GetWebhookSubscriptionKey(Guid subscriptionId)
    {
        return $"webhook{Separator}{subscriptionId}";
    }

    /// <summary>
    /// Generates cache key for configuration search results.
    /// </summary>
    public static string GetSearchKey(string query, Guid? applicationId = null)
    {
        var key = $"search{Separator}{Uri.EscapeDataString(query)}";
        if (applicationId.HasValue)
            key += $"{Separator}{applicationId}";

        return key;
    }

    /// <summary>
    /// Gets all cache key patterns that should be invalidated when a configuration changes.
    /// </summary>
    public static IEnumerable<string> GetInvalidationPatternsForConfiguration(Guid configurationId, Guid applicationId)
    {
        yield return GetConfigurationKey(configurationId);
        yield return GetConfigurationKeysKey(configurationId);
        yield return GetConfigurationVersionsKey(configurationId);
        yield return GetApplicationConfigurationsKey(applicationId);
        yield return "search*"; // Invalidate all search results
    }

    /// <summary>
    /// Gets all cache key patterns that should be invalidated when an application changes.
    /// </summary>
    public static IEnumerable<string> GetInvalidationPatternsForApplication(Guid applicationId)
    {
        yield return GetApplicationKey(applicationId);
        yield return GetApplicationConfigurationsKey(applicationId);
        yield return GetAllApplicationsKey();
        yield return $"app{Separator}{applicationId}*"; // All keys related to this app
    }
}
