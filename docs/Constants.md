# Constants

`AppConstants` (in `Common/Constants.cs`, namespace `DotnetConfigServer.Common`) is a static class that centralizes application-wide limits, format versions, and literal values shared across the codebase. Keeping them in one place means validation rules, encryption settings, and HTTP contract details can be changed without hunting through services and models. All members are `const`, so they are compile-time values and can be used in attributes, default initializers, and string interpolation.

## API

The constants are grouped into nested static classes. Access them as `AppConstants.<Group>.<Member>`.

### Configuration
```csharp
public static class AppConstants.Configuration
```
Length limits for user-supplied configuration text.

- `MaxKeyLength` (`256`) - maximum length of configuration names and key names. Enforced by `Configuration` and `ConfigurationKey` model validation.
- `MaxDescriptionLength` (`1024`) - maximum length of a configuration description. Enforced by `Configuration` model validation.
- `MaxValueLength` (`10_000`) - maximum length of a configuration key value. Enforced by `ConfigurationKey` model validation.

### Encryption
```csharp
public static class AppConstants.Encryption
```
Parameters for the AES-based value encryption and the ciphertext version tag.

- `AesKeySize` (`256`) - AES key size in bits, applied to the AES instances used by `EncryptionService`.
- `AesSaltSize` (`16`) - size in bytes of the random salt generated for each key derivation.
- `AesIterations` (`10000`) - PBKDF2 iteration count for key derivation. Currently not referenced by any code outside this file.
- `LegacyCiphertextVersion` (`1`) - version assumed for ciphertext that has no `v<n>:` prefix. Keeps secrets written before version tagging decryptable.
- `CurrentCiphertextVersion` (`2`) - version written by `EncryptionService` for all new encryptions. `EncryptionKeyRotationWorker` uses it to find values still encrypted with an older version. Bump it when the on-disk ciphertext layout changes.

### Versioning
```csharp
public static class AppConstants.Versioning
```
Retention settings for configuration history.

- `MaxVersionHistory` (`100`) - default number of configuration versions retained per application. Used as the default for `Application.MaxVersionHistory`.

### Webhook
```csharp
public static class AppConstants.Webhook
```
Defaults and limits for webhook subscriptions.

- `MaxWebhookUrl` (`2048`) - maximum length of a webhook URL. Enforced by `WebhookSubscription` model validation.
- `MaxRetries` (`5`) - default retry count for a webhook subscription. Used as the default for `WebhookSubscription.MaxRetries`.
- `TimeoutSeconds` (`30`) - intended default timeout for webhook delivery. Currently not referenced by any code outside this file; `WebhookNotifierOptions` defines its own timeout default.

### Validation
```csharp
public static class AppConstants.Validation
```
Regular expression patterns used for input validation.

- `UrlPattern` (`^https?://`) - requires a webhook URL to start with `http://` or `https://`.
- `KeyPattern` (`^[a-zA-Z0-9_\-\.]+$`) - allows only ASCII letters, digits, underscores, hyphens, and dots in configuration key names.

### Api
```csharp
public static class AppConstants.Api
```
Values that form part of the HTTP contract.

- `ApiKeyHeader` (`"X-API-Key"`) - name of the request header that carries the API key. Read by the rate-limiting middleware to identify clients.

## Usage

```csharp
using DotnetConfigServer.Common;

if (value.Length > AppConstants.Configuration.MaxValueLength)
    errors.AddError("Value", $"Value cannot exceed {AppConstants.Configuration.MaxValueLength} characters");
```

Because the values are `const`, changing one requires recompiling every consumer. Tests should reference the constants rather than repeating the literal numbers, so that a limit change does not silently break assertions.
