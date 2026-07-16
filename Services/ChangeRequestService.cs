#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotnetConfigServer.Common;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using DotnetConfigServer.Repositories;

namespace DotnetConfigServer.Services;

/// <summary>
/// Manages configuration change requests and the approval workflow.
/// </summary>
public sealed class ChangeRequestService : IChangeRequestService
{
    private readonly IChangeRequestRepository _repository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ChangeRequestService> _logger;

    public ChangeRequestService(
        IChangeRequestRepository repository,
        IConfigurationService configurationService,
        ILogger<ChangeRequestService> logger)
    {
        _repository = repository;
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Submits a new change request for review.
    /// </summary>
    public async Task<ChangeRequest> SubmitAsync(ChangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestedBy))
            throw new ValidationException("RequestedBy is required", new Dictionary<string, List<string>>());

        request.Status = ChangeRequestStatus.Pending;
        request.RequestedAt = DateTime.UtcNow;

        await _repository.AddAsync(request);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Change request {Id} submitted by {User} for configuration {ConfigId}",
            request.Id, request.RequestedBy, request.ConfigurationId);

        return request;
    }

    /// <summary>
    /// Returns all change requests for a configuration, optionally filtered by status.
    /// </summary>
    public async Task<List<ChangeRequest>> GetByConfigurationAsync(Guid configurationId, ChangeRequestStatus? status = null)
        => await _repository.GetByConfigurationAsync(configurationId, status);

    /// <summary>
    /// Returns all pending change requests across all configurations.
    /// </summary>
    public async Task<List<ChangeRequest>> GetPendingAsync()
        => await _repository.GetPendingAsync();

    /// <summary>
    /// Returns a single change request by ID.
    /// </summary>
    public async Task<ChangeRequest?> GetByIdAsync(Guid id)
        => await _repository.GetByIdAsync(id);

    /// <summary>
    /// Approves a pending change request.
    /// The caller is responsible for applying the change after approval when <paramref name="applyImmediately"/> is true.
    /// </summary>
    public async Task<ChangeRequest> ApproveAsync(Guid id, string reviewerId, string? comment = null, bool applyImmediately = true)
    {
        var request = await _repository.GetByIdAsync(id)
            ?? throw new ConfigurationNotFoundException($"Change request {id} not found");

        if (request.Status != ChangeRequestStatus.Pending)
            throw new ConfigurationException($"Change request {id} is not pending (status: {request.Status})");

        request.Approve(reviewerId, comment);
        await _repository.UpdateAsync(request);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Change request {Id} approved by {Reviewer}", id, reviewerId);

        if (applyImmediately)
            await ApplyAsync(request, reviewerId);

        return request;
    }

    /// <summary>
    /// Rejects a pending change request.
    /// </summary>
    public async Task<ChangeRequest> RejectAsync(Guid id, string reviewerId, string? comment = null)
    {
        var request = await _repository.GetByIdAsync(id)
            ?? throw new ConfigurationNotFoundException($"Change request {id} not found");

        if (request.Status != ChangeRequestStatus.Pending)
            throw new ConfigurationException($"Change request {id} is not pending (status: {request.Status})");

        request.Reject(reviewerId, comment);
        await _repository.UpdateAsync(request);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Change request {Id} rejected by {Reviewer}", id, reviewerId);
        return request;
    }

    /// <summary>
    /// Cancels a pending change request (requester or admin action).
    /// </summary>
    public async Task<ChangeRequest> CancelAsync(Guid id, string userId)
    {
        var request = await _repository.GetByIdAsync(id)
            ?? throw new ConfigurationNotFoundException($"Change request {id} not found");

        if (request.Status != ChangeRequestStatus.Pending)
            throw new ConfigurationException($"Only pending change requests can be cancelled");

        request.Cancel();
        await _repository.UpdateAsync(request);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Change request {Id} cancelled by {User}", id, userId);
        return request;
    }

    /// <summary>
    /// Applies an approved change request by executing its payload against the configuration service.
    /// </summary>
    private async Task ApplyAsync(ChangeRequest request, string appliedBy)
    {
        try
        {
            switch (request.Operation)
            {
                case ChangeRequestOperation.UpdateKey:
                {
                    var payload = JsonSerializer.Deserialize<UpdateKeyPayload>(request.Payload)
                        ?? throw new ConfigurationException("Invalid UpdateKey payload");
                    if (request.ConfigurationKeyId is null)
                        throw new ConfigurationException("ConfigurationKeyId is required for UpdateKey");
                    await _configurationService.UpdateKeyAsync(request.ConfigurationKeyId.Value, payload.Value, appliedBy);
                    break;
                }
                case ChangeRequestOperation.CreateKey:
                {
                    var key = JsonSerializer.Deserialize<ConfigurationKey>(request.Payload)
                        ?? throw new ConfigurationException("Invalid CreateKey payload");
                    await _configurationService.AddKeyAsync(request.ConfigurationId, key, appliedBy);
                    break;
                }
                case ChangeRequestOperation.DeleteKey:
                {
                    if (request.ConfigurationKeyId is null)
                        throw new ConfigurationException("ConfigurationKeyId is required for DeleteKey");
                    await _configurationService.DeleteKeyAsync(request.ConfigurationKeyId.Value, appliedBy);
                    break;
                }
                default:
                    throw new ConfigurationException($"Unsupported operation: {request.Operation}");
            }

            request.MarkApplied(appliedBy);
            await _repository.UpdateAsync(request);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Change request {Id} applied by {User}", request.Id, appliedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply change request {Id}", request.Id);
            throw;
        }
    }

    private sealed record UpdateKeyPayload(string Value);
}
