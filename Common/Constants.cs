#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetConfigServer.Common;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    public static class Configuration
    {
        public const int MaxKeyLength = 256;
        public const int MaxDescriptionLength = 1024;
        public const int MaxValueLength = 10_000;
    }

    public static class Encryption
    {
        public const int AesKeySize = 256;
        public const int AesSaltSize = 16;
        public const int AesIterations = 10000;

        /// <summary>
        /// Ciphertext format version emitted for values that predate the version-tag
        /// prefix. Ciphertext without a recognizable "v&lt;n&gt;:" header is assumed to be
        /// this version so existing stored secrets keep decrypting after the upgrade.
        /// </summary>
        public const int LegacyCiphertextVersion = 1;

        /// <summary>
        /// Ciphertext format version written by <see cref="Services.EncryptionService"/>
        /// for every new encryption. Bump this when the on-disk ciphertext layout changes
        /// so older builds/keys can still be told apart from newer ones.
        /// </summary>
        public const int CurrentCiphertextVersion = 2;
    }

    public static class Versioning
    {
        public const int MaxVersionHistory = 100;
    }

    public static class Webhook
    {
        public const int MaxWebhookUrl = 2048;
        public const int MaxRetries = 5;
        public const int TimeoutSeconds = 30;
    }

    public static class Validation
    {
        public const string UrlPattern = @"^https?://";
        public const string KeyPattern = @"^[a-zA-Z0-9_\-\.]+$";
    }

    public static class Api
    {
        public const string ApiKeyHeader = "X-API-Key";
    }
}
