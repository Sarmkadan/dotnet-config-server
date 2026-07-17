using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ConfigurationException"/>.
/// </summary>
public static class ConfigurationExceptionJsonExtensionsJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes a <see cref="ConfigurationException"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The exception to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the exception.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
	public static string ToJson(this ConfigurationException value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="ConfigurationException"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized <see cref="ConfigurationException"/> instance, or null if the JSON is null, empty, or whitespace.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
	/// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
	public static ConfigurationException? FromJson(string? json)
	{
		ArgumentNullException.ThrowIfNull(json);

		return string.IsNullOrWhiteSpace(json)
			? null
			: JsonSerializer.Deserialize<ConfigurationException>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a <see cref="ConfigurationException"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">The resulting <see cref="ConfigurationException"/> instance if deserialization succeeds; otherwise, null.</param>
	/// <returns>True if deserialization succeeds; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string? json, out ConfigurationException? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			value = JsonSerializer.Deserialize<ConfigurationException>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}