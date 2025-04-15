#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Base exception for all DotnetConfigServer-specific exceptions
/// </summary>
public class DotnetConfigServerException : Exception
{
    public string? ErrorCode { get; set; }
    public object? Details { get; set; }

    public DotnetConfigServerException(string message) : base(message)
    {
    }

    public DotnetConfigServerException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DotnetConfigServerException(string message, string errorCode, object? details = null) : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}