using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Analytics.Application.Abstractions;
using Analytics.Infrastructure.Persistence;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence.Repositories;
using SpendBear.Infrastructure.Core.Extensions;

namespace Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with retry logic and custom migrations schema
        services.AddPostgreSqlContext<AnalyticsDbContext>(
            configuration,
            migrationsHistoryTableSchema: "analytics");

        services.AddScoped<IAnalyticsUnitOfWork>(sp => sp.GetRequiredService<AnalyticsDbContext>());
        services.AddScoped<IAnalyticSnapshotRepository, AnalyticSnapshotRepository>();

        return services;
    }
}
