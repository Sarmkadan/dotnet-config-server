#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for ConfigurationSnapshot entity
/// </summary>
public sealed class ConfigurationSnapshotRepository : BaseRepository<ConfigurationSnapshot>, IConfigurationSnapshotRepository
{
    public ConfigurationSnapshotRepository(ApplicationDbContext context, ILogger<ConfigurationSnapshotRepository> logger) : base(context, logger)
    {
        _logger.LogInformation("Initializing ConfigurationSnapshotRepository");
    }

    public async Task<List<ConfigurationSnapshot>> GetByConfigurationAsync(Guid configurationId)
    {
        _logger.LogInformation("Fetching snapshots for configuration {ConfigurationId}", configurationId);
        try
        {
            var result = await _dbSet.Where(s => s.ConfigurationId == configurationId)
                .OrderByDescending(s => s.CreatedAt).ToListAsync();
            _logger.LogInformation("Successfully retrieved {Count} snapshots for configuration {ConfigurationId}", result.Count, configurationId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch snapshots for configuration {ConfigurationId}", configurationId);
            throw;
        }
    }

    public async Task<ConfigurationSnapshot?> GetLatestSnapshotAsync(Guid configurationId)
    {
        _logger.LogInformation("Fetching latest snapshot for configuration {ConfigurationId}", configurationId);
        try
        {
            var result = await _dbSet.Where(s => s.ConfigurationId == configurationId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
            _logger.LogInformation("Successfully retrieved latest snapshot for configuration {ConfigurationId}", configurationId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch latest snapshot for configuration {ConfigurationId}", configurationId);
            throw;
        }
    }
}
