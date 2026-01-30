using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Analytics.Application.Features.EventHandlers;
using SpendBear.SharedKernel;
using Spending.Domain.Events;

namespace Analytics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        // No MediatR as per project guidelines.
        // Handlers will be registered directly where needed.
        services.AddScoped<IEventHandler<TransactionCreatedEvent>, TransactionCreatedEventHandler>();
        services.AddScoped<IEventHandler<TransactionUpdatedEvent>, TransactionUpdatedEventHandler>();
        services.AddScoped<IEventHandler<TransactionDeletedEvent>, TransactionDeletedEventHandler>();
        
        services.AddScoped<Features.Queries.GetMonthlySummary.GetMonthlySummaryHandler>();

        return services;
    }
}
