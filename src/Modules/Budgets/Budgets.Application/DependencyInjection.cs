using Budgets.Application.Features.Budgets.CreateBudget;
using Budgets.Application.Features.Budgets.DeleteBudget;
using Budgets.Application.Features.Budgets.GetBudgets;
using Budgets.Application.Features.Budgets.UpdateBudget;
using Budgets.Application.Features.EventHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Budgets.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetsApplication(this IServiceCollection services)
    {
        // Register handlers
        services.AddScoped<CreateBudgetHandler>();
        services.AddScoped<GetBudgetsHandler>();
        services.AddScoped<UpdateBudgetHandler>();
        services.AddScoped<DeleteBudgetHandler>();
        services.AddScoped<TransactionCreatedEventHandler>();

        return services;
    }
}
