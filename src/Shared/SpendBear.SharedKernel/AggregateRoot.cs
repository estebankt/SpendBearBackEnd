namespace SpendBear.SharedKernel;

/// <summary>
/// Base class for aggregate roots in the domain model.
/// An aggregate root is the entry point to an aggregate and enforces consistency boundaries.
/// It collects domain events that are published when the aggregate is persisted.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    /// <summary>
    /// Raises a domain event. The event will be collected and published after persistence.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events. Called after events have been published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
