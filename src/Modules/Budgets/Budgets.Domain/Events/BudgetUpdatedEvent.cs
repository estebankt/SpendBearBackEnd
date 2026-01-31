using SpendBear.SharedKernel;

namespace Budgets.Domain.Events;

public sealed record BudgetUpdatedEvent(
    Guid BudgetId,
    Guid UserId
) : DomainEvent();
