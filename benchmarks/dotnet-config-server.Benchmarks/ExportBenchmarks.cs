using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DotnetConfigServer.Common;
using DotnetConfigServer.Formatters;
using DotnetConfigServer.Models;
using Environment = DotnetConfigServer.Common.Environment;

namespace DotnetConfigServer.Benchmarks;

/// <summary>
/// Benchmark suite comparing the legacy in-memory export path
/// (<see cref="ConfigurationExporter.ExportAsJson"/>, which materializes the entire
/// serialized payload as a single string) against the streaming export path
/// (<see cref="ConfigurationExporter.WriteAsJsonAsync"/>, which writes directly onto a
/// destination stream via <see cref="System.Text.Json.Utf8JsonWriter"/> and never holds
/// more than one record and one write buffer in memory at a time).
/// Run with a large record count to observe the Large Object Heap / Gen2 allocation
/// difference between the two approaches.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExportBenchmarks
{
    /// <summary>
    /// Number of configuration records to export in each benchmark iteration.
    /// Set high enough (50,000) to make Large Object Heap allocations from the
    /// buffered export path visible in the memory diagnoser output.
    /// </summary>
    [Params(50_000)]
    public int RecordCount { get; set; }

    private List<Configuration> _configurations = [];

    /// <summary>
    /// Builds the in-memory fixture of configuration records used by every benchmark
    /// iteration, sized according to <see cref="RecordCount"/>.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _configurations = Enumerable.Range(0, RecordCount)
            .Select(i => new Configuration
            {
                Id = Guid.NewGuid(),
                ApplicationId = Guid.NewGuid(),
                Name = $"Configuration_{i}",
                Description = $"Benchmark configuration record number {i}",
                Environment = Environment.Production,
                IsActive = true,
                IsEncrypted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "benchmark"
            })
            .ToList();
    }

    /// <summary>
    /// Baseline: serializes the whole configuration set into a single in-memory string
    /// before it can be written anywhere, spiking Large Object Heap allocations for
    /// large tenants.
    /// </summary>
    /// <returns>The serialized JSON string, to keep the call from being optimized away.</returns>
    [Benchmark(Baseline = true)]
    public string Buffered_ExportAsJson() => ConfigurationExporter.ExportAsJson(_configurations, pretty: false);

    /// <summary>
    /// Streams the configuration set directly onto a destination stream using
    /// <see cref="Utf8JsonWriter"/>, never materializing the full serialized payload
    /// in managed memory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous write operation.</returns>
    [Benchmark]
    public async Task Streaming_WriteAsJsonAsync()
    {
        await using var destination = Stream.Null;
        var source = ConfigurationExporter.ToAsyncEnumerable(_configurations);
        await ConfigurationExporter.WriteAsJsonAsync(destination, source, pretty: false);
    }
}
