# VersioningBenchmarks

## Overview

`VersioningBenchmarks` is a performance benchmarking suite for the versioning subsystem of the dotnet-config-server project. It measures the throughput and latency of core version management operations—creation, retrieval, publishing, archival, deprecation, rollback, history queries, and cleanup—under realistic load conditions. The class uses BenchmarkDotNet infrastructure and provides setup and teardown hooks to prepare an isolated test environment before each benchmark run.

## API

### GlobalSetup
```csharp
public async Task GlobalSetup()
```
Initializes the benchmarking environment once before all benchmark iterations. This method prepares the underlying data store, seeds any prerequisite configuration data, and ensures the versioning service is in a clean, ready state. It does not return a value. Throws if the environment cannot be provisioned or if a required dependency is unavailable.

### GlobalCleanup
```csharp
public async Task GlobalCleanup()
```
Tears down the global benchmarking environment after all iterations have completed. It removes seeded data, disposes of resources acquired during `GlobalSetup`, and restores the system to its original state. This method does not return a value. Throws if resource disposal fails or if the cleanup operation encounters an unrecoverable state.

### CreateVersion
```csharp
public async Task CreateVersion()
```
Benchmarks the creation of a new configuration version. The operation constructs a version entry with a representative payload and persists it through the versioning service. Returns when the version has been committed. Throws if the underlying store rejects the write, if a version with conflicting identifiers already exists, or if the operation exceeds the configured timeout.

### GetVersion
```csharp
public async Task GetVersion()
```
Benchmarks retrieval of a single configuration version by its identifier. The method fetches a previously created version and measures the latency of the read path. Returns when the version is fully deserialized. Throws if the requested version does not exist or if the data store is unreachable.

### GetVersions
```csharp
public async Task GetVersions()
```
Benchmarks retrieval of a collection of configuration versions, typically filtered by a parent configuration scope. The operation exercises pagination and sorting logic. Returns when the result set is materialized. Throws if the underlying query fails or if the filter parameters are malformed.

### GetActiveVersion
```csharp
public async Task GetActiveVersion()
```
Benchmarks resolution of the currently active version for a given configuration scope. This operation evaluates activation rules and returns the version marked as active. Returns when the active version is resolved. Throws if no active version is defined, if multiple active versions conflict, or if the resolution logic encounters an error.

### PublishVersion
```csharp
public async Task PublishVersion()
```
Benchmarks the transition of a version from a draft or staged state to a published state. The operation validates that the version meets publish criteria, updates its status, and broadcasts the change. Returns when the publish action is committed. Throws if the version is not in a publishable state, if validation fails, or if a concurrency conflict is detected.

### ArchiveVersion
```csharp
public async Task ArchiveVersion()
```
Benchmarks archiving a previously published version. The operation marks the version as archived, making it read-only and removing it from active resolution. Returns when the archival is committed. Throws if the version is already archived, if it is still referenced by active configurations, or if the state transition is invalid.

### DeprecateVersion
```csharp
public async Task DeprecateVersion()
```
Benchmarks deprecating a version, which flags it as no longer recommended for use while keeping it available for existing consumers. The operation updates the version’s deprecation metadata. Returns when the deprecation is committed. Throws if the version is already deprecated, if it is in a state that forbids deprecation, or if the metadata update fails.

### Rollback
```csharp
public async Task Rollback()
```
Benchmarks rolling back a configuration scope to a previous version. The operation deactivates the current version and activates the specified target version, recording the rollback event. Returns when the rollback is committed. Throws if the target version does not exist, if the rollback violates versioning constraints, or if the activation swap fails.

### GetVersionHistory
```csharp
public async Task GetVersionHistory()
```
Benchmarks retrieval of the full version history for a configuration scope, including state transitions and timestamps. The method exercises the history query path and materializes the complete timeline. Returns when the history list is fully assembled. Throws if the history store is unavailable or if the query parameters are invalid.

### CleanupOldVersions
```csharp
public async Task CleanupOldVersions()
```
Benchmarks the bulk cleanup of versions that exceed a retention policy threshold. The operation identifies versions eligible for removal, deletes them, and reclaims associated storage. Returns when the cleanup batch is committed. Throws if the retention policy evaluation fails, if a version is still referenced and cannot be removed, or if the bulk delete operation fails.

### CreateVersionWithManyKeys
```csharp
public async Task CreateVersionWithManyKeys()
```
Benchmarks the creation of a configuration version containing a large number of key-value pairs. This exercises the write path under high-payload conditions and measures serialization, validation, and storage overhead. Returns when the version is committed. Throws if the payload exceeds size limits, if key uniqueness constraints are violated, or if the store cannot handle the payload volume.

## Usage

### Example 1: Running a Single Benchmark Method
```csharp
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<VersioningBenchmarks>();
```
This executes all benchmark methods defined in `VersioningBenchmarks`, including `GlobalSetup` and `GlobalCleanup` automatically. The output includes mean execution time, allocated memory, and standard deviation for each versioning operation.

### Example 2: Programmatic Invocation with Custom Configuration
```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.Default
        .WithWarmupCount(3)
        .WithIterationCount(10)
        .WithInvocationCount(1));

var benchmarks = new VersioningBenchmarks();
await benchmarks.GlobalSetup();

try
{
    await benchmarks.CreateVersion();
    await benchmarks.PublishVersion();
    await benchmarks.GetActiveVersion();
    await benchmarks.Rollback();
}
finally
{
    await benchmarks.GlobalCleanup();
}
```

## Notes

- **Thread Safety:** The class is designed for single-threaded benchmark execution. `GlobalSetup` and `GlobalCleanup` are invoked once per benchmark run, while individual benchmark methods are called sequentially by the BenchmarkDotNet harness. Concurrent invocation of benchmark methods from multiple threads is not supported and may produce corrupted state or race conditions in the underlying version store.
- **State Isolation:** Each benchmark method assumes a clean or known state. If a method like `PublishVersion` is called without a preceding `CreateVersion`, it will throw because no publishable version exists. The harness typically sequences operations correctly, but manual callers must ensure proper ordering.
- **Large Payloads:** `CreateVersionWithManyKeys` may allocate significant memory. Ensure the test environment has sufficient heap space to avoid out-of-memory exceptions during benchmarking.
- **Retention Policies:** `CleanupOldVersions` relies on a configured retention policy. If the policy is not set or is set to an indefinite retention period, the method may perform a no-op cleanup and return successfully without removing any versions.
- **Timeouts:** All methods are subject to the default operation timeout configured in the versioning service. If the underlying store is slow or unresponsive, methods will throw a timeout exception rather than hang indefinitely.
- **Idempotency:** Lifecycle methods such as `ArchiveVersion` and `DeprecateVersion` are not idempotent. Calling them on an already archived or deprecated version will throw. Ensure the version is in the correct state before invoking these benchmarks.
