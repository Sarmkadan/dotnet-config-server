#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Validation helpers for ServiceExtensions configuration and parameters
/// </summary>
public static class ServiceExtensionsValidation
{
    /// <summary>
    /// Validates configuration parameters for ServiceExtensions extension methods
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>List of validation problems, empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if configuration is null</exception>
    public static IReadOnlyList<string> Validate(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var problems = new List<string>();

        // Validate DefaultConnection string exists and is not empty
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            problems.Add("Configuration is missing or empty DefaultConnection string");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates configuration parameters for ServiceExtensions extension methods
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public static bool IsValid(this IConfiguration configuration)
    {
        return Validate(configuration).Count == 0;
    }

    /// <summary>
    /// Ensures configuration parameters for ServiceExtensions extension methods are valid
    /// </summary>
    /// <param name="configuration">Configuration to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if configuration is null</exception>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid with list of problems</exception>
    public static void EnsureValid(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var problems = Validate(configuration);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "ServiceExtensions configuration is invalid. " +
                string.Join(" ", problems),
                nameof(configuration));
        }
    }

    /// <summary>
    /// Validates service collection parameter
    /// </summary>
    /// <param name="services">Service collection to validate</param>
    /// <returns>List of validation problems, empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
    public static IReadOnlyList<string> Validate(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates service collection parameter
    /// </summary>
    /// <param name="services">Service collection to validate</param>
    /// <returns>True if service collection is valid, false otherwise</returns>
    public static bool IsValid(this IServiceCollection services)
    {
        return Validate(services).Count == 0;
    }

    /// <summary>
    /// Ensures service collection parameter is valid
    /// </summary>
    /// <param name="services">Service collection to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
    public static void EnsureValid(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
    }

    /// <summary>
    /// Validates service provider parameter
    /// </summary>
    /// <param name="serviceProvider">Service provider to validate</param>
    /// <returns>List of validation problems, empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null</exception>
    public static IReadOnlyList<string> Validate(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates service provider parameter
    /// </summary>
    /// <param name="serviceProvider">Service provider to validate</param>
    /// <returns>True if service provider is valid, false otherwise</returns>
    public static bool IsValid(this IServiceProvider serviceProvider)
    {
        return Validate(serviceProvider).Count == 0;
    }

    /// <summary>
    /// Ensures service provider parameter is valid
    /// </summary>
    /// <param name="serviceProvider">Service provider to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null</exception>
    public static void EnsureValid(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
    }
}