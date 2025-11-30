using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Analytics.Infrastructure.Persistence;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence.Repositories;
using SpendBear.SharedKernel; // For IUnitOfWork if needed by consumers

namespace Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "analytics"))); // Configure migrations history table schema

        services.AddScoped<IAnalyticSnapshotRepository, AnalyticSnapshotRepository>();

        return services;
    }
}
