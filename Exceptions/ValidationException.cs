#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Thrown when validation fails
/// </summary>
sealed public class ValidationException : DotnetConfigServerException
{
    public Dictionary<string, List<string>> Errors { get; set; }

    public ValidationException(string message, Dictionary<string, List<string>> errors) : base(message, "VALIDATION_FAILED", errors)
    {
        Errors = errors;
    }

    public ValidationException(string message, Dictionary<string, List<string>> errors, string errorCode) : base(message, errorCode, errors)
    {
        Errors = errors;
    }

    public ValidationException(string fieldName, string message) : base($"Validation failed: {fieldName} - {message}", "VALIDATION_FAILED")
    {
        Errors = new() { { fieldName, new() { message } } };
    }
}