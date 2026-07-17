# ValidationRulesController

The `ValidationRulesController` provides endpoints for managing validation rules that can be applied to configuration data in the dotnet-config-server. It allows clients to define, retrieve, update, and delete validation rules, as well as validate configuration data against those rules.

## API

### `ValidationRulesController`
The controller class that exposes endpoints for validation rule management. Inherits from `ControllerBase`.

### `public async Task<IActionResult> GetRules()`
Retrieves all validation rules currently stored in the system.

- **Parameters**: None
- **Return value**: `IActionResult` containing a collection of validation rules.
- **Exceptions**: May throw if the underlying storage fails to retrieve rules.

### `public async Task<IActionResult> CreateRule([FromBody] ValidationRule rule)`
Creates a new validation rule in the system.

- **Parameters**:
  - `rule`: The validation rule to create, provided in the request body.
- **Return value**: `IActionResult` with the created rule and HTTP 201 status on success.
- **Exceptions**: May throw if the rule is invalid, already exists, or storage fails.

### `public async Task<IActionResult> GetRule(Guid id)`
Retrieves a specific validation rule by its unique identifier.

- **Parameters**:
  - `id`: The unique identifier of the rule to retrieve.
- **Return value**: `IActionResult` containing the requested rule or HTTP 404 if not found.
- **Exceptions**: May throw if the identifier is malformed or storage fails.

### `public async Task<IActionResult> UpdateRule(Guid id, [FromBody] ValidationRule rule)`
Updates an existing validation rule identified by `id`.

- **Parameters**:
  - `id`: The unique identifier of the rule to update.
  - `rule`: The updated rule data, provided in the request body.
- **Return value**: `IActionResult` with HTTP 204 on success or HTTP 404 if the rule does not exist.
- **Exceptions**: May throw if the identifier is malformed, the rule is invalid, or storage fails.

### `public async Task<IActionResult> DeleteRule(Guid id)`
Deletes a validation rule by its unique identifier.

- **Parameters**:
  - `id`: The unique identifier of the rule to delete.
- **Return value**: `IActionResult` with HTTP 204 on success or HTTP 404 if the rule does not exist.
- **Exceptions**: May throw if the identifier is malformed or storage fails.

### `public async Task<IActionResult> ValidateConfiguration([FromBody] ConfigurationData config, [FromQuery] Guid? versionId)`
Validates a configuration against the set of active validation rules.

- **Parameters**:
  - `config`: The configuration data to validate, provided in the request body.
  - `versionId`: Optional identifier for a specific configuration version to validate against.
- **Return value**: `IActionResult` containing validation results or HTTP 400 if validation fails.
- **Exceptions**: May throw if the configuration data is malformed or storage fails.

### `public Guid? VersionId`
Gets the optional version identifier associated with the current request context.

- **Return value**: `Guid?` representing the version identifier, or `null` if not set.
- **Usage**: Used internally to scope operations like validation to a specific configuration version.
