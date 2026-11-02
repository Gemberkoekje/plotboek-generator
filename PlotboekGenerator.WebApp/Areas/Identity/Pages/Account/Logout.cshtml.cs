using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using PlotboekGenerator.WebApp.Authentication;

namespace PlotboekGenerator.WebApp.Areas.Identity.Pages.Account;

[IgnoreAntiforgeryToken]
public sealed class LogoutModel(
    SignInManager<PlotboekUser> signInManager,
    IAuthenticationSchemeProvider schemes,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPost(string returnUrl = "")
    {
        // Capture the provider's id_token (persisted at login) and which provider it was before the
        // cookie is cleared, so we can end the session there too — otherwise the next login silently
        // reuses it.
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var idToken = authResult.Properties?.GetTokenValue("id_token");
        var provider = authResult.Principal?.FindFirstValue(ClaimTypes.AuthenticationMethod);

        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");

        var endSession = await EndSessionEndpointAsync(provider, idToken);
        if (endSession is not null)
        {
            // RP-initiated logout at the provider's end_session endpoint. The id_token_hint lets the
            // provider end the session without a confirmation prompt. (No post_logout_redirect_uri is
            // sent, so this needs no client registration; the provider shows its logged-out page.)
            return Redirect($"{endSession}?id_token_hint={Uri.EscapeDataString(idToken!)}");
        }

        // A provider without a session to end (or no id_token) — local logout only.
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage();
    }

    /// <summary>
    /// The end_session endpoint of the OpenID Connect provider the user signed in with, from its
    /// discovery document; <c>null</c> when there is no session to end there.
    /// </summary>
    private async Task<string?> EndSessionEndpointAsync(string? provider, string? idToken)
    {
        if (string.IsNullOrEmpty(idToken) || !await OidcProviders.IsOidcSchemeAsync(schemes, provider))
        {
            return null;
        }

        try
        {
            var options = oidcOptions.Get(provider);
            var configuration = await options.ConfigurationManager!.GetConfigurationAsync(HttpContext.RequestAborted);
            return string.IsNullOrWhiteSpace(configuration.EndSessionEndpoint) ? null : configuration.EndSessionEndpoint;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or OperationCanceledException)
        {
            // The local session is already gone; failing to reach the provider must not fail the logout.
            logger.LogWarning(ex, "Could not read the end-session endpoint of {Provider}; signed out locally only.", provider);
            return null;
        }
    }
}
