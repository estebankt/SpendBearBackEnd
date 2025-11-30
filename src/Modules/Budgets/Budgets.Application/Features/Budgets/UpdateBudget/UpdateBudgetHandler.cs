using Budgets.Application.Features.Budgets.DTOs;
using Budgets.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Budgets.Application.Features.Budgets.UpdateBudget;

public sealed class UpdateBudgetHandler
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBudgetHandler(IBudgetRepository budgetRepository, IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDto>> Handle(
        UpdateBudgetCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(command.Id, cancellationToken);
        if (budget is null)
            return Result.Failure<BudgetDto>(new Error("Budget.NotFound", "Budget not found"));

        if (budget.UserId != userId)
            return Result.Failure<BudgetDto>(new Error("Budget.Unauthorized", "You can only update your own budgets"));

        budget.Update(
            command.Name,
            command.Amount,
            command.Period,
            command.StartDate,
            command.CategoryId,
            command.WarningThreshold
        );

        await _budgetRepository.UpdateAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BudgetDto.FromEntity(budget));
    }
}
