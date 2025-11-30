using Budgets.Application.Features.Budgets.DTOs;
using Budgets.Domain.Entities;
using Budgets.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Budgets.Application.Features.Budgets.GetBudgets;

public sealed class GetBudgetsHandler
{
    private readonly IBudgetRepository _budgetRepository;

    public GetBudgetsHandler(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<Result<List<BudgetDto>>> Handle(
        GetBudgetsQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        List<Budget> budgets;

        if (query.ActiveOnly && query.Date.HasValue)
        {
            if (query.CategoryId.HasValue)
            {
                budgets = await _budgetRepository.GetBudgetsByCategoryAsync(
                    userId,
                    query.CategoryId.Value,
                    query.Date.Value,
                    cancellationToken);
            }
            else
            {
                budgets = await _budgetRepository.GetActiveBudgetsForUserAsync(
                    userId,
                    query.Date.Value,
                    cancellationToken);
            }
        }
        else
        {
            budgets = await _budgetRepository.GetByUserIdAsync(userId, cancellationToken);
        }

        var dtos = budgets.Select(BudgetDto.FromEntity).ToList();
        return Result.Success(dtos);
    }
}
