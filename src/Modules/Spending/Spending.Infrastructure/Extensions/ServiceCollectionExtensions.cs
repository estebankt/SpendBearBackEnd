using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Extensions;
using Spending.Application.Abstractions;
using Spending.Domain.Repositories;
using Spending.Infrastructure.Data;
using Spending.Infrastructure.Data.Repositories;

namespace Spending.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with retry logic
        services.AddPostgreSqlContext<SpendingDbContext>(configuration);

        // Register module-specific UnitOfWork
        services.AddScoped<ISpendingUnitOfWork>(sp => sp.GetRequiredService<SpendingDbContext>());

        // Register repositories
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}
