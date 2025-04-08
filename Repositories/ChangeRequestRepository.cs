#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using DotnetConfigServer.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for ChangeRequest entity
/// </summary>
public sealed class ChangeRequestRepository : BaseRepository<ChangeRequest>, IChangeRequestRepository
{
    public ChangeRequestRepository(ApplicationDbContext context, ILogger<ChangeRequestRepository> logger)
        : base(context, logger) { }

    public async Task<List<ChangeRequest>> GetByConfigurationAsync(Guid configurationId, ChangeRequestStatus? status = null)
    {
        var query = _dbSet.Where(r => r.ConfigurationId == configurationId);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        return await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
    }

    public async Task<List<ChangeRequest>> GetPendingAsync()
    {
        return await _dbSet.Where(r => r.Status == ChangeRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt).ToListAsync();
    }
}
