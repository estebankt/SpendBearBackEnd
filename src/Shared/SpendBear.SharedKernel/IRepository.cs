namespace SpendBear.SharedKernel;

/// <summary>
/// Base repository interface for aggregate roots.
/// Repositories provide collection-like access to aggregates.
/// </summary>
public interface IRepository<TAggregate> where TAggregate : AggregateRoot
{
    /// <summary>
    /// Gets the Unit of Work this repository belongs to.
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Gets an aggregate by its identifier.
    /// </summary>
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate.
    /// </summary>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an aggregate from the repository.
    /// </summary>
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
