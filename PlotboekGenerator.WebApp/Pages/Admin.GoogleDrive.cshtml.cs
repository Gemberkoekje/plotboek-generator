using System.Globalization;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlotboekGenerator.WebApp.Resources;

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
            return BadRequest(AdminUi.DriveClientIdOntbreekt);
        }

        if (string.IsNullOrWhiteSpace(authEndpoint))
        {
            return BadRequest(AdminUi.DriveAuthEndpointOntbreekt);
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
            ErrorMessage = string.Format(
                CultureInfo.CurrentCulture,
                result.Errors.Count == 1 ? AdminUi.DriveSyncMetFoutenOne : AdminUi.DriveSyncMetFoutenMany,
                result.Checked,
                result.Uploaded,
                result.Errors.Count,
                string.Join("; ", result.Errors));
        }
        else
        {
            SuccessMessage = string.Format(
                CultureInfo.CurrentCulture,
                result.Checked == 1 ? AdminUi.DriveSyncVoltooidOne : AdminUi.DriveSyncVoltooidMany,
                result.Checked,
                result.Uploaded);
        }
    }
}
