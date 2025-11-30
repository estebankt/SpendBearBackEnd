using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Features.Commands.MarkNotificationAsRead;
using Notifications.Application.Features.EventHandlers;
using Notifications.Application.Features.Queries.GetNotifications;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<BudgetWarningEventHandler>();
        services.AddScoped<BudgetExceededEventHandler>();
        services.AddScoped<GetNotificationsHandler>();
        services.AddScoped<MarkNotificationAsReadHandler>();

        return services;
    }
}
