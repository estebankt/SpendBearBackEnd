using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core.Data;
using StatementImport.Domain.Entities;
using StatementImport.Domain.Repositories;

namespace StatementImport.Infrastructure.Persistence.Repositories;

public class StatementUploadRepository : BaseRepository<StatementUpload>, IStatementUploadRepository
{
    private readonly StatementImportDbContext _context;

    public StatementUploadRepository(StatementImportDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<StatementUpload?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StatementUpload>()
            .Include(s => s.ParsedTransactions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<StatementUpload>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StatementUpload>()
            .Include(s => s.ParsedTransactions)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UploadedAt)
            .ToListAsync(cancellationToken);
    }
}
