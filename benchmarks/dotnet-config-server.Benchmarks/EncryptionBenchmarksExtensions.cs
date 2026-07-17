using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        var decryptedText = benchmarks.DecryptSync();

        // Verify the roundtrip produces the original plain text
        if (!string.Equals(benchmarks.GetPlainText(), decryptedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Decrypted text does not match original plain text after roundtrip.");
        }

        return decryptedText;
    }

    /// <summary>
    /// Benchmarks encryption and decryption operations with asynchronous APIs.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>The decrypted text after async encryption and decryption roundtrip.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the decrypted text does not match the original plain text.</exception>
    public static async Task<string> EncryptDecryptRoundtripAsync(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var cipherText = await benchmarks.EncryptAsync();
        var decryptedText = await benchmarks.DecryptAsync();

        // Verify the roundtrip produces the original plain text
        if (!string.Equals(benchmarks.GetPlainText(), decryptedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Decrypted text does not match original plain text after async roundtrip.");
        }

        return decryptedText;
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
    /// <exception cref="InvalidOperationException">Thrown when encryption or decryption operations fail.</exception>
    public static IReadOnlyDictionary<string, TimeSpan> CompareSmallVsLargeTextEncryption(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var results = new Dictionary<string, TimeSpan>();

        // Time small text encryption and verify correctness
        var smallTextStopwatch = Stopwatch.StartNew();
        var encryptedSmallText = benchmarks.EncryptSync();
        var decryptedSmallText = benchmarks.DecryptSync();
        smallTextStopwatch.Stop();

        if (!string.Equals(benchmarks.GetPlainText(), decryptedSmallText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Small text encryption/decryption roundtrip failed - decrypted text does not match original.");
        }

        results["SmallText"] = smallTextStopwatch.Elapsed;

        // Time large text encryption and verify correctness
        var largeTextStopwatch = Stopwatch.StartNew();
        var encryptedLargeText = benchmarks.EncryptLargeText();
        var decryptedLargeText = benchmarks.DecryptLargeText();
        largeTextStopwatch.Stop();

        if (!string.Equals(new string('A', 1024), decryptedLargeText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Large text encryption/decryption roundtrip failed - decrypted text does not match original.");
        }

        results["LargeText"] = largeTextStopwatch.Elapsed;

        return results;
    }

    /// <summary>
    /// Gets the plain text used for benchmarking encryption operations.
    /// </summary>
    /// <param name="benchmarks">The encryption benchmarks instance.</param>
    /// <returns>The plain text string used in benchmarks.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    private static string GetPlainText(this EncryptionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks.GetType().GetField("_plainText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(benchmarks) as string
            ?? throw new InvalidOperationException("Plain text field not initialized in EncryptionBenchmarks.");
    }
}