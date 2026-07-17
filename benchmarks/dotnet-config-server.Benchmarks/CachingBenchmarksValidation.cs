using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotnetConfigServer.Benchmarks;
using DotnetConfigServer.Models;

/// <summary>
/// Provides validation helpers for <see cref="CachingBenchmarks"/> instances.
/// Validates that benchmark configuration values are within expected ranges and not in default/empty states.
/// </summary>
public static class CachingBenchmarksValidation
{
    private static readonly FieldInfo _testApplicationIdField = typeof(CachingBenchmarks).GetField("_testApplicationId", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_testApplicationId' not found in CachingBenchmarks");
    private static readonly FieldInfo _testConfigurationIdField = typeof(CachingBenchmarks).GetField("_testConfigurationId", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_testConfigurationId' not found in CachingBenchmarks");
    private static readonly FieldInfo _serviceProviderField = typeof(CachingBenchmarks).GetField("_serviceProvider", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_serviceProvider' not found in CachingBenchmarks");
    private static readonly FieldInfo _memoryCacheField = typeof(CachingBenchmarks).GetField("_memoryCache", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_memoryCache' not found in CachingBenchmarks");
    private static readonly FieldInfo _configurationServiceField = typeof(CachingBenchmarks).GetField("_configurationService", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_configurationService' not found in CachingBenchmarks");
    private static readonly FieldInfo _testKeysField = typeof(CachingBenchmarks).GetField("_testKeys", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Field '_testKeys' not found in CachingBenchmarks");
    private static readonly FieldInfo _cachePrefixField = typeof(CachingBenchmarks).GetField("CachePrefix", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException("Field/constant 'CachePrefix' not found in CachingBenchmarks");

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

        // Validate Guid fields using pattern matching
        var testApplicationId = (Guid?)_testApplicationIdField.GetValue(value);
        if (testApplicationId == default)
        {
            errors.Add("Field '_testApplicationId' must not be default (Guid.Empty).");
        }

        var testConfigurationId = (Guid?)_testConfigurationIdField.GetValue(value);
        if (testConfigurationId == default)
        {
            errors.Add("Field '_testConfigurationId' must not be default (Guid.Empty).");
        }

        // Validate service provider using pattern matching
        var serviceProvider = _serviceProviderField.GetValue(value);
        if (serviceProvider is null)
        {
            errors.Add("Field '_serviceProvider' must not be null.");
        }

        // Validate memory cache using pattern matching
        var memoryCache = _memoryCacheField.GetValue(value);
        if (memoryCache is null)
        {
            errors.Add("Field '_memoryCache' must not be null.");
        }

        // Validate configuration service using pattern matching
        var configurationService = _configurationServiceField.GetValue(value);
        if (configurationService is null)
        {
            errors.Add("Field '_configurationService' must not be null.");
        }

        // Validate test keys collection
        var testKeys = _testKeysField.GetValue(value) as List<ConfigurationKey>;
        if (testKeys is null)
        {
            errors.Add("Field '_testKeys' must not be null.");
        }
        else if (testKeys.Count == 0)
        {
            errors.Add("Field '_testKeys' must contain at least one item.");
        }

        // Validate cache prefix using guard clause and pattern matching
        var cachePrefix = _cachePrefixField.GetValue(null) as string;
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
        => Validate(value).Count == 0;

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