using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;

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
            Message = $"Google Drive autorisatie geweigerd: {error}";
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            Success = false;
            Message = "Geen autorisatiecode ontvangen van Google.";
            return;
        }

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Success = false;
            Message = "Google OAuth credentials zijn niet geconfigureerd.";
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
                Message = "Geen refresh token ontvangen. Ga naar https://myaccount.google.com/permissions, verwijder de toegang voor deze app en probeer het opnieuw.";
                return;
            }

            RefreshToken = tokenResponse.RefreshToken;
            Success = true;
            Message = "Google Drive autorisatie geslaagd! Kopieer de refresh token hieronder en voeg deze toe aan je secrets.json.";
        }
        catch (TokenResponseException ex)
        {
            logger.LogError(ex, "Failed to exchange Google Drive authorization code for tokens");
            Success = false;
            Message = $"Fout bij het uitwisselen van de autorisatiecode: {ex.Error?.Error}";
        }
    }
}
