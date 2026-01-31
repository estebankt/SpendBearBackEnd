using Spending.Domain.Entities;

namespace Spending.Domain.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<List<Category>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Category>> GetSystemCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllAvailableCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSystemCategoryNameAsync(string name, CancellationToken cancellationToken = default);
}
