#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Common;
using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for Configuration entity
/// </summary>
public sealed class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
{
    public ConfigurationRepository(ApplicationDbContext context, ILogger<ConfigurationRepository> logger)
        : base(context, logger) { }

    public async Task<List<Configuration>> GetByApplicationIdAsync(Guid applicationId)
    {
        _logger.LogInformation("GetByApplicationIdAsync called with {ApplicationId}", applicationId);
        try
        {
            return await _dbSet.Where(c => c.ApplicationId == applicationId && c.IsActive)
                .OrderBy(c => c.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configurations by application ID {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<Configuration?> GetByNameAsync(string name, Guid applicationId)
    {
        _logger.LogInformation("GetByNameAsync called with {Name} and {ApplicationId}", name, applicationId);
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return await _dbSet.FirstOrDefaultAsync(c =>
                c.Name == name && c.ApplicationId == applicationId && c.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration by name {Name} and application ID {ApplicationId}", name, applicationId);
            throw;
        }
    }

    public async Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null)
    {
        _logger.LogInformation("SearchAsync called with {Query} and {ApplicationId}", query, applicationId);
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(query);
            var configurations = _dbSet.Where(c => c.IsActive);

            if (applicationId.HasValue)
            {
                configurations = configurations.Where(c => c.ApplicationId == applicationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                configurations = configurations.Where(c =>
                    c.Name.Contains(query) ||
                    c.Description != null && c.Description.Contains(query));
            }

            return await configurations.OrderBy(c => c.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search configurations with query {Query} and application ID {ApplicationId}", query, applicationId);
            throw;
        }
    }

    public async Task<int> GetCountByApplicationAsync(Guid applicationId)
    {
        _logger.LogInformation("GetCountByApplicationAsync called with {ApplicationId}", applicationId);
        try
        {
            ArgumentNullException.ThrowIfNull(applicationId);
            return await _dbSet.CountAsync(c => c.ApplicationId == applicationId && c.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get count by application ID {ApplicationId}", applicationId);
            throw;
        }
    }

    public async Task<List<Configuration>> GetDeletedBeforeAsync(DateTime cutoff)
    {
        _logger.LogInformation("GetDeletedBeforeAsync called with {Cutoff}", cutoff);
        try
        {
            ArgumentNullException.ThrowIfNull(cutoff);
            return await _dbSet.Where(c => !c.IsActive && c.DeletedAt != null && c.DeletedAt < cutoff)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get deleted configurations before {Cutoff}", cutoff);
            throw;
        }
    }
}
