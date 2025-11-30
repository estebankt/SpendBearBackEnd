using SpendBear.SharedKernel;
using Spending.Domain.Repositories;
using Spending.Domain.ValueObjects;
using Spending.Application.Features.Transactions.CreateTransaction;

namespace Spending.Application.Features.Transactions.UpdateTransaction;

public sealed class UpdateTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTransactionHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionDto>> Handle(
        UpdateTransactionCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get existing transaction
        var transaction = await _transactionRepository.GetByIdAsync(command.TransactionId, cancellationToken);
        if (transaction == null)
            return Result.Failure<TransactionDto>(new Error("Transaction.NotFound", "Transaction not found"));

        // Verify ownership
        if (transaction.UserId != userId)
            return Result.Failure<TransactionDto>(new Error("Transaction.Unauthorized", "You don't have permission to update this transaction"));

        // Create Money value object
        var moneyResult = Money.Create(command.Amount, command.Currency);
        if (moneyResult.IsFailure)
            return Result.Failure<TransactionDto>(moneyResult.Error);

        // Update transaction
        transaction.Update(
            moneyResult.Value,
            command.Date,
            command.Description,
            command.CategoryId,
            command.Type
        );

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var dto = new TransactionDto(
            transaction.Id,
            transaction.Amount.Amount,
            transaction.Amount.Currency,
            transaction.Date,
            transaction.Description,
            transaction.CategoryId,
            transaction.Type
        );

        return Result.Success(dto);
    }
}
