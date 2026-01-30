using SpendBear.SharedKernel;

namespace Spending.Domain.Entities;

public class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsSystemCategory { get; private set; }

    private Category() { }

    private Category(string name, string? description, Guid userId, bool isSystemCategory = false)
    {
        Name = name;
        Description = description;
        UserId = userId;
        IsSystemCategory = isSystemCategory;
    }

    public static Result<Category> Create(string name, string? description, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(new Error("Category.InvalidName", "Name is required."));

        if (userId == Guid.Empty)
            return Result.Failure<Category>(new Error("Category.InvalidUser", "UserId is required."));

        return Result.Success(new Category(name, description, userId, isSystemCategory: false));
    }

    public static Result<Category> CreateSystemCategory(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(new Error("Category.InvalidName", "Name is required."));

        return Result.Success(new Category(name, description, Guid.Empty, isSystemCategory: true));
    }
}
