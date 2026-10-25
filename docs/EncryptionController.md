# EncryptionController

The `EncryptionController` provides HTTP endpoints for encryption key rotation and on-demand re-encryption of configuration secrets in the dotnet-config-server. It exposes two operations: marking an encryption key as rotated, and migrating every encrypted value of a configuration to its current primary key.

## Routing

- Base route: `api/v1/configurations/{configurationId}/encryption`
- Produces: `application/json`
- Decorated with `[ApiController]`, so model validation failures return `400 BadRequest` automatically.

## API

### `EncryptionController`
The controller class exposing encryption management endpoints. It depends on `IEncryptionService`, `IConfigurationKeyRepository` and `ILogger<EncryptionController>`.

### `public async Task<IActionResult> RotateKey([FromRoute] string oldKeyId, [FromQuery] string userId)`
Marks an encryption key as rotated: it is no longer the primary key but remains active for decryption. This is the first step of a key rotation.

**Route**
- `POST /api/v1/configurations/{configurationId}/encryption/keys/{oldKeyId}/rotate?userId={userId}`

**Parameters**
- `oldKeyId` (route): The identifier of the key being rotated out.
- `userId` (query): The identifier of the user performing the rotation.

**Request body**
- None.

**Return value**
- `204 NoContent` on success, with no response body.
- `404 NotFound` if the service throws `ConfigurationNotFoundException`. Body: `{ "error": "<message>" }`.
- `500 InternalServerError` for any other exception. Body: `{ "error": "Internal server error" }`. The exception is logged at error level.

**Exceptions**
- Throws `ArgumentNullException` if `oldKeyId` or `userId` is null. This check runs before the `try` block, so the exception is not converted into a response by the controller.

---

### `public async Task<IActionResult> ReEncryptConfiguration([FromRoute] Guid configurationId, [FromQuery] string userId)`
Re-encrypts every encrypted value belonging to a configuration with its current primary encryption key. Values still protected by an older key version are migrated.

The action loads the configuration's keys from `IConfigurationKeyRepository`, keeps only those with `IsEncrypted == true`, passes them to `IEncryptionService.ReEncryptConfigurationAsync`, and then persists each one with `IConfigurationKeyRepository.UpdateAsync`.

**Route**
- `POST /api/v1/configurations/{configurationId}/encryption/re-encrypt?userId={userId}`

**Parameters**
- `configurationId` (route): The configuration whose values should be migrated.
- `userId` (query): The identifier of the user requesting the migration.

**Request body**
- None.

**Response body (200 OK)**
```json
{
  "configurationId": "3f2a9c1e-8b7d-4e6a-9f10-2d5c7b8a1e44",
  "migratedCount": 3
}
```
- `configurationId`: The configuration that was processed.
- `migratedCount`: The number of encrypted keys that were loaded and passed through re-encryption. Keys that were already on the primary key are included in this count.

**Return value**
- `200 OK` with the body above on success.
- `400 BadRequest` if a `ConfigurationException` is thrown, including `ConfigurationNotFoundException`. Body: `{ "error": "<message>" }`.
- `500 InternalServerError` for any other exception. Body: `{ "error": "Internal server error" }`. The exception is logged at error level.

**Exceptions**
- Throws `ArgumentNullException` if `userId` is null. As with `RotateKey`, this check runs before the `try` block.

---

## Usage

```csharp
// Example 1: Rotate a key, then migrate the configuration's secrets
var configurationId = Guid.Parse("3f2a9c1e-8b7d-4e6a-9f10-2d5c7b8a1e44");
var oldKeyId = "key-2026-01";
var userId = "ops-user-42";

var rotateResponse = await client.PostAsync(
    $"/api/v1/configurations/{configurationId}/encryption/keys/{oldKeyId}/rotate?userId={userId}",
    null);
rotateResponse.EnsureSuccessStatusCode(); // 204 NoContent

var reEncryptResponse = await client.PostAsync(
    $"/api/v1/configurations/{configurationId}/encryption/re-encrypt?userId={userId}",
    null);
reEncryptResponse.EnsureSuccessStatusCode();

var result = await reEncryptResponse.Content.ReadFromJsonAsync<JsonElement>();
Console.WriteLine($"Migrated {result.GetProperty("migratedCount").GetInt32()} encrypted values.");
```

```csharp
// Example 2: Handle a failed re-encryption
var response = await client.PostAsync(
    $"/api/v1/configurations/{configurationId}/encryption/re-encrypt?userId={userId}",
    null);

if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadFromJsonAsync<JsonElement>();
    Console.WriteLine($"Re-encryption failed ({(int)response.StatusCode}): {error.GetProperty("error").GetString()}");
}
```

## Notes

- Both endpoints take `userId` as a query parameter rather than from the request body or an authenticated principal. The controller does not validate it beyond the null check.
- `RotateKey` declares the `{configurationId}` route segment, but the action has no matching parameter, so the segment is not used. The rotation is applied to `oldKeyId` regardless of the configuration in the URL. Clients should still pass the configuration that owns the key for consistency with the route.
- `ReEncryptConfiguration` returns `400 BadRequest` for a missing configuration, not `404 NotFound`, because `ConfigurationNotFoundException` derives from `ConfigurationException` and the action catches the base type.
- `ReEncryptConfiguration` persists keys only after `ReEncryptConfigurationAsync` completes without error, so a failure in the service leaves stored values unchanged. Every encrypted key is re-encrypted on each call, so repeating the request is safe; it only repeats the work.
- Plain (unencrypted) keys are never passed to re-encryption and are not updated.
- Error responses for unexpected failures never include exception details; the details are written to the log only.
