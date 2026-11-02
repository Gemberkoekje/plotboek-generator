using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;

namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// Registers OpenID Connect sign-in providers the one way the login page expects them.
/// </summary>
public sealed class OidcProviders
{
    private OidcProviders()
    {
    }

    /// <summary>
    /// Registers one provider. The external identity lands in Identity's external cookie, so the
    /// login page's <c>ExternalLoginSignInAsync</c> flow picks it up and the provider's subject
    /// (<c>sub</c>) becomes the external login's provider key.
    /// </summary>
    /// <param name="authentication">The authentication builder.</param>
    /// <param name="provider">The provider to register.</param>
    public static void Add(AuthenticationBuilder authentication, OidcProviderSettings provider)
    {
        ArgumentNullException.ThrowIfNull(authentication);
        ArgumentNullException.ThrowIfNull(provider);

        authentication.AddOpenIdConnect(provider.Scheme, provider.DisplayName, options =>
        {
            options.Authority = provider.Authority;
            options.ClientId = provider.ClientId;

            // A public client (PKCE) has no secret; only a confidential client needs one.
            if (!string.IsNullOrWhiteSpace(provider.ClientSecret))
            {
                options.ClientSecret = provider.ClientSecret;
            }

            options.ResponseType = "code";
            options.UsePkce = true;

            // Allow http metadata only against a local (http) issuer for development.
            options.RequireHttpsMetadata = provider.Authority.StartsWith("https", StringComparison.OrdinalIgnoreCase);

            options.Scope.Clear();
            foreach (var scope in provider.Scopes)
            {
                options.Scope.Add(scope);
            }

            // The tokens ride along to the login page: the role resolver reads them, and the
            // id_token lets logout end the session at the provider too.
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.CallbackPath = provider.CallbackPath;
            options.SignedOutCallbackPath = provider.SignedOutCallbackPath;

            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.TokenValidationParameters.NameClaimType = "name";

            options.Events.OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OidcProviders>>();
                logger.LogError("{Provider} authentication failed: {Error}", provider.Scheme, context.Failure?.Message);
                return Task.CompletedTask;
            };
        });
    }

    /// <summary>Whether <paramref name="scheme"/> is a registered OpenID Connect scheme.</summary>
    /// <param name="schemes">The scheme provider.</param>
    /// <param name="scheme">The scheme name, e.g. the provider a user signed in with.</param>
    /// <returns><c>true</c> for an OpenID Connect scheme.</returns>
    public static async Task<bool> IsOidcSchemeAsync(IAuthenticationSchemeProvider schemes, string? scheme)
    {
        ArgumentNullException.ThrowIfNull(schemes);

        if (string.IsNullOrWhiteSpace(scheme))
        {
            return false;
        }

        var registered = await schemes.GetSchemeAsync(scheme);
        return registered is not null && typeof(OpenIdConnectHandler).IsAssignableFrom(registered.HandlerType);
    }
}
