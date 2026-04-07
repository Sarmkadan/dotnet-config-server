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
/// Executes rollbacks and stores rollback metadata in the audit log.
/// </summary>
sealed public class RollbackService : IRollbackService
{
    private readonly IVersioningService _versioningService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<RollbackService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RollbackService"/>.
    /// </summary>
    public RollbackService(
        IVersioningService versioningService,
        IAuditLogRepository auditLogRepository,
        ILogger<RollbackService> logger)
    {
        _versioningService = versioningService;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RollbackResult> ExecuteRollbackAsync(Guid configurationId, Guid targetVersionId, string reason, string userId)
    {
        var targetVersion = await _versioningService.GetVersionAsync(targetVersionId);
        if (targetVersion is null || targetVersion.ConfigurationId != configurationId)
            throw new ConfigurationNotFoundException(targetVersionId.ToString());

        var newVersion = await _versioningService.RollbackAsync(configurationId, targetVersionId, userId);
        var publishedVersion = await _versioningService.PublishVersionAsync(newVersion.Id, userId);
        var performedAt = DateTime.UtcNow;

        var auditEntry = AuditLog.CreateEntry(
            configurationId,
            "Rollback",
            publishedVersion.Id.ToString(),
            $"Rollback to {targetVersion.VersionNumber}",
            userId,
            null,
            reason,
            JsonSerializer.Serialize(new RollbackAuditMetadata
            {
                RestoredFromVersionId = targetVersion.Id,
                Reason = reason,
                PerformedBy = userId,
                PerformedAt = performedAt,
                KeysRestored = publishedVersion.KeyCount
            }));
        auditEntry.ActionType = AuditActionType.ConfigurationUpdated;
        auditEntry.Timestamp = performedAt;

        await _auditLogRepository.AddAsync(auditEntry);
        await _auditLogRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Rollback executed for configuration {ConfigurationId} to version {TargetVersionId} by {UserId}",
            configurationId,
            targetVersionId,
            userId);

        return new RollbackResult
        {
            ConfigurationId = configurationId,
            NewVersion = publishedVersion.GetSummary(),
            RestoredFromVersion = targetVersion.GetSummary(),
            Reason = reason,
            PerformedBy = userId,
            PerformedAt = performedAt,
            KeysRestored = publishedVersion.KeyCount
        };
    }

    /// <inheritdoc />
    public async Task<List<RollbackRecord>> GetRollbackHistoryAsync(Guid configurationId)
    {
        var logs = await _auditLogRepository.GetByConfigurationAsync(configurationId);

        return logs
            .Where(log => string.Equals(log.EntityType, "Rollback", StringComparison.OrdinalIgnoreCase))
            .Select(MapRecord)
            .OrderByDescending(record => record.PerformedAt)
            .ToList();
    }

    private static RollbackRecord MapRecord(AuditLog auditLog)
    {
        var metadata = string.IsNullOrWhiteSpace(auditLog.NewValues)
            ? null
            : JsonSerializer.Deserialize<RollbackAuditMetadata>(auditLog.NewValues);

        return new RollbackRecord
        {
            Id = auditLog.Id,
            ConfigurationId = auditLog.ConfigurationId,
            NewVersionId = Guid.TryParse(auditLog.EntityId, out var newVersionId) ? newVersionId : Guid.Empty,
            RestoredFromVersionId = metadata?.RestoredFromVersionId ?? Guid.Empty,
            Reason = metadata?.Reason ?? auditLog.Details ?? string.Empty,
            PerformedBy = metadata?.PerformedBy ?? auditLog.UserId,
            PerformedAt = metadata?.PerformedAt ?? auditLog.Timestamp
        };
    }

    private sealed class RollbackAuditMetadata
    {
        public Guid RestoredFromVersionId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string PerformedBy { get; set; } = string.Empty;

        public DateTime PerformedAt { get; set; }

        public int KeysRestored { get; set; }
    }
}
