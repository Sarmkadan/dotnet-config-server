using System;
using System.Text.Json;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="VersioningBenchmarks"/>.
/// </summary>
public static class VersioningBenchmarksJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	/// <summary>
	/// Serializes a <see cref="VersioningBenchmarks"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The benchmarks instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the benchmarks.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this VersioningBenchmarks value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		return indented
			? JsonSerializer.Serialize(value, _jsonSerializerOptions)
			: JsonSerializer.Serialize(value);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="VersioningBenchmarks"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized benchmarks instance, or null if deserialization fails.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static VersioningBenchmarks? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			return JsonSerializer.Deserialize<VersioningBenchmarks>(json, _jsonSerializerOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="VersioningBenchmarks"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized benchmarks instance if successful.</param>
	/// <returns>True if deserialization succeeds; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string json, out VersioningBenchmarks? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			value = JsonSerializer.Deserialize<VersioningBenchmarks>(json, _jsonSerializerOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}
