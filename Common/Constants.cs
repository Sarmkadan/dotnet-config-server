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
    public const string ApplicationName = "DotnetConfigServer";
    public const string ApplicationVersion = "1.0.0";
    public const string ApiVersion = "v1";

    public static class Configuration
    {
        public const int MaxKeyLength = 256;
        public const int MaxDescriptionLength = 1024;
        public const int MaxValueLength = 10_000;
        public const int MinKeyLength = 1;
        public const string DefaultEnvironment = "Development";
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
        public const string VersionNumberFormat = "{0}.{1}.{2}";
    }

    public static class Webhook
    {
        public const int MaxWebhookUrl = 2048;
        public const int MaxRetries = 5;
        public const int TimeoutSeconds = 30;
        public const int BatchSize = 100;
    }

    public static class Validation
    {
        public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        public const string UrlPattern = @"^https?://";
        public const string KeyPattern = @"^[a-zA-Z0-9_\-\.]+$";
    }

    public static class Cache
    {
        public const int ConfigurationCacheDurationMinutes = 15;
        public const int VersionCacheDurationMinutes = 30;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
    }

    public static class Api
    {
        public const string ApiKeyHeader = "X-API-Key";
        public const string CorrelationIdHeader = "X-Correlation-Id";
        public const string RequestIdHeader = "X-Request-Id";
    }
}

/// <summary>
/// Error codes used throughout the application
/// </summary>
public static class ErrorCodes
{
    public const string InvalidInput = "INVALID_INPUT";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string EncryptionFailed = "ENCRYPTION_FAILED";
    public const string DatabaseError = "DATABASE_ERROR";
}

/// <summary>
/// Common response messages
/// </summary>
public static class ResponseMessages
{
    public const string Success = "Operation completed successfully";
    public const string Created = "Resource created successfully";
    public const string Updated = "Resource updated successfully";
    public const string Deleted = "Resource deleted successfully";
    public const string BadRequest = "Invalid request data";
    public const string NotFound = "Requested resource not found";
    public const string InternalError = "An unexpected error occurred";
    public const string Unauthorized = "Authentication required";
    public const string Forbidden = "You do not have permission to perform this action";
}
