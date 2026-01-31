namespace SpendBear.SharedKernel;

/// <summary>
/// Base class for all domain events.
/// Domain events capture the occurrence of something meaningful in the domain.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime OccurredOn { get; }

    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    protected DomainEvent(Guid eventId, DateTime occurredOn)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
    }
}
