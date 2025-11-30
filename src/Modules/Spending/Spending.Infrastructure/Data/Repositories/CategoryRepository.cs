using Microsoft.EntityFrameworkCore;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;

namespace Spending.Infrastructure.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly SpendingDbContext _context;

    public CategoryRepository(SpendingDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Name == name && c.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Set<Category>().AddAsync(category, cancellationToken);
    }

    public async Task<List<Category>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Category>()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
