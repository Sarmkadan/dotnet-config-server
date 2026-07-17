#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DotnetConfigServer.Events;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for domain event operations.
/// </summary>
public static class DomainEventExtensionsJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes a domain event to a JSON string.
	/// </summary>
	/// <param name="value">The domain event to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the domain event.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this DomainEvent value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);
		var options = indented
			? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
			: _jsonSerializerOptions;
		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a domain event.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized domain event, or null if the input is null, empty, whitespace, or deserialization fails.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static DomainEvent? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		try
		{
			return JsonSerializer.Deserialize<DomainEvent>(json, _jsonSerializerOptions);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a domain event.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized domain event if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string json, [NotNullWhen(true)] out DomainEvent? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			value = null;
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<DomainEvent>(json, _jsonSerializerOptions);
			return value is not null;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}
