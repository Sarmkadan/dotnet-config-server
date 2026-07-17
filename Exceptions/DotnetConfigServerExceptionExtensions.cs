#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Extension methods for <see cref="DotnetConfigServerException"/>.
/// </summary>
public static class DotnetConfigServerExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception has the specified error code.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <param name="code">The error code to compare.</param>
    /// <returns>True if the exception's <see cref="DotnetConfigServerException.ErrorCode"/> equals <paramref name="code"/>; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is null or empty.</exception>
    public static bool HasErrorCode(this DotnetConfigServerException exception, string code)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(code);
        return exception.ErrorCode == code;
    }

    /// <summary>
    /// Attempts to cast the <see cref="DotnetConfigServerException.Details"/> property to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="exception">The exception instance.</param>
    /// <returns>The casted details if successful; otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static T? GetDetailsAs<T>(this DotnetConfigServerException exception) where T : class
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Details as T;
    }

    /// <summary>
    /// Returns a combined message that includes the exception message and, if present, the details.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A string containing the message and details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string GetFullMessage(this DotnetConfigServerException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.Details != null
            ? $"{exception.Message}: {exception.Details}"
            : exception.Message;
    }

    /// <summary>
    /// Returns the error code if present; otherwise returns the supplied default value.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <param name="defaultValue">The default value to return if <see cref="DotnetConfigServerException.ErrorCode"/> is null.</param>
    /// <returns>The error code or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string? GetErrorCodeOrDefault(this DotnetConfigServerException exception, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.ErrorCode ?? defaultValue;
    }
}
