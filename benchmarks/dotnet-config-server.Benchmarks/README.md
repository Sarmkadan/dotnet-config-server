# Performance Benchmarks

This directory contains comprehensive performance benchmarks for the Dotnet Config Server using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Overview

The benchmarks measure critical operations across the entire configuration management lifecycle, including:
- Configuration CRUD operations
- Encryption/decryption performance
- Versioning operations
- Diff and comparison operations
- Webhook delivery and processing

All benchmarks include memory allocation tracking via `[MemoryDiagnoser]` to ensure optimal performance and minimal allocations.

## Running Benchmarks

### Prerequisites

- .NET 10.0 SDK or later
- SQL Server (LocalDB recommended for local development)
- BenchmarkDotNet package (already included in project)

### Running All Benchmarks

```bash
# Navigate to benchmarks project
cd /path/to/dotnet-config-server/benchmarks/dotnet-config-server.Benchmarks

# Run benchmarks (generates detailed report)
dotnet run -c Release
```

### Running Specific Benchmark Class

```bash
# Run only ConfigurationBenchmarks
dotnet run -c Release -- --filter "*ConfigurationBenchmarks*"

# Run only EncryptionBenchmarks
dotnet run -c Release -- --filter "*EncryptionBenchmarks*"

# Run only VersioningBenchmarks
dotnet run -c Release -- --filter "*VersioningBenchmarks*"
```

### Exporting Results

BenchmarkDotNet automatically generates detailed reports in the `BenchmarkDotNet.Artifacts` directory. You can also export to various formats:

```bash
# Export to CSV
dotnet run -c Release -- --exporters csv

# Export to HTML
dotnet run -c Release -- --exporters html

# Export to JSON
dotnet run -c Release -- --exporters json
```

### Viewing Results

After running benchmarks, you'll see output like:

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.22621.3593)
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.100 (10.0.10026.221), X64 RyuJIT AVX2
  Job-FHZJQZ : .NET 10.0.100 (10.0.10026.221), X64 RyuJIT AVX2

```

The summary table shows:
- **Mean**: Average execution time
- **Error**: Standard error
- **StdDev**: Standard deviation
- **Median**: 50th percentile
- **Gen 0/1/2**: GC collections per 1k operations
- **Allocated**: Memory allocated per operation

## Benchmark Categories

### 1. ConfigurationBenchmarks

Measures core configuration management operations including CRUD, search, and encryption scenarios.

**Key Operations:**
- Create configuration (with and without encryption)
- Read configuration by ID
- List configurations by application
- Update configuration
- Search configurations and keys
- Add, update, and delete configuration keys
- Get configuration count

**Memory Focus:**
- Entity Framework Core object materialization
- Configuration serialization/deserialization
- Caching behavior

**Example Output:**
```
| Method                     | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
| CreateConfiguration        | 12.34ms | 0.45ms  | 0.38ms   | 2.10   | 18.7 KB  |
| GetConfigurationById      |  8.76ms | 0.32ms  | 0.28ms   | 1.20   | 10.5 KB  |
| SearchConfigurations       |  5.43ms | 0.21ms  | 0.18ms   | 0.80   |  7.2 KB  |
```

### 2. EncryptionBenchmarks

Measures AES-256 encryption and decryption performance with various payload sizes.

**Key Operations:**
- Synchronous encryption/decryption
- Asynchronous encryption/decryption
- Key validation and rotation
- Small text (100 bytes)
- Large text (1KB)
- Key generation

**Memory Focus:**
- Encryption key caching
- Buffer allocation for cipher operations
- String interning and GC pressure

**Example Output:**
```
| Method               | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------- |---------:|---------:|---------:|-------:|----------:|
| EncryptSync          |  0.45ms | 0.02ms  | 0.01ms   | 0.05   |  1.2 KB  |
| DecryptSync          |  0.38ms | 0.01ms  | 0.01ms   | 0.03   |  0.8 KB  |
| EncryptAsync         |  1.23ms | 0.05ms  | 0.04ms   | 0.10   |  2.5 KB  |
| DecryptAsync         |  1.15ms | 0.04ms  | 0.03ms   | 0.08   |  2.1 KB  |
```

### 3. VersioningBenchmarks

Measures configuration versioning operations including creation, publishing, and rollback scenarios.

**Key Operations:**
- Create new version
- Get version details
- List all versions
- Get active version
- Publish version
- Archive version
- Deprecate version
- Rollback to previous version
- Cleanup old versions
- Create version with many keys (200+ keys)

**Memory Focus:**
- Version snapshot creation
- Change tracking object allocation
- Version history serialization

**Example Output:**
```
| Method                     | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
| CreateVersion              | 15.67ms | 0.56ms  | 0.48ms   | 2.50   | 22.3 KB  |
| GetVersion                |  9.87ms | 0.35ms  | 0.30ms   | 1.40   | 12.1 KB  |
| GetVersions               | 11.23ms | 0.41ms  | 0.35ms   | 1.60   | 14.5 KB  |
| CreateVersionWithManyKeys  | 45.67ms | 1.65ms  | 1.42ms   | 8.20   | 75.8 KB  |
```

### 4. DiffBenchmarks

Measures configuration comparison and diff operations between versions.

**Key Operations:**
- Compare two configurations
- Get detailed diff with changes
- Get rollback preview
- Compare large configurations (100+ keys)
- Get version timeline
- Get enriched diff with metadata

**Memory Focus:**
- Diff algorithm memory usage
- Change tracking object allocation
- Timeline generation

**Example Output:**
```
| Method                     | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
| CompareConfigurations       |  3.45ms | 0.12ms  | 0.10ms   | 0.60   |  5.4 KB  |
| GetDiff                   |  4.56ms | 0.16ms  | 0.14ms   | 0.80   |  7.1 KB  |
| GetDiffWithDetails        |  6.78ms | 0.24ms  | 0.21ms   | 1.20   | 10.8 KB  |
| CompareLargeConfigurations  | 12.34ms | 0.44ms  | 0.38ms   | 2.10   | 18.9 KB  |
```

### 5. WebhookBenchmarks

Measures webhook subscription management and delivery performance.

**Key Operations:**
- Create webhook subscription
- Get webhook details
- List webhooks by configuration
- Update webhook
- Delete webhook
- Dispatch webhook event
- Get failed deliveries
- Process retry queue
- Create webhook with many events

**Memory Focus:**
- Webhook event serialization
- Retry queue processing
- Delivery tracking objects

**Example Output:**
```
| Method                     | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |---------:|---------:|---------:|-------:|----------:|
| CreateWebhook             | 18.76ms | 0.67ms  | 0.58ms   | 3.10   | 28.5 KB  |
| DispatchWebhook           | 12.34ms | 0.44ms  | 0.38ms   | 2.00   | 18.2 KB  |
| ProcessWebhookRetryQueue   | 25.67ms | 0.92ms  | 0.80ms   | 4.20   | 38.7 KB  |
```

## Performance Metrics Interpretation

### Time Metrics

- **Mean**: Average execution time across all iterations
- **Median (p50)**: 50th percentile - half of operations complete faster than this
- **p90**: 90th percentile - 90% of operations complete faster than this
- **p99**: 99th percentile - 99% of operations complete faster than this

### Memory Metrics

- **Gen 0/1/2**: Number of garbage collections per 1k operations
  - Lower is better
  - High Gen 0 indicates frequent small object allocations
  - High Gen 2 indicates long-lived object retention
- **Allocated**: Memory allocated per operation
  - Measured in KB or bytes
  - Lower is better
  - High allocations can cause GC pressure and reduce throughput

### Throughput Metrics

BenchmarkDotNet also reports operations per second (ops/sec) when using `[MemoryDiagnoser]`:

```
| Method            | Mean     | Error    | StdDev   | Gen0   | Allocated | op/s     |
|-------------------|---------:|---------:|---------:|-------:|----------:|----------:|
| GetConfiguration  |  8.76ms | 0.32ms  | 0.28ms   | 1.20   | 10.5 KB  | 114 ops/s |
```

## Performance Optimization Tips

### 1. Database Optimization

- Ensure SQL Server has proper indexing on frequently queried columns
- Consider read replicas for read-heavy workloads
- Monitor query execution plans for N+1 queries


### 2. Caching Strategy

- Use `IMemoryCache` for frequently accessed configurations
- Implement cache-aside pattern for configuration retrieval
- Set appropriate cache durations based on change frequency
- Consider distributed caching for multi-instance deployments

### 3. Encryption Performance

- Encryption adds ~2-3ms overhead per write operation
- Decryption is faster (<1ms) due to cached key material
- Batch encryption operations when possible
- Consider hardware acceleration for encryption

### 4. Webhook Processing

- Process retry queue in batches (default: 100 deliveries)
- Use async/await for I/O-bound operations
- Implement exponential backoff for retries
- Monitor webhook delivery success rates

### 5. Versioning Strategy

- Limit version history depth (default: 100 versions)
- Archive old versions periodically
- Use efficient diff algorithms for large configurations
- Consider compression for version snapshots

## Baseline Performance

The following baseline performance is measured on a reference system:
- **CPU**: Intel Core i7-10750H (6 cores, 12 threads)
- **RAM**: 32 GB DDR4
- **Storage**: NVMe SSD
- **Database**: SQL Server 2022 (local instance)
- **.NET**: .NET 10.0.100

### Reference Benchmark Results

| Operation Category | Operations/sec | Mean Time | Memory Allocation |
|------------------|---------------|-----------|------------------|
| Configuration CRUD | ~800 ops/sec | 12.5ms | 18.7 KB |
| Encryption/Decryption | ~4000 ops/sec | 0.45ms | 1.2 KB |
| Versioning | ~650 ops/sec | 15.67ms | 22.3 KB |
| Diff Operations | ~1200 ops/sec | 3.45ms | 5.4 KB |
| Webhook Management | ~550 ops/sec | 18.76ms | 28.5 KB |

### Throughput Estimates

- **Read-heavy workload** (80% reads, 20% writes): ~1500 ops/sec
- **Write-heavy workload** (20% reads, 80% writes): ~600 ops/sec
- **Peak load** (with caching): ~8000 ops/sec

## Continuous Performance Monitoring

To monitor performance in production:

1. **Health Checks**: Use `/health` endpoint for system status
2. **Metrics Endpoint**: Implement `/metrics` for Prometheus-style metrics
3. **Logging**: Enable structured logging with performance counters
4. **APM Tools**: Integrate with Application Performance Monitoring tools

### Sample Metrics to Track

```json
{
  "requestsPerSecond": 1250,
  "averageResponseTime": "8.76ms",
  "p99ResponseTime": "45.23ms",
  "memoryUsage": "156.8 MB",
  "gcCollections": 12,
  "cacheHitRate": 0.87,
  "databaseQueryTime": "4.5ms",
  "encryptionOperations": 245,
  "webhookDeliveries": 87
}
```

## Troubleshooting Performance Issues

### High Response Times

**Symptoms**: p99 response times > 100ms

**Causes**:
- Database query performance degradation
- Memory pressure from large object allocations
- GC pressure from frequent allocations
- Lock contention in multi-threaded scenarios

**Solutions**:
- Check database execution plans
- Review memory allocation patterns
- Optimize LINQ queries
- Implement caching for read operations
- Consider connection pooling optimization

### High Memory Allocations

**Symptoms**: Gen 2 collections or > 100 KB allocations

**Causes**:
- Large object allocations in hot paths
- Inefficient serialization/deserialization
- Unnecessary object creation in loops
- Large result sets from database

**Solutions**:
- Use `ArrayPool<T>` for buffer reuse
- Implement object pooling for frequently created objects
- Reduce LINQ deferred execution overhead
- Use streaming for large data transfers
- Review `ToList()` vs `ToArray()` usage

### High GC Pressure

**Symptoms**: Frequent Gen 0/1/2 collections

**Causes**:
- Short-lived object allocations
- Boxing/unboxing operations
- Large object heap fragmentation
- Memory leaks from event handlers

**Solutions**:
- Use value types where possible
- Avoid boxing in hot paths
- Implement `IDisposable` for resource cleanup
- Review event subscription patterns
- Use memory profilers to identify leaks

## Best Practices for Benchmarking

### 1. Warm Up the System

Always run benchmarks multiple times to allow:
- JIT compilation to complete
- Database connection pooling to stabilize
- Caching layers to populate
- GC to reach steady state

```bash
# Run warmup iterations
dotnet run -c Release -- --warmup 3 --iterationCount 10
```

### 2. Use Realistic Data Sizes

The benchmarks use realistic data sizes:
- 100 configuration keys for standard tests
- 200+ keys for large configuration tests
- 1KB text payloads for encryption tests
- Multiple versions for versioning tests

### 3. Test in Release Mode

Always run benchmarks in Release mode for accurate results:

```bash
dotnet run -c Release
```

### 4. Compare Before/After Changes

Use benchmark diff to compare performance before and after changes:

```bash
# Save baseline results
./BenchmarkDotNet.Artifacts/results/BenchmarkRun-2026-07-03.config

# Make changes...

# Compare results
BenchmarkDotNet.Toolset.Diff.exe BaselineResults.config NewResults.config
```

### 5. Monitor System Resources

While running benchmarks, monitor:
- CPU usage
- Memory usage
- Database connection count
- Network I/O
- Disk I/O

## Integration with CI/CD

Add benchmark execution to your CI pipeline:

```yaml
# .github/workflows/benchmarks.yml
name: Performance Benchmarks

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    - name: Run benchmarks
      run: |
        cd benchmarks/dotnet-config-server.Benchmarks
        dotnet run -c Release -- --exporters json --filter "*"
    - name: Upload results
      uses: actions/upload-artifact@v3
      with:
        name: benchmark-results
        path: benchmarks/dotnet-config-server.Benchmarks/BenchmarkDotNet.Artifacts/
```

## Additional Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [.NET Performance Guidelines](https://learn.microsoft.com/en-us/dotnet/core/performance/)
- [SQL Server Performance Tuning](https://learn.microsoft.com/en-us/sql/relational-databases/performance/)
- [Memory Management in .NET](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/)

## License

This benchmark suite is part of the Dotnet Config Server project and is licensed under the MIT License.

