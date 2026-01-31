using Budgets.Domain.Enums;

namespace Budgets.Application.Features.Budgets.UpdateBudget;

public sealed record UpdateBudgetCommand(
    Guid Id,
    string Name,
    decimal Amount,
    BudgetPeriod Period,
    DateTime StartDate,
    Guid? CategoryId,
    decimal WarningThreshold
);
