using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using InlineDrawing = DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    /// <summary>A4 width in twips, used when the document declares no page size.</summary>
    private const double FallbackPageWidth = 11906;

    /// <summary>A4 height in twips, used when the document declares no page size.</summary>
    private const double FallbackPageHeight = 16838;

    /// <summary>One inch in twips, used when the document declares no page margins.</summary>
    private const double FallbackMargin = 1440;

    /// <summary>English Metric Units per twip (914400 EMU per inch / 1440 twips per inch).</summary>
    private const double EmusPerTwip = 635;

    /// <summary>Word's default left/right table cell margin in twips.</summary>
    private const double DefaultCellSideMargin = 108;

    /// <summary>Width of a default tab stop in twips.</summary>
    private const double DefaultTabWidth = 720;

    /// <summary>Line-spacing values are expressed in 240ths of a line for the automatic rule.</summary>
    private const double TwentiethsOfALine = 240;

    /// <summary>Guards against cyclic <c>w:basedOn</c> chains in the style definitions.</summary>
    private const int MaxStyleNestingDepth = 12;

    /// <summary>
    /// Minimum number of lines Word's widow/orphan control keeps together on either side of a
    /// page boundary when a paragraph is split.
    /// </summary>
    private const int WidowControlLines = 2;

    /// <summary>Usable (inside the margins) page dimensions in twips.</summary>
    private sealed record PageGeometry(double UsableWidth, double UsableHeight);

    /// <summary>Resolved left/right/top/bottom table cell margins in twips.</summary>
    private sealed record CellMargins(double Left, double Right, double Top, double Bottom);

    /// <summary>
    /// Walks the document body simulating Word's page flow to determine which page each
    /// <see cref="BookmarkStart"/> ends up on. Unlike simply counting explicit page breaks
    /// this accounts for content that overflows onto additional pages - a table of contents
    /// spanning four pages, a long encounter table, a rich-text summary and so on.
    /// </summary>
    /// <param name="body">The document body to walk.</param>
    /// <param name="mainPart">Supplies style definitions for the walk's own measurements.</param>
    /// <param name="removeRedundantRowPageBreaks">
    /// When <c>true</c>, also strips a table row's forced <c>w:pageBreakBefore</c>
    /// (<see cref="ApplyPageBreakBefore(OpenXmlElement)"/>) whenever this same walk determines,
    /// as an exact fact (nothing at all has been placed on the current page yet - not an
    /// estimate), that the row already sits at the very top of a fresh page. Per the OOXML spec a
    /// redundant <c>pageBreakBefore</c> should be a no-op there, but not every renderer honours
    /// that nuance faithfully - Google Docs' DOCX import has been observed inserting a genuinely
    /// blank page for exactly this case, the same class of bug this codebase's own page-flow
    /// estimator had before being fixed (see the "bare page-break paragraph" hardening-pass entry
    /// in feature-paginanummering.md). Removing the now-provably-unnecessary break at generation
    /// time sidesteps whatever the target renderer does with it, rather than depending on it being
    /// interpreted correctly. Deliberately does NOT extend to "the previous row's estimated height
    /// suggests it just overflowed onto a fresh page" - that judgement depends on the same
    /// approximate glyph-width measurements this estimator's own docs admit can be off by a few
    /// percent, and being wrong would silently and permanently strip a break the real render still
    /// needs (a structural error the Drive-render correction cycle can never undo, since it only
    /// nudges printed numbers). Defaults to <c>false</c> so existing pure page-number lookups
    /// (including every test) are unaffected; the real document-generation pipeline opts in via
    /// <see cref="ResolvePageRefFieldsToStaticText(OpenXmlElement, MainDocumentPart, IReadOnlyDictionary{string, int})"/>.
    /// </param>
    /// <remarks>
    /// The result is an approximation. Line breaking is estimated from Calibri's glyph widths
    /// rather than run through a real shaper, so a paragraph landing within a few percent of a
    /// line boundary can come out one line short or long. Measured against LibreOffice's
    /// rendering of a 245-page plotboek it places every heading on the right page bar a handful,
    /// and those are off by one - where the previous page-break-counting approach drifted further
    /// out the longer the document got.
    /// </remarks>
    private static Dictionary<string, int> ComputeBookmarkPageNumbers(
        OpenXmlElement body, MainDocumentPart mainPart, bool removeRedundantRowPageBreaks = false)
    {
        var bookmarkPages = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var geometry = ReadPageGeometry(body);
        var styles = BuildStyleMetrics(mainPart);
        var flow = new PageFlow(geometry.UsableHeight);

        FlowBlocks(body.ChildElements, flow, geometry.UsableWidth, styles, bookmarkPages, removeRedundantRowPageBreaks);

        return bookmarkPages;
    }

    /// <summary>
    /// Flows a sequence of block-level elements through the page, recording the page each
    /// bookmark lands on.
    /// </summary>
    private static void FlowBlocks(
        IEnumerable<OpenXmlElement> blocks,
        PageFlow flow,
        double availableWidth,
        StyleMetrics styles,
        Dictionary<string, int> bookmarkPages,
        bool removeRedundantRowPageBreaks = false)
    {
        var list = blocks.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
                case BookmarkStart bookmark:
                    RecordBookmark(bookmark, flow.CurrentPage, bookmarkPages);
                    break;
                case Paragraph paragraph:
                    var nextBlock = i + 1 < list.Count ? list[i + 1] : null;
                    FlowParagraph(paragraph, flow, availableWidth, styles, bookmarkPages, nextBlock);
                    break;
                case Table table:
                    FlowTable(table, flow, availableWidth, styles, bookmarkPages, removeRedundantRowPageBreaks);
                    break;
                case SdtElement sdt when GetContentContainer(sdt) is { } content:
                    FlowBlocks(content.ChildElements, flow, availableWidth, styles, bookmarkPages, removeRedundantRowPageBreaks);
                    break;
                case CustomXmlBlock customXml:
                    FlowBlocks(customXml.ChildElements, flow, availableWidth, styles, bookmarkPages, removeRedundantRowPageBreaks);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Flows a single paragraph, splitting it at explicit page breaks and honouring
    /// <c>pageBreakBefore</c> plus the keep-together behaviour of heading styles.
    /// </summary>
    /// <param name="paragraph">The paragraph to flow.</param>
    /// <param name="flow">The page walker to advance.</param>
    /// <param name="availableWidth">Usable width at this point in the document, in twips.</param>
    /// <param name="styles">Resolved paragraph styles, keyed by style id.</param>
    /// <param name="bookmarkPages">Accumulates each bookmark's resolved page as it is reached.</param>
    /// <param name="nextBlock">
    /// The block immediately following this paragraph in the same sequence, if any - used to
    /// honour <c>w:keepNext</c> (stay attached to whatever comes right after). Only tracked within
    /// one level of the document tree: a heading at the very end of a repeater clone or table cell
    /// does not see what follows the clone itself, the same scoping <see cref="FlowBlocks"/> already
    /// has for everything else.
    /// </param>
    private static void FlowParagraph(
        Paragraph paragraph,
        PageFlow flow,
        double availableWidth,
        StyleMetrics styles,
        Dictionary<string, int> bookmarkPages,
        OpenXmlElement nextBlock)
    {
        var format = ResolveParagraphFormat(paragraph, styles);
        if (format.PageBreakBefore && !flow.IsAtPageStart)
            flow.NewPage();

        var segments = MeasureParagraphSegments(paragraph, format, styles, availableWidth);
        var last = segments.Count - 1;

        if (format.KeepTogether || format.KeepWithNext)
        {
            var requiredHeight = segments[0].Height + format.SpacingBefore;

            // w:keepNext only requires the paragraph that follows to be able to START alongside
            // this one, not that all of it fits - matching Word's own behaviour.
            if (format.KeepWithNext && nextBlock is not null)
                requiredHeight += MeasureFirstLineHeight(nextBlock, availableWidth, styles);

            EnsureFits(flow, requiredHeight);
        }

        foreach (var bookmark in paragraph.Descendants<BookmarkStart>())
            RecordBookmark(bookmark, flow.CurrentPage, bookmarkPages);

        for (var i = 0; i <= last; i++)
        {
            if (i > 0) flow.NewPage();
            if (i == 0) flow.Advance(format.SpacingBefore);

            flow.AdvanceParagraph(segments[i]);

            if (i == last) flow.Advance(format.SpacingAfter);
        }
    }

    /// <summary>
    /// Flows a table row by row. Rows are kept together on a single page unless they are
    /// taller than a page themselves.
    /// </summary>
    private static void FlowTable(
        Table table,
        PageFlow flow,
        double availableWidth,
        StyleMetrics styles,
        Dictionary<string, int> bookmarkPages,
        bool removeRedundantRowPageBreaks = false)
    {
        var grid = ReadGridWidths(table);
        var tableMargins = ReadDefaultCellMargins(table);

        // Walk every direct child, not just table.Elements<TableRow>(): a row-level repeater's own
        // bookmark (InsertBookmarkIfNeeded, for any repeated item carrying a BookmarkName) lands as
        // a Table-level sibling of its TableRow, not nested inside it, so it would otherwise never
        // be recorded and its PAGEREF field would stay an unresolved live field.
        foreach (var child in table.ChildElements)
        {
            if (child is BookmarkStart tableLevelBookmark)
            {
                RecordBookmark(tableLevelBookmark, flow.CurrentPage, bookmarkPages);
                continue;
            }

            if (child is not TableRow row) continue;

            if (HasPageBreakBefore(row))
            {
                if (removeRedundantRowPageBreaks && flow.IsAtPageStart)
                {
                    // The row is already, provably, at the very top of a fresh page (nothing
                    // placed there yet - an exact fact tracked from cumulative advances, not an
                    // estimate) - the forced break is a no-op per the OOXML spec, but not every
                    // renderer honours that faithfully (Google Docs has been observed inserting a
                    // genuinely blank page for exactly this case). Strip it so the exported
                    // document never depends on that nuance.
                    //
                    // Deliberately NOT extended to "the previous row's own height estimate implies
                    // it just overflowed onto a fresh page": that judgement rests on the same
                    // approximate glyph-width measurements this estimator's own docs admit can be
                    // off by a few percent. Being wrong there would silently and permanently strip
                    // a break Google's real rendering still needs - turning a self-correcting
                    // printed-number error (the Drive comparison fixes it next cycle) into a
                    // non-self-correcting structural one (the correction mechanism only adjusts
                    // printed numbers, never restores a removed break).
                    foreach (var redundantBreak in row.Descendants<PageBreakBefore>().ToList())
                        redundantBreak.Remove();
                }
                else if (!flow.IsAtPageStart)
                {
                    flow.NewPage();
                }
            }

            // Rows are placed whole. Word may split a tall row between its lines, but in practice
            // it keeps them together often enough that moving the row is the closer model; a row
            // taller than a page still spills, because EnsureFits leaves those alone.
            var height = MeasureRow(row, grid, tableMargins, availableWidth, styles);
            EnsureFits(flow, height);

            foreach (var bookmark in row.Descendants<BookmarkStart>())
                RecordBookmark(bookmark, flow.CurrentPage, bookmarkPages);

            flow.Advance(height);
        }
    }

    /// <summary>Moves to the next page when the block does not fit in the remaining space.</summary>
    private static void EnsureFits(PageFlow flow, double height)
    {
        if (height > 0 && height <= flow.PageHeight && flow.Remaining < height)
            flow.NewPage();
    }

    private static void RecordBookmark(BookmarkStart bookmark, int page, Dictionary<string, int> bookmarkPages)
    {
        var name = bookmark.Name?.Value;
        if (!string.IsNullOrEmpty(name) && !bookmarkPages.ContainsKey(name))
            bookmarkPages[name] = page;
    }

    /// <summary>
    /// Estimates the height of just the first rendered line of a block - enough to check that a
    /// <c>w:keepNext</c> paragraph and whatever follows it can at least START together, without
    /// measuring the whole of what follows.
    /// </summary>
    private static double MeasureFirstLineHeight(OpenXmlElement block, double availableWidth, StyleMetrics styles)
        => block switch
        {
            Paragraph paragraph => FirstSegmentLineHeight(paragraph, availableWidth, styles),
            Table table => FirstRowHeight(table, availableWidth, styles),
            SdtElement sdt when GetContentContainer(sdt)?.ChildElements.FirstOrDefault() is { } first
                => MeasureFirstLineHeight(first, availableWidth, styles),
            CustomXmlBlock customXml when customXml.ChildElements.FirstOrDefault() is { } first
                => MeasureFirstLineHeight(first, availableWidth, styles),
            _ => 0,
        };

    private static double FirstSegmentLineHeight(Paragraph paragraph, double availableWidth, StyleMetrics styles)
    {
        var format = ResolveParagraphFormat(paragraph, styles);
        var segments = MeasureParagraphSegments(paragraph, format, styles, availableWidth);
        var first = segments[0];

        return first.Lines > 0 ? first.Height / first.Lines : first.Height;
    }

    private static double FirstRowHeight(Table table, double availableWidth, StyleMetrics styles)
    {
        var firstRow = table.Elements<TableRow>().FirstOrDefault();
        if (firstRow is null) return 0;

        return MeasureRow(firstRow, ReadGridWidths(table), ReadDefaultCellMargins(table), availableWidth, styles);
    }

    // ========= Measuring =========

    /// <summary>Returns the stacked height of a sequence of block-level elements in twips.</summary>
    private static double MeasureBlocks(IEnumerable<OpenXmlElement> blocks, double availableWidth, StyleMetrics styles)
    {
        var total = 0d;

        foreach (var block in blocks)
        {
            total += block switch
            {
                Paragraph paragraph => MeasureParagraph(paragraph, availableWidth, styles),
                Table table => MeasureTable(table, availableWidth, styles),
                SdtElement sdt when GetContentContainer(sdt) is { } content
                    => MeasureBlocks(content.ChildElements, availableWidth, styles),
                CustomXmlBlock customXml => MeasureBlocks(customXml.ChildElements, availableWidth, styles),
                _ => 0,
            };
        }

        return total;
    }

    private static double MeasureParagraph(Paragraph paragraph, double availableWidth, StyleMetrics styles)
    {
        var format = ResolveParagraphFormat(paragraph, styles);
        var segments = MeasureParagraphSegments(paragraph, format, styles, availableWidth);

        return segments.Sum(segment => segment.Height) + format.SpacingBefore + format.SpacingAfter;
    }

    private static double MeasureTable(Table table, double availableWidth, StyleMetrics styles)
    {
        var grid = ReadGridWidths(table);
        var tableMargins = ReadDefaultCellMargins(table);

        return table.Elements<TableRow>()
            .Sum(row => MeasureRow(row, grid, tableMargins, availableWidth, styles));
    }

    /// <summary>
    /// Returns the height of a table row: the tallest cell, adjusted by an explicit
    /// <c>trHeight</c> when the row declares one.
    /// </summary>
    private static double MeasureRow(
        TableRow row,
        IReadOnlyList<double> grid,
        CellMargins tableMargins,
        double availableWidth,
        StyleMetrics styles)
    {
        var height = 0d;
        var column = 0;

        foreach (var cell in row.Elements<TableCell>())
        {
            var span = cell.TableCellProperties?.GridSpan?.Val?.Value ?? 1;
            var width = ResolveCellWidth(cell, grid, column, span, availableWidth);
            var margins = ResolveCellMargins(cell, tableMargins);
            var content = MeasureBlocks(cell.ChildElements, width - margins.Left - margins.Right, styles);

            height = Math.Max(height, content + margins.Top + margins.Bottom);
            column += span;
        }

        return ApplyRowHeight(row, height);
    }

    private static double ApplyRowHeight(TableRow row, double measured)
    {
        var declared = row.TableRowProperties?.GetFirstChild<TableRowHeight>();
        if (declared?.Val?.Value is not > 0)
            return measured;

        double value = declared.Val.Value;
        var rule = declared.HeightType?.Value ?? HeightRuleValues.AtLeast;

        if (rule == HeightRuleValues.Exact)
            return value;

        return rule == HeightRuleValues.AtLeast
            ? Math.Max(measured, value)
            : measured;
    }

    /// <summary>
    /// Measures a paragraph as one height per page-break-delimited segment. A paragraph without
    /// explicit page breaks yields a single segment.
    /// </summary>
    private static List<ParagraphSegment> MeasureParagraphSegments(
        Paragraph paragraph,
        ParagraphFormat format,
        StyleMetrics styles,
        double availableWidth)
    {
        var width = Math.Max(1, availableWidth - format.IndentLeft - format.IndentRight);
        var measure = new ParagraphMeasure(width * (1 - RaggedRightAllowance), format);

        foreach (var node in paragraph.Descendants())
        {
            switch (node)
            {
                case Text text:
                    var run = ResolveRunFormat(text.Parent, format, styles);
                    measure.AddText(MeasureText(text.Text, run.Size, run.Bold), run.Size);
                    break;
                case TabChar:
                    measure.AddTab(DefaultTabWidth);
                    break;
                case Break pageBreak when pageBreak.Type?.Value == BreakValues.Page:
                    measure.PageBreak();
                    break;
                case Break:
                    measure.LineBreak();
                    break;
                case InlineDrawing drawing:
                    measure.AddDrawing(EmuToTwips(drawing.Extent?.Cy?.Value ?? 0));
                    break;
                default:
                    break;
            }
        }

        return measure.Close();
    }

    private static double EmuToTwips(long emus) => emus / EmusPerTwip;

    // ========= Geometry, grids and margins =========

    private static PageGeometry ReadPageGeometry(OpenXmlElement body)
    {
        var section = body.Elements<SectionProperties>().LastOrDefault()
            ?? body.Descendants<SectionProperties>().FirstOrDefault();

        var size = section?.GetFirstChild<PageSize>();
        var margin = section?.GetFirstChild<PageMargin>();

        double width = size?.Width?.Value ?? FallbackPageWidth;
        double height = size?.Height?.Value ?? FallbackPageHeight;
        double left = margin?.Left?.Value ?? FallbackMargin;
        double right = margin?.Right?.Value ?? FallbackMargin;
        double top = margin?.Top?.Value ?? FallbackMargin;
        double bottom = margin?.Bottom?.Value ?? FallbackMargin;

        return new PageGeometry(
            Math.Max(FallbackMargin, width - left - right),
            Math.Max(FallbackMargin, height - top - bottom));
    }

    private static List<double> ReadGridWidths(Table table)
        => [.. table.GetFirstChild<TableGrid>()?
            .Elements<GridColumn>()
            .Select(column => double.TryParse(column.Width?.Value, out var width) ? width : 0)
            ?? []];

    private static double ResolveCellWidth(
        TableCell cell,
        IReadOnlyList<double> grid,
        int column,
        int span,
        double availableWidth)
    {
        var declared = cell.TableCellProperties?.TableCellWidth;
        if (declared?.Type?.Value == TableWidthUnitValues.Dxa &&
            double.TryParse(declared.Width?.Value, out var width) && width > 0)
        {
            return width;
        }

        var fromGrid = 0d;
        for (var i = column; i < column + span && i < grid.Count; i++)
            fromGrid += grid[i];

        if (fromGrid > 0) return fromGrid;

        // No declared width anywhere - the normal case for a table built from pasted/authored
        // HTML, which never emits column widths. Assume the same even split across columns that
        // Word's own auto-layout applies to an unconstrained table, rather than treating the cell
        // as spanning the whole page: that would undercount how many lines its content wraps onto,
        // and every page number after the table with it.
        return grid.Count > 0 ? availableWidth / grid.Count * span : availableWidth;
    }

    private static CellMargins ReadDefaultCellMargins(Table table)
    {
        var defaults = table.GetFirstChild<TableProperties>()?.TableCellMarginDefault;
        return new CellMargins(
            ReadMargin(defaults?.TableCellLeftMargin?.Width?.Value, defaults?.StartMargin?.Width?.Value, DefaultCellSideMargin),
            ReadMargin(defaults?.TableCellRightMargin?.Width?.Value, defaults?.EndMargin?.Width?.Value, DefaultCellSideMargin),
            ReadMargin(null, defaults?.TopMargin?.Width?.Value, 0),
            ReadMargin(null, defaults?.BottomMargin?.Width?.Value, 0));
    }

    private static CellMargins ResolveCellMargins(TableCell cell, CellMargins fallback)
    {
        var declared = cell.TableCellProperties?.TableCellMargin;
        if (declared is null) return fallback;

        return new CellMargins(
            ReadMargin(null, declared.LeftMargin?.Width?.Value, fallback.Left),
            ReadMargin(null, declared.RightMargin?.Width?.Value, fallback.Right),
            ReadMargin(null, declared.TopMargin?.Width?.Value, fallback.Top),
            ReadMargin(null, declared.BottomMargin?.Width?.Value, fallback.Bottom));
    }

    /// <summary>
    /// Reads a cell margin width, which the SDK exposes either as an <c>short</c> (the legacy
    /// <c>tblCellMar</c> elements) or as a dxa string, falling back to a default.
    /// </summary>
    private static double ReadMargin(short? legacy, string dxa, double fallback)
    {
        if (legacy is > 0) return legacy.Value;
        return double.TryParse(dxa, out var value) && value >= 0 ? value : fallback;
    }

    private static bool HasPageBreakBefore(TableRow row)
        => row.Descendants<PageBreakBefore>().Any(breakBefore => breakBefore.Val?.Value != false);

    // ========= Paragraph formatting =========

    /// <summary>
    /// Resolves the effective formatting of a paragraph: its style chain first, then the direct
    /// formatting on the paragraph itself.
    /// </summary>
    private static ParagraphFormat ResolveParagraphFormat(Paragraph paragraph, StyleMetrics styles)
    {
        var properties = paragraph.ParagraphProperties;
        var styleId = properties?.ParagraphStyleId?.Val?.Value;

        var format = !string.IsNullOrEmpty(styleId) && styles.Formats.TryGetValue(styleId, out var fromStyle)
            ? fromStyle.Clone()
            : styles.Default.Clone();

        // The paragraph mark's run properties drive the height of an otherwise empty paragraph.
        ApplyRunFormatting(properties?.ParagraphMarkRunProperties, format);
        ApplyParagraphFormatting(properties, format);

        return format;
    }

    private static StyleMetrics BuildStyleMetrics(MainDocumentPart mainPart)
    {
        var metrics = new StyleMetrics();
        var styles = mainPart?.StyleDefinitionsPart?.Styles;
        if (styles is null) return metrics;

        var baseFormat = new ParagraphFormat();
        ApplyRunFormatting(styles.DocDefaults?.RunPropertiesDefault?.RunPropertiesBaseStyle, baseFormat);
        ApplyParagraphFormatting(styles.DocDefaults?.ParagraphPropertiesDefault?.ParagraphPropertiesBaseStyle, baseFormat);

        foreach (var style in styles.Elements<Style>())
        {
            var id = style.StyleId?.Value;
            if (string.IsNullOrEmpty(id)) continue;

            metrics.Formats[id] = ResolveStyleFormat(styles, id, baseFormat, depth: 0);
        }

        var defaultStyleId = styles.Elements<Style>()
            .FirstOrDefault(style => style.Type?.Value == StyleValues.Paragraph && style.Default?.Value == true)
            ?.StyleId?.Value;

        metrics.Default = !string.IsNullOrEmpty(defaultStyleId) && metrics.Formats.TryGetValue(defaultStyleId, out var normal)
            ? normal.Clone()
            : baseFormat;

        return metrics;
    }

    private static ParagraphFormat ResolveStyleFormat(Styles styles, string styleId, ParagraphFormat baseFormat, int depth)
    {
        var style = styles.Elements<Style>()
            .FirstOrDefault(candidate => string.Equals(candidate.StyleId?.Value, styleId, StringComparison.Ordinal));

        if (style is null || depth > MaxStyleNestingDepth)
            return baseFormat.Clone();

        var basedOn = style.BasedOn?.Val?.Value;
        var format = string.IsNullOrEmpty(basedOn)
            ? baseFormat.Clone()
            : ResolveStyleFormat(styles, basedOn, baseFormat, depth + 1);

        ApplyRunFormatting(style.StyleRunProperties, format);
        ApplyParagraphFormatting(style.StyleParagraphProperties, format);

        return format;
    }

    private static void ApplyRunFormatting(OpenXmlElement runProperties, ParagraphFormat format)
    {
        var size = runProperties?.GetFirstChild<FontSize>()?.Val?.Value;
        if (!string.IsNullOrEmpty(size) && int.TryParse(size, out var halfPoints) && halfPoints > 0)
            format.FontSizeHalfPoints = halfPoints;
    }

    private static void ApplyParagraphFormatting(OpenXmlElement paragraphProperties, ParagraphFormat format)
    {
        if (paragraphProperties is null) return;

        ApplySpacing(paragraphProperties.GetFirstChild<SpacingBetweenLines>(), format);
        ApplyIndentation(paragraphProperties.GetFirstChild<Indentation>(), format);

        if (paragraphProperties.GetFirstChild<KeepLines>() is not null)
            format.KeepTogether = true;

        if (paragraphProperties.GetFirstChild<KeepNext>() is not null)
            format.KeepWithNext = true;

        var pageBreak = paragraphProperties.GetFirstChild<PageBreakBefore>();
        if (pageBreak is not null)
            format.PageBreakBefore = pageBreak.Val?.Value != false;
    }

    private static void ApplySpacing(SpacingBetweenLines spacing, ParagraphFormat format)
    {
        if (spacing is null) return;

        if (double.TryParse(spacing.Before?.Value, out var before) && before >= 0)
            format.SpacingBefore = before;

        if (double.TryParse(spacing.After?.Value, out var after) && after >= 0)
            format.SpacingAfter = after;

        if (double.TryParse(spacing.Line?.Value, out var line) && line > 0)
        {
            format.LineSpacing = line;
            format.LineRule = spacing.LineRule?.Value ?? LineSpacingRuleValues.Auto;
        }
    }

    private static void ApplyIndentation(Indentation indentation, ParagraphFormat format)
    {
        if (indentation is null) return;

        if (double.TryParse(indentation.Left?.Value ?? indentation.Start?.Value, out var left) && left >= 0)
            format.IndentLeft = left;

        if (double.TryParse(indentation.Right?.Value ?? indentation.End?.Value, out var right) && right >= 0)
            format.IndentRight = right;
    }

    // ========= State =========

    /// <summary>
    /// Tracks the current page and how much of it is filled while walking the document.
    /// </summary>
    private sealed class PageFlow(double pageHeight)
    {
        private double _used;

        /// <summary>Usable height of a single page in twips.</summary>
        internal double PageHeight { get; } = pageHeight > 0 ? pageHeight : FallbackPageHeight;

        /// <summary>One-based number of the page the walker is currently on.</summary>
        internal int CurrentPage { get; private set; } = 1;

        /// <summary>Vertical space still free on the current page, in twips.</summary>
        internal double Remaining => Math.Max(0, PageHeight - _used);

        /// <summary>Whether the current page is still completely empty.</summary>
        internal bool IsAtPageStart => _used <= 0;

        /// <summary>Starts a new page, as an explicit page break does.</summary>
        internal void NewPage()
        {
            CurrentPage++;
            _used = 0;
        }

        /// <summary>Consumes vertical space, spilling onto further pages when it does not fit.</summary>
        internal void Advance(double height)
        {
            _used += Math.Max(0, height);

            while (_used > PageHeight)
            {
                _used -= PageHeight;
                CurrentPage++;
            }
        }

        /// <summary>
        /// Consumes a paragraph line by line, honouring Word's widow/orphan control: a paragraph
        /// split over a page boundary leaves at least two lines on each side, and one that cannot
        /// be split that way moves to the next page whole.
        /// </summary>
        internal void AdvanceParagraph(ParagraphSegment segment)
        {
            if (segment.Lines <= 1 || segment.Height <= 0)
            {
                Advance(segment.Height);
                return;
            }

            var lineHeight = segment.Height / segment.Lines;
            var remaining = segment.Lines;

            while (true)
            {
                var fits = (int)Math.Floor(Remaining / lineHeight);
                if (fits >= remaining)
                {
                    _used += remaining * lineHeight;
                    return;
                }

                var atPageStart = IsAtPageStart;
                var keep = fits;

                if (!atPageStart && (keep < WidowControlLines || remaining - keep < WidowControlLines))
                    keep = 0;
                else if (remaining - keep < WidowControlLines)
                    keep = remaining - WidowControlLines;

                if (atPageStart && keep < 1)
                    keep = 1;

                _used += keep * lineHeight;
                remaining -= keep;
                NewPage();
            }
        }
    }

    /// <summary>
    /// One page-break-delimited run of a paragraph: its height and how many lines it spans,
    /// so the flow can split it line by line at a page boundary.
    /// </summary>
    private sealed record ParagraphSegment(double Height, int Lines);

    /// <summary>Accumulates the rendered height of one paragraph, split at explicit page breaks.</summary>
    private sealed class ParagraphMeasure(double width, ParagraphFormat format)
    {
        private readonly List<ParagraphSegment> _segments = [];
        private readonly double _width = width > 0 ? width : 1;
        private double _pendingWidth;
        private double _drawingHeight;
        private double _textHeight;
        private int _textLines;
        private int _tallestRun;

        /// <summary>Adds a run of text; the tallest run on a line determines that line's height.</summary>
        internal void AddText(double advance, int fontSizeHalfPoints)
        {
            _pendingWidth += advance;
            _tallestRun = Math.Max(_tallestRun, fontSizeHalfPoints);
        }

        internal void AddTab(double advance) => _pendingWidth += advance;

        internal void AddDrawing(double height) => _drawingHeight += height;

        /// <summary>
        /// Closes the current line, as a soft line break does. Always at least one line - a
        /// blank line (two consecutive breaks) still takes up vertical space.
        /// </summary>
        internal void LineBreak() => CloseLine(forceMinimumOneLine: true);

        /// <summary>
        /// Closes the current segment, as an explicit page break does. Does not force a minimum
        /// line: a page break with nothing pending before it (a bare page-break paragraph, or one
        /// immediately following another break) closes a genuinely empty, zero-height segment
        /// rather than manufacturing a phantom line that was never really there.
        /// </summary>
        internal void PageBreak() => _segments.Add(CloseSegment(forceMinimumOneLine: false));

        /// <summary>
        /// Closes the final segment and returns one entry per segment. Always forces a minimum
        /// one line: the paragraph's own mark - its trailing, otherwise-empty content - still
        /// occupies a line, the same way an entirely empty paragraph does in Word.
        /// </summary>
        internal List<ParagraphSegment> Close()
        {
            _segments.Add(CloseSegment(forceMinimumOneLine: true));
            return _segments;
        }

        private ParagraphSegment CloseSegment(bool forceMinimumOneLine)
        {
            CloseLine(forceMinimumOneLine);

            // An inline picture is one unsplittable line box, however tall it is.
            var segment = _drawingHeight > _textHeight
                ? new ParagraphSegment(_drawingHeight, 1)
                : new ParagraphSegment(_textHeight, _textLines);

            _textHeight = 0;
            _textLines = 0;
            _drawingHeight = 0;
            return segment;
        }

        private void CloseLine(bool forceMinimumOneLine)
        {
            if (!forceMinimumOneLine && _pendingWidth <= 0 && _tallestRun == 0)
                return;

            var rawLines = (int)Math.Ceiling(_pendingWidth / _width);
            var lines = forceMinimumOneLine ? Math.Max(1, rawLines) : rawLines;
            var size = _tallestRun > 0 ? _tallestRun : format.FontSizeHalfPoints;

            _textLines += lines;
            _textHeight += lines * ComputeLineHeight(format, size);
            _pendingWidth = 0;
            _tallestRun = 0;
        }
    }

    /// <summary>Paragraph formatting resolved from the style chain plus direct formatting.</summary>
    private sealed class ParagraphFormat
    {
        internal int FontSizeHalfPoints { get; set; } = DefaultFontSizeHalfPoints;

        internal double SpacingBefore { get; set; }

        internal double SpacingAfter { get; set; }

        internal double LineSpacing { get; set; }

        internal LineSpacingRuleValues LineRule { get; set; } = LineSpacingRuleValues.Auto;

        internal double IndentLeft { get; set; }

        internal double IndentRight { get; set; }

        internal bool KeepTogether { get; set; }

        internal bool KeepWithNext { get; set; }

        internal bool PageBreakBefore { get; set; }

        internal ParagraphFormat Clone() => (ParagraphFormat)MemberwiseClone();
    }

    /// <summary>Paragraph formats resolved once per document, keyed by style id.</summary>
    private sealed class StyleMetrics
    {
        internal Dictionary<string, ParagraphFormat> Formats { get; } =
            new Dictionary<string, ParagraphFormat>(StringComparer.OrdinalIgnoreCase);

        internal ParagraphFormat Default { get; set; } = new ParagraphFormat();
    }
}
