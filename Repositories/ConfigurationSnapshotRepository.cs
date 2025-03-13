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
sealed public class ConfigurationSnapshotRepository : BaseRepository<ConfigurationSnapshot>, IConfigurationSnapshotRepository
{
    public ConfigurationSnapshotRepository(ApplicationDbContext context, ILogger<ConfigurationSnapshotRepository> logger)
        : base(context, logger) { }

    public async Task<List<ConfigurationSnapshot>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet.Where(s => s.ConfigurationId == configurationId)
            .OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public async Task<ConfigurationSnapshot?> GetLatestSnapshotAsync(Guid configurationId)
    {
        return await _dbSet.Where(s => s.ConfigurationId == configurationId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
