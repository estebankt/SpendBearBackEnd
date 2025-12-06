using Microsoft.AspNetCore.Authentication.JwtBearer;
using SpendBear.Infrastructure.Core;
using SpendBear.Infrastructure.Core.Extensions;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Extensions;
using Identity.Application.Extensions;
using Spending.Infrastructure.Extensions;
using Spending.Application.Extensions;
using Budgets.Infrastructure;
using Budgets.Application;
using Notifications.Infrastructure;
using Notifications.Application;
using Analytics.Infrastructure;
using Analytics.Application;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using SpendBear.SharedKernel;
using SpendBear.Infrastructure.Core.Events;
using SpendBear.Api.Middleware;

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
        .AddApplicationPart(typeof(Spending.Api.Controllers.TransactionsController).Assembly)
        .AddApplicationPart(typeof(Budgets.Api.Controllers.BudgetsController).Assembly)
        .AddApplicationPart(typeof(Identity.Api.Controllers.IdentityController).Assembly)
        .AddApplicationPart(typeof(Notifications.Api.Controllers.NotificationsController).Assembly)
        .AddApplicationPart(typeof(Analytics.Api.Controllers.AnalyticsController).Assembly);

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
              policy.WithOrigins("http://localhost:3000")
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



    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");

    // Development-only: Add test user when no auth token present
    if (app.Environment.IsDevelopment())
    {
        app.UseMiddleware<SpendBear.Api.Middleware.DevelopmentAuthMiddleware>();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
