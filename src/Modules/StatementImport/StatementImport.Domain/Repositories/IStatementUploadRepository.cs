using SpendBear.SharedKernel;
using StatementImport.Domain.Entities;

namespace StatementImport.Domain.Repositories;

public interface IStatementUploadRepository : IRepository<StatementUpload>
{
    Task<StatementUpload?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StatementUpload>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
