#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Thrown when database operation fails
/// </summary>
sealed public class DatabaseException : DotnetConfigServerException
{
    public DatabaseException(string message) : base(message, "DATABASE_ERROR")
    {
    }

    public DatabaseException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "DATABASE_ERROR";
    }

    public DatabaseException(string message, string errorCode, object? details = null) : base(message, errorCode, details)
    {
    }
}