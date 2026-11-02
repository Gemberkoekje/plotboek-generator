using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// The default <see cref="IRoleResolver"/>: the e-mail addresses under <c>Authentication:Admins</c>
/// are Admin, and a provider with a <see cref="OidcProviderSettings.RoleClaim"/> grants the highest
/// role whose value under <c>Authentication:Roles:{Admin|User|Reader|NPC}</c> appears in that claim.
/// A sign-in that earns no role that way gets <c>Authentication:DefaultRole</c>.
/// </summary>
/// <remarks>
/// The role claim is looked for among the login's claims first, then as a top-level claim in its
/// id_token and access_token: providers differ in where they put roles, and an OpenID Connect
/// handler does not copy every token claim onto the identity.
/// </remarks>
public sealed class ClaimRoleResolver(IConfiguration configuration) : IRoleResolver
{
    /// <summary>The configuration key of the role a sign-in gets when nothing grants it one.</summary>
    public const string DefaultRoleKey = "Authentication:DefaultRole";

    private static readonly string[] GrantableRoles = [UserRoles.Admin, UserRoles.User, UserRoles.Reader, UserRoles.NPC];

    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly string? _defaultRole = ReadDefaultRole(configuration);

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
            return _defaultRole;
        }

        var granted = RoleClaimValues(login, provider.RoleClaim);
        foreach (var role in GrantableRoles)
        {
            if (ReadList($"Authentication:Roles:{role}").Exists(granted.Contains))
            {
                return role;
            }
        }

        return _defaultRole ?? UserRoles.Guest;
    }

    /// <summary>
    /// Reads <c>Authentication:DefaultRole</c>: one of the <see cref="UserRoles"/> but Admin, written
    /// in any case. Left empty, a provider without a role claim says nothing about roles and one with
    /// a role claim that grants nothing makes the user a Guest, as before the setting existed.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The default role, or <c>null</c> when none is configured.</returns>
    /// <exception cref="InvalidOperationException">The value is not a role, or it is Admin.</exception>
    public static string? ReadDefaultRole(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = configuration[DefaultRoleKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Admin for everyone who signs in is never what a deployment means: admins are listed by
        // address under Authentication:Admins.
        var allowed = UserRoles.All.Where(role => role != UserRoles.Admin).ToList();
        return allowed.Find(role => string.Equals(role, value.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"{DefaultRoleKey} is '{value}'. Use one of: {string.Join(", ", allowed)}. Admins are listed under Authentication:Admins.");
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
