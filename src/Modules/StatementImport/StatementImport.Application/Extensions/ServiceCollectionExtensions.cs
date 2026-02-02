using Microsoft.Extensions.DependencyInjection;
using StatementImport.Application.Features.CancelImport;
using StatementImport.Application.Features.ConfirmImport;
using StatementImport.Application.Features.GetPendingImport;
using StatementImport.Application.Features.GetUserImports;
using StatementImport.Application.Features.UpdateParsedTransactions;
using StatementImport.Application.Features.UploadStatement;

namespace StatementImport.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStatementImportApplication(this IServiceCollection services)
    {
        services.AddScoped<UploadStatementHandler>();
        services.AddScoped<GetPendingImportHandler>();
        services.AddScoped<UpdateParsedTransactionsHandler>();
        services.AddScoped<ConfirmImportHandler>();
        services.AddScoped<CancelImportHandler>();
        services.AddScoped<GetUserImportsHandler>();

        return services;
    }
}
