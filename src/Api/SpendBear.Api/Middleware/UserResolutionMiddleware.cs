using System.Security.Claims;
using Identity.Domain.Repositories;

namespace SpendBear.Api.Middleware;

public class UserResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public UserResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if user is not authenticated
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Check if we already have a valid user_id claim (e.g. from DevelopmentAuthMiddleware)
        var existingUserId = context.User.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(existingUserId) && Guid.TryParse(existingUserId, out _))
        {
            await _next(context);
            return;
        }

        // Extract Auth0 ID from sub or NameIdentifier
        var auth0Id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                      ?? context.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(auth0Id))
        {
            // Resolve IUserRepository from the scoped service provider
            // We use context.RequestServices because IUserRepository is likely scoped (EF Core)
            var userRepository = context.RequestServices.GetService<IUserRepository>();

            if (userRepository != null)
            {
                var user = await userRepository.GetByAuth0IdAsync(auth0Id);
                if (user != null)
                {
                    // Add the user_id claim with the internal GUID
                    var identity = context.User.Identity as ClaimsIdentity;
                    identity?.AddClaim(new Claim("user_id", user.Id.ToString()));
                }
            }
        }

        await _next(context);
    }
}
