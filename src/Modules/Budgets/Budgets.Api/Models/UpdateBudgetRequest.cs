using Budgets.Domain.Enums;

namespace Budgets.Api.Models;

public sealed record UpdateBudgetRequest(
    string Name,
    decimal Amount,
    BudgetPeriod Period,
    DateTime StartDate,
    Guid? CategoryId,
    decimal WarningThreshold
);
