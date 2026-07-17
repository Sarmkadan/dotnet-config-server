#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Exceptions;

/// <summary>
/// Extension methods for <see cref="NotFoundException"/>.
/// </summary>
public static class NotFoundExceptionExtensions
{
    /// <summary>
    /// Returns a message indicating that the requested entity was not found.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A message indicating that the requested entity was not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string GetNotFoundMessage(this NotFoundException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Entity '{exception.Details}' not found";
    }

    /// <summary>
    /// Returns a message indicating that the requested entity was not found, including the entity's ID.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A message indicating that the requested entity was not found, including the entity's ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string GetNotFoundMessageWithId(this NotFoundException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Entity '{exception.Details}' with ID '{exception.Details}' not found";
    }
}
