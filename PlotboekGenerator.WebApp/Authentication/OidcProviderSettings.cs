using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// One OpenID Connect sign-in provider, as a deployment configures it under
/// <c>Authentication:Oidc:Providers:{n}</c>.
/// </summary>
public sealed record OidcProviderSettings
{
    /// <summary>The configuration section that lists the providers.</summary>
    public const string SectionKey = "Authentication:Oidc:Providers";

    /// <summary>
    /// The authentication scheme, which is also the provider name stored with every external login.
    /// Changing it unlinks every account that signed in with it.
    /// </summary>
    required public string Scheme { get; init; }

    /// <summary>The name on the login button.</summary>
    required public string DisplayName { get; init; }

    /// <summary>The issuer URL; its discovery document is read from here.</summary>
    required public string Authority { get; init; }

    /// <summary>The client registered at the provider.</summary>
    required public string ClientId { get; init; }

    /// <summary>The client secret, for a confidential client; empty for a public (PKCE) client.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>The scopes asked for.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];

    /// <summary>
    /// Where the provider sends the browser back, below the path base. Register the full URI at the
    /// provider. Every provider needs its own.
    /// </summary>
    required public string CallbackPath { get; init; }

    /// <summary>Where the provider sends the browser back after signing out, below the path base.</summary>
    required public string SignedOutCallbackPath { get; init; }

    /// <summary>
    /// The claim that carries the user's roles at this provider, read by
    /// <see cref="ClaimRoleResolver"/>; empty when the provider grants no roles.
    /// </summary>
    public string RoleClaim { get; init; } = string.Empty;

    /// <summary>
    /// Reads every provider listed under <see cref="SectionKey"/>.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The providers in configuration order; empty when none are listed.</returns>
    /// <exception cref="InvalidOperationException">
    /// When a listed provider misses its scheme, authority or client id, or two share a scheme: a
    /// half-filled entry is a paste that went wrong, not a provider switched off.
    /// </exception>
    public static IReadOnlyList<OidcProviderSettings> ReadAll(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var providers = new List<OidcProviderSettings>();
        foreach (var entry in configuration.GetSection(SectionKey).GetChildren())
        {
            var scheme = Required(entry, "Scheme");
            if (providers.Exists(p => string.Equals(p.Scheme, scheme, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Two sign-in providers under {SectionKey} share the scheme '{scheme}'. Each needs its own.");
            }

            var scopes = entry.GetSection("Scopes").GetChildren()
                .Select(scope => scope.Value)
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .ToList();

            providers.Add(new OidcProviderSettings
            {
                Scheme = scheme,
                DisplayName = Optional(entry, "DisplayName") ?? scheme,
                Authority = Required(entry, "Authority"),
                ClientId = Required(entry, "ClientId"),
                ClientSecret = Optional(entry, "ClientSecret") ?? string.Empty,
                Scopes = scopes.Count > 0 ? scopes! : ["openid", "profile", "email"],
                CallbackPath = Optional(entry, "CallbackPath") ?? $"/signin-oidc-{scheme.ToLowerInvariant()}",
                SignedOutCallbackPath = Optional(entry, "SignedOutCallbackPath") ?? $"/signout-callback-oidc-{scheme.ToLowerInvariant()}",
                RoleClaim = Optional(entry, "RoleClaim") ?? string.Empty,
            });
        }

        return providers;
    }

    private static string Required(IConfigurationSection entry, string key)
        => Optional(entry, key) ?? throw new InvalidOperationException(
            $"{entry.Path}:{key} is not configured. A listed sign-in provider needs a Scheme, an Authority and a ClientId; remove the entry to switch the provider off.");

    private static string? Optional(IConfigurationSection entry, string key)
        => string.IsNullOrWhiteSpace(entry[key]) ? null : entry[key]!.Trim();
}
