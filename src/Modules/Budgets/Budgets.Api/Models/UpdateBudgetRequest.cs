using Budgets.Domain.Enums;

namespace Budgets.Api.Models;

public sealed record UpdateBudgetRequest(
    string Name,
    decimal Amount,
    BudgetPeriod Period,
    DateTime? StartDate = null,
    Guid? CategoryId = null,
    decimal WarningThreshold = 80
);
