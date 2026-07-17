#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Represents validation results for service extensions configuration
/// </summary>
/// <param name="IsValid">Indicates whether the configuration is valid</param>
/// <param name="Problems">List of validation problems, empty if valid</param>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Problems)
{
    /// <summary>
    /// Creates a new valid validation result with no problems
    /// </summary>
    public static ValidationResult Valid => new(true, Array.Empty<string>());

    /// <summary>
    /// Creates a new invalid validation result with the specified problems
    /// </summary>
    /// <param name="problems">List of validation problems</param>
    /// <exception cref="ArgumentNullException">Thrown if problems is null</exception>
    public static ValidationResult Invalid(IReadOnlyList<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);
        return new ValidationResult(false, problems);
    }
}

/// <summary>
/// Provides System.Text.Json serialization helpers for ValidationResult
/// </summary>
public static class ServiceExtensionsValidationJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes the ValidationResult to a JSON string
    /// </summary>
    /// <param name="value">The ValidationResult to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the ValidationResult</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this ValidationResult value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a ValidationResult instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized ValidationResult instance, or null if JSON is empty or invalid</returns>
    public static ValidationResult? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ValidationResult>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a ValidationResult instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">The deserialized ValidationResult instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeds; otherwise, false</returns>
    public static bool TryFromJson(string json, out ValidationResult? value)
    {
        if (string.IsNullOrEmpty(json))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ValidationResult>(json, _jsonOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}