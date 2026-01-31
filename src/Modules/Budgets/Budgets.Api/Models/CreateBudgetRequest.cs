using Budgets.Domain.Enums;

namespace Budgets.Api.Models;

public sealed record CreateBudgetRequest(
    string Name,
    decimal Amount,
    string Currency,
    BudgetPeriod Period,
    DateTime? StartDate = null,
    Guid? CategoryId = null,
    decimal WarningThreshold = 80
);
