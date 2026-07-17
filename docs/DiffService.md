# DiffService

The `DiffService` provides functionality to compare and retrieve configuration versions, enabling change tracking, historical analysis, and version comparison between different states of configuration data.

## API

### `public DiffService`

Initializes a new instance of the `DiffService` with required dependencies for configuration comparison and retrieval.

### `public async Task<ConfigurationDiff> GenerateDiffAsync`

Generates a new configuration diff by comparing the current state of the configuration with the previous state.

- **Parameters**: None
- **Return value**: A `Task<ConfigurationDiff>` representing the generated diff between the current and previous configuration states.
- **Exceptions**: Throws if the configuration cannot be retrieved or compared.

### `public async Task<ConfigurationDiff?> GetDiffAsync`

Retrieves a specific configuration diff by its unique identifier.

- **Parameters**:
  - `diffId` (`string`): The unique identifier of the diff to retrieve.
- **Return value**: A `Task<ConfigurationDiff?>` representing the diff if found; otherwise, `null`.
- **Exceptions**: Throws if the identifier is invalid or the diff cannot be retrieved.

### `public async Task<List<ConfigurationDiff>> GetDiffsAsync`

Retrieves a list of all configuration diffs, optionally filtered by a time range.

- **Parameters**:
  - `from` (`DateTimeOffset?`): The start of the time range (inclusive). If `null`, no lower bound is applied.
  - `to` (`DateTimeOffset?`): The end of the time range (inclusive). If `null`, no upper bound is applied.
- **Return value**: A `Task<List<ConfigurationDiff>>` containing all matching diffs, ordered chronologically.
- **Exceptions**: Throws if the time range is invalid or diffs cannot be retrieved.

### `public async Task<ConfigurationDiff?> GetLatestDiffAsync`

Retrieves the most recent configuration diff.

- **Parameters**: None
- **Return value**: A `Task<ConfigurationDiff?>` representing the latest diff if available; otherwise, `null`.
- **Exceptions**: Throws if the latest diff cannot be retrieved.

### `public async Task<List<DiffEntry>> GetKeyHistoryAsync`

Retrieves the historical changes for a specific configuration key.

- **Parameters**:
  - `key` (`string`): The configuration key to query.
- **Return value**: A `Task<List<DiffEntry>>` containing the list of changes for the key, ordered chronologically.
- **Exceptions**: Throws if the key is invalid or history cannot be retrieved.

### `public async Task<ConfigurationDiffSummary> CompareVersionsAsync`

Compares two specific versions of the configuration and returns a summary of differences.

- **Parameters**:
  - `version1` (`string`): The identifier of the first version to compare.
  - `version2` (`string`): The identifier of the second version to compare.
- **Return value**: A `Task<ConfigurationDiffSummary>` containing a summary of the differences between the two versions.
- **Exceptions**: Throws if either version identifier is invalid or the comparison cannot be performed.

## Usage

### Example 1: Generate and Retrieve a Diff
