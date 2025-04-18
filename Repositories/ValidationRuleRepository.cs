#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Repository for <see cref="ValidationRule"/> entities.
/// </summary>
public sealed class ValidationRuleRepository : BaseRepository<ValidationRule>, IValidationRuleRepository
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRuleRepository"/>.
    /// </summary>
    public ValidationRuleRepository(ApplicationDbContext context, ILogger<ValidationRuleRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetByConfigurationAsync(Guid configurationId)
    {
        return await _dbSet
            .Where(rule => rule.ConfigurationId == configurationId && rule.IsActive)
            .OrderBy(rule => rule.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetGlobalRulesAsync()
    {
        return await _dbSet
            .Where(rule => rule.ConfigurationId == null && rule.IsActive)
            .OrderBy(rule => rule.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ValidationRule>> GetApplicableRulesAsync(Guid configurationId)
    {
        return await _dbSet
            .Where(rule => rule.IsActive && (rule.ConfigurationId == null || rule.ConfigurationId == configurationId))
            .OrderBy(rule => rule.Name)
            .ToListAsync();
    }
}
