using SpendBear.SharedKernel;
using Spending.Domain.Entities;

namespace Spending.Domain.Events;

public sealed record TransactionCreatedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    Guid CategoryId,
    DateTime Date
) : DomainEvent();
