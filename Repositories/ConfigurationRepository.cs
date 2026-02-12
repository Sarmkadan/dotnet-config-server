#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

using DotnetConfigServer.Exceptions;
namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for Configuration entity
/// </summary>
sealed public class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
{
    public ConfigurationRepository(ApplicationDbContext context, ILogger<ConfigurationRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets all configurations for an application
    /// </summary>
    public async Task<List<Configuration>> GetByApplicationIdAsync(Guid applicationId)
    {
        try
        {
            return await _dbSet
                .Where(c => c.ApplicationId == applicationId && c.DeletedAt is null)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for application {AppId}", applicationId);
            throw new DatabaseException($"Error retrieving configurations: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a configuration by name
    /// </summary>
    public async Task<Configuration?> GetByNameAsync(string name, Guid applicationId)
    {
        try
        {
            return await _dbSet
                .Where(c => c.Name == name && c.ApplicationId == applicationId && c.DeletedAt is null)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration by name");
            throw new DatabaseException($"Error retrieving configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Searches for configurations
    /// </summary>
    public async Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null)
    {
        try
        {
            var dbQuery = _dbSet.Where(c => c.DeletedAt is null);

            if (applicationId.HasValue)
                dbQuery = dbQuery.Where(c => c.ApplicationId == applicationId.Value);

            if (!string.IsNullOrWhiteSpace(query))
                dbQuery = dbQuery.Where(c => EF.Functions.Like(c.Name, $"%{query}%") ||
                                            EF.Functions.Like(c.Description, $"%{query}%"));

            return await dbQuery.OrderByDescending(c => c.UpdatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configurations");
            throw new DatabaseException($"Error searching configurations: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets count of configurations for an application
    /// </summary>
    public async Task<int> GetCountByApplicationAsync(Guid applicationId)
    {
        try
        {
            return await _dbSet
                .Where(c => c.ApplicationId == applicationId && c.DeletedAt is null)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration count");
            throw new DatabaseException($"Error getting count: {ex.Message}", ex);
        }
    }
}
