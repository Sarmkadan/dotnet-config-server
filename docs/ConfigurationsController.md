# ConfigurationsController

The `ConfigurationsController` provides HTTP endpoints for managing configuration entries in the dotnet-config-server. It supports CRUD operations for configuration values, key-based retrieval, and search functionality across configurations and keys.

## API

### `ConfigurationsController`
The controller class exposing endpoints for configuration management.

### `public async Task<IActionResult> Create([FromBody] ConfigurationEntry entry)`
Creates a new configuration entry.

**Parameters**
- `entry`: The configuration entry to create. Must not be null.

**Return value**
- `201 Created` on success with the created entry.
- `400 BadRequest` if the entry is invalid or already exists.

**Exceptions**
- Throws `ArgumentNullException` if `entry` is null.

---

### `public async Task<IActionResult> GetById([FromRoute] string id)`
Retrieves a configuration entry by its unique identifier.

**Parameters**
- `id`: The unique identifier of the configuration entry.

**Return value**
- `200 OK` with the configuration entry if found.
- `404 NotFound` if no entry matches the given `id`.

---

### `public async Task<IActionResult> GetByApplication([FromRoute] string application)`
Retrieves all configuration entries associated with a specific application.

**Parameters**
- `application`: The name of the application to filter by.

**Return value**
- `200 OK` with a list of matching configuration entries.
- `404 NotFound` if no entries exist for the given `application`.

---

### `public async Task<IActionResult> Update([FromRoute] string id, [FromBody] ConfigurationEntry entry)`
Updates an existing configuration entry.

**Parameters**
- `id`: The unique identifier of the configuration entry to update.
- `entry`: The updated configuration entry. Must not be null.

**Return value**
- `200 OK` with the updated entry on success.
- `400 BadRequest` if the entry is invalid or does not exist.
- `404 NotFound` if no entry matches the given `id`.

**Exceptions**
- Throws `ArgumentNullException` if `entry` is null.

---

### `public async Task<IActionResult> Delete([FromRoute] string id)`
Deletes a configuration entry by its unique identifier.

**Parameters**
- `id`: The unique identifier of the configuration entry to delete.

**Return value**
- `204 NoContent` on successful deletion.
- `404 NotFound` if no entry matches the given `id`.

---

### `public async Task<IActionResult> GetKeys()`
Retrieves all unique keys present in the configuration store.

**Return value**
- `200 OK` with a list of all keys.

---

### `public async Task<IActionResult> AddKey([FromRoute] string key)`
Adds a new key to the configuration store.

**Parameters**
- `key`: The key to add. Must not be null or empty.

**Return value**
- `201 Created` on success.
- `400 BadRequest` if the key is invalid or already exists.
- `409 Conflict` if the key is reserved or invalid.

**Exceptions**
- Throws `ArgumentNullException` if `key` is null.

---

### `public async Task<IActionResult> Search([FromQuery] string query)`
Searches configuration entries by a query string.

**Parameters**
- `query`: The search query. Can be null or empty to return all entries.

**Return value**
- `200 OK` with a list of matching configuration entries.

---

### `public async Task<IActionResult> SearchKeys([FromQuery] string query)`
Searches configuration keys by a query string.

**Parameters**
- `query`: The search query. Can be null or empty to return all keys.

**Return value**
- `200 OK` with a list of matching keys.

## Usage

```csharp
// Example 1: Create and retrieve a configuration entry
var entry = new ConfigurationEntry
{
    Application = "MyApp",
    Key = "ApiEndpoint",
    Value = "https://api.example.com"
};

var createResponse = await client.PostAsJsonAsync("/api/configurations", entry);
createResponse.EnsureSuccessStatusCode();

var getResponse = await client.GetAsync($"/api/configurations/{entry.Id}");
var retrievedEntry = await getResponse.Content.ReadFromJsonAsync<ConfigurationEntry>();
```

```csharp
// Example 2: Search and update configurations
var searchResponse = await client.GetAsync("/api/configurations/search?query=Api");
var entries = await searchResponse.Content.ReadFromJsonAsync<List<ConfigurationEntry>>();

if (entries.Any())
{
    var firstEntry = entries.First();
    firstEntry.Value = "https://new-api.example.com";

    var updateResponse = await client.PutAsJsonAsync($"/api/configurations/{firstEntry.Id}", firstEntry);
    updateResponse.EnsureSuccessStatusCode();
}
```

## Notes

- All methods are thread-safe and may be called concurrently.
- The controller assumes that `ConfigurationEntry` and key uniqueness are enforced at the data layer.
- Search operations are case-sensitive unless the underlying storage normalizes case.
- Concurrent modifications to the same entry may result in race conditions; clients should implement retry logic for `409 Conflict` responses.
