# DiffBenchmarks

Provides a set of asynchronous benchmark scenarios for measuring the performance of configuration diff operations in the dotnet-config-server project. The class encapsulates common setup, cleanup, and diff‑related workloads used to compare configuration snapshots, generate rollback previews, and analyze large‑scale configuration changes.

## API

### GlobalSetup
```csharp
public async Task GlobalSetup()
```
**Purpose** – Prepares shared resources required by all benchmark methods (e.g., initializes configuration stores, loads baseline data).  
**Parameters** – None.  
**Return value** – A `Task` that completes when the setup operation finishes.  
**Throws** –  
- `InvalidOperationException` if required services cannot be resolved.  
- `IOException` if loading baseline configuration files fails.  

### GlobalCleanup
```csharp
public async Task GlobalCleanup()
```
**Purpose** – Releases resources allocated during `GlobalSetup` (e.g., disposes of configuration providers, clears caches).  
**Parameters** – None.  
**Return value** – A `Task` that completes when cleanup is finished.  
**Throws** –  
- `ObjectDisposedException` if called after resources have already been disposed.  
- `InvalidOperationException` if cleanup logic encounters an inconsistent state.  

### CompareConfigurations
```csharp
public async Task CompareConfigurations()
```
**Purpose** – Executes a diff comparison between two configuration snapshots without returning detailed change information.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the comparison finishes.  
**Throws** –  
- `ArgumentNullException` if either snapshot is not initialized (internal state).  
- `InvalidOperationException` if the snapshots are of incompatible formats.  

### GetDiff
```csharp
public async Task GetDiff()
```
**Purpose** – Retrieves the set of differences between two configurations as a lightweight diff result.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the diff has been computed.  
**Throws** –  
- `InvalidOperationException` if the underlying diff algorithm fails to converge.  

### GetDiffWithDetails
```csharp
public async Task GetDiffWithDetails()
```
**Purpose** – Retrieves a detailed diff, including added, removed, and modified entries with full value snapshots.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the detailed diff is available.  
**Throws** –  
- `InvalidOperationException` if detail enrichment cannot be performed due to missing metadata.  

### GetRollbackPreview
```csharp
public async Task GetRollbackPreview()
```
**Purpose** – Generates a preview of the configuration state that would result from rolling back the detected changes.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the rollback preview is produced.  
**Throws** –  
- `InvalidOperationException` if the rollback logic cannot reconstruct a prior state.  

### CompareLargeConfigurations
```csharp
public async Task CompareLargeConfigurations()
```
**Purpose** – Performs a diff operation on configurations that exceed typical size thresholds to stress‑test performance.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the large‑scale comparison finishes.  
**Throws** –  
- `OutOfMemoryException` if the operation exceeds available memory.  
- `TimeoutException` if the comparison does not finish within the internal benchmark timeout.  

### GetDiffTimeline
```csharp
public async Task GetDiffTimeline()
```
**Purpose** – Computes a chronological sequence of diffs across multiple configuration versions.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the timeline is built.  
**Throws** –  
- `InvalidOperationException` if version ordering cannot be determined.  

### GetEnrichedDiff
```csharp
public async Task GetEnrichedDiff()
```
**Purpose** – Returns a diff augmented with contextual information such as source tags, timestamps, and impact assessments.  
**Parameters** – None.  
**Return value** – A `Task` that completes when the enriched diff is ready.  
**Throws** –  
- `InvalidOperationException` if enrichment data is unavailable.  

## Usage

```csharp
using System.Threading.Tasks;
using DotNetConfigServer.Benchmarks; // namespace containing DiffBenchmarks

public class BenchmarkRunner
{
    public async Task RunAll()
    {
        var bench = new DiffBenchmarks();

        // Prepare shared state once per benchmark suite
        await bench.GlobalSetup();

        try
        {
            // Example: measure a basic diff operation
            await bench.GetDiff();

            // Example: measure a large‑scale comparison
            await bench.CompareLargeConfigurations();
        }
        finally
        {
            // Ensure resources are released even if an error occurs
            await bench.GlobalCleanup();
        }
    }
}
```

```csharp
using System.Threading.Tasks;
using DotNetConfigServer.Benchmarks;

public class SelectiveBenchmark
{
    public async Task RunSelective()
    {
        var bench = new DiffBenchmarks();

        await bench.GlobalSetup();

        try
        {
            // Obtain a detailed diff for analysis
            await bench.GetDiffWithDetails();

            // Preview what a rollback would look like
            await bench.GetRollbackPreview();
        }
        finally
        {
            await bench.GlobalCleanup();
        }
    }
}
```

## Notes

- The class holds mutable state (e.g., loaded configuration snapshots) that is initialized by `GlobalSetup` and disposed by `GlobalCleanup`. Calling any benchmark method before `GlobalSetup` or after `GlobalCleanup` may result in `InvalidOperationException` or undefined behavior.  
- Instance members are **not** thread‑safe. Concurrent invocation of any two methods on the same `DiffBenchmarks` instance can lead to race conditions because they share internal buffers or caches. For parallel benchmarking, create separate instances per thread or synchronize access externally.  
- Methods that return `Task` do not produce a value; any result data is stored internally or emitted via side effects (e.g., logging, metrics). If a caller needs the actual diff payload, they should derive from this type or modify the implementation to expose the result.  
- `CompareLargeConfigurations` is designed to stress memory and CPU usage; it may throw `OutOfMemoryException` on constrained environments. Adjust test data size or increase available memory accordingly.  
- All asynchronous methods are intended to be awaited; fire‑and‑forget usage (`method();`) is discouraged because exceptions would be unobserved and resources might not be cleaned up properly.  
- The class does not inherit from any benchmark framework base type; it is a plain helper meant to be invoked manually or wrapped by a benchmark harness such as BenchmarkDotNet.  
- No static state is used, so multiple instances do not interfere with each other aside from potential contention on shared external resources (e.g., file system, database) accessed during setup or execution.
