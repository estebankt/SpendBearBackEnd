using Spending.Domain.Entities;
using Spending.Domain.Repositories;
using SpendBear.Infrastructure.Core.Data;

namespace Spending.Infrastructure.Data.Repositories;

public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    public TransactionRepository(SpendingDbContext context) : base(context)
    {
    }
}
