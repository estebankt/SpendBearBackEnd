using Microsoft.Extensions.DependencyInjection;

namespace Notifications.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
