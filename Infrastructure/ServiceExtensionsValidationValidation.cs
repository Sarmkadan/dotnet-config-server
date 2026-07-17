#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Validation helpers for the ServiceExtensionsValidation class itself
/// </summary>
public static class ServiceExtensionsValidationValidation
{
    /// <summary>
    /// Validates that ServiceExtensionsValidation class members are properly implemented
    /// </summary>
    /// <returns>List of validation problems, empty if valid</returns>
    public static IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        // Validate that all required methods exist and are callable
        try
        {
            // Test IConfiguration extension methods
            var config = default(IConfiguration);
            var configValidateResult = config.Validate();
            var configIsValidResult = config.IsValid();
            config.EnsureValid();

            // Test IServiceCollection extension methods
            var services = default(IServiceCollection);
            var servicesValidateResult = services.Validate();
            var servicesIsValidResult = services.IsValid();
            services.EnsureValid();

            // Test IServiceProvider extension methods
            var serviceProvider = default(IServiceProvider);
            var providerValidateResult = serviceProvider.Validate();
            var providerIsValidResult = serviceProvider.IsValid();
            serviceProvider.EnsureValid();
        }
        catch (Exception ex)
        {
            problems.Add($"ServiceExtensionsValidation methods threw exception: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if ServiceExtensionsValidation class is properly implemented
    /// </summary>
    /// <returns>True if ServiceExtensionsValidation is valid, false otherwise</returns>
    public static bool IsValid()
    {
        return Validate().Count == 0;
    }

    /// <summary>
    /// Ensures ServiceExtensionsValidation class is properly implemented
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if ServiceExtensionsValidation is invalid with list of problems</exception>
    public static void EnsureValid()
    {
        var problems = Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "ServiceExtensionsValidation class is invalid. " +
                string.Join(" ", problems),
                nameof(ServiceExtensionsValidation));
        }
    }
}