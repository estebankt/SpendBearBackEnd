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
            categoryResult.Value.Description
        );

        return Result.Success(dto);
    }
}
