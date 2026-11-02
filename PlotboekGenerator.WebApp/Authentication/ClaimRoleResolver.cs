using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// The default <see cref="IRoleResolver"/>: the e-mail addresses under <c>Authentication:Admins</c>
/// are Admin, and a provider with a <see cref="OidcProviderSettings.RoleClaim"/> grants the highest
/// role whose value under <c>Authentication:Roles:{Admin|User|NPC}</c> appears in that claim.
/// </summary>
/// <remarks>
/// The role claim is looked for among the login's claims first, then as a top-level claim in its
/// id_token and access_token: providers differ in where they put roles, and an OpenID Connect
/// handler does not copy every token claim onto the identity.
/// </remarks>
public sealed class ClaimRoleResolver(IConfiguration configuration) : IRoleResolver
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <inheritdoc />
    public string? ResolveRole(ExternalLoginInfo login)
    {
        ArgumentNullException.ThrowIfNull(login);

        var email = login.Principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email)
            && ReadList("Authentication:Admins").Contains(email, StringComparer.OrdinalIgnoreCase))
        {
            return UserRoles.Admin;
        }

        var provider = OidcProviderSettings.ReadAll(_configuration)
            .FirstOrDefault(p => string.Equals(p.Scheme, login.LoginProvider, StringComparison.Ordinal));
        if (provider is null || string.IsNullOrWhiteSpace(provider.RoleClaim))
        {
            return null;
        }

        var granted = RoleClaimValues(login, provider.RoleClaim);
        foreach (var role in new[] { UserRoles.Admin, UserRoles.User, UserRoles.NPC })
        {
            if (ReadList($"Authentication:Roles:{role}").Exists(granted.Contains))
            {
                return role;
            }
        }

        return UserRoles.Guest;
    }

    private static HashSet<string> RoleClaimValues(ExternalLoginInfo login, string roleClaim)
    {
        var values = new HashSet<string>(
            login.Principal.FindAll(roleClaim).Select(claim => claim.Value),
            StringComparer.Ordinal);

        foreach (var tokenName in new[] { "id_token", "access_token" })
        {
            var token = login.AuthenticationTokens?.FirstOrDefault(t => t.Name == tokenName)?.Value;
            AddTopLevelClaim(values, JwtPayload.Parse(token), roleClaim);
        }

        return values;
    }

    private static void AddTopLevelClaim(HashSet<string> values, JsonDocument? payload, string claim)
    {
        using (payload)
        {
            if (payload is null || !payload.RootElement.TryGetProperty(claim, out var element))
            {
                return;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                values.Add(element.GetString()!);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String))
                {
                    values.Add(item.GetString()!);
                }
            }
        }
    }

    /// <summary>
    /// Reads a list that is either a configuration array (<c>Key:0</c>, <c>Key:1</c>, …) or one value
    /// separated by commas or semicolons, the form an environment variable takes.
    /// </summary>
    private List<string> ReadList(string key)
    {
        var children = _configuration.GetSection(key).GetChildren().Select(child => child.Value);
        var joined = (_configuration[key] ?? string.Empty).Split([',', ';']);

        return [.. children.Concat(joined)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())];
    }
}
