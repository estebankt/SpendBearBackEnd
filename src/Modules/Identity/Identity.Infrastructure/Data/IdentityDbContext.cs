using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core.Data;
using SpendBear.SharedKernel;

namespace Identity.Infrastructure.Data;

public class IdentityDbContext : BaseDbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Apply configurations specific to the Identity module here
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
