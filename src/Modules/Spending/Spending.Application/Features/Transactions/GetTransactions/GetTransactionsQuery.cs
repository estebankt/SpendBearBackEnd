using Spending.Domain.Entities;

namespace Spending.Application.Features.Transactions.GetTransactions;

public sealed record GetTransactionsQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? CategoryId = null,
    TransactionType? Type = null,
    int PageNumber = 1,
    int PageSize = 50
);
