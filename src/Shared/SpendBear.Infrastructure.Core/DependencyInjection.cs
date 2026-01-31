using Microsoft.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Events;
using SpendBear.SharedKernel; // For IDomainEventDispatcher

namespace SpendBear.Infrastructure.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
