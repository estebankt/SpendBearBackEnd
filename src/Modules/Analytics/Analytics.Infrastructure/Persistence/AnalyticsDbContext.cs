using Microsoft.EntityFrameworkCore;
using Analytics.Domain.Entities;
using SpendBear.Infrastructure.Core.Data;
using SpendBear.SharedKernel;
using Analytics.Infrastructure.Persistence.Configurations;
using Analytics.Application.Abstractions;

namespace Analytics.Infrastructure.Persistence;

public sealed class AnalyticsDbContext : BaseDbContext, IAnalyticsUnitOfWork
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options, domainEventDispatcher)
    {
    }

    public DbSet<AnalyticSnapshot> AnalyticSnapshots => Set<AnalyticSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("analytics");
        modelBuilder.ApplyConfiguration(new AnalyticSnapshotConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
