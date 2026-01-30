using System.Security.Claims;

namespace SpendBear.Api.Middleware;

/// <summary>
/// Development-only middleware that adds a test user claim when no authentication is present.
/// This allows testing API endpoints without Auth0 tokens in development.
/// NEVER use this in production!
/// </summary>
public class DevelopmentAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DevelopmentAuthMiddleware> _logger;

    public DevelopmentAuthMiddleware(RequestDelegate next, ILogger<DevelopmentAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply in development when no authorization header is present
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var testUserId = "00000000-0000-0000-0000-000000000001"; // Fixed test user ID

            var claims = new List<Claim>
            {
                new Claim("user_id", testUserId),
                new Claim(ClaimTypes.NameIdentifier, testUserId),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@spendbear.com")
            };

            var identity = new ClaimsIdentity(claims, "DevelopmentAuth");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;

            _logger.LogWarning("Development mode: Using test user ID {UserId}", testUserId);
        }

        await _next(context);
    }
}
