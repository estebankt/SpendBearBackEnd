using SpendBear.SharedKernel;
using Spending.Application.Features.Transactions.CreateTransaction;
using Spending.Domain.Entities;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.Services;

public class SpendingTransactionCreationService : ITransactionCreationService
{
    private readonly CreateTransactionHandler _createTransactionHandler;

    public SpendingTransactionCreationService(CreateTransactionHandler createTransactionHandler)
    {
        _createTransactionHandler = createTransactionHandler;
    }

    public async Task<Result> CreateTransactionAsync(
        Guid userId,
        decimal amount,
        string currency,
        DateTime date,
        string description,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTransactionCommand(amount, currency, date, description, categoryId, TransactionType.Expense);
        var result = await _createTransactionHandler.Handle(command, userId, cancellationToken);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
