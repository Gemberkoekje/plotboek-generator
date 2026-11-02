using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace PlotboekGenerator.infrastructure.Generator;

/// <summary>
/// A merged document's bytes plus the page-layout estimator's own, uncorrected view of where
/// every bookmark landed - the baseline a caller needs to persist to compute next cycle's
/// Google Drive-derived correction (see <c>PageNumberCorrectionService</c>).
/// </summary>
public sealed record MergeResult(
    byte[] Bytes,
    ImmutableDictionary<string, int> BookmarkPages,
    ImmutableDictionary<string, string> BookmarkHeadingText);

public static partial class DocxTemplateEngine
{
    /// <summary>
    /// Merge a strongly-typed model into an embedded .docx template and return bytes.
    /// </summary>
    public static byte[] MergeModelFromEmbedded(string resourceName, object model, Assembly assembly = null)
        => MergeModelFromEmbeddedWithCorrections(resourceName, model, null, assembly).Bytes;

    /// <summary>
    /// Merge a strongly-typed model into an embedded .docx template, nudging PAGEREF page
    /// numbers by <paramref name="corrections"/> (see
    /// <see cref="ResolvePageRefFieldsToStaticText(OpenXmlElement, MainDocumentPart, IReadOnlyDictionary{string, int})"/>),
    /// and return both the bytes and the estimator's own uncorrected bookmark data so a caller can
    /// persist it as the next cycle's correction baseline.
    /// </summary>
    public static MergeResult MergeModelFromEmbeddedWithCorrections(
        string resourceName, object model, IReadOnlyDictionary<string, int> corrections, Assembly assembly = null)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));

        assembly ??= typeof(DocxTemplateEngine).Assembly;

        using var templateStream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        return MergeModelWithCorrections(templateStream, model, corrections);
    }

    /// <summary>
    /// Merge a strongly-typed model into the .docx template read from <paramref name="template"/>,
    /// nudging PAGEREF page numbers by <paramref name="corrections"/>; see
    /// <see cref="MergeModelFromEmbeddedWithCorrections"/>. The stream is read, not disposed.
    /// </summary>
    public static MergeResult MergeModelWithCorrections(
        Stream template, object model, IReadOnlyDictionary<string, int> corrections)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));
        if (model is null) throw new ArgumentNullException(nameof(model));

        using var ms = new MemoryStream();
        template.CopyTo(ms);
        ms.Position = 0;

        Dictionary<string, int> bookmarkPages;
        Dictionary<string, string> headingText;
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            (bookmarkPages, headingText) = MergeCore(doc, model, corrections);
        }

        return new MergeResult(
            ms.ToArray(),
            bookmarkPages.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
            headingText.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
    }

    private static (Dictionary<string, int> BookmarkPages, Dictionary<string, string> HeadingText) MergeCore(
        WordprocessingDocument doc, object model, IReadOnlyDictionary<string, int> corrections)
    {
        var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart missing.");
        var body = mainPart.Document?.Body ?? throw new InvalidOperationException("Document body missing.");

        // Extract the TOC repeater template before binding so it is not expanded
        // (or removed) by the normal repeater logic.  After binding we will
        // populate it from the heading paragraphs found in the merged document.
        var tocInfo = ExtractTocRepeater(body);

        // Walk, expand repeaters, then fill values
        BindElement(body, model);

        // Auto-build the Table of Contents from heading paragraphs discovered
        // in the merged document, then insert the expanded rows where the
        // repeater used to be.
        var headingText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (tocInfo is not null)
        {
            var headingStyles = BuildHeadingStyleMap(mainPart);
            PopulateAutoTableOfContents(body, tocInfo, headingStyles, headingText);
        }

        // Remove the native Word TOC field (renders as a grey bar) since we
        // build our own custom table of contents.  Must run before unwrapping
        // so that SDT-wrapped TOC variants are still detectable.
        RemoveNativeTocField(body);

        // Replace images in picture content controls (e.g. Voorblad)
        BindImageSdtElements(body, model, mainPart);

        // Final pass: unwrap all remaining SDT elements to convert to plain text
        UnwrapAllSdtElements(body);

        // Shrink the cover picture when the plotboek name would otherwise be
        // pushed onto a second page.  Must run before page numbers are resolved
        // because it changes how many pages the cover occupies.
        FitCoverImageToFirstPage(body, mainPart);

        // Replace PAGEREF complex fields with static page numbers so that
        // renderers without field support (Google Docs/Drive) show the
        // correct page numbers.  Must run after unwrap because bookmarks
        // and page breaks need to be in their final positions.
        var bookmarkPages = ResolvePageRefFieldsToStaticText(body, mainPart, corrections);

        // Enable auto-update of remaining fields (PAGE in footer, etc.)
        EnsureUpdateFieldsOnOpen(doc);

        mainPart.Document.Save();

        return (bookmarkPages, headingText);
    }

    // ========= Binding =========

    private static void BindElement(OpenXmlElement node, object ctx)
    {
        if (node is SdtElement sdt)
        {
            BindSdtElement(sdt, ctx);
            return;
        }

        BindChildElements(node, ctx);
    }

    private static void BindSdtElement(SdtElement sdt, object ctx)
    {
        var tag = GetTagValue(sdt);

        if (IsRepeaterElement(sdt))
        {
            ExpandRepeater(sdt, ctx, tag);
            return;
        }

        BindSdtContent(sdt, ctx);
        TryBindSdtValue(sdt, ctx, tag);
    }

    private static bool IsRepeaterElement(SdtElement sdt)
        => sdt.SdtProperties?.GetFirstChild<SdtRepeatedSection>() is not null;

    private static void BindSdtContent(SdtElement sdt, object ctx)
    {
        var container = GetContentContainer(sdt);
        if (container == null) return;

        var children = container.ChildElements.ToList();
        foreach (var child in children)
            BindElement(child, ctx);
    }

    private static void TryBindSdtValue(SdtElement sdt, object ctx, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || IsSpecialTag(tag))
            return;

        // Check for PAGEREF field binding (e.g. MeerInformatie with a bookmark reference)
        if (TrySetPageRefField(sdt, ctx, tag))
            return;

        if (!TryResolveValue(ctx, tag!, out var str))
            return;

        if (string.IsNullOrEmpty(str))
        {
            RemoveParentTableRow(sdt);
        }
        else if (IsHtmlContent(str))
        {
            SetSdtRichText(sdt, str);
            // UnwrapSdt only if the SDT is still in the tree (the table-cell
            // path inside SetSdtRichText may have already removed it).
            if (sdt.Parent != null)
                UnwrapSdt(sdt);
        }
        else
        {
            SetSdtText(sdt, str);
            if (str == PlotboekGenerator.core.Generator.Templates.PageData.NogNietIngevuld || ShouldApplyIncompleteStyle(ctx, tag))
            {
                ApplyPlaceholderStyle(sdt);
            }
            UnwrapSdt(sdt);
        }
    }

    private static void BindChildElements(OpenXmlElement node, object ctx)
    {
        foreach (var child in node.ChildElements.ToList())
            BindElement(child, ctx);
    }

    internal static void ExpandRepeater(SdtElement repeater, object ctx, string path)
    {
        if (!TryResolveEnumerable(ctx, path, out var items))
        {
            // If no items, just remove the repeater block
            repeater.Remove();
            return;
        }

        var parent = repeater.Parent ?? throw new InvalidOperationException("Repeater without parent.");
        OpenXmlElement insertAfter = repeater;
        SdtElement previousClone = null;

        foreach (var item in items)
        {
            var clone = (SdtElement)repeater.CloneNode(deep: true);

            // Mark clone as a normal scope so it won't be treated as a repeater again
            SetTagValue(clone, "Scope");

            // Insert clone into the document tree before binding so that nested
            // repeaters can walk up to the document root and compute globally
            // unique bookmark IDs.
            parent.InsertAfter(clone, insertAfter);

            BindRepeaterClone(clone, item);
            insertAfter = InsertBookmarkIfNeeded(parent, clone, item);
            ApplyRepeaterRowStyles(clone, previousClone, item);

            previousClone = clone;
        }

        // Remove template repeater
        repeater.Remove();
    }

    /// <summary>
    /// Binds the inner children of a repeater clone using the given item as context.
    /// </summary>
    private static void BindRepeaterClone(SdtElement clone, object item)
    {
        var container = GetContentContainer(clone);
        if (container != null)
        {
            var innerChildren = container.ChildElements.ToList();
            foreach (var child in innerChildren)
                BindElement(child, item!);
        }
    }

    /// <summary>
    /// Inserts a bookmark around the clone if the item has a <c>BookmarkName</c>.
    /// Returns the new insert-after anchor element.
    /// </summary>
    private static OpenXmlElement InsertBookmarkIfNeeded(OpenXmlElement parent, SdtElement clone, object item)
    {
        var bookmarkName = ResolveBookmarkName(item);
        if (!string.IsNullOrEmpty(bookmarkName))
        {
            var bmIdStr = ComputeNextBookmarkId(parent).ToString();
            parent.InsertBefore(new BookmarkStart { Id = bmIdStr, Name = bookmarkName }, clone);
            var bmEnd = new BookmarkEnd { Id = bmIdStr };
            parent.InsertAfter(bmEnd, clone);
            return bmEnd;
        }

        return clone;
    }

    /// <summary>
    /// Applies row styles (border hiding, meeting formatting, black text) to a repeater clone.
    /// </summary>
    private static void ApplyRepeaterRowStyles(SdtElement clone, SdtElement previousClone, object item)
    {
        // Remove horizontal borders between rows that belong to the same group
        if (ResolveHideTopBorder(item))
        {
            SetCellBorders(clone, top: true);
            if (previousClone != null)
                SetCellBorders(previousClone, bottom: true);
        }

        // Apply meeting styling (bold text + light gray background)
        if (ResolveIsMeeting(item))
        {
            ApplyMeetingStyle(clone);
        }

        // Force black text on all tijdschema rows (meetings and regular encounters)
        if (IsTijdschemaItem(item))
        {
            ForceBlackText(clone);
        }

        // Start this row on a new page (evening programme after "Avondeten")
        if (ResolvePageBreakBefore(item))
        {
            ApplyPageBreakBefore(clone);
        }
    }

    /// <summary>
    /// Computes the next available bookmark ID by scanning all existing BookmarkStart
    /// elements in the document tree rooted at the given element's topmost ancestor.
    /// </summary>
    private static int ComputeNextBookmarkId(OpenXmlElement element)
    {
        var root = element;
        while (root.Parent != null) root = root.Parent;
        return root.Descendants<BookmarkStart>()
            .Select(b => int.TryParse(b.Id?.Value, out var id) ? id : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
    }

    // ========= Model resolution =========

    private static bool TryResolveValue(object ctx, string path, out string result)
    {
        result = string.Empty;
        var value = ResolvePath(ctx, path);
        if (value is null) return false;

        if (value is string s)
        {
            result = s;
            return true;
        }

        // Collections should be expanded with a repeater SDT ("Repeat:...").
        // Avoid calling ToString() on IEnumerable implementations (e.g. ImmutableList<T>)
        // which would write the type name into the document.
        if (value is IEnumerable && value is not string)
        {
            return false;
        }

        if (value is DateTime dt)
        {
            result = dt.ToString("yyyy-MM-dd HH:mm");
            return true;
        }

        result = value.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryResolveEnumerable(object ctx, string path, out IEnumerable<object> items)
    {
        items = Enumerable.Empty<object>();
        var value = ResolvePath(ctx, path);
        if (value is null) return false;

        if (value is string)
            return false; // strings are IEnumerable<char>, but not a repeater

        if (value is IEnumerable enumerable)
        {
            items = enumerable.Cast<object>();
            return true;
        }

        return false;
    }

    private static object ResolvePath(object ctx, string path)
    {
        // Support simple dot paths and dictionary keys, case-insensitive
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = ctx;

        foreach (var part in parts)
        {
            if (current is null)
                return null;

            // IDictionary<string, object?>
            if (current is IDictionary<string, object> dictObj)
            {
                current = TryGetDictCaseInsensitive(dictObj, part);
                continue;
            }

            // IDictionary<string, string>
            if (current is IDictionary<string, string> dictStr)
            {
                current = TryGetDictCaseInsensitive(dictStr, part);
                continue;
            }

            // Try property/field
            var type = current.GetType();
            var prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                current = prop.GetValue(current);
                continue;
            }

            var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                current = field.GetValue(current);
                continue;
            }

            // Not found
            return null;
        }

        return current;
    }

    private static object TryGetDictCaseInsensitive(IDictionary<string, object> dict, string key)
    {
        if (!dict.Keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
            return null;
        return dict.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    private static object TryGetDictCaseInsensitive(IDictionary<string, string> dict, string key)
    {
        if (!dict.Keys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
            return null;
        return dict.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
    }

    // ========= Tag utilities =========

    private static bool IsSpecialTag(string tag)
        => !string.IsNullOrWhiteSpace(tag) &&
           (tag.StartsWith("Repeat:", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tag, "Scope", StringComparison.OrdinalIgnoreCase));

    private static string GetTagValue(SdtElement sdt)
        => sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;

    private static void SetTagValue(SdtElement sdt, string value)
    {
        var pr = sdt.SdtProperties ??= new SdtProperties();
        var tag = pr.GetFirstChild<Tag>();
        if (tag is null)
        {
            pr.AppendChild(new Tag() { Val = value });
        }
        else
        {
            tag.Val = value;
        }
    }

    private static void RemoveParentTableRow(SdtElement sdt)
    {
        var row = sdt?
            .Ancestors<TableRow>()
            .FirstOrDefault();

        row?.Remove();
    }
}
