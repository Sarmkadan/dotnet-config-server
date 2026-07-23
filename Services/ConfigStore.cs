#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using DotnetConfigServer.Exceptions;
using DotnetConfigServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Services;

/// <summary>
/// Default <see cref="IConfigStore"/> implementation backed by the relational
/// <see cref="ApplicationDbContext"/>. All operations honor the supplied
/// <see cref="CancellationToken"/>, which repository-based access does not.
/// </summary>
public sealed class ConfigStore : IConfigStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigStore"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public ConfigStore(ApplicationDbContext context, ILogger<ConfigStore> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Configuration?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Configurations
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error retrieving configuration {ConfigurationId} from store", id);
            throw new DatabaseException($"Error retrieving configuration: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Configuration> SetAsync(Configuration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        try
        {
            var existing = await _context.Configurations
                .FirstOrDefaultAsync(c => c.Id == configuration.Id, cancellationToken);

            if (existing is null)
            {
                await _context.Configurations.AddAsync(configuration, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(configuration);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return configuration;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error persisting configuration {ConfigurationId} to store", configuration.Id);
            throw new DatabaseException($"Error persisting configuration: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConfigurationVersion>> ListVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ConfigurationVersions
                .Where(v => v.ConfigurationId == configurationId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error listing versions for configuration {ConfigurationId}", configurationId);
            throw new DatabaseException($"Error listing versions: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Configurations
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (existing is null)
            {
                return false;
            }

            _context.Configurations.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error deleting configuration {ConfigurationId} from store", id);
            throw new DatabaseException($"Error deleting configuration: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Config store reachability check failed");
            return false;
        }
    }
}
