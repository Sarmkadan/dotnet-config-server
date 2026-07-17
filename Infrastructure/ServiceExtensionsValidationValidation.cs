#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Validation helpers for the ServiceExtensionsValidation class itself
/// </summary>
public static class ServiceExtensionsValidationValidation
{
    /// <summary>
    /// Validates that ServiceExtensionsValidation class members are properly implemented
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate against</param>
    /// <returns>List of validation problems, empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null</exception>
    public static IReadOnlyList<string> Validate(IServiceProvider? serviceProvider = null)
    {
        var problems = new List<string>();

        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

        // Validate IConfiguration extension methods
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        if (configuration == null)
        {
            problems.Add("IConfiguration service could not be resolved from service provider");
        }
        else
        {
            try
            {
                if (!configuration.IsValid())
                {
                    problems.Add("IConfiguration.IsValid() returned false");
                }
            }
            catch (Exception ex)
            {
                problems.Add($"IConfiguration validation threw exception: {ex.Message}");
            }
        }

        // Validate IServiceCollection extension methods are available
        // These are compile-time checks - we verify the extension methods exist by attempting to call them
        try
        {
            // These calls will compile only if the extension methods exist in ServiceExtensionsValidation
            var services = new ServiceCollection();
            services.Validate();
            services.IsValid();
            services.EnsureValid();
        }
        catch (Exception ex)
        {
            problems.Add($"IServiceCollection extension methods validation failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if ServiceExtensionsValidation class is properly implemented
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate against</param>
    /// <returns>True if ServiceExtensionsValidation is valid, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null</exception>
    public static bool IsValid(IServiceProvider? serviceProvider = null)
    {
        return Validate(serviceProvider).Count == 0;
    }

    /// <summary>
    /// Ensures ServiceExtensionsValidation class is properly implemented
    /// </summary>
    /// <param name="serviceProvider">The service provider to validate against</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null</exception>
    /// <exception cref="ArgumentException">Thrown if ServiceExtensionsValidation is invalid with list of problems</exception>
    public static void EnsureValid(IServiceProvider? serviceProvider = null)
    {
        var problems = Validate(serviceProvider);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "ServiceExtensionsValidation class is invalid. " +
                string.Join(" ", problems),
                nameof(ServiceExtensionsValidation));
        }
    }
}