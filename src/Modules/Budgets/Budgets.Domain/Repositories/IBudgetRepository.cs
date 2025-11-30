using Budgets.Domain.Entities;
using SpendBear.SharedKernel;

namespace Budgets.Domain.Repositories;

public interface IBudgetRepository : IRepository<Budget>
{
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Budget>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Budget>> GetActiveBudgetsForUserAsync(Guid userId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<Budget>> GetBudgetsByCategoryAsync(Guid userId, Guid categoryId, DateTime date, CancellationToken cancellationToken = default);
    Task AddAsync(Budget budget, CancellationToken cancellationToken = default);
    Task UpdateAsync(Budget budget, CancellationToken cancellationToken = default);
    Task DeleteAsync(Budget budget, CancellationToken cancellationToken = default);
}
