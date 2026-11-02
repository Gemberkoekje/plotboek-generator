using System.Collections.Immutable;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Computes per-bookmark page-number corrections for the Google Drive sync cycle, derived from
/// how the previously-synced file actually renders in Google Drive.
/// </summary>
public interface IPageNumberCorrectionService
{
    /// <summary>
    /// Exports the file currently sitting in Drive (from the last sync cycle) as a PDF, reads
    /// where each of <paramref name="priorState"/>'s known headings actually landed, and diffs
    /// that against what the estimator claimed at the time to produce a per-bookmark offset.
    /// </summary>
    /// <remarks>
    /// Never throws: any failure (Drive unavailable, no prior file yet, a PDF that can't be
    /// parsed) is logged as a warning and degrades to an empty correction set, so a caller never
    /// needs its own try/catch around this step.
    /// </remarks>
    /// <returns>
    /// Per-bookmark page offsets (<c>real - claimed</c>), only for bookmarks a match could
    /// actually be found for. Empty when nothing could be computed.
    /// </returns>
    Task<ImmutableDictionary<string, int>> ComputeCorrectionsAsync(
        GoogleDriveSyncState priorState,
        string folderUrl,
        string fileName,
        CancellationToken cancellationToken = default);
}
