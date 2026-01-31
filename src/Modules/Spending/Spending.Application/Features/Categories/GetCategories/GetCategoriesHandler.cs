using SpendBear.SharedKernel;
using Spending.Domain.Repositories;
using Spending.Application.Features.Categories.CreateCategory;

namespace Spending.Application.Features.Categories.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoriesHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<List<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAvailableCategoriesForUserAsync(userId, cancellationToken);

        var dtos = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.IsSystemCategory
        )).ToList();

        return Result.Success(dtos);
    }
}
