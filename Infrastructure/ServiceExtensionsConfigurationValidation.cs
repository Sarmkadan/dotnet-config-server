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
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static IReadOnlyList<string> Validate(this ServiceExtensionsConfiguration value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate DataServices
        if (value.DataServices is not null)
        {
            if (value.DataServices.Length == 0)
            {
                problems.Add("DataServices array is empty");
            }
            else
            {
                for (int i = 0; i < value.DataServices.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.DataServices[i]))
                    {
                        problems.Add($"DataServices[{i}] is null or whitespace");
                    }
                }
            }
        }

        // Validate BusinessServices
        if (value.BusinessServices is not null)
        {
            if (value.BusinessServices.Length == 0)
            {
                problems.Add("BusinessServices array is empty");
            }
            else
            {
                for (int i = 0; i < value.BusinessServices.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.BusinessServices[i]))
                    {
                        problems.Add($"BusinessServices[{i}] is null or whitespace");
                    }
                }
            }
        }

        // Validate WebhookClient
        if (value.WebhookClient is not null)
        {
            if (value.WebhookClient.Length == 0)
            {
                problems.Add("WebhookClient array is empty");
            }
            else
            {
                for (int i = 0; i < value.WebhookClient.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.WebhookClient[i]))
                    {
                        problems.Add($"WebhookClient[{i}] is null or whitespace");
                    }
                }
            }
        }

        // Validate SwaggerConfiguration
        if (value.SwaggerConfiguration is not null)
        {
            if (value.SwaggerConfiguration.Length == 0)
            {
                problems.Add("SwaggerConfiguration array is empty");
            }
            else
            {
                for (int i = 0; i < value.SwaggerConfiguration.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.SwaggerConfiguration[i]))
                    {
                        problems.Add($"SwaggerConfiguration[{i}] is null or whitespace");
                    }
                }
            }
        }

        // Validate DatabaseInitialization
        if (value.DatabaseInitialization is not null)
        {
            if (value.DatabaseInitialization.Length == 0)
            {
                problems.Add("DatabaseInitialization array is empty");
            }
            else
            {
                for (int i = 0; i < value.DatabaseInitialization.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(value.DatabaseInitialization[i]))
                    {
                        problems.Add($"DatabaseInitialization[{i}] is null or whitespace");
                    }
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the ServiceExtensionsConfiguration instance is valid
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration instance to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static bool IsValid(this ServiceExtensionsConfiguration value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the ServiceExtensionsConfiguration instance is valid
    /// </summary>
    /// <param name="value">The ServiceExtensionsConfiguration instance to validate</param>
    /// <exception cref="ArgumentException">Thrown when value is not valid, containing the validation problems</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static void EnsureValid(this ServiceExtensionsConfiguration value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ServiceExtensionsConfiguration is not valid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}