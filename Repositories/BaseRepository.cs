// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Data;
using Microsoft.EntityFrameworkCore;

namespace DotnetConfigServer.Repositories;

/// <summary>
/// Base repository implementation with common CRUD operations
/// </summary>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<BaseRepository<T>> _logger;

    protected BaseRepository(ApplicationDbContext context, ILogger<BaseRepository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity by ID {Id}", id);
            throw new DatabaseException($"Error retrieving entity: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Adds a new entity
    /// </summary>
    public virtual async Task AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity");
            throw new DatabaseException($"Error adding entity: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates an entity
    /// </summary>
    public virtual async Task UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity");
            throw new DatabaseException($"Error updating entity: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes an entity
    /// </summary>
    public virtual async Task DeleteAsync(T entity)
    {
        try
        {
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity");
            throw new DatabaseException($"Error deleting entity: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all entities
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities");
            throw new DatabaseException($"Error retrieving entities: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    public virtual async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error");
            throw new DatabaseException($"Database update error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes");
            throw new DatabaseException($"Error saving changes: {ex.Message}", ex);
        }
    }
}
