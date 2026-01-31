using Microsoft.EntityFrameworkCore;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;
using SpendBear.Infrastructure.Core.Data;

namespace Spending.Infrastructure.Data.Repositories;

public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    private readonly SpendingDbContext _context;

    public TransactionRepository(SpendingDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<(List<Transaction> Items, int TotalCount)> GetTransactionsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? categoryId,
        TransactionType? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Transaction>()
            .Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
