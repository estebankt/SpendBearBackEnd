using System.Security.Claims;

namespace SpendBear.SharedKernel.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts user ID from JWT token claims.
    /// Supports both user tokens (user_id claim) and client credentials tokens (sub claim).
    /// For testing with Auth0 client credentials flow, uses the sub claim as user ID.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        // First try to get user_id claim (standard user token)
        var userIdClaim = user.FindFirst("user_id")?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // Fallback to sub claim (used in client credentials flow or non-Auth0 tokens)
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
        {
            // Try to parse as GUID first (in case it's already a GUID)
            if (Guid.TryParse(subClaim, out var subGuid))
            {
                return subGuid;
            }
        }

        return null;
    }
}
