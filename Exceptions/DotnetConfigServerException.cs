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
        ArgumentNullException.ThrowIfNull(message);
    }

    public DotnetConfigServerException(string message, Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }

    public DotnetConfigServerException(string message, string errorCode, object? details = null) : base(message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(errorCode);

        ErrorCode = errorCode;
        Details = details;
    }

    public override string ToString() => $"DotnetConfigServerException {{ ErrorCode = {ErrorCode}, Details = {Details} }}";
}
