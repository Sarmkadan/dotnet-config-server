using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DotnetConfigServer.Models;
using DotnetConfigServer.Services;

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

		// Check if GlobalSetup has been called by verifying private fields are initialized
		var plainTextField = value.GetType().GetField("_plainText", BindingFlags.Instance | BindingFlags.NonPublic);
		if (plainTextField?.GetValue(value) is not string plainText || string.IsNullOrEmpty(plainText))
		{
			problems.Add("Plain text field is not initialized. GlobalSetup() must be called first.");
		}

		var largePlainTextField = value.GetType().GetField("_largePlainText", BindingFlags.Instance | BindingFlags.NonPublic);
		if (largePlainTextField?.GetValue(value) is not string largePlainText || string.IsNullOrEmpty(largePlainText))
		{
			problems.Add("Large plain text field is not initialized. GlobalSetup() must be called first.");
		}

		var encryptionServiceField = value.GetType().GetField("_encryptionService", BindingFlags.Instance | BindingFlags.NonPublic);
		if (encryptionServiceField?.GetValue(value) is not IEncryptionService encryptionService)
		{
			problems.Add("Encryption service is not initialized. GlobalSetup() must be called first.");
		}

		var testKeyField = value.GetType().GetField("_testKey", BindingFlags.Instance | BindingFlags.NonPublic);
		if (testKeyField?.GetValue(value) is not EncryptionKey testKey)
		{
			problems.Add("Test encryption key is not initialized. GlobalSetup() must be called first.");
		}

		var testConfigurationIdField = value.GetType().GetField("_testConfigurationId", BindingFlags.Instance | BindingFlags.NonPublic);
		if (testConfigurationIdField?.GetValue(value) is not Guid testConfigurationId)
		{
			problems.Add("Test configuration ID is not initialized. GlobalSetup() must be called first.");
		}

		// If initialization checks passed, verify the benchmark methods can execute without throwing
		if (problems.Count == 0)
		{
			try
			{
				// Test synchronous operations
				_ = value.EncryptSync();
				_ = value.DecryptSync();

				// Test asynchronous operations
				_ = value.EncryptAsync();
				_ = value.DecryptAsync();

				// Test key operations
				_ = value.ValidateKey();
				_ = value.GenerateNewKey();
				_ = value.RotateKey();

				// Test large text operations
				_ = value.EncryptLargeText();
				_ = value.DecryptLargeText();
				_ = value.EncryptLargeTextAsync();
				_ = value.DecryptLargeTextAsync();
			}
			catch (Exception ex)
			{
				problems.Add($"Benchmark methods failed to execute: {ex.Message} ({ex.GetType().Name})");
			}
		}

		return problems.AsReadOnly();
	}

	/// <summary>
	/// Determines whether the specified <see cref="EncryptionBenchmarks"/> instance is valid.
	/// </summary>
	/// <param name="value">The benchmarks instance to check.</param>
	/// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
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