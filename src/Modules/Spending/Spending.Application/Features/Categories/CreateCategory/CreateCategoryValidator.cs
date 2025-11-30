using SpendBear.SharedKernel;

namespace Spending.Application.Features.Categories.CreateCategory;

public static class CreateCategoryValidator
{
    public static Result Validate(CreateCategoryCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result.Failure(new Error("CreateCategory.InvalidName", "Category name is required."));

        if (command.Name.Length > 100)
            return Result.Failure(new Error("CreateCategory.InvalidName", "Category name cannot exceed 100 characters."));

        if (command.Description?.Length > 500)
            return Result.Failure(new Error("CreateCategory.InvalidDescription", "Description cannot exceed 500 characters."));

        return Result.Success();
    }
}
