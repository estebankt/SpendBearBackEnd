using Microsoft.Extensions.DependencyInjection;
using Spending.Domain.Repositories;
using Spending.Infrastructure.Data.Repositories;

namespace Spending.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        return services;
    }
}
