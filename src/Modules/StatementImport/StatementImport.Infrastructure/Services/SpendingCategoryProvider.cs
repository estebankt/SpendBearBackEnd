using Spending.Domain.Repositories;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.Services;

public class SpendingCategoryProvider : ICategoryProvider
{
    private readonly ICategoryRepository _categoryRepository;

    public SpendingCategoryProvider(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<CategoryInfo>> GetAvailableCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAvailableCategoriesForUserAsync(userId, cancellationToken);
        return categories.Select(c => new CategoryInfo(c.Id, c.Name, c.Description)).ToList();
    }

    public async Task<Guid?> GetCategoryIdByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAvailableCategoriesForUserAsync(userId, cancellationToken);
        var match = categories.FirstOrDefault(c =>
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match?.Id;
    }
}
