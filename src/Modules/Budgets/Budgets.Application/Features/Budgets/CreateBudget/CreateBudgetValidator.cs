using SpendBear.SharedKernel;

namespace Budgets.Application.Features.Budgets.CreateBudget;

public static class CreateBudgetValidator
{
    public static Result Validate(CreateBudgetCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result.Failure(new Error("CreateBudget.InvalidName", "Budget name is required"));

        if (command.Name.Length > 100)
            return Result.Failure(new Error("CreateBudget.NameTooLong", "Budget name cannot exceed 100 characters"));

        if (command.Amount <= 0)
            return Result.Failure(new Error("CreateBudget.InvalidAmount", "Amount must be greater than zero"));

        if (string.IsNullOrWhiteSpace(command.Currency) || command.Currency.Length != 3)
            return Result.Failure(new Error("CreateBudget.InvalidCurrency", "Currency must be a 3-letter code"));

        if (command.StartDate == default)
            return Result.Failure(new Error("CreateBudget.InvalidStartDate", "Start date is required"));

        if (command.WarningThreshold < 0 || command.WarningThreshold > 100)
            return Result.Failure(new Error("CreateBudget.InvalidThreshold", "Warning threshold must be between 0 and 100"));

        return Result.Success();
    }
}
