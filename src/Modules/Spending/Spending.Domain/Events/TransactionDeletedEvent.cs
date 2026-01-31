using SpendBear.SharedKernel;
using Spending.Domain.Entities; // Added this

namespace Spending.Domain.Events;

public sealed record TransactionDeletedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency, // Though not strictly needed for reversal logic, good for completeness
    TransactionType Type,
    Guid CategoryId,
    DateTime Date
) : DomainEvent();
