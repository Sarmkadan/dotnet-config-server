#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetConfigServer.Repositories;

namespace DotnetConfigServer.Services;

/// <summary>
/// Service for executing batch operations on configurations and keys.
/// Provides efficient bulk update, delete, and import operations.
/// </summary>
public interface IBatchOperationService
{
    /// <summary>
    /// Updates multiple configuration keys in a batch.
    /// </summary>
    Task<BatchOperationResult> UpdateKeysAsync(List<KeyUpdateRequest> updates, string userId);

    /// <summary>
    /// Deletes multiple configuration keys in a batch.
    /// </summary>
    Task<BatchOperationResult> DeleteKeysAsync(List<Guid> keyIds, string userId);

    /// <summary>
    /// Gets the status of a batch operation.
    /// </summary>
    Task<BatchOperationStatus> GetStatusAsync(Guid operationId);

    /// <summary>
    /// Cancels a pending batch operation.
    /// </summary>
    Task CancelAsync(Guid operationId);
}

sealed public class BatchOperationService : IBatchOperationService
{
    private readonly IConfigurationKeyRepository _keyRepository;
    private readonly ILogger<BatchOperationService> _logger;
    private readonly ConcurrentDictionary<Guid, BatchOperationContext> _operations = new();

    public BatchOperationService(
        IConfigurationKeyRepository keyRepository,
        ILogger<BatchOperationService> logger)
    {
        _keyRepository = keyRepository;
        _logger = logger;
    }

    public async Task<BatchOperationResult> UpdateKeysAsync(List<KeyUpdateRequest> updates, string userId)
    {
        if (updates is null || updates.Count == 0)
            return new BatchOperationResult { Success = true, OperationId = Guid.Empty };

        var operationId = Guid.NewGuid();
        var context = new BatchOperationContext
        {
            OperationId = operationId,
            TotalItems = updates.Count,
            StartedAt = DateTime.UtcNow
        };

        _operations[operationId] = context;

        _logger.LogInformation(
            "Starting batch update operation {OpId} for {Count} keys by user {UserId}",
            operationId, updates.Count, userId);

        var result = new BatchOperationResult { OperationId = operationId };

        try
        {
            int processed = 0;

            foreach (var update in updates)
            {
                if (context.IsCancelled)
                {
                    _logger.LogInformation("Batch update operation {OpId} was cancelled at item {Processed}/{Total}",
                        operationId, processed, updates.Count);
                    break;
                }

                try
                {
                    var key = await _keyRepository.GetByIdAsync(update.KeyId);
                    if (key is not null)
                    {
                        key.Value = update.NewValue;
                        key.UpdatedBy = userId;
                        key.UpdatedAt = DateTime.UtcNow;

                        await _keyRepository.UpdateAsync(key);
                        processed++;
                    }
                    else
                    {
                        result.Errors.Add($"Key {update.KeyId} not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating key {KeyId} in operation {OpId}", update.KeyId, operationId);
                    result.Errors.Add($"Error updating key {update.KeyId}: {ex.Message}");
                }

                Interlocked.Exchange(ref context._processedItems, processed);
            }

            await _keyRepository.SaveChangesAsync();
            context.CompletedAt = DateTime.UtcNow;
            context.Status = context.IsCancelled ? "cancelled" : "completed";

            result.SuccessCount = processed;
            result.ErrorCount = updates.Count - processed;
            result.Success = result.ErrorCount == 0 && !context.IsCancelled;

            _logger.LogInformation(
                "Batch update operation {OpId} {Status}: {Success} updates, {Errors} errors, elapsed {Elapsed}ms",
                operationId, context.Status, processed, result.ErrorCount,
                (long)(context.CompletedAt.Value - context.StartedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch update operation {OpId} failed", operationId);
            context.Status = "failed";
            context.Error = ex.Message;
            result.Success = false;
        }

        return result;
    }

    public async Task<BatchOperationResult> DeleteKeysAsync(List<Guid> keyIds, string userId)
    {
        if (keyIds is null || keyIds.Count == 0)
            return new BatchOperationResult { Success = true, OperationId = Guid.Empty };

        var operationId = Guid.NewGuid();
        var context = new BatchOperationContext
        {
            OperationId = operationId,
            TotalItems = keyIds.Count,
            StartedAt = DateTime.UtcNow
        };

        _operations[operationId] = context;

        _logger.LogInformation(
            "Starting batch delete operation {OpId} for {Count} keys by user {UserId}",
            operationId, keyIds.Count, userId);

        var result = new BatchOperationResult { OperationId = operationId };
        var deleted = 0;

        try
        {
            foreach (var keyId in keyIds)
            {
                if (context.IsCancelled)
                {
                    _logger.LogInformation("Batch delete operation {OpId} was cancelled at item {Processed}/{Total}",
                        operationId, deleted, keyIds.Count);
                    break;
                }

                try
                {
                    var key = await _keyRepository.GetByIdAsync(keyId);
                    if (key is not null)
                    {
                        await _keyRepository.DeleteAsync(key);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting key {KeyId} in operation {OpId}", keyId, operationId);
                    result.Errors.Add($"Error deleting key {keyId}: {ex.Message}");
                }

                Interlocked.Increment(ref context._processedItems);
            }

            await _keyRepository.SaveChangesAsync();
            context.CompletedAt = DateTime.UtcNow;
            context.Status = context.IsCancelled ? "cancelled" : "completed";

            result.SuccessCount = deleted;
            result.ErrorCount = keyIds.Count - deleted;
            result.Success = result.ErrorCount == 0 && !context.IsCancelled;

            _logger.LogInformation(
                "Batch delete operation {OpId} {Status}: {Deleted} deletions, elapsed {Elapsed}ms",
                operationId, context.Status, deleted,
                (long)(context.CompletedAt.Value - context.StartedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch delete operation {OpId} failed", operationId);
            context.Status = "failed";
            context.Error = ex.Message;
            result.Success = false;
        }

        return result;
    }

    public Task<BatchOperationStatus> GetStatusAsync(Guid operationId)
    {
        if (_operations.TryGetValue(operationId, out var context))
        {
            return Task.FromResult(new BatchOperationStatus
            {
                OperationId = operationId,
                Status = context.Status,
                Progress = context.TotalItems > 0
                    ? (double)Volatile.Read(ref context._processedItems) / context.TotalItems
                    : 0,
                ProcessedItems = Volatile.Read(ref context._processedItems),
                TotalItems = context.TotalItems,
                StartedAt = context.StartedAt,
                CompletedAt = context.CompletedAt,
                Error = context.Error
            });
        }

        return Task.FromResult(new BatchOperationStatus { OperationId = operationId, Status = "not_found" });
    }

    public Task CancelAsync(Guid operationId)
    {
        if (_operations.TryGetValue(operationId, out var context) && context.Status == "in_progress")
        {
            context.IsCancelled = true;
            context.Status = "cancelling";
            _logger.LogInformation("Batch operation {OpId} cancellation requested", operationId);
        }

        return Task.CompletedTask;
    }

    private class BatchOperationContext
    {
        public Guid OperationId { get; set; }
        public string Status { get; set; } = "in_progress";
        public int TotalItems { get; set; }
        public int _processedItems;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
        public volatile bool IsCancelled;
    }
}

sealed public class KeyUpdateRequest
{
    public Guid KeyId { get; set; }
    public string NewValue { get; set; } = string.Empty;
}

sealed public class BatchOperationResult
{
    public Guid OperationId { get; set; }
    public bool Success { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

sealed public class BatchOperationStatus
{
    public Guid OperationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Progress { get; set; }
    public int ProcessedItems { get; set; }
    public int TotalItems { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
