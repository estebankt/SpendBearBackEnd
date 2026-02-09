using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Spending.Infrastructure.Data;
using SpendBear.SharedKernel;

namespace Spending.Infrastructure.Services;

public sealed class TransactionReadService : ITransactionReadService
{
    private readonly SpendingDbContext _dbContext;

    public TransactionReadService(SpendingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async IAsyncEnumerable<TransactionReadModel> GetAllTransactionsAsync(
        Guid userId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var transactions = _dbContext.Set<Spending.Domain.Entities.Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Date)
            .AsAsyncEnumerable();

        await foreach (var t in transactions.WithCancellation(cancellationToken))
        {
            yield return new TransactionReadModel(
                t.Id,
                t.UserId,
                t.Amount.Amount,
                t.Amount.Currency,
                (int)t.Type,
                t.CategoryId,
                t.Date);
        }
    }

    public async Task<List<Guid>> GetAllUserIdsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Spending.Domain.Entities.Transaction>()
            .AsNoTracking()
            .Select(t => t.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
