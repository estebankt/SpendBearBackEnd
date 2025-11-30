using SpendBear.SharedKernel;

namespace Spending.Domain.Events;

public sealed record TransactionDeletedEvent(
    Guid TransactionId,
    Guid UserId
) : DomainEvent();
