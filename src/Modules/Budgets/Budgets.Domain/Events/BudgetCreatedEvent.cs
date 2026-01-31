using SpendBear.SharedKernel;

namespace Budgets.Domain.Events;

public sealed record BudgetCreatedEvent(
    Guid BudgetId,
    Guid UserId,
    string Name,
    decimal Amount,
    string Currency,
    Guid? CategoryId
) : DomainEvent();
