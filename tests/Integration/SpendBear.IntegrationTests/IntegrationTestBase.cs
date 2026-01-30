using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace SpendBear.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    protected WebApplicationFactory<Program> Factory = null!;
    protected HttpClient Client = null!;
    protected IServiceProvider Services = null!;

    public virtual async Task InitializeAsync()
    {
        // Start PostgreSQL container
        await _postgresContainer.StartAsync();

        var connectionString = _postgresContainer.GetConnectionString();

        // Create web application factory
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override connection string with test container
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connectionString
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    // Ensure event dispatcher is registered
                    services.AddSingleton<SpendBear.SharedKernel.IDomainEventDispatcher, SpendBear.Infrastructure.Core.Events.DomainEventDispatcher>();

                    // Remove existing DbContext registrations and re-add with test connection string
                    RemoveAndRegisterDbContext<Spending.Infrastructure.Data.SpendingDbContext>(services, connectionString, "spending");
                    RemoveAndRegisterDbContext<Budgets.Infrastructure.Persistence.BudgetsDbContext>(services, connectionString, "budgets");
                    RemoveAndRegisterDbContext<Identity.Infrastructure.Data.IdentityDbContext>(services, connectionString, "identity");
                    RemoveAndRegisterDbContext<Notifications.Infrastructure.Persistence.NotificationsDbContext>(services, connectionString, "notifications");
                    RemoveAndRegisterDbContext<Analytics.Infrastructure.Persistence.AnalyticsDbContext>(services, connectionString, "analytics");
                });
            });

        Client = Factory.CreateClient();
        Services = Factory.Services;

        // Apply migrations
        await ApplyMigrationsAsync();
    }

    private static void RemoveAndRegisterDbContext<TContext>(IServiceCollection services, string connectionString, string schema)
        where TContext : DbContext
    {
        // Remove existing registrations
        services.RemoveAll(typeof(DbContextOptions<TContext>));
        services.RemoveAll(typeof(TContext));

        // Register with test connection string
        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
    }

    private async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();

        // Apply migrations for each module
        var spendingDb = scope.ServiceProvider.GetRequiredService<Spending.Infrastructure.Data.SpendingDbContext>();
        await spendingDb.Database.MigrateAsync();

        var budgetsDb = scope.ServiceProvider.GetRequiredService<Budgets.Infrastructure.Persistence.BudgetsDbContext>();
        await budgetsDb.Database.MigrateAsync();

        var identityDb = scope.ServiceProvider.GetRequiredService<Identity.Infrastructure.Data.IdentityDbContext>();
        await identityDb.Database.MigrateAsync();

        var notificationsDb = scope.ServiceProvider.GetRequiredService<Notifications.Infrastructure.Persistence.NotificationsDbContext>();
        await notificationsDb.Database.MigrateAsync();

        var analyticsDb = scope.ServiceProvider.GetRequiredService<Analytics.Infrastructure.Persistence.AnalyticsDbContext>();
        await analyticsDb.Database.MigrateAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await Factory.DisposeAsync();
        Client.Dispose();
    }
}
