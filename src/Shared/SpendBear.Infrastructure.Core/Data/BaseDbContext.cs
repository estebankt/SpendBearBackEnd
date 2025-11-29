using Microsoft.EntityFrameworkCore;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core.Data;

/// <summary>
/// Base DbContext for all module contexts.
/// Provides common functionality like domain event handling and auditing.
/// </summary>
public abstract class BaseDbContext : DbContext
{
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Saves changes and publishes domain events.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Get all aggregate roots with domain events
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        // Collect all domain events before saving
        var domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        // Save changes to database
        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events after successful save
        // Note: In a real implementation, you would publish these to an event bus
        // For now, we just clear them
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore domain events collection
        modelBuilder.Ignore<IDomainEvent>();
    }
}
