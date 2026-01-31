using Budgets.Application.Features.Budgets.DTOs;
using Budgets.Domain.Entities;
using Budgets.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Budgets.Application.Features.Budgets.CreateBudget;

public sealed class CreateBudgetHandler
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBudgetHandler(IBudgetRepository budgetRepository, IUnitOfWork unitOfWork)
    {
        _budgetRepository = budgetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BudgetDto>> Handle(
        CreateBudgetCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var budgetResult = Budget.Create(
            command.Name,
            command.Amount,
            command.Currency,
            command.Period,
            command.StartDate,
            userId,
            command.CategoryId,
            command.WarningThreshold
        );

        if (budgetResult.IsFailure)
            return Result.Failure<BudgetDto>(budgetResult.Error);

        await _budgetRepository.AddAsync(budgetResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BudgetDto.FromEntity(budgetResult.Value));
    }
}
