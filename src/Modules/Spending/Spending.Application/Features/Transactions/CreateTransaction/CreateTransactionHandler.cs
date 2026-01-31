using SpendBear.SharedKernel;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;
using Spending.Domain.ValueObjects;

namespace Spending.Application.Features.Transactions.CreateTransaction;

public sealed class CreateTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTransactionHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TransactionDto>> Handle(
        CreateTransactionCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Create Money value object
        var moneyResult = Money.Create(command.Amount, command.Currency);
        if (moneyResult.IsFailure)
            return Result.Failure<TransactionDto>(moneyResult.Error);

        // Create Transaction aggregate
        var transactionResult = Transaction.Create(
            moneyResult.Value,
            command.Date,
            command.Description,
            command.CategoryId,
            userId,
            command.Type
        );

        if (transactionResult.IsFailure)
            return Result.Failure<TransactionDto>(transactionResult.Error);

        // Add to repository
        await _transactionRepository.AddAsync(transactionResult.Value, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var dto = new TransactionDto(
            transactionResult.Value.Id,
            transactionResult.Value.Amount.Amount,
            transactionResult.Value.Amount.Currency,
            transactionResult.Value.Date,
            transactionResult.Value.Description,
            transactionResult.Value.CategoryId,
            transactionResult.Value.Type
        );

        return Result.Success(dto);
    }
}
