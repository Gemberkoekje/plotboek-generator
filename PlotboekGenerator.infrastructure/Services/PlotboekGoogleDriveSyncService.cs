using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Marten document that tracks per-plotboek event stream versions for Google Drive sync.
/// </summary>
public sealed class GoogleDriveSyncState
{
    /// <summary>The plotboek ID string, used as the Marten document ID.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The Marten event stream version of the plotboek aggregate.</summary>
    public long PlotboekVersion { get; init; }

    /// <summary>The Marten event stream versions of each pagina that belongs to this plotboek.</summary>
    public ImmutableDictionary<string, long> PaginaVersions { get; init; } = [];

    /// <summary>UTC timestamp of the last successful sync to Google Drive.</summary>
    public DateTime? LastSyncedAt { get; init; }

    /// <summary>
    /// The page-layout estimator's own, uncorrected bookmark-&gt;page numbers from the last
    /// successful generation - the "what we claimed" baseline the next cycle diffs against the
    /// real Google Drive render to compute a page-number correction.
    /// </summary>
    public ImmutableDictionary<string, int> EstimatedBookmarkPages { get; init; } = [];

    /// <summary>
    /// For each bookmark in <see cref="EstimatedBookmarkPages"/>, the exact heading/entity text to
    /// search for in the next cycle's exported PDF.
    /// </summary>
    public ImmutableDictionary<string, string> BookmarkHeadingText { get; init; } = [];
}

/// <summary>
/// Background sync service that checks each plotboek for changes and uploads updated DOCX files to Google Drive.
/// Designed to be called from an external cronjob via a lightweight API endpoint.
/// </summary>
public sealed class PlotboekGoogleDriveSyncService(
    IDocumentStore documentStore,
    Marten.IDocumentStore martenStore,
    IGoogleDriveService googleDriveService,
    IPlotboekExportDataBuilder exportDataBuilder,
    IDocumentGenerator documentGenerator,
    IPageNumberCorrectionService pageNumberCorrectionService,
    IConfiguration configuration,
    ILogger<PlotboekGoogleDriveSyncService> logger)
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>
    /// Checks all plotboeken for changes since the last sync and uploads updated DOCX files to Google Drive.
    /// Returns a summary of what was processed.
    /// </summary>
    public async Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        if (!googleDriveService.IsConfigured)
        {
            return new SyncResult { Skipped = true, Message = "Google Drive is not configured." };
        }

        // Kill switch for the Drive-render-based page-number correction: live by default once
        // deployed, but can be disabled instantly via config if it ever misbehaves, without
        // affecting the rest of the sync (uploads still happen, just with the raw estimate).
        var correctionEnabled = configuration.GetValue("GoogleDrive:EnablePageNumberCorrection", true);

        using var session = documentStore.OpenSession();
        using var martenSession = martenStore.LightweightSession();
        var plotboeken = await session.QueryAllAsync<Plotboek.ExportData>(cancellationToken);

        var checked_ = 0;
        var uploaded = 0;
        var errors = new List<string>();

        foreach (var plotboek in plotboeken)
        {
            if (string.IsNullOrWhiteSpace(plotboek.GoogleDriveFolderUrl))
                continue;

            if (string.Equals(plotboek.EventId, "Evergreen", StringComparison.Ordinal))
                continue;

            checked_++;

            try
            {
                var plotboekIdStr = plotboek.PlotboekId.ToString();
                var priorState = await session.LoadAsync<GoogleDriveSyncState>(plotboekIdStr, cancellationToken);

                var changed = await HasChangedAsync(session, martenSession, plotboek, priorState, cancellationToken);
                if (!changed)
                {
                    logger.LogDebug("Plotboek {PlotboekId} has not changed since last sync, skipping", plotboek.PlotboekId);
                    continue;
                }

                logger.LogInformation("Plotboek {PlotboekId} has changed, uploading to Google Drive", plotboek.PlotboekId);

                var result = await UploadPlotboekCoreAsync(session, martenSession, plotboek, priorState, correctionEnabled, cancellationToken);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(string.Join(" ", result.Messages.Select(message => message.Message)));
                }

                uploaded++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync plotboek {PlotboekId} to Google Drive", plotboek.PlotboekId);
                errors.Add($"{plotboek.PlotboekId}: {ex.Message}");
            }
        }

        return new SyncResult
        {
            Checked = checked_,
            Uploaded = uploaded,
            Errors = errors,
        };
    }

    /// <summary>
    /// Generates and uploads a single plotboek to Google Drive right now, regardless of whether it
    /// has changed since the last sync - the manual "Upload naar Google Drive" button's entry
    /// point. Shares <see cref="UploadPlotboekCoreAsync"/> with <see cref="SyncAsync"/> so a manual
    /// upload benefits from the exact same page-number correction and sync-state bookkeeping as the
    /// automatic cron sync, rather than a second, divergent upload path.
    /// </summary>
    public async Task<Result<string>> UploadPlotboekAsync(PlotboekId plotboekId, CancellationToken cancellationToken = default)
    {
        if (!googleDriveService.IsConfigured)
        {
            return Result.WithMessages<string>(ValidationMessage.Error("Google Drive is not configured."));
        }

        using var session = documentStore.OpenSession();
        using var martenSession = martenStore.LightweightSession();

        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == plotboekId.ToString(), cancellationToken);
        if (plotboek is null)
        {
            return Result.WithMessages<string>(ValidationMessage.Error($"Plotboek {plotboekId} not found."));
        }

        if (string.IsNullOrWhiteSpace(plotboek.GoogleDriveFolderUrl))
        {
            return Result.WithMessages<string>(ValidationMessage.Error("No Google Drive folder URL configured for this plotboek."));
        }

        var correctionEnabled = configuration.GetValue("GoogleDrive:EnablePageNumberCorrection", true);
        var priorState = await session.LoadAsync<GoogleDriveSyncState>(plotboekId.ToString(), cancellationToken);

        return await UploadPlotboekCoreAsync(session, martenSession, plotboek, priorState, correctionEnabled, cancellationToken);
    }

    /// <summary>
    /// Builds, corrects, and uploads one plotboek's DOCX, then updates its sync-state baseline.
    /// The one place this happens - both <see cref="SyncAsync"/> (gated on "has this changed") and
    /// <see cref="UploadPlotboekAsync"/> (always, on manual request) call into this, so neither path
    /// can silently diverge from the other in what it uploads or what baseline it leaves behind.
    /// </summary>
    private async Task<Result<string>> UploadPlotboekCoreAsync(
        IDocumentSession session,
        Marten.IDocumentSession martenSession,
        Plotboek.ExportData plotboek,
        GoogleDriveSyncState priorState,
        bool correctionEnabled,
        CancellationToken cancellationToken)
    {
        var exportData = await exportDataBuilder.BuildAsync(plotboek.PlotboekId, cancellationToken);
        if (!exportData.IsValid)
        {
            return Result.WithMessages<string>(exportData.Messages.ToArray());
        }

        var driveFileName = $"GENERATED_Plotboek-{plotboek.Jaar}-{plotboek.EventId}.docx";

        var corrections = ImmutableDictionary<string, int>.Empty;
        if (correctionEnabled && priorState is not null)
        {
            corrections = await pageNumberCorrectionService.ComputeCorrectionsAsync(
                priorState, plotboek.GoogleDriveFolderUrl, driveFileName, cancellationToken);
        }
        else
        {
            logger.LogInformation(
                "Skipping Google Drive page-number correction for {FileName}: {Reason}",
                driveFileName,
                !correctionEnabled ? "disabled via GoogleDrive:EnablePageNumberCorrection" : "no prior sync state for this plotboek yet");
        }

        var document = await documentGenerator.GeneratePlotboekDocumentWithCorrectionsAsync(
            exportData.Value, corrections, cancellationToken);
        if (!document.IsValid)
        {
            return Result.WithMessages<string>(document.Messages.ToArray());
        }

        var fileUrl = await googleDriveService.UploadOrUpdateFileAsync(
            plotboek.GoogleDriveFolderUrl, driveFileName, document.Value.Bytes, ContentType, cancellationToken);

        await UpdateSyncStateAsync(session, martenSession, plotboek, document.Value, cancellationToken);

        logger.LogInformation("Successfully synced {FileName} for plotboek {PlotboekId}", driveFileName, plotboek.PlotboekId);

        return fileUrl;
    }

    private static async Task<bool> HasChangedAsync(
        IDocumentSession session,
        Marten.IDocumentSession martenSession,
        Plotboek.ExportData plotboek,
        GoogleDriveSyncState state,
        CancellationToken cancellationToken)
    {
        var plotboekIdStr = plotboek.PlotboekId.ToString();

        // If we've never synced this plotboek, it's "changed"
        if (state == null)
        {
            return true;
        }

        // Check the plotboek's own event stream version
        var plotboekStreamState = await martenSession.Events.FetchStreamStateAsync((Uuid)plotboek.PlotboekId, cancellationToken);
        if (plotboekStreamState != null && plotboekStreamState.Version != state.PlotboekVersion)
            return true;

        // Check all pagina event stream versions
        var pages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == plotboekIdStr && !p.IsVerwijderd, cancellationToken);

        // If the number of (non-deleted) pages changed, that's a change
        if (pages.Count != state.PaginaVersions.Count)
            return true;

        foreach (var paginaId in pages.Select(page => page.PaginaId))
        {
            var paginaStreamState = await martenSession.Events.FetchStreamStateAsync((Uuid)paginaId, cancellationToken);
            var currentVersion = paginaStreamState?.Version ?? 0;

            if (!state.PaginaVersions.TryGetValue(paginaId.ToString(), out var lastVersion) || currentVersion != lastVersion)
                return true;
        }

        return false;
    }

    private async Task UpdateSyncStateAsync(
        IDocumentSession session,
        Marten.IDocumentSession martenSession,
        Plotboek.ExportData plotboek,
        GeneratedDocument document,
        CancellationToken cancellationToken)
    {
        var plotboekIdStr = plotboek.PlotboekId.ToString();

        var plotboekStreamState = await martenSession.Events.FetchStreamStateAsync((Uuid)plotboek.PlotboekId, cancellationToken);

        var pages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == plotboekIdStr && !p.IsVerwijderd, cancellationToken);

        var paginaVersions = new Dictionary<string, long>();
        foreach (var paginaId in pages.Select(page => page.PaginaId))
        {
            var paginaStreamState = await martenSession.Events.FetchStreamStateAsync((Uuid)paginaId, cancellationToken);
            paginaVersions[paginaId.ToString()] = paginaStreamState?.Version ?? 0;
        }

        var state = new GoogleDriveSyncState
        {
            Id = plotboekIdStr,
            PlotboekVersion = plotboekStreamState?.Version ?? 0,
            PaginaVersions = paginaVersions.ToImmutableDictionary(),
            LastSyncedAt = Clock.UtcNow(),
            EstimatedBookmarkPages = document.BookmarkPages,
            BookmarkHeadingText = document.BookmarkHeadingText,
        };

        session.Store(state);
        await session.SaveChangesAsync(cancellationToken);
    }

    public sealed class SyncResult
    {
        public bool Skipped { get; init; }

        public string Message { get; init; } = string.Empty;

        public int Checked { get; init; }

        public int Uploaded { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];
    }
}
