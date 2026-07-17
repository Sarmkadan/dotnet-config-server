# BaseRepository

The `BaseRepository` class is a generic, asynchronous data-access abstraction that provides basic CRUD operations for entities in a .NET application using Entity Framework Core. It is designed to reduce boilerplate code while allowing derived classes to customize behavior via virtual methods or override implementations.

## API

### `public virtual async Task<T?> GetByIdAsync(TKey id)`

Retrieves a single entity by its identifier asynchronously.

- **Parameters**
  - `id` (`TKey`): The unique identifier of the entity to retrieve.
- **Return value**
  - `Task<T?>`: A task that represents the asynchronous operation. The task result contains the entity if found; otherwise, `null`.
- **Exceptions**
  - Throws `ArgumentNullException` if `id` is `null`.
  - Throws `DbUpdateException` if the underlying database operation fails.

---

### `public virtual async Task AddAsync(T entity)`

Adds a new entity to the repository asynchronously.

- **Parameters**
  - `entity` (`T`): The entity to add.
- **Return value**
  - `Task`: A task that represents the asynchronous operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `entity` is `null`.
  - Throws `DbUpdateException` if the underlying database operation fails.

---

### `public virtual async Task UpdateAsync(T entity)`

Updates an existing entity in the repository asynchronously.

- **Parameters**
  - `entity` (`T`): The entity to update.
- **Return value**
  - `Task`: A task that represents the asynchronous operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `entity` is `null`.
  - Throws `InvalidOperationException` if the entity is not tracked by the context.
  - Throws `DbUpdateException` if the underlying database operation fails.

---

### `public virtual async Task DeleteAsync(T entity)`

Removes an entity from the repository asynchronously.

- **Parameters**
  - `entity` (`T`): The entity to remove.
- **Return value**
  - `Task`: A task that represents the asynchronous operation.
- **Exceptions**
  - Throws `ArgumentNullException` if `entity` is `null`.
  - Throws `InvalidOperationException` if the entity is not tracked by the context.
  - Throws `DbUpdateException` if the underlying database operation fails.

---
### `public virtual async Task<List<T>> GetAllAsync()`

Retrieves all entities from the repository asynchronously.

- **Return value**
  - `Task<List<T>>`: A task that represents the asynchronous operation. The task result contains a list of all entities.
- **Exceptions**
  - Throws `DbUpdateException` if the underlying database operation fails.

---
### `public virtual async Task SaveChangesAsync()`

Persists all pending changes to the underlying data store asynchronously.

- **Return value**
  - `Task`: A task that represents the asynchronous operation.
- **Exceptions**
  - Throws `DbUpdateException` if the underlying database operation fails.

## Usage

### Example 1: Basic CRUD with a derived repository

```csharp
public class ProductRepository : BaseRepository<Product, int>
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }
}

var repo = new ProductRepository(dbContext);

// Add a new product
await repo.AddAsync(new Product { Name = "Laptop", Price = 999.99m });

// Update the product
var product = await repo.GetByIdAsync(1);
if (product != null)
{
    product.Price = 899.99m;
    await repo.UpdateAsync(product);
}

// Delete the product
await repo.DeleteAsync(product);

// Save all changes
await repo.SaveChangesAsync();
```

### Example 2: Bulk retrieval and filtering

```csharp
public class ConfigurationRepository : BaseRepository<Configuration, string>
{
    public ConfigurationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Configuration>> GetByEnvironmentAsync(string env)
    {
        return await Context.Set<T>()
            .Where(c => c.Environment == env)
            .ToListAsync();
    }
}

var repo = new ConfigurationRepository(dbContext);
var configs = await repo.GetByEnvironmentAsync("Production");
```

## Notes

- **Thread safety**: This class is not thread-safe. Concurrent calls to any method may lead to race conditions or inconsistent state. External synchronization is required when used in multi-threaded scenarios.
- **Entity state**: Methods like `UpdateAsync` and `DeleteAsync` assume the entity is already tracked by the `DbContext`. If not, they will throw `InvalidOperationException`. Consider attaching the entity first or using `AddAsync` for new entities.
- **Null handling**: All methods validate input parameters for `null` and throw `ArgumentNullException` immediately. Ensure non-null arguments are passed.
- **Transaction scope**: `SaveChangesAsync` commits all pending changes in a single transaction. If partial failure occurs, the entire transaction is rolled back by EF Core.
