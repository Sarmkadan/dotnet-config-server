#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Models;
using Microsoft.Extensions.Options;

namespace DotnetConfigServer.Infrastructure;

/// <summary>
/// Startup validation for <see cref="DotnetConfigServerOptions"/>. Covers cross-field rules and
/// URL checks that data annotations cannot express. Failure messages name the property and the
/// offending value so the host error at startup points directly at the bad setting.
/// </summary>
public sealed class DotnetConfigServerOptionsValidator : IValidateOptions<DotnetConfigServerOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DotnetConfigServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateEncryptionKeySize(options.Encryption, failures);
        ValidateTrustedOrigins(options.Security.TrustedOrigins, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// The key size in bits must match the AES variant named by <c>Algorithm</c>.
    /// An unknown or missing algorithm is reported by the data annotations on the options class.
    /// </summary>
    private static void ValidateEncryptionKeySize(EncryptionOptions encryption, List<string> failures)
    {
        var requiredKeySize = encryption.Algorithm switch
        {
            "AES128" => 128,
            "AES192" => 192,
            "AES256" => 256,
            _ => (int?)null
        };

        if (requiredKeySize is int expected && encryption.KeySize != expected)
        {
            failures.Add(
                $"EncryptionOptions.KeySize must be {expected} for Algorithm '{encryption.Algorithm}', got '{encryption.KeySize}'");
        }
    }

    /// <summary>
    /// Each trusted origin must be an absolute URL with an http or https scheme.
    /// </summary>
    private static void ValidateTrustedOrigins(string[] origins, List<string> failures)
    {
        for (var i = 0; i < origins.Length; i++)
        {
            var origin = origins[i];
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add(
                    $"SecurityOptions.TrustedOrigins[{i}] must be an absolute http or https URL, got '{origin}'");
            }
        }
    }
}
