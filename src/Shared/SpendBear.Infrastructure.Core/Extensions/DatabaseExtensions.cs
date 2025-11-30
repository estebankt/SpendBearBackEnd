using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SpendBear.Infrastructure.Core.Extensions;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds PostgreSQL database context with standard configuration.
    /// </summary>
    public static IServiceCollection AddPostgreSqlContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                npgsqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("Logging:EnableDetailedErrors"))
            {
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Configures database with health checks.
    /// </summary>
    public static IServiceCollection AddDatabaseHealthChecks<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TContext>(
                name: $"{typeof(TContext).Name}-health",
                tags: new[] { "database", "ready" });

        return services;
    }
}
