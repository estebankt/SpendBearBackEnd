using SpendBear.SharedKernel;

namespace Budgets.Domain.Events;

public sealed record BudgetWarningEvent(
    Guid BudgetId,
    Guid UserId,
    string BudgetName,
    decimal BudgetAmount,
    decimal CurrentSpent,
    decimal PercentageUsed,
    decimal ThresholdPercentage
) : DomainEvent();
