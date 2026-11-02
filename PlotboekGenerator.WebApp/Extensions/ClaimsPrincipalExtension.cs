using System.Security.Claims;

namespace PlotboekGenerator.WebApp.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ClaimsPrincipal"/> to retrieve user information from the database.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Retrieves the current authenticated <see cref="PlotboekUser"/> from the database session based on the claims principal.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current authenticated user.</param>
    /// <param name="session">The <see cref="IDocumentSession"/> used to query users from the database.</param>
    /// <returns>
    /// The <see cref="PlotboekUser"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public static async Task<PlotboekUser?> GetUserAsync(this ClaimsPrincipal user, IDocumentSession session, CancellationToken ct = default)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
            return null;

        // Try to get the user ID claim (this should be the PlotboekUser.Id from Identity)
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        // If the NameIdentifier is a GUID (from ASP.NET Core Identity), use it directly
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return await session.SingleOrDefaultAsync<PlotboekUser>(u => u.Id == userId.ToString(), ct);
        }

        // If not a GUID, it might be an external provider ID
        // In this case, we need to find the user by email or other claims
        var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(emailClaim))
        {
            return await session.SingleOrDefaultAsync<PlotboekUser>(u => u.Email == emailClaim, ct);
        }

        return null;
    }

    /// <summary>
    /// Gets the user's email from claims.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current authenticated user.</param>
    /// <returns>The user's email if found; otherwise, <c>null</c>.</returns>
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Gets the user's name from claims.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current authenticated user.</param>
    /// <returns>The user's name if found; otherwise, <c>null</c>.</returns>
    public static string? GetName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Gets the external login provider name.
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> representing the current authenticated user.</param>
    /// <returns>The login provider name if found; otherwise, <c>null</c>.</returns>
    public static string? GetLoginProvider(this ClaimsPrincipal user)
    {
        return user.FindFirst("LoginProvider")?.Value;
    }
}
