#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Provides validation helpers for ServiceExtensionsConfiguration
/// </summary>
public static class ServiceExtensionsConfigurationValidation
{
    /// <summary>
    /// Validates the ServiceExtensionsConfiguration instance
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration instance to validate</param>
    /// <returns>A list of validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ServiceExtensionsConfiguration value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate DataServices
        ValidateStringArray(value.DataServices, nameof(value.DataServices), problems);

        // Validate BusinessServices
        ValidateStringArray(value.BusinessServices, nameof(value.BusinessServices), problems);

        // Validate WebhookClient
        ValidateStringArray(value.WebhookClient, nameof(value.WebhookClient), problems);

        // Validate SwaggerConfiguration
        ValidateStringArray(value.SwaggerConfiguration, nameof(value.SwaggerConfiguration), problems);

        // Validate DatabaseInitialization
        ValidateStringArray(value.DatabaseInitialization, nameof(value.DatabaseInitialization), problems);

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a string array property
    /// </summary>
    /// <param name="array">The array to validate</param>
    /// <param name="propertyName">The name of the property being validated</param>
    /// <param name="problems">The collection to add validation problems to</param>
    private static void ValidateStringArray(string[]? array, string propertyName, List<string> problems)
    {
        if (array is not null)
        {
            if (array.Length == 0)
            {
                problems.Add($"{propertyName} array is empty");
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(array[i]))
                    {
                        problems.Add($"{propertyName}[{i}] is null or whitespace");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determines whether the ServiceExtensionsConfiguration instance is valid
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration instance to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this ServiceExtensionsConfiguration? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the ServiceExtensionsConfiguration instance is valid
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration instance to validate</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid, containing the validation problems</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static void EnsureValid(this ServiceExtensionsConfiguration value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ServiceExtensionsConfiguration is not valid. Problems:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}