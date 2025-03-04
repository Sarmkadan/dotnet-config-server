#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    private readonly Dictionary<Guid, BatchOperationContext> _operations = new();
    private readonly object _lock = new();

    public BatchOperationService(
        IConfigurationKeyRepository keyRepository,
        ILogger<BatchOperationService> logger)
    {
        _keyRepository = keyRepository;
        _logger = logger;
    }

    public async Task<BatchOperationResult> UpdateKeysAsync(List<KeyUpdateRequest> updates, string userId)
    {
        var operationId = Guid.NewGuid();
        var context = new BatchOperationContext
        {
            OperationId = operationId,
            TotalItems = updates.Count,
            StartedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _operations[operationId] = context;
        }

        _logger.LogInformation("Starting batch update operation {OpId} for {Count} keys", operationId, updates.Count);

        var result = new BatchOperationResult { OperationId = operationId };

        try
        {
            int processed = 0;

            foreach (var update in updates)
            {
                try
                {
                    var key = await _keyRepository.GetByIdAsync(update.KeyId);
                    if (key != null)
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
                    _logger.LogError(ex, "Error updating key {KeyId}", update.KeyId);
                    result.Errors.Add($"Error updating key {update.KeyId}: {ex.Message}");
                }

                context.ProcessedItems = processed;
                context.Progress = (double)processed / updates.Count;
            }

            await _keyRepository.SaveChangesAsync();
            context.CompletedAt = DateTime.UtcNow;
            context.Status = "completed";

            result.SuccessCount = processed;
            result.ErrorCount = updates.Count - processed;
            result.Success = result.ErrorCount == 0;

            _logger.LogInformation("Batch update operation {OpId} completed: {Success} updates, {Errors} errors",
                operationId, processed, result.ErrorCount);
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
        var operationId = Guid.NewGuid();
        var context = new BatchOperationContext
        {
            OperationId = operationId,
            TotalItems = keyIds.Count,
            StartedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _operations[operationId] = context;
        }

        _logger.LogInformation("Starting batch delete operation {OpId} for {Count} keys", operationId, keyIds.Count);

        var result = new BatchOperationResult { OperationId = operationId };
        var deleted = 0;

        try
        {
            foreach (var keyId in keyIds)
            {
                try
                {
                    var key = await _keyRepository.GetByIdAsync(keyId);
                    if (key != null)
                    {
                        await _keyRepository.DeleteAsync(key);
                        deleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting key {KeyId}", keyId);
                    result.Errors.Add($"Error deleting key {keyId}: {ex.Message}");
                }

                context.ProcessedItems++;
                context.Progress = (double)context.ProcessedItems / keyIds.Count;
            }

            await _keyRepository.SaveChangesAsync();
            context.CompletedAt = DateTime.UtcNow;
            context.Status = "completed";

            result.SuccessCount = deleted;
            result.ErrorCount = keyIds.Count - deleted;
            result.Success = result.ErrorCount == 0;

            _logger.LogInformation("Batch delete operation {OpId} completed: {Deleted} deletions", operationId, deleted);
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

    public async Task<BatchOperationStatus> GetStatusAsync(Guid operationId)
    {
        lock (_lock)
        {
            if (_operations.TryGetValue(operationId, out var context))
            {
                return new BatchOperationStatus
                {
                    OperationId = operationId,
                    Status = context.Status,
                    Progress = context.Progress,
                    ProcessedItems = context.ProcessedItems,
                    TotalItems = context.TotalItems,
                    StartedAt = context.StartedAt,
                    CompletedAt = context.CompletedAt,
                    Error = context.Error
                };
            }
        }

        return await Task.FromResult(new BatchOperationStatus { Status = "not_found" });
    }

    public async Task CancelAsync(Guid operationId)
    {
        lock (_lock)
        {
            if (_operations.TryGetValue(operationId, out var context) && context.Status == "in_progress")
            {
                context.Status = "cancelled";
                _logger.LogInformation("Batch operation {OpId} cancelled", operationId);
            }
        }

        await Task.CompletedTask;
    }

    private class BatchOperationContext
    {
        public Guid OperationId { get; set; }
        public string Status { get; set; } = "in_progress";
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public double Progress { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
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
