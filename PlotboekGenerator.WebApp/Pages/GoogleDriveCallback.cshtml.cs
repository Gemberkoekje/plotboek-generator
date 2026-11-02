using System.Globalization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

[Authorize(Roles = UserRoles.Admin)]
public sealed class GoogleDriveCallbackModel(
    IConfiguration configuration,
    ILogger<GoogleDriveCallbackModel> logger) : PageModel
{
    public string? Message { get; private set; }

    public bool Success { get; private set; }

    public string? RefreshToken { get; private set; }

    public async Task OnGet(string? code, string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            Success = false;
            Message = string.Format(CultureInfo.CurrentCulture, AdminUi.CallbackGeweigerd, error);
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            Success = false;
            Message = AdminUi.CallbackGeenCode;
            return;
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Success = false;
            Message = AdminUi.CallbackNietGeconfigureerd;
            return;
        }

        try
        {
            var redirectUri = Url.PageLink("/GoogleDriveCallback");
            using var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                },
                Scopes = [DriveService.Scope.Drive],
            });

            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                "user",
                code,
                redirectUri,
                CancellationToken.None);

            if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                Success = false;
                Message = string.Format(CultureInfo.CurrentCulture, AdminUi.CallbackGeenRefreshToken, "https://myaccount.google.com/permissions");
                return;
            }

            RefreshToken = tokenResponse.RefreshToken;
            Success = true;
            Message = AdminUi.CallbackSucces;
        }
        catch (TokenResponseException ex)
        {
            logger.LogError(ex, "Failed to exchange Google Drive authorization code for tokens");
            Success = false;
            Message = string.Format(CultureInfo.CurrentCulture, AdminUi.CallbackUitwisselenMislukt, ex.Error?.Error);
        }
    }
}
