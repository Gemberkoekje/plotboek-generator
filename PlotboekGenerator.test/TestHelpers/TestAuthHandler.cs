using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// Authentication handler that always succeeds and produces claims
/// matching the seeded test user: an SL who is also an admin, unless the request's
/// <see cref="RolesHeader"/> names other roles.
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme the integration factories register this handler under.</summary>
    public const string SchemeName = "TestScheme";

    /// <summary>The id of the signed-in test user; specs seed a user with it.</summary>
    public const string UserId = "00000000-0000-0000-0000-000000000001";

    /// <summary>The name of the signed-in test user.</summary>
    public const string UserName = "TestSL";

    /// <summary>A request header that replaces the user's roles, comma-separated, for specs about another role.</summary>
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var roles = Request.Headers.TryGetValue(RolesHeader, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [UserRoles.User, UserRoles.Admin];
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(ClaimTypes.Name, UserName),
        }
        .Concat(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
