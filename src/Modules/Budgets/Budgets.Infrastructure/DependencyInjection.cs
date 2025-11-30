using Budgets.Domain.Repositories;
using Budgets.Infrastructure.Persistence;
using Budgets.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpendBear.SharedKernel;

namespace Budgets.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetsInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<BudgetsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
