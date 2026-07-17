#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetConfigServer.Events;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for domain event validation operations.
/// </summary>
public static class DomainEventValidationJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes domain event validation results to a JSON string.
    /// </summary>
    /// <param name="validationProblems">The validation problems to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the validation problems.</returns>
    /// <exception cref="ArgumentNullException">Thrown if validationProblems is null.</exception>
    public static string ToJson(this IReadOnlyList<string> validationProblems, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(validationProblems);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true,
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(validationProblems, options);
    }

    /// <summary>
    /// Deserializes a JSON string containing domain event validation problems.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A list of validation problems, or null if the JSON is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown if json is null.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized.</exception>
    public static IReadOnlyList<string>? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string containing domain event validation problems.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized validation problems if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out IReadOnlyList<string>? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<IReadOnlyList<string>>(json, _jsonOptions);
            return value != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}