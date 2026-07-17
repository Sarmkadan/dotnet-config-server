# EncryptionService

The `EncryptionService` class provides cryptographic operations for encrypting and decrypting sensitive configuration values in `dotnet-config-server`. It supports both synchronous and asynchronous operations, key rotation, and re-encryption of existing configuration values. The service uses AES encryption with configurable key management, allowing for secure storage and retrieval of encrypted data.

## API

### `Encrypt(string plainText)`
Encrypts a plain text string synchronously.

**Parameters:**
- `plainText` (string): The text to encrypt. Must not be `null` or empty.

**Returns:**
- `string`: The encrypted cipher text in Base64 format.

**Throws:**
- `ArgumentNullException`: If `plainText` is `null`.
- `ArgumentException`: If `plainText` is empty.
- `InvalidOperationException`: If no valid encryption key is available.

---

### `Decrypt(string cipherText)`
Decrypts a cipher text string synchronously.

**Parameters:**
- `cipherText` (string): The encrypted text in Base64 format. Must not be `null` or empty.

**Returns:**
- `string`: The decrypted plain text.

**Throws:**
- `ArgumentNullException`: If `cipherText` is `null`.
- `ArgumentException`: If `cipherText` is empty or malformed.
- `InvalidOperationException`: If no valid encryption key is available or decryption fails.

---

### `EncryptAsync(string plainText)`
Asynchronously encrypts a plain text string.

**Parameters:**
- `plainText` (string): The text to encrypt. Must not be `null` or empty.

**Returns:**
- `Task<string>`: A task resolving to the encrypted cipher text in Base64 format.

**Throws:**
- `ArgumentNullException`: If `plainText` is `null`.
- `ArgumentException`: If `plainText` is empty.
- `InvalidOperationException`: If no valid encryption key is available.

---

### `DecryptAsync(string cipherText)`
Asynchronously decrypts a cipher text string.

**Parameters:**
- `cipherText` (string): The encrypted text in Base64 format. Must not be `null` or empty.

**Returns:**
- `Task<string>`: A task resolving to the decrypted plain text.

**Throws:**
- `ArgumentNullException`: If `cipherText` is `null`.
- `ArgumentException`: If `cipherText` is empty or malformed.
- `InvalidOperationException`: If no valid encryption key is available or decryption fails.

---

### `ValidateKey()`
Validates the current encryption key's integrity and availability.

**Returns:**
- `bool`: `true` if a valid key is available; otherwise, `false`.

---

### `GetPrimaryKeyAsync()`
Asynchronously retrieves the primary encryption key.

**Returns:**
- `Task<EncryptionKey?>`: A task resolving to the primary `EncryptionKey` if available, otherwise `null`.

---

### `GetKeyAsync()`
Asynchronously retrieves the most recent valid encryption key (primary or fallback).

**Returns:**
- `Task<EncryptionKey?>`: A task resolving to the most recent `EncryptionKey` if available, otherwise `null`.

---

### `GenerateNewKey()`
Generates a new encryption key with a secure random initialization vector (IV).

**Returns:**
- `EncryptionKey`: A new `EncryptionKey` instance.

---

### `RotateKeyAsync()`
Asynchronously rotates the encryption key by generating a new primary key and re-encrypting existing configuration values.

**Returns:**
- `Task`: A task representing the asynchronous operation.

**Throws:**
- `InvalidOperationException`: If key rotation fails or no valid key exists.

---

### `ReEncryptConfigurationAsync()`
Asynchronously re-encrypts all configuration values using the current primary key. Useful after key rotation.

**Returns:**
- `Task`: A task representing the asynchronous operation.

**Throws:**
- `InvalidOperationException`: If re-encryption fails.

## Usage

### Example 1: Encrypting and Decrypting Configuration Values
