using Microsoft.AspNetCore.Authentication.JwtBearer;
using SpendBear.Infrastructure.Core.Extensions;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Extensions;
using Identity.Application.Extensions;
using Spending.Infrastructure.Extensions;
using Spending.Application.Extensions;
using Budgets.Infrastructure;
using Budgets.Application;
using Serilog;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

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

    builder.Services.AddControllers();

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

            return Task.CompletedTask;
        });
    });
    builder.Services.AddPostgreSqlContext<IdentityDbContext>(builder.Configuration);
    builder.Services.AddIdentityInfrastructure();
    builder.Services.AddIdentityApplication();

    // Spending Module
    builder.Services.AddSpendingInfrastructure(builder.Configuration);
    builder.Services.AddSpendingApplication();

    // Budgets Module
    builder.Services.AddBudgetsInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);
    builder.Services.AddBudgetsApplication();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

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
