using System.Globalization;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    public bool GoogleDriveConfigured => googleDriveService.IsConfigured;

    [TempData]
    public string? GoogleDriveUploadMessage { get; set; }

    [TempData]
    public bool GoogleDriveUploadSuccess { get; set; }

    private async Task<IActionResult> HandleUploadToDrive()
    {
        if (!CanEditCurrentPlotboek())
        {
            return ReadOnlyMutationResult();
        }

        if (!PlotboekId.HasValue)
        {
            return Page();
        }

        var folderUrl = PlotboekSettings?.GoogleDriveFolderUrl;
        if (string.IsNullOrWhiteSpace(folderUrl))
        {
            GoogleDriveUploadSuccess = false;
            GoogleDriveUploadMessage = PlotboekUi.GoogleDriveGeenFolder;
            return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
        }

        if (!googleDriveService.IsConfigured)
        {
            GoogleDriveUploadSuccess = false;
            GoogleDriveUploadMessage = PlotboekUi.GoogleDriveNietGeconfigureerd;
            return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
        }

        var driveFileName = $"GENERATED_Plotboek-{Jaar}-{EventId}.docx";

        try
        {
            // Shares the exact same generation, page-number correction, and sync-state
            // bookkeeping as the automatic Google Drive sync (PlotboekGoogleDriveSyncService) -
            // this button forces an upload right now regardless of whether anything changed,
            // instead of duplicating that logic in a second, divergent path.
            // In the deployment's language, like the export and the nightly sync.
            var result = await CultuurScope.InAsync(
                localization?.DefaultCulture,
                () => googleDriveSyncService.UploadPlotboekAsync(PlotboekId, HttpContext.RequestAborted));
            if (!result.IsValid)
            {
                GoogleDriveUploadSuccess = false;
                GoogleDriveUploadMessage = string.Join(" ", result.Messages.Select(message => message.Message));
                return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
            }

            // Save the Google Drive file URL on the plotboek aggregate
            using var writeSession = documentStore.OpenSession();
            var plotboek = await plotboekRepository.GetByIdAsync(PlotboekId, CancellationToken.None).ConfigureAwait(false);
            if (await User.GetUserAsync(writeSession) is PlotboekUser user && plotboek.IsValid)
            {
                var updated = plotboek.Value.Aanpassen(
                    new Plotboek.GoogleDriveFileUrlData
                    {
                        GoogleDriveFileUrl = result.Value,
                    },
                    user);

                if (updated.IsValid)
                {
                    try
                    {
                        await plotboekRepository.SaveAsync(updated.Value, CancellationToken.None).ConfigureAwait(false);
                        writeSession.Store(updated.Value.GetData());
                        await writeSession.SaveChangesAsync(CancellationToken.None);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning(ex, "Failed to persist Google Drive URL for plotboek {PlotboekId}", PlotboekId);
                    }
                }
            }

            GoogleDriveUploadSuccess = true;
            GoogleDriveUploadMessage = string.Format(CultureInfo.CurrentCulture, PlotboekUi.GoogleDriveGeupload, driveFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload DOCX to Google Drive for {Jaar}-{EventId}", Jaar, EventId);
            GoogleDriveUploadSuccess = false;
            GoogleDriveUploadMessage = string.Format(CultureInfo.CurrentCulture, PlotboekUi.GoogleDriveFout, ex.Message);
        }

        return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
    }
}
