using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="VersioningBenchmarks"/> instances.
/// </summary>
public static class VersioningBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="VersioningBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this VersioningBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate service provider
        if (value._serviceProvider is null)
        {
            problems.Add("ServiceProvider is null");
        }

        // Validate test configuration ID
        if (value._testConfigurationId == Guid.Empty)
        {
            problems.Add("TestConfigurationId is empty (Guid.Empty)");
        }

        // Validate created versions list
        if (value._createdVersions is null)
        {
            problems.Add("_createdVersions collection is null");
        }
        else if (value._createdVersions.Count > 0)
        {
            // Check for empty GUIDs in created versions
            foreach (var versionId in value._createdVersions)
            {
                if (versionId == Guid.Empty)
                {
                    problems.Add("CreatedVersions contains empty Guid (Guid.Empty)");
                    break;
                }
            }
        }

        // Validate versioning service
        if (value._versioningService is null)
        {
            problems.Add("VersioningService is null");
        }

        // Validate configuration service
        if (value._configurationService is null)
        {
            problems.Add("ConfigurationService is null");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="VersioningBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this VersioningBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="VersioningBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid. The exception message contains the list of validation problems.</exception>
    public static void EnsureValid(this VersioningBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"VersioningBenchmarks instance is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}