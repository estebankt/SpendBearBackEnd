using Budgets.Application.Abstractions;
using Budgets.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Budgets.Application.Features.Budgets.DeleteBudget;

public sealed class DeleteBudgetHandler
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IBudgetsUnitOfWork _unitOfWork;

    public DeleteBudgetHandler(IBudgetRepository budgetRepository, IBudgetsUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteBudgetCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(command.Id, cancellationToken);
        if (budget is null)
            return Result.Failure(new Error("Budget.NotFound", "Budget not found"));

        if (budget.UserId != userId)
            return Result.Failure(new Error("Budget.Unauthorized", "You can only delete your own budgets"));

        await _budgetRepository.DeleteAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
