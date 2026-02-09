namespace SpendBear.SharedKernel;

public record TransactionReadModel(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    string Currency,
    int Type,
    Guid CategoryId,
    DateTime Date);

public interface ITransactionReadService
{
    IAsyncEnumerable<TransactionReadModel> GetAllTransactionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<Guid>> GetAllUserIdsAsync(CancellationToken cancellationToken);
}
