using SpendBear.SharedKernel;

namespace Spending.Application.Features.Transactions.UpdateTransaction;

public static class UpdateTransactionValidator
{
    public static Result Validate(UpdateTransactionCommand command)
    {
        if (command.TransactionId == Guid.Empty)
            return Result.Failure(new Error("UpdateTransaction.InvalidId", "Transaction ID is required."));

        if (command.Amount == 0)
            return Result.Failure(new Error("UpdateTransaction.InvalidAmount", "Amount cannot be zero."));

        if (string.IsNullOrWhiteSpace(command.Currency))
            return Result.Failure(new Error("UpdateTransaction.InvalidCurrency", "Currency is required."));

        if (command.CategoryId == Guid.Empty)
            return Result.Failure(new Error("UpdateTransaction.InvalidCategory", "Category ID is required."));

        if (command.Date > DateTime.UtcNow.AddDays(1))
            return Result.Failure(new Error("UpdateTransaction.InvalidDate", "Transaction date cannot be in the future."));

        if (string.IsNullOrWhiteSpace(command.Description))
            return Result.Failure(new Error("UpdateTransaction.InvalidDescription", "Description is required."));

        if (command.Description.Length > 500)
            return Result.Failure(new Error("UpdateTransaction.InvalidDescription", "Description cannot exceed 500 characters."));

        return Result.Success();
    }
}
