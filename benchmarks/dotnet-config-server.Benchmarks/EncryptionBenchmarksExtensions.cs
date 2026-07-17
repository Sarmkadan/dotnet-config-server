using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Extension methods for <see cref="EncryptionBenchmarks"/> providing additional benchmark scenarios
/// for encryption operations.
/// </summary>
public static class EncryptionBenchmarksExtensions
{
    /// <summary>
    /// Benchmarks the complete encryption and decryption roundtrip operation using synchronous APIs.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>The decrypted text after encryption and decryption roundtrip.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static string EncryptDecryptRoundtrip(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // Encrypt and immediately decrypt the plain text
        var cipherText = benchmarks.EncryptSync();
        return benchmarks.DecryptSync();
    }

    /// <summary>
    /// Benchmarks encryption and decryption operations with asynchronous APIs.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>The decrypted text after async encryption and decryption roundtrip.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static async Task<string> EncryptDecryptRoundtripAsync(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var cipherText = await benchmarks.EncryptAsync();
        return await benchmarks.DecryptAsync();
    }

    /// <summary>
    /// Benchmarks key generation followed by validation of the generated key.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>True if the generated key is valid, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static bool GenerateAndValidateKey(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // Generate a new key and validate it
        var newKey = benchmarks.GenerateNewKey();
        return benchmarks.ValidateKey();
    }

    /// <summary>
    /// Benchmarks encryption performance comparison between small and large text inputs.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>A dictionary containing encryption times for small and large text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static IReadOnlyDictionary<string, TimeSpan> CompareSmallVsLargeTextEncryption(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var results = new Dictionary<string, TimeSpan>();

        // Time small text encryption
        var smallTextStopwatch = System.Diagnostics.Stopwatch.StartNew();
        benchmarks.EncryptSync();
        smallTextStopwatch.Stop();
        results["SmallText"] = smallTextStopwatch.Elapsed;

        // Time large text encryption
        var largeTextStopwatch = System.Diagnostics.Stopwatch.StartNew();
        benchmarks.EncryptLargeText();
        largeTextStopwatch.Stop();
        results["LargeText"] = largeTextStopwatch.Elapsed;

        return results;
    }
}