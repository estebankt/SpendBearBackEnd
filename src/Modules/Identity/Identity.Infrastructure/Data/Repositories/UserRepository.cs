using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core.Data;

namespace Identity.Infrastructure.Data.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default)
    {
        return !await DbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByAuth0IdAsync(string auth0Id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id, cancellationToken);
    }
}
