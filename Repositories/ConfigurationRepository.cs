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
sealed public class ConfigurationRepository : BaseRepository<Configuration>, IConfigurationRepository
{
    public ConfigurationRepository(ApplicationDbContext context, ILogger<ConfigurationRepository> logger)
        : base(context, logger) { }

    public async Task<List<Configuration>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbSet.Where(c => c.ApplicationId == applicationId && c.IsActive)
            .OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Configuration?> GetByNameAsync(string name, Guid applicationId)
    {
        return await _dbSet.FirstOrDefaultAsync(c =>
            c.Name == name && c.ApplicationId == applicationId && c.IsActive);
    }

    public async Task<List<Configuration>> SearchAsync(string query, Guid? applicationId = null)
    {
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

    public async Task<int> GetCountByApplicationAsync(Guid applicationId)
    {
        return await _dbSet.CountAsync(c => c.ApplicationId == applicationId && c.IsActive);
    }
}
