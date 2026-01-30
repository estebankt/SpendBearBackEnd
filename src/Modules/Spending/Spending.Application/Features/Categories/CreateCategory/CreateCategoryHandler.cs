using SpendBear.SharedKernel;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;

namespace Spending.Application.Features.Categories.CreateCategory;

public sealed class CreateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> Handle(
        CreateCategoryCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Validate that user is not trying to create a category with system category name
        var isSystemCategory = await _categoryRepository.IsSystemCategoryNameAsync(
            command.Name,
            cancellationToken);

        if (isSystemCategory)
        {
            return Result.Failure<CategoryDto>(new Error(
                "CreateCategory.SystemCategoryExists",
                $"A system category named '{command.Name}' already exists. Please choose a different name."));
        }

        // Check if user already has a category with this name
        var existingCategory = await _categoryRepository.GetByNameAsync(
            command.Name,
            userId,
            cancellationToken);

        if (existingCategory != null && !existingCategory.IsSystemCategory)
        {
            return Result.Failure<CategoryDto>(new Error(
                "CreateCategory.DuplicateName",
                $"You already have a category named '{command.Name}'."));
        }

        // Create Category entity
        var categoryResult = Category.Create(
            command.Name,
            command.Description,
            userId
        );

        if (categoryResult.IsFailure)
            return Result.Failure<CategoryDto>(categoryResult.Error);

        // Add to repository
        await _categoryRepository.AddAsync(categoryResult.Value, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        var dto = new CategoryDto(
            categoryResult.Value.Id,
            categoryResult.Value.Name,
            categoryResult.Value.Description,
            categoryResult.Value.IsSystemCategory
        );

        return Result.Success(dto);
    }
}
