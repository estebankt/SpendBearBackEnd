using SpendBear.SharedKernel;

namespace Spending.Application.Features.Transactions.CreateTransaction;

public static class CreateTransactionValidator
{
    public static Result Validate(CreateTransactionCommand command)
    {
        if (command.Amount == 0)
            return Result.Failure(new Error("CreateTransaction.InvalidAmount", "Amount cannot be zero."));

        if (string.IsNullOrWhiteSpace(command.Currency))
            return Result.Failure(new Error("CreateTransaction.InvalidCurrency", "Currency is required."));

        if (command.CategoryId == Guid.Empty)
            return Result.Failure(new Error("CreateTransaction.InvalidCategory", "Category ID is required."));

        if (command.Date > DateTime.UtcNow.AddDays(1))
            return Result.Failure(new Error("CreateTransaction.InvalidDate", "Transaction date cannot be in the future."));

        if (string.IsNullOrWhiteSpace(command.Description))
            return Result.Failure(new Error("CreateTransaction.InvalidDescription", "Description is required."));

        if (command.Description.Length > 500)
            return Result.Failure(new Error("CreateTransaction.InvalidDescription", "Description cannot exceed 500 characters."));

        return Result.Success();
    }
}
