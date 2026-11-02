using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    /// <summary>
    /// Convention: for any SDT tag "X", if the context object has a non-empty
    /// property named "XBookmark", the field is rendered as an optional text
    /// prefix followed by a PAGEREF pointing to that bookmark.
    /// </summary>
    private static bool TrySetPageRefField(SdtElement sdt, object ctx, string tag)
    {
        var bookmarkPropName = $"{tag}Bookmark";
        var prop = ctx.GetType().GetProperty(
            bookmarkPropName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null) return false;

        var bookmarkName = prop.GetValue(ctx) as string;
        if (string.IsNullOrEmpty(bookmarkName)) return false;

        TryResolveValue(ctx, tag, out var displayText);
        // displayText may be empty for pure page-number references (e.g. TOC page column)
        SetSdtContentWithPageRef(sdt, displayText ?? string.Empty, bookmarkName);
        if (sdt.Parent != null)
            UnwrapSdt(sdt);
        return true;
    }

    /// <summary>
    /// Replaces the SDT content with an optional display text followed by a PAGEREF field.
    /// When textPrefix is empty only the PAGEREF field is rendered (e.g. TOC page column).
    /// </summary>
    private static void SetSdtContentWithPageRef(SdtElement sdt, string textPrefix, string bookmarkName)
    {
        var templateRunProps = sdt.Descendants<RunProperties>()
            .FirstOrDefault()
            ?.CloneNode(true) as RunProperties;

        var para = new Paragraph();

        // Text prefix run (omitted for pure page-number references)
        if (!string.IsNullOrEmpty(textPrefix))
        {
            var textRun = new Run();
            if (templateRunProps != null)
                textRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
            textRun.AppendChild(new Text(textPrefix) { Space = SpaceProcessingModeValues.Preserve });
            para.AppendChild(textRun);
        }

        // PAGEREF field (with \h for hyperlink)
        AppendPageRefField(para, bookmarkName, templateRunProps);

        switch (sdt)
        {
            case SdtBlock sdtBlock:
                var blockContent = sdtBlock.SdtContentBlock;
                if (blockContent != null)
                {
                    blockContent.RemoveAllChildren();
                    blockContent.AppendChild(para);
                }
                break;
            case SdtCell sdtCell:
                var tc = sdtCell.SdtContentCell?.GetFirstChild<TableCell>();
                if (tc != null)
                {
                    tc.RemoveAllChildren<Paragraph>();
                    tc.AppendChild(para);
                }
                break;
            case SdtRun sdtRun:
                var runContent = sdtRun.SdtContentRun;
                if (runContent != null)
                {
                    runContent.RemoveAllChildren();
                    foreach (var child in para.ChildElements.ToList())
                    {
                        child.Remove();
                        runContent.AppendChild(child);
                    }
                }
                break;
            default:
                SetSdtText(sdt, textPrefix + "?");
                break;
        }

        var pr = sdt.SdtProperties ??= new SdtProperties();
        pr.RemoveAllChildren<ShowingPlaceholder>();
    }

    /// <summary>
    /// Appends the OpenXML runs for a PAGEREF field to the given container.
    /// The field is later resolved to static text by
    /// <see cref="ResolvePageRefFieldsToStaticText(OpenXmlElement, MainDocumentPart)"/>.
    /// </summary>
    private static void AppendPageRefField(OpenXmlCompositeElement container, string bookmarkName, RunProperties templateRunProps)
    {
        // Field begin
        var beginRun = new Run();
        if (templateRunProps != null) beginRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
        beginRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
        container.AppendChild(beginRun);

        // Instruction text
        var instrRun = new Run();
        if (templateRunProps != null) instrRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
        instrRun.AppendChild(new FieldCode($" PAGEREF {bookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve });
        container.AppendChild(instrRun);

        // Separate
        var sepRun = new Run();
        if (templateRunProps != null) sepRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
        sepRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
        container.AppendChild(sepRun);

        // Placeholder result (resolved to a static page number before save)
        var resultRun = new Run();
        if (templateRunProps != null) resultRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
        resultRun.AppendChild(new Text("?") { Space = SpaceProcessingModeValues.Preserve });
        container.AppendChild(resultRun);

        // Field end
        var endRun = new Run();
        if (templateRunProps != null) endRun.AppendChild((RunProperties)templateRunProps.CloneNode(true));
        endRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
        container.AppendChild(endRun);
    }

    /// <summary>
    /// Ensures the document settings include UpdateFieldsOnOpen so that
    /// PAGEREF fields are recalculated when the document is opened in Word.
    /// </summary>
    private static void EnsureUpdateFieldsOnOpen(WordprocessingDocument doc)
    {
        var settingsPart = doc.MainDocumentPart!.DocumentSettingsPart;
        if (settingsPart is null)
        {
            settingsPart = doc.MainDocumentPart.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings();
        }

        settingsPart.Settings ??= new Settings();

        if (settingsPart.Settings.GetFirstChild<UpdateFieldsOnOpen>() is null)
        {
            settingsPart.Settings.PrependChild(new UpdateFieldsOnOpen { Val = true });
        }

        settingsPart.Settings.Save();
    }

    // ========= PAGEREF - static text resolution =========

    /// <summary>
    /// Maximum number of pages a persisted Drive-derived correction may shift an estimate, in
    /// either direction. Bounds how much damage a bad PDF-text match (stale state, an
    /// unexpectedly reused heading string) can do to a single bookmark's page number.
    /// </summary>
    private const int MaxCorrectionMagnitude = 5;

    /// <summary>
    /// Post-processes the merged document to resolve all PAGEREF complex fields
    /// into static page-number text.  This is necessary because Google Docs/Drive
    /// cannot calculate Word complex fields.
    /// </summary>
    internal static void ResolvePageRefFieldsToStaticText(OpenXmlElement body)
        => ResolvePageRefFieldsToStaticText(body, null, null);

    /// <summary>
    /// Post-processes the merged document to resolve all PAGEREF complex fields
    /// into static page-number text.  Page numbers come from
    /// <see cref="ComputeBookmarkPageNumbers"/>, which simulates Word's page flow so
    /// content overflowing onto extra pages is accounted for.
    /// </summary>
    /// <param name="body">The merged document body.</param>
    /// <param name="mainPart">
    /// The document part supplying the style definitions.  When omitted, Word's
    /// defaults (Calibri 11pt, no paragraph spacing) are assumed.
    /// </param>
    internal static void ResolvePageRefFieldsToStaticText(OpenXmlElement body, MainDocumentPart mainPart)
        => ResolvePageRefFieldsToStaticText(body, mainPart, null);

    /// <summary>
    /// Post-processes the merged document to resolve all PAGEREF complex fields into static
    /// page-number text, optionally nudged by <paramref name="corrections"/> - per-bookmark page
    /// offsets derived from a previous Google Drive render, keyed by the same bookmark names
    /// <see cref="ComputeBookmarkPageNumbers"/> itself produces.
    /// </summary>
    /// <param name="body">The merged document body.</param>
    /// <param name="mainPart">
    /// The document part supplying the style definitions.  When omitted, Word's
    /// defaults (Calibri 11pt, no paragraph spacing) are assumed.
    /// </param>
    /// <param name="corrections">
    /// Optional per-bookmark page offsets (<c>real - claimed</c> from a prior sync cycle).  Each
    /// offset is clamped to <see cref="MaxCorrectionMagnitude"/> pages and the result is never let
    /// below page 1, so a stale or mismatched entry cannot inject a wild page number.
    /// </param>
    /// <returns>
    /// The estimator's own, uncorrected bookmark-&gt;page dictionary - what this generation
    /// actually claims, before any correction was applied. Callers that persist a baseline for the
    /// next cycle's correction (see <c>PageNumberCorrectionService</c>) need this, not the
    /// corrected numbers actually printed in the document.
    /// </returns>
    internal static Dictionary<string, int> ResolvePageRefFieldsToStaticText(
        OpenXmlElement body, MainDocumentPart mainPart, IReadOnlyDictionary<string, int> corrections)
    {
        var bookmarkPages = ComputeBookmarkPageNumbers(body, mainPart, removeRedundantRowPageBreaks: true);
        if (bookmarkPages.Count == 0) return bookmarkPages;

        var resolvedPages = corrections is { Count: > 0 }
            ? ApplyCorrections(bookmarkPages, corrections)
            : bookmarkPages;

        // Snapshot the list so we can mutate the tree while iterating.
        foreach (var fieldChar in body.Descendants<FieldChar>().ToList())
        {
            if (fieldChar.FieldCharType?.Value != FieldCharValues.Begin)
                continue;

            var beginRun = fieldChar.Parent as Run;
            if (beginRun is null) continue;

            ReplacePageRefField(beginRun, resolvedPages);
        }

        return bookmarkPages;
    }

    /// <summary>
    /// Applies a clamped per-bookmark offset on top of the raw estimate. Unknown keys in
    /// <paramref name="corrections"/> (an entity that no longer exists this generation) are
    /// silently ignored, matching how <see cref="ReplacePageRefField"/> already ignores an
    /// unresolved bookmark rather than failing the whole merge.
    /// </summary>
    private static Dictionary<string, int> ApplyCorrections(
        Dictionary<string, int> raw, IReadOnlyDictionary<string, int> corrections)
    {
        var result = new Dictionary<string, int>(raw, StringComparer.OrdinalIgnoreCase);

        foreach (var (bookmark, offset) in corrections)
        {
            if (!result.TryGetValue(bookmark, out var page)) continue;

            var clampedOffset = Math.Clamp(offset, -MaxCorrectionMagnitude, MaxCorrectionMagnitude);
            result[bookmark] = Math.Max(1, page + clampedOffset);
        }

        return result;
    }

    /// <summary>
    /// Replaces a single PAGEREF field (starting at the given begin run) with
    /// a static text run containing the resolved page number.
    /// </summary>
    private static void ReplacePageRefField(Run beginRun, Dictionary<string, int> bookmarkPages)
    {
        // Instruction run is expected to be the next sibling.
        var instrRun = NextSiblingRun(beginRun);
        var instrCode = instrRun?.GetFirstChild<FieldCode>()?.Text;
        if (instrCode is null) return;

        var match = PageRefInstrRegex().Match(instrCode);
        if (!match.Success) return;

        var bookmarkName = match.Groups[1].Value;
        if (!bookmarkPages.TryGetValue(bookmarkName, out var pageNumber))
            return;

        // Collect every run that belongs to this field (begin -> end).
        var fieldRuns = CollectFieldRuns(beginRun);

        // Preserve formatting from the result run (first text-bearing run
        // between Separate and End) or fall back to the begin run.
        var templateProps = ResolveFieldResultRunProperties(fieldRuns, beginRun);

        var replacementRun = new Run();
        replacementRun.AppendChild(templateProps);
        replacementRun.AppendChild(
            new Text(pageNumber.ToString()) { Space = SpaceProcessingModeValues.Preserve });

        // Insert the static run and remove the original field runs.
        var parent = beginRun.Parent;
        parent?.InsertBefore(replacementRun, beginRun);
        foreach (var run in fieldRuns)
            run.Remove();
    }

    /// <summary>
    /// Collects all <see cref="Run"/> elements belonging to a complex field,
    /// starting from the begin run up to and including the end run.
    /// </summary>
    private static List<Run> CollectFieldRuns(Run beginRun)
    {
        var runs = new List<Run> { beginRun };
        var current = NextSiblingRun(beginRun);
        while (current is not null)
        {
            runs.Add(current);
            if (current.GetFirstChild<FieldChar>()?.FieldCharType?.Value == FieldCharValues.End)
                break;
            current = NextSiblingRun(current);
        }
        return runs;
    }

    /// <summary>
    /// Returns the <see cref="RunProperties"/> from the first text-bearing run
    /// between the Separate and End field chars (i.e. the field result run).
    /// Falls back to the <paramref name="fallbackRun"/> properties when no
    /// result run is found.
    /// </summary>
    private static RunProperties ResolveFieldResultRunProperties(List<Run> fieldRuns, Run fallbackRun)
    {
        var pastSeparate = false;
        foreach (var run in fieldRuns)
        {
            var fc = run.GetFirstChild<FieldChar>();
            if (fc?.FieldCharType?.Value == FieldCharValues.Separate)
            {
                pastSeparate = true;
                continue;
            }
            if (fc?.FieldCharType?.Value == FieldCharValues.End)
                break;
            if (pastSeparate && run.GetFirstChild<Text>() is not null)
                return run.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties();
        }
        return fallbackRun.RunProperties?.CloneNode(true) as RunProperties ?? new RunProperties();
    }

    /// <summary>
    /// Returns the next sibling that is a <see cref="Run"/>, skipping over
    /// non-Run siblings (e.g. <see cref="BookmarkStart"/>, <see cref="BookmarkEnd"/>).
    /// </summary>
    private static Run NextSiblingRun(OpenXmlElement element)
    {
        var sibling = element.NextSibling();
        while (sibling is not null && sibling is not Run)
            sibling = sibling.NextSibling();
        return sibling as Run;
    }

    [GeneratedRegex(@"PAGEREF\s+(\S+)", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex PageRefInstrRegex();
}
