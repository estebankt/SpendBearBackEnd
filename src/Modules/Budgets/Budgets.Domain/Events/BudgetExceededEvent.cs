using SpendBear.SharedKernel;

namespace Budgets.Domain.Events;

public sealed record BudgetExceededEvent(
    Guid BudgetId,
    Guid UserId,
    string BudgetName,
    decimal BudgetAmount,
    decimal CurrentSpent,
    decimal ExceededBy
) : DomainEvent();
