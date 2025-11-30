using Spending.Domain.Entities;

namespace Spending.Application.Features.Transactions.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    Guid TransactionId,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);
