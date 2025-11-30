using Budgets.Domain.Entities;
using Budgets.Domain.Enums;

namespace Budgets.Application.Features.Budgets.DTOs;

public sealed record BudgetDto(
    Guid Id,
    string Name,
    decimal Amount,
    string Currency,
    BudgetPeriod Period,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CategoryId,
    decimal CurrentSpent,
    decimal RemainingAmount,
    decimal PercentageUsed,
    decimal WarningThreshold,
    bool IsExceeded,
    bool WarningTriggered
)
{
    public static BudgetDto FromEntity(Budget budget)
    {
        return new BudgetDto(
            budget.Id,
            budget.Name,
            budget.Amount,
            budget.Currency,
            budget.Period,
            budget.StartDate,
            budget.EndDate,
            budget.CategoryId,
            budget.CurrentSpent,
            budget.RemainingAmount,
            budget.PercentageUsed,
            budget.WarningThreshold,
            budget.IsExceeded,
            budget.WarningTriggered
        );
    }
}
