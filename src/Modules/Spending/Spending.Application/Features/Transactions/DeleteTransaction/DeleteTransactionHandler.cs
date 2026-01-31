using SpendBear.SharedKernel;
using Spending.Application.Abstractions;
using Spending.Domain.Repositories;

namespace Spending.Application.Features.Transactions.DeleteTransaction;

public sealed class DeleteTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISpendingUnitOfWork _unitOfWork;

    public DeleteTransactionHandler(
        ITransactionRepository transactionRepository,
        ISpendingUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteTransactionCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get existing transaction
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null)
            return Result.Failure(new Error("Transaction.NotFound", "Transaction not found"));

        // Verify ownership
        if (transaction.UserId != userId)
            return Result.Failure(new Error("Transaction.Unauthorized", "You don't have permission to delete this transaction"));

        // Mark as deleted and raise event
        transaction.Delete();

        // Remove from repository
        await _transactionRepository.DeleteAsync(transaction, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
