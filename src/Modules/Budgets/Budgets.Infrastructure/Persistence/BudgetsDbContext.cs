using Budgets.Domain.Entities;
using Budgets.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Budgets.Infrastructure.Persistence;

public sealed class BudgetsDbContext : DbContext
{
    public BudgetsDbContext(DbContextOptions<BudgetsDbContext> options) : base(options)
    {
    }

    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("budgets");
        modelBuilder.ApplyConfiguration(new BudgetConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
