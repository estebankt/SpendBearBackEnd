using Budgets.Domain.Enums;

namespace Budgets.Application.Features.Budgets.CreateBudget;

public sealed record CreateBudgetCommand(
    string Name,
    decimal Amount,
    string Currency,
    BudgetPeriod Period,
    DateTime StartDate,
    Guid? CategoryId = null,
    decimal WarningThreshold = 80
);
