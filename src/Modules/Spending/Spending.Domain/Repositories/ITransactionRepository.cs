using SpendBear.SharedKernel;
using Spending.Domain.Entities;

namespace Spending.Domain.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<(List<Transaction> Items, int TotalCount)> GetTransactionsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
