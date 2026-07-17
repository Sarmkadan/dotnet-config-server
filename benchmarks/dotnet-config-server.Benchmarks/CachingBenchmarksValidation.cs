using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace DotnetConfigServer.Benchmarks;
using DotnetConfigServer.Models;

/// <summary>
/// Provides validation helpers for <see cref="CachingBenchmarks"/> instances.
/// Validates that benchmark configuration values are within expected ranges and not in default/empty states.
/// </summary>
public static class CachingBenchmarksValidation
{
    private static readonly FieldInfo _testApplicationIdField = typeof(CachingBenchmarks).GetField("_testApplicationId", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _testConfigurationIdField = typeof(CachingBenchmarks).GetField("_testConfigurationId", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _serviceProviderField = typeof(CachingBenchmarks).GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _memoryCacheField = typeof(CachingBenchmarks).GetField("_memoryCache", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _configurationServiceField = typeof(CachingBenchmarks).GetField("_configurationService", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _testKeysField = typeof(CachingBenchmarks).GetField("_testKeys", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _cachePrefixField = typeof(CachingBenchmarks).GetField("CachePrefix", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>
    /// Validates the specified <see cref="CachingBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>A list of validation messages. Empty list if validation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CachingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Guid fields
        var testApplicationId = (Guid?)_testApplicationIdField?.GetValue(value);
        if (testApplicationId == default)
        {
            errors.Add("Field '_testApplicationId' must not be default (Guid.Empty).");
        }

        var testConfigurationId = (Guid?)_testConfigurationIdField?.GetValue(value);
        if (testConfigurationId == default)
        {
            errors.Add("Field '_testConfigurationId' must not be default (Guid.Empty).");
        }

        // Validate service provider
        var serviceProvider = _serviceProviderField?.GetValue(value);
        if (serviceProvider is null)
        {
            errors.Add("Field '_serviceProvider' must not be null.");
        }

        // Validate memory cache
        var memoryCache = _memoryCacheField?.GetValue(value);
        if (memoryCache is null)
        {
            errors.Add("Field '_memoryCache' must not be null.");
        }

        // Validate configuration service
        var configurationService = _configurationServiceField?.GetValue(value);
        if (configurationService is null)
        {
            errors.Add("Field '_configurationService' must not be null.");
        }

        // Validate test keys collection
        var testKeys = _testKeysField?.GetValue(value) as List<ConfigurationKey>;
        if (testKeys is null)
        {
            errors.Add("Field '_testKeys' must not be null.");
        }
        else if (testKeys.Count == 0)
        {
            errors.Add("Field '_testKeys' must contain at least one item.");
        }

        // Validate cache prefix
        var cachePrefix = _cachePrefixField?.GetValue(value) as string;
        if (string.IsNullOrEmpty(cachePrefix))
        {
            errors.Add("Field 'CachePrefix' must not be null or empty.");
        }
        else if (cachePrefix.Length > 100)
        {
            errors.Add("Field 'CachePrefix' must not exceed 100 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CachingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this CachingBenchmarks value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CachingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when validation fails, containing a list of validation messages.</exception>
    public static void EnsureValid(this CachingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"CachingBenchmarks validation failed:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", errors)}",
                nameof(value));
        }
    }
}