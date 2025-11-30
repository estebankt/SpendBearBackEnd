namespace Spending.Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description
);
