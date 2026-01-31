using Budgets.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Features.Commands.MarkNotificationAsRead;
using Notifications.Application.Features.EventHandlers;
using Notifications.Application.Features.Queries.GetNotifications;
using SpendBear.SharedKernel;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        // Register event handlers with IEventHandler<T> interface
        services.AddScoped<IEventHandler<BudgetWarningEvent>, BudgetWarningEventHandler>();
        services.AddScoped<IEventHandler<BudgetExceededEvent>, BudgetExceededEventHandler>();

        // Register query and command handlers
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();

        return services;
    }
}
