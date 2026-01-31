using Microsoft.EntityFrameworkCore;
using Analytics.Domain.Entities;
using SpendBear.Infrastructure.Core.Data;
using Analytics.Infrastructure.Persistence.Configurations;

namespace Analytics.Infrastructure.Persistence;

public sealed class AnalyticsDbContext : BaseDbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
        : base(options)
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
