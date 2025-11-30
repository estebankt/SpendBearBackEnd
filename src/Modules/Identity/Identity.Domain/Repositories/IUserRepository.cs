using SpendBear.SharedKernel;
using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByAuth0IdAsync(string auth0Id, CancellationToken cancellationToken = default);
}
