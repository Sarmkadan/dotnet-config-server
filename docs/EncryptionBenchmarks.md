# EncryptionBenchmarks

A benchmarking utility for evaluating the performance and correctness of symmetric encryption operations in the `dotnet-config-server` project. It measures throughput and latency for encrypting and decrypting data synchronously and asynchronously, including large payloads, and validates key integrity before and after operations.

## API

### `public async Task GlobalSetup()`
Initializes the benchmark environment by generating a new encryption key and preparing any required resources. This method must be called before any encryption or decryption operations are performed. Throws `InvalidOperationException` if the key generation fails or if called more than once without an intervening `GlobalCleanup`.

### `public async Task GlobalCleanup()`
Releases resources allocated during `GlobalSetup`, including the current encryption key. After this call, any subsequent encryption or decryption operations will fail until `GlobalSetup` is called again. Throws `InvalidOperationException` if called before `GlobalSetup`.

### `public string EncryptSync(string plaintext)`
Encrypts the given plaintext synchronously using the current encryption key. Returns the base64-encoded ciphertext. Throws `ArgumentNullException` if `plaintext` is `null`. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public string DecryptSync(string ciphertext)`
Decrypts the given base64-encoded ciphertext synchronously using the current encryption key. Returns the original plaintext. Throws `ArgumentNullException` if `ciphertext` is `null`. Throws `FormatException` if `ciphertext` is not valid base64. Throws `CryptographicException` if decryption fails due to incorrect key or corrupted data. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public async Task<string> EncryptAsync(string plaintext)`
Asynchronously encrypts the given plaintext using the current encryption key. Returns a `Task<string>` that resolves to the base64-encoded ciphertext. Throws `ArgumentNullException` if `plaintext` is `null`. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public async Task<string> DecryptAsync(string ciphertext)`
Asynchronously decrypts the given base64-encoded ciphertext using the current encryption key. Returns a `Task<string>` that resolves to the original plaintext. Throws `ArgumentNullException` if `ciphertext` is `null`. Throws `FormatException` if `ciphertext` is not valid base64. Throws `CryptographicException` if decryption fails due to incorrect key or corrupted data. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public bool ValidateKey()`
Validates the current encryption key by attempting to encrypt and then decrypt a known test vector. Returns `true` if the round-trip succeeds; otherwise, returns `false`. This method does not throw exceptions under normal operation but may throw `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public EncryptionKey GenerateNewKey()`
Generates a new cryptographically secure encryption key and returns it as an `EncryptionKey` instance. The returned key is suitable for immediate use with encryption methods. This method does not throw exceptions under normal operation.

### `public async Task RotateKey()`
Asynchronously replaces the current encryption key with a newly generated one. Any in-progress encryption or decryption operations using the old key will complete normally, but subsequent operations will use the new key. Throws `InvalidOperationException` if called before `GlobalSetup`.

### `public string EncryptLargeText(string plaintext)`
Synchronously encrypts large plaintext using a chunked encryption strategy to avoid memory pressure. Returns the base64-encoded concatenated ciphertext chunks. Throws `ArgumentNullException` if `plaintext` is `null`. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public string DecryptLargeText(string ciphertext)`
Synchronously decrypts large ciphertext that was encrypted using `EncryptLargeText`. Returns the original plaintext. Throws `ArgumentNullException` if `ciphertext` is `null`. Throws `FormatException` if `ciphertext` is not valid base64. Throws `CryptographicException` if decryption fails due to incorrect key or corrupted data. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public async Task<string> EncryptLargeTextAsync(string plaintext)`
Asynchronously encrypts large plaintext using a chunked strategy to avoid memory pressure. Returns a `Task<string>` that resolves to the base64-encoded concatenated ciphertext chunks. Throws `ArgumentNullException` if `plaintext` is `null`. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

### `public async Task<string> DecryptLargeTextAsync(string ciphertext)`
Asynchronously decrypts large ciphertext that was encrypted using `EncryptLargeTextAsync`. Returns a `Task<string>` that resolves to the original plaintext. Throws `ArgumentNullException` if `ciphertext` is `null`. Throws `FormatException` if `ciphertext` is not valid base64. Throws `CryptographicException` if decryption fails due to incorrect key or corrupted data. Throws `InvalidOperationException` if no key has been set via `GlobalSetup`.

## Usage
