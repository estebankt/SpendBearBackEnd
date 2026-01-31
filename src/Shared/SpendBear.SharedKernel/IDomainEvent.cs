namespace SpendBear.SharedKernel;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something meaningful that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
