using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SpendBear.Infrastructure.Core;
using SpendBear.Infrastructure.Core.Extensions;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Extensions;
using Identity.Application.Extensions;
using Spending.Infrastructure.Data;
using Spending.Infrastructure.Extensions;
using Spending.Application.Extensions;
using Budgets.Infrastructure.Persistence;
using Budgets.Infrastructure;
using Budgets.Application;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure;
using Notifications.Application;
using Analytics.Infrastructure.Persistence;
using Analytics.Infrastructure;
using Analytics.Application;
using StatementImport.Infrastructure.Persistence;
using StatementImport.Infrastructure.Extensions;
using StatementImport.Application.Extensions;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using SpendBear.SharedKernel;
using SpendBear.Infrastructure.Core.Events;
using SpendBear.Api.Middleware;
using SpendBear.Api.Seeding;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Web Application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        })
        .AddApplicationPart(typeof(Spending.Api.Controllers.TransactionsController).Assembly)
        .AddApplicationPart(typeof(Budgets.Api.Controllers.BudgetsController).Assembly)
        .AddApplicationPart(typeof(Identity.Api.Controllers.IdentityController).Assembly)
        .AddApplicationPart(typeof(Notifications.Api.Controllers.NotificationsController).Assembly)
        .AddApplicationPart(typeof(Analytics.Api.Controllers.AnalyticsController).Assembly)
        .AddApplicationPart(typeof(StatementImport.Api.Controllers.StatementImportController).Assembly);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}"; // e.g., "https://dev-abc.us.auth0.com/"
            options.Audience = builder.Configuration["Auth0:Audience"]; // e.g., "https://spendbear.api"
        });
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            if (document.Components == null)
            {
                document.Components = new();
            }

            if (document.Components.SecuritySchemes == null)
            {
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
            }

            document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Input your Bearer token to access this API"
            });

            document.Info = new()
            {
                Title = "SpendBear API",
                Version = "v1",
                Description = "Personal finance management API for tracking transactions, budgets, and analytics"
            };

            return Task.CompletedTask;
        });
    });
    // Infrastructure Core (Event Dispatcher, etc.)
    builder.Services.AddInfrastructureCore();

    builder.Services.AddPostgreSqlContext<IdentityDbContext>(builder.Configuration);
    builder.Services.AddIdentityInfrastructure();
    builder.Services.AddIdentityApplication();

    builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowFrontend",
          policy =>
          {
              policy.SetIsOriginAllowed(origin => true) // Allow any origin
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
          });
  });

    // Infrastructure Core
    builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();


    // Spending Module
    builder.Services.AddSpendingInfrastructure(builder.Configuration);
    builder.Services.AddSpendingApplication();


    // Budgets Module
    builder.Services.AddBudgetsInfrastructure(builder.Configuration);
    builder.Services.AddBudgetsApplication();

    // Notifications Module
    builder.Services.AddNotificationsInfrastructure(builder.Configuration);
    builder.Services.AddNotificationsApplication();

    // Analytics Module
    builder.Services.AddAnalyticsInfrastructure(builder.Configuration);
    builder.Services.AddAnalyticsApplication();

    // StatementImport Module
    builder.Services.AddStatementImportInfrastructure(builder.Configuration);
    builder.Services.AddStatementImportApplication();

    var app = builder.Build();

    // Global exception handler - must be first to catch all exceptions
    app.UseGlobalExceptionHandler();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapGet("/", () => Results.Redirect("/scalar"));
    }



    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("AllowFrontend");


    // Apply database migrations on startup
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                Log.Information("Applying database migrations...");
                await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
                await services.GetRequiredService<SpendingDbContext>().Database.MigrateAsync();
                await services.GetRequiredService<BudgetsDbContext>().Database.MigrateAsync();
                await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
                await services.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();
                await services.GetRequiredService<StatementImportDbContext>().Database.MigrateAsync();
                Log.Information("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred while migrating the database.");
                throw;
            }
        }
    }


    // Development-only: Seed test data and add test user when no auth token present
    if (app.Environment.IsDevelopment())
    {
        await DevelopmentDataSeeder.SeedAsync(
            app.Configuration,
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder"));

        app.UseMiddleware<DevelopmentAuthMiddleware>();
    }

    app.UseAuthentication();
    app.UseMiddleware<UserResolutionMiddleware>();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint for deployment smoke tests
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
