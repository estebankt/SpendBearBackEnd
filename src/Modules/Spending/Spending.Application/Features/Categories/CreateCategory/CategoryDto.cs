namespace Spending.Application.Features.Categories.CreateCategory;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemCategory
);
