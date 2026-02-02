namespace StatementImport.Application.Abstractions;

public interface ICategoryProvider
{
    Task<List<CategoryInfo>> GetAvailableCategoriesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> GetCategoryIdByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default);
}
