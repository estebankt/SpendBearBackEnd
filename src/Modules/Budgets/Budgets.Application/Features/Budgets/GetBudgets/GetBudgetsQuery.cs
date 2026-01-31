namespace Budgets.Application.Features.Budgets.GetBudgets;

public sealed record GetBudgetsQuery(
    bool ActiveOnly = false,
    Guid? CategoryId = null,
    DateTime? Date = null
);
