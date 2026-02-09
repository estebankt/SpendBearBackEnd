using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Events;
using SpendBear.Infrastructure.Core.Outbox;
using SpendBear.SharedKernel;

namespace SpendBear.Infrastructure.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.Configure<OutboxProcessorOptions>(configuration.GetSection("Outbox"));
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
