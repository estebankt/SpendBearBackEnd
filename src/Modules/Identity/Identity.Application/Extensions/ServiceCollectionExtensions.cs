using FluentValidation;
using Identity.Application.Features.GetProfile;
using Identity.Application.Features.RegisterUser;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(RegisterUserValidator).Assembly);
        
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<GetProfileHandler>();
        
        return services;
    }
}
