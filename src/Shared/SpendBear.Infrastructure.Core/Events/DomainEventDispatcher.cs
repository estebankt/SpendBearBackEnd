using Microsoft.Extensions.DependencyInjection;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();

        // Use the runtime type to resolve handlers, since domainEvent may be
        // typed as IDomainEvent at compile time but be a concrete event at runtime.
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("Handle")!;
            var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
            await task;
        }
    }
}
