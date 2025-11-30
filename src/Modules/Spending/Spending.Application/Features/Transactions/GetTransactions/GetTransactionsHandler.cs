using SpendBear.SharedKernel;
using Spending.Domain.Repositories;
using Spending.Application.Features.Transactions.CreateTransaction;

namespace Spending.Application.Features.Transactions.GetTransactions;

public sealed class GetTransactionsHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<PagedResult<TransactionDto>>> Handle(
        GetTransactionsQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetTransactionsAsync(
            userId,
            query.StartDate,
            query.EndDate,
            query.CategoryId,
            query.Type,
            query.PageNumber,
            query.PageSize,
            cancellationToken
        );

        var dtos = transactions.Items.Select(t => new TransactionDto(
            t.Id,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Date,
            t.Description,
            t.CategoryId,
            t.Type
        )).ToList();

        var result = new PagedResult<TransactionDto>(
            dtos,
            transactions.TotalCount,
            query.PageNumber,
            query.PageSize
        );

        return Result.Success(result);
    }
}
