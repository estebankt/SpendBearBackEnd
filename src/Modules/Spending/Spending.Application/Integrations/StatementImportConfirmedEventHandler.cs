using SpendBear.SharedKernel;
using Spending.Application.Abstractions;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;
using Spending.Domain.ValueObjects;
using StatementImport.Domain.Events;

namespace Spending.Application.Integrations;

public sealed class StatementImportConfirmedEventHandler : IEventHandler<StatementImportConfirmedEvent>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISpendingUnitOfWork _unitOfWork;

    public StatementImportConfirmedEventHandler(
        ITransactionRepository transactionRepository,
        ISpendingUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(StatementImportConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (var importData in domainEvent.Transactions)
        {
            // In our system, positive amounts in statement are usually expenses
            // but let's be consistent with TransactionType.
            // Based on Spending module: Expense = 1, Income = 2.
            // If amount > 0, it's an expense. If amount < 0, it's income.
            var type = importData.Amount >= 0 ? TransactionType.Expense : TransactionType.Income;
            var absAmount = Math.Abs(importData.Amount);

            var moneyResult = Money.Create(absAmount, importData.Currency);
            if (moneyResult.IsFailure) continue;

            var transactionResult = Transaction.Create(
                moneyResult.Value,
                importData.Date,
                importData.Description,
                importData.CategoryId,
                domainEvent.UserId,
                type
            );

            if (transactionResult.IsSuccess)
            {
                await _transactionRepository.AddAsync(transactionResult.Value, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
