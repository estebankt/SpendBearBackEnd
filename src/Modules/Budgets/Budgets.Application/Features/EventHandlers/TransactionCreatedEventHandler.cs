using Budgets.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Budgets.Application.Features.EventHandlers;

public sealed class TransactionCreatedEventHandler
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionCreatedEventHandler(IBudgetRepository budgetRepository, IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        Guid transactionId,
        Guid userId,
        decimal amount,
        string currency,
        int transactionType, // 0 = Expense, 1 = Income
        Guid categoryId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        // Only process expenses for budget tracking
        if (transactionType != 0) // Not an expense
            return;

        // Get all active budgets for this user at the transaction date
        var budgets = await _budgetRepository.GetActiveBudgetsForUserAsync(userId, date, cancellationToken);

        foreach (var budget in budgets)
        {
            // Skip if budget currency doesn't match transaction currency
            if (budget.Currency != currency)
                continue;

            // Apply transaction to budget if:
            // 1. Budget has no category (applies to all transactions)
            // 2. Budget category matches transaction category
            if (budget.CategoryId == null || budget.CategoryId == categoryId)
            {
                budget.RecordTransaction(amount);
                await _budgetRepository.UpdateAsync(budget, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
