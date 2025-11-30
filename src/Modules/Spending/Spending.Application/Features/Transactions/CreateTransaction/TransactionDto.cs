using Spending.Domain.Entities;

namespace Spending.Application.Features.Transactions.CreateTransaction;

public sealed record TransactionDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);
