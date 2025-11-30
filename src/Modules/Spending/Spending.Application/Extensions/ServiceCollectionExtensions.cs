using Microsoft.Extensions.DependencyInjection;
using Spending.Application.Features.Categories.CreateCategory;
using Spending.Application.Features.Transactions.CreateTransaction;
using Spending.Application.Features.Transactions.GetTransactions;

namespace Spending.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendingApplication(this IServiceCollection services)
    {
        // Register handlers
        services.AddScoped<CreateTransactionHandler>();
        services.AddScoped<GetTransactionsHandler>();
        services.AddScoped<CreateCategoryHandler>();

        return services;
    }
}
