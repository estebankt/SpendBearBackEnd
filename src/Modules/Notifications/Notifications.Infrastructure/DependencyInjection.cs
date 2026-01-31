using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Services;
using Notifications.Domain.Repositories;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Persistence.Repositories;
using Notifications.Infrastructure.Services;
using SendGrid.Extensions.DependencyInjection;
using SpendBear.Infrastructure.Core.Extensions;
using SpendBear.SharedKernel;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with retry logic and custom migrations schema
        services.AddPostgreSqlContext<NotificationsDbContext>(
            configuration,
            migrationsHistoryTableSchema: "notifications");

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var sendGridApiKey = configuration["SendGrid:ApiKey"];
        if (!string.IsNullOrEmpty(sendGridApiKey))
        {
            services.AddSendGrid(options =>
            {
                options.ApiKey = sendGridApiKey;
            });
            services.AddScoped<IEmailService, SendGridEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, FakeEmailService>();
        }

        return services;
    }
}
