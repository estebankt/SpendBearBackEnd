using SpendBear.SharedKernel;
using Spending.Domain.Entities;

namespace Spending.Domain.Events;

public sealed record TransactionUpdatedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    Guid CategoryId,
    DateTime Date,
    decimal OldAmount,
    TransactionType OldType,
    Guid OldCategoryId,
    DateTime OldDate
) : DomainEvent();
