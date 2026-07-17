# ConfigurationVersion

Represents a version of a configuration in the dotnet-config-server, tracking its lifecycle status, contents, and metadata.

## API

### `Id`
Gets the unique identifier for this configuration version.

### `ConfigurationId`
Gets the identifier of the configuration this version belongs to.

### `VersionNumber`
Gets the semantic version string for this configuration version.

### `Status`
Gets the lifecycle status of this configuration version (e.g., Draft, Published, Archived).

### `ReleaseNotes`
Gets optional release notes associated with this configuration version.

### `CreatedAt`
Gets the timestamp when this configuration version was created.

### `PublishedAt`
Gets the optional timestamp when this configuration version was published.

### `ArchivedAt`
Gets the optional timestamp when this configuration version was archived.

### `CreatedBy`
Gets the identifier of the user who created this configuration version.

### `PublishedBy`
Gets the optional identifier of the user who published this configuration version.

### `ArchivedBy`
Gets the optional identifier of the user who archived this configuration version.

### `KeyCount`
Gets the number of configuration keys in this version.

### `HasEncryptedKeys`
Gets a value indicating whether this version contains encrypted keys.

### `PreviousVersionId`
Gets the optional identifier of the previous configuration version in the sequence.

### `ChangesSummary`
Gets an optional summary of changes introduced in this version.

### `Keys`
Gets the list of configuration keys included in this version.

### `Publish()`
Publishes this configuration version, changing its status to Published and setting the PublishedAt timestamp.

### `Archive()`
Archives this configuration version, changing its status to Archived and setting the ArchivedAt timestamp.

### `Deprecate()`
Deprecates this configuration version, marking it as no longer recommended for use.

### `IncrementVersion()`
Static method that generates the next version number based on the current version.

## Usage
