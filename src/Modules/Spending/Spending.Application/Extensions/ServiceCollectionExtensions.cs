using Microsoft.Extensions.DependencyInjection;
using Spending.Application.Features.Categories.CreateCategory;
using Spending.Application.Features.Categories.GetCategories;
using Spending.Application.Features.Transactions.CreateTransaction;
using Spending.Application.Features.Transactions.UpdateTransaction;
using Spending.Application.Features.Transactions.DeleteTransaction;
using Spending.Application.Features.Transactions.GetTransactions;

namespace Spending.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpendingApplication(this IServiceCollection services)
    {
        // Register transaction handlers
        services.AddScoped<CreateTransactionHandler>();
        services.AddScoped<UpdateTransactionHandler>();
        services.AddScoped<DeleteTransactionHandler>();
        services.AddScoped<GetTransactionsHandler>();

        // Register category handlers
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<GetCategoriesHandler>();

        return services;
    }
}
