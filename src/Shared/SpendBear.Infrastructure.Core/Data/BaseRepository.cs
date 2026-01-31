using Microsoft.EntityFrameworkCore;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core.Data;

/// <summary>
/// Base repository implementation for aggregate roots.
/// </summary>
public abstract class BaseRepository<TAggregate> : IRepository<TAggregate>
    where TAggregate : AggregateRoot
{
    protected readonly DbContext Context;
    protected readonly DbSet<TAggregate> DbSet;

    protected BaseRepository(DbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TAggregate>();
    }

    public IUnitOfWork UnitOfWork => (IUnitOfWork)Context;

    public virtual async Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(aggregate, cancellationToken);
    }

    public virtual Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbSet.Update(aggregate);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(aggregate);
        return Task.CompletedTask;
    }
}
