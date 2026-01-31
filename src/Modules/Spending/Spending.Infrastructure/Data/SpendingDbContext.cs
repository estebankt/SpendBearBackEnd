using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core.Data;
using Spending.Application.Abstractions;

namespace Spending.Infrastructure.Data;

public class SpendingDbContext : BaseDbContext, ISpendingUnitOfWork
{
    public SpendingDbContext(DbContextOptions<SpendingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
