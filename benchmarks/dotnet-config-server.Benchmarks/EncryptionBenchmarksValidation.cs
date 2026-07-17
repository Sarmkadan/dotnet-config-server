using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="EncryptionBenchmarks"/> instances.
/// </summary>
public static class EncryptionBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="EncryptionBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this EncryptionBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate private fields indirectly through public members
        // GlobalSetup should have been called, so we can check if services are initialized
        try
        {
            // These methods should not throw if properly initialized
            _ = value.EncryptSync();
            _ = value.DecryptSync();
            _ = value.EncryptAsync();
            _ = value.DecryptAsync();
            _ = value.ValidateKey();
            _ = value.GenerateNewKey();
            _ = value.RotateKey();
            _ = value.EncryptLargeText();
            _ = value.DecryptLargeText();
            _ = value.EncryptLargeTextAsync();
            _ = value.DecryptLargeTextAsync();
        }
        catch (Exception ex)
        {
            problems.Add($"Benchmark methods are not properly initialized: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="EncryptionBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this EncryptionBenchmarks value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="EncryptionBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this EncryptionBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"EncryptionBenchmarks instance is not valid. Problems: {string.Join("; ", problems)}");
        }
    }
}
