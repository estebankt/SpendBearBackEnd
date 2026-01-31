using Budgets.Application.Abstractions;
using Budgets.Domain.Entities;
using Budgets.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SpendBear.SharedKernel;

namespace Budgets.Infrastructure.Persistence.Repositories;

public sealed class BudgetRepository : IBudgetRepository
{
    private readonly BudgetsDbContext _context;
    private readonly IBudgetsUnitOfWork _unitOfWork;

    public BudgetRepository(BudgetsDbContext context, IBudgetsUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public IUnitOfWork UnitOfWork => _unitOfWork;

    public async Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<List<Budget>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Budget>> GetActiveBudgetsForUserAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId && b.StartDate <= date && b.EndDate >= date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Budget>> GetBudgetsByCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Budgets
            .Where(b => b.UserId == userId &&
                       b.CategoryId == categoryId &&
                       b.StartDate <= date &&
                       b.EndDate >= date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        await _context.Budgets.AddAsync(budget, cancellationToken);
    }

    public Task UpdateAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Update(budget);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Budget budget, CancellationToken cancellationToken = default)
    {
        _context.Budgets.Remove(budget);
        return Task.CompletedTask;
    }
}
