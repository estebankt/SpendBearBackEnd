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

        // Fallback to sub claim (used in client credentials flow)
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(subClaim))
        {
            // For client credentials tokens, sub is like "G2adFM5N3DLBhsJMT7Yj3jtxdECgNwJU@clients"
            // We'll use a deterministic GUID based on the sub claim for testing
            // In production, you'd want actual user registration flow

            // Try to parse as GUID first (in case it's already a GUID)
            if (Guid.TryParse(subClaim, out var subGuid))
            {
                return subGuid;
            }

            // For client credentials, create a deterministic test user ID
            // This allows testing with machine-to-machine tokens
            return CreateDeterministicGuid(subClaim);
        }

        return null;
    }

    /// <summary>
    /// Creates a deterministic GUID from a string (for testing purposes)
    /// </summary>
    private static Guid CreateDeterministicGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
