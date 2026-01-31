using Budgets.Application.Abstractions;
using Budgets.Domain.Repositories;
using Budgets.Infrastructure.Persistence;
using Budgets.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Extensions;

namespace Budgets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with retry logic
        services.AddPostgreSqlContext<BudgetsDbContext>(configuration);

        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetsUnitOfWork, UnitOfWork>();

        return services;
    }
}
