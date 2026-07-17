# WebhookBenchmarks

The `WebhookBenchmarks` class provides a set of benchmark methods for measuring the performance of webhook operations in the `dotnet-config-server` project. It is designed to be used with a benchmarking framework such as BenchmarkDotNet. The class includes lifecycle methods (`GlobalSetup`, `GlobalCleanup`) that prepare and tear down the test environment, and individual benchmark methods that exercise specific webhook API endpoints (create, read, update, delete, dispatch, retry, etc.). All methods are asynchronous and return `Task`.

## API

### `public async Task GlobalSetup()`
Initializes the benchmark environment. This method is called once before any benchmark runs. It sets up any required resources (e.g., database connections, configuration data) needed by the subsequent benchmark methods.  
**Parameters:** None.  
**Returns:** A `Task` representing the asynchronous setup operation.  
**Throws:** May throw if the environment cannot be initialized (e.g., database connection failure, missing configuration).

### `public async Task GlobalCleanup()`
Cleans up resources after all benchmarks have completed. This method is called once after the last benchmark run. It disposes of any objects created during setup and removes test data.  
**Parameters:** None.  
**Returns:** A `Task` representing the asynchronous cleanup operation.  
**Throws:** May throw if cleanup fails (e.g., unable to delete test data).

### `public async Task CreateWebhook()`
Benchmarks the creation of a single webhook.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the webhook has been created.  
**Throws:** May throw if the webhook creation fails (e.g., invalid payload, database error).

### `public async Task GetWebhook()`
Benchmarks retrieving a single webhook by its identifier.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the webhook has been fetched.  
**Throws:** May throw if the webhook does not exist or the retrieval fails.

### `public async Task GetWebhooksByConfiguration()`
Benchmarks retrieving all webhooks associated with a specific configuration.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the list of webhooks has been fetched.  
**Throws:** May throw if the configuration does not exist or the query fails.

### `public async Task UpdateWebhook()`
Benchmarks updating an existing webhook’s properties (e.g., URL, events).  
**Parameters:** None.  
**Returns:** A `Task` that completes when the webhook has been updated.  
**Throws:** May throw if the webhook does not exist or the update fails.

### `public async Task DeleteWebhook()`
Benchmarks deleting a webhook.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the webhook has been deleted.  
**Throws:** May throw if the webhook does not exist or the deletion fails.

### `public async Task DispatchWebhook()`
Benchmarks dispatching a webhook event (sending the payload to the configured URL).  
**Parameters:** None.  
**Returns:** A `Task` that completes when the dispatch attempt has finished.  
**Throws:** May throw if the webhook is not found, the target URL is unreachable, or the HTTP request fails.

### `public async Task GetFailedDeliveries()`
Benchmarks retrieving the list of failed delivery attempts for a webhook.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the failed deliveries have been fetched.  
**Throws:** May throw if the webhook does not exist or the query fails.

### `public async Task ProcessWebhookRetryQueue()`
Benchmarks processing the retry queue for failed webhook deliveries. This typically involves re‑dispatching previously failed deliveries.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the retry processing has finished.  
**Throws:** May throw if the retry logic encounters errors (e.g., database corruption, network issues).

### `public async Task CreateWebhookWithManyEvents()`
Benchmarks creating a webhook that subscribes to a large number of events.  
**Parameters:** None.  
**Returns:** A `Task` that completes when the webhook with many events has been created.  
**Throws:** May throw if the event list is invalid or the creation fails.

## Usage

The following examples demonstrate how to use `WebhookBenchmarks` with BenchmarkDotNet and in a manual test scenario.

### Example 1: Running benchmarks with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Running;
using YourNamespace.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<WebhookBenchmarks>();
    }
}
```

### Example 2: Manual invocation for verification

```csharp
using System;
using System.Threading.Tasks;
using YourNamespace.Benchmarks;

public class ManualTest
{
    public static async Task Main()
    {
        var benchmarks = new WebhookBenchmarks();

        try
        {
            await benchmarks.GlobalSetup();
            await benchmarks.CreateWebhook();
            await benchmarks.GetWebhook();
            await benchmarks.UpdateWebhook();
            await benchmarks.DeleteWebhook();
            Console.WriteLine("All manual benchmarks completed successfully.");
        }
        finally
        {
            await benchmarks.GlobalCleanup();
        }
    }
}
```

## Notes

- **Edge cases:**  
  - `CreateWebhookWithManyEvents` may stress the system’s ability to handle large payloads; ensure the underlying storage and serialization can accommodate the event count.  
  - `DispatchWebhook` depends on network connectivity; timeouts or unreachable endpoints will cause exceptions.  
  - `ProcessWebhookRetryQueue` assumes that failed deliveries exist; if the queue is empty, the method may complete quickly without performing any work.  
  - `GlobalSetup` and `GlobalCleanup` are called once per benchmark run; if a benchmark fails mid‑run, cleanup may still be invoked to prevent resource leaks.

- **Thread safety:**  
  - Instances of `WebhookBenchmarks` are **not thread‑safe**. Each benchmark method should be executed sequentially within a single thread.  
  - The class is intended to be used with benchmarking frameworks that guarantee single‑threaded execution per instance.  
  - Do not share the same instance across multiple concurrent benchmark runs.
