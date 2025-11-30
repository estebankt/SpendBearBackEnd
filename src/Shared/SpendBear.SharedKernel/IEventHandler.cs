namespace SpendBear.SharedKernel;

/// <summary>
/// Marker interface for domain event handlers.
/// </summary>
/// <typeparam name="TDomainEvent"></typeparam>
public interface IEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
