using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// See <see cref="IPageNumberCorrectionService"/>. The PDF-text-extraction core
/// (<see cref="ExtractRealPages"/>) is pure and independently unit-tested; this class only adds
/// the Drive round-trip and the never-throws contract around it.
/// </summary>
public sealed class PageNumberCorrectionService(
    IGoogleDriveService googleDriveService,
    ILogger<PageNumberCorrectionService> logger) : IPageNumberCorrectionService
{
    /// <summary>Max baseline distance (PDF points) for two words to count as being on the same visual line.</summary>
    private const double LineBaselineTolerance = 2d;

    public async Task<ImmutableDictionary<string, int>> ComputeCorrectionsAsync(
        GoogleDriveSyncState priorState,
        string folderUrl,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(priorState);

        if (priorState.BookmarkHeadingText.IsEmpty)
        {
            // First-ever sync for this plotboek (or the last one didn't record any headings) -
            // nothing to correct against yet. Not a failure, just nothing to do.
            logger.LogInformation(
                "Skipping Google Drive page-number correction for {FileName}: no heading baseline recorded from a prior sync yet",
                fileName);
            return ImmutableDictionary<string, int>.Empty;
        }

        try
        {
            var pdfBytes = await googleDriveService.TryExportCurrentFileAsPdfAsync(folderUrl, fileName, cancellationToken);
            if (pdfBytes is null)
            {
                logger.LogInformation(
                    "Skipping Google Drive page-number correction for {FileName}: no existing file found in the Drive folder to compare against",
                    fileName);
                return ImmutableDictionary<string, int>.Empty;
            }

            var realPages = ExtractRealPages(pdfBytes, priorState.BookmarkHeadingText);
            var corrections = DiffAgainstClaimed(realPages, priorState.EstimatedBookmarkPages);
            LogCorrectionSummary(priorState.BookmarkHeadingText, realPages, priorState.EstimatedBookmarkPages, corrections, fileName);
            return corrections;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to compute Google Drive page-number corrections for {FileName}; falling back to the uncorrected estimate",
                fileName);
            return ImmutableDictionary<string, int>.Empty;
        }
    }

    /// <summary>
    /// Logs, for every expected heading, whether it was found in the rendered PDF and whether its
    /// page actually changed from what was claimed - so both a systematic matching gap (a
    /// punctuation/Unicode pattern the exact-word matcher can't handle, leaving a bookmark
    /// uncorrected cycle after cycle) and the corrections that DID apply are visible in production
    /// logs, rather than only the aggregate corrections dictionary a caller downstream sees.
    /// </summary>
    private void LogCorrectionSummary(
        IReadOnlyDictionary<string, string> expectedTexts,
        ImmutableDictionary<string, int> realPages,
        ImmutableDictionary<string, int> claimedPages,
        ImmutableDictionary<string, int> corrections,
        string fileName)
    {
        var found = new List<string>();
        var notFound = new List<string>();
        var changed = new List<string>();

        foreach (var (key, text) in expectedTexts)
        {
            if (!realPages.TryGetValue(key, out var realPage))
            {
                notFound.Add(text);
                continue;
            }

            found.Add($"{text} (p{realPage})");

            if (corrections.TryGetValue(key, out var offset) && offset != 0)
            {
                var claimedPage = claimedPages.TryGetValue(key, out var claimed) ? claimed : realPage - offset;
                changed.Add($"{text}: p{claimedPage} -> p{realPage} ({(offset > 0 ? "+" : string.Empty)}{offset})");
            }
        }

        logger.LogInformation(
            "Google Drive page-number correction for {FileName}: {FoundCount}/{TotalCount} headings found, {ChangedCount} pages changed. " +
            "Found: {FoundHeadings}. Not found: {NotFoundHeadings}. Changed: {ChangedHeadings}.",
            fileName,
            found.Count,
            expectedTexts.Count,
            changed.Count,
            found.Count == 0 ? "(none)" : string.Join(" | ", found),
            notFound.Count == 0 ? "(none)" : string.Join(" | ", notFound),
            changed.Count == 0 ? "(none)" : string.Join(" | ", changed));
    }

    private static ImmutableDictionary<string, int> DiffAgainstClaimed(
        ImmutableDictionary<string, int> realPages, ImmutableDictionary<string, int> claimedPages)
    {
        var corrections = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (bookmark, realPage) in realPages)
        {
            if (claimedPages.TryGetValue(bookmark, out var claimedPage))
                corrections[bookmark] = realPage - claimedPage;
        }

        return corrections.ToImmutable();
    }

    /// <summary>
    /// For each expected heading, scans every page of the rendered PDF for a run of consecutive
    /// words matching its text, and returns the page of whichever matching occurrence has the
    /// largest font size - the real heading, as opposed to the same string appearing as ordinary
    /// body text elsewhere in the document.
    /// </summary>
    internal static ImmutableDictionary<string, int> ExtractRealPages(
        byte[] pdfBytes, IReadOnlyDictionary<string, string> expectedTexts)
    {
        using var document = PdfDocument.Open(pdfBytes);

        // Extract every page's words once; every heading lookup below reuses this same list
        // rather than re-parsing the PDF per heading. Each word's text is normalized once here
        // too (NFKC - collapses ligature glyphs like the "fi" in a Google-rendered PDF back to
        // plain "f"+"i", among other compatibility-equivalent forms), rather than repeating that
        // work for every heading's search over the same page.
        var pageWords = document.GetPages()
            .Select(page =>
            {
                var words = page.GetWords().ToList();
                var normalizedTexts = words.Select(w => NormalizeForMatching(w.Text)).ToArray();
                return (page.Number, Words: words, NormalizedTexts: normalizedTexts);
            })
            .ToList();

        var result = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, text) in expectedTexts)
        {
            var page = FindPageOfLargestMatch(pageWords, text);
            if (page is not null)
                result[key] = page.Value;
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Normalizes text before comparison so visually-identical text that differs only in Unicode
    /// representation still matches: NFKC compatibility normalization collapses a ligature glyph
    /// (e.g. U+FB01 "fi") back to its plain letter sequence, among other compatibility-equivalent
    /// forms a PDF renderer might choose over the source document's plain characters.
    /// </summary>
    internal static string NormalizeForMatching(string text) => text.Normalize(NormalizationForm.FormKC);

    private static int? FindPageOfLargestMatch(
        List<(int Number, List<Word> Words, string[] NormalizedTexts)> pageWords, string text)
    {
        // Split on any Unicode whitespace (not just literal U+0020) so a non-breaking space (a
        // common paste artifact in a plain, non-HTML title field) doesn't fuse two words into one
        // expected token that can never match PdfPig's own whitespace-based word extraction.
        var expectedWords = NormalizeForMatching(text).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        // Drop a leading run of purely decorative tokens (e.g. an icon prefix built from several
        // emoji, like "⚔️👯⭐❗🎶💡 CA 1.1 ..."). PdfPig's own word extractor breaks a "word" on a
        // font change or a large glyph-advance gap (see DefaultWordExtractor.nextFontDiffers /
        // nextBigSpace) - icon glyphs almost always render in a different font with irregular
        // widths, so the SAME icon block PdfPig actually sees is typically split into several of
        // its own words, not the one literal token a plain whitespace split assumes. Matching
        // never depends on reproducing that split correctly - it just skips past decoration
        // entirely and matches on the real title text instead.
        expectedWords = expectedWords.SkipWhile(w => !w.Any(char.IsLetterOrDigit)).ToArray();
        if (expectedWords.Length == 0) return null;

        int? bestPage = null;
        var bestFontSize = -1d;

        foreach (var (number, words, normalizedTexts) in pageWords)
        {
            var fontSize = LargestMatchFontSize(words, normalizedTexts, expectedWords);
            if (fontSize is null || fontSize.Value <= bestFontSize) continue;

            bestFontSize = fontSize.Value;
            bestPage = number;
        }

        return bestPage;
    }

    /// <summary>
    /// Finds every run of consecutive words on the page matching <paramref name="expectedWords"/>
    /// exactly, and returns the largest font size among them - a page can legitimately contain
    /// the same phrase more than once (a heading plus a body-text mention of the same words).
    /// </summary>
    internal static double? LargestMatchFontSize(List<Word> words, string[] normalizedTexts, string[] expectedWords)
    {
        double? best = null;
        var firstWord = expectedWords[0];

        for (var start = 0; start <= words.Count - expectedWords.Length; start++)
        {
            // Cheap short-circuit before checking the rest of a multi-word phrase.
            if (!string.Equals(normalizedTexts[start], firstWord, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!MatchesFrom(words, normalizedTexts, start, expectedWords))
                continue;

            var fontSize = MaxFontSize(words, start, expectedWords.Length);
            if (best is null || fontSize > best.Value)
                best = fontSize;
        }

        return best;
    }

    private static bool MatchesFrom(List<Word> words, string[] normalizedTexts, int start, string[] expectedWords)
    {
        for (var i = 1; i < expectedWords.Length; i++)
        {
            if (!string.Equals(normalizedTexts[start + i], expectedWords[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return IsStandaloneLine(words, start, expectedWords.Length);
    }

    /// <summary>
    /// A word run only counts as a real match if it is not merely embedded inside a longer line
    /// of text - e.g. a short heading's words also being the opening words of a longer, unrelated
    /// heading ("Extra informatie" inside "Extra informatie over het programma"). Requires that
    /// nothing else with real (letter/digit) content follows the run on the same visual line.
    /// </summary>
    /// <remarks>
    /// Deliberately does not reject a match merely because something precedes it on the same
    /// line: since <see cref="FindPageOfLargestMatch"/> already skips a leading decorative icon
    /// prefix when building <c>expectedWords</c>, the real heading's actual match in the PDF
    /// legitimately has non-matched content (the icons) immediately before it on the same line.
    /// Only content containing an actual letter or digit immediately before the run - i.e.
    /// looking like it could itself be a real, unrelated word - disqualifies the match; purely
    /// decorative content is ignored either way.
    /// </remarks>
    private static bool IsStandaloneLine(List<Word> words, int start, int length)
    {
        if (start > 0 && SameLine(words[start - 1], words[start]) && words[start - 1].Text.Any(char.IsLetterOrDigit))
            return false;

        var end = start + length;
        if (end < words.Count && SameLine(words[end - 1], words[end]))
            return false;

        return true;
    }

    /// <summary>Two words are on the same visual line if their baselines are within a small tolerance.</summary>
    private static bool SameLine(Word a, Word b)
        => Math.Abs(a.BoundingBox.Bottom - b.BoundingBox.Bottom) < LineBaselineTolerance;

    private static double MaxFontSize(List<Word> words, int start, int wordCount)
    {
        var max = 0d;

        for (var i = 0; i < wordCount; i++)
        {
            foreach (var letter in words[start + i].Letters)
                max = Math.Max(max, letter.FontSize);
        }

        return max;
    }
}
