using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Extensions;
using StatementImport.Application.Abstractions;
using StatementImport.Domain.Repositories;
using StatementImport.Infrastructure.FileStorage;
using StatementImport.Infrastructure.Persistence;
using StatementImport.Infrastructure.Persistence.Repositories;
using StatementImport.Infrastructure.Services;

namespace StatementImport.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStatementImportInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with retry logic
        services.AddPostgreSqlContext<StatementImportDbContext>(configuration);

        // Register module-specific UnitOfWork
        services.AddScoped<IStatementImportUnitOfWork>(sp => sp.GetRequiredService<StatementImportDbContext>());

        // Register repositories
        services.AddScoped<IStatementUploadRepository, StatementUploadRepository>();

        // Register services
        services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
        services.AddScoped<IStatementParsingService, OpenAiStatementParsingService>();
        services.AddScoped<ICategoryProvider, SpendingCategoryProvider>();
        services.AddScoped<ITransactionCreationService, SpendingTransactionCreationService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
