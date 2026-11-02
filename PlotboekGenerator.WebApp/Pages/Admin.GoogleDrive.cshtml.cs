using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class AdminModel
{
    public bool GoogleDriveConfigured => googleDriveService.IsConfigured;

    public bool GoogleDriveNeedsAuthorization => googleDriveService.NeedsOAuthAuthorization;

    public bool GoogleDriveTokenInvalid { get; private set; }

    public async Task LoadGoogleDriveStatus(CancellationToken cancellationToken = default)
    {
        if (googleDriveService.IsConfigured)
            GoogleDriveTokenInvalid = !await googleDriveService.ValidateTokenAsync(cancellationToken);
    }

    public IActionResult OnGetAuthorizeGoogleDrive()
    {
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var clientId = configuration["Authentication:Google:ClientId"];
        var authEndpoint = configuration["GoogleDrive:AuthorizationEndpoint"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest("Google OAuth Client ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(authEndpoint))
        {
            return BadRequest("Google OAuth Authorization Endpoint is not configured.");
        }

        var redirectUri = Url.PageLink("/GoogleDriveCallback");
        var authUrl = authEndpoint +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(DriveService.Scope.Drive)}" +
            "&access_type=offline" +
            "&prompt=consent";

        return Redirect(authUrl);
    }

    private async Task HandleSyncGoogleDrive()
    {
        var syncService = HttpContext.RequestServices.GetRequiredService<PlotboekGoogleDriveSyncService>();
        var result = await syncService.SyncAsync();

        if (result.Skipped)
        {
            ErrorMessage = result.Message;
        }
        else if (result.Errors.Count > 0)
        {
            ErrorMessage = $"Sync voltooid met fouten — {result.Checked} gecontroleerd, {result.Uploaded} geüpload, {result.Errors.Count} fout(en): {string.Join("; ", result.Errors)}";
        }
        else
        {
            SuccessMessage = $"Sync voltooid — {result.Checked} plotboeken gecontroleerd, {result.Uploaded} geüpload.";
        }
    }
}
