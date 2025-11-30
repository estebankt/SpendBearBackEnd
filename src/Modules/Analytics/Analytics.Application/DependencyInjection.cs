using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Analytics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services)
    {
        // No MediatR as per project guidelines.
        // Handlers will be registered directly where needed.

        return services;
    }
}
