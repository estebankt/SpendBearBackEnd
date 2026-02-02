using SpendBear.SharedKernel;

namespace StatementImport.Application.Abstractions;

public interface ITransactionCreationService
{
    Task<Result> CreateTransactionAsync(
        Guid userId,
        decimal amount,
        string currency,
        DateTime date,
        string description,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
