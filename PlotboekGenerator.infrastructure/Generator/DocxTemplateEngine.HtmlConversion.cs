using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Net;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    internal static bool IsHtmlContent(string value)
        => !string.IsNullOrEmpty(value)
           && value.TrimStart().StartsWith('<')
           && HtmlBlockDetectRegex().IsMatch(value);

    private static void SetSdtRichText(SdtElement sdt, string html)
    {
        // Preserve the run properties from the template as a style baseline
        var templateRunProps = sdt
            .Descendants<RunProperties>()
            .FirstOrDefault()
            ?.CloneNode(true) as RunProperties;

        // Strip placeholder colour so rich text always renders in the
        // document's default colour (black) rather than inheriting a
        // grey placeholder tint from the template content control.
        templateRunProps?.RemoveAllChildren<Color>();

        // Preserve paragraph properties (spacing, alignment, etc.) so
        // generated paragraphs match the template's formatting.
        var templateParaProps = sdt
            .Descendants<ParagraphProperties>()
            .FirstOrDefault()
            ?.CloneNode(true) as ParagraphProperties;

        var blocks = ConvertHtmlToBlocks(html, templateRunProps, templateParaProps);

        switch (sdt)
        {
            case SdtBlock sdtBlock:
                if (!SetSdtBlockRichText(sdtBlock, blocks))
                    return;
                break;
            case SdtCell sdtCell:
                if (!SetSdtCellRichText(sdtCell, blocks))
                    return;
                break;
            default:
                if (TrySetSdtInlineRichText(sdt, blocks))
                    return;
                SetSdtText(sdt, DecodeHtml(StripHtmlTags(html)));
                break;
        }

        var pr = sdt.SdtProperties ??= new SdtProperties();
        pr.RemoveAllChildren<ShowingPlaceholder>();
    }

    private static bool SetSdtBlockRichText(SdtBlock sdtBlock, List<OpenXmlElement> blocks)
    {
        var blockContent = sdtBlock.SdtContentBlock;
        if (blockContent == null)
            return false;

        blockContent.RemoveAllChildren();
        foreach (var block in blocks)
            blockContent.AppendChild(block);

        if (!blockContent.HasChildren)
        {
            var emptyPara = new Paragraph();
            emptyPara.AppendChild(new Run()).AppendChild(new Text(string.Empty));
            blockContent.AppendChild(emptyPara);
        }

        return true;
    }

    private static bool SetSdtCellRichText(SdtCell sdtCell, List<OpenXmlElement> blocks)
    {
        var tc = sdtCell.SdtContentCell?.GetFirstChild<TableCell>();
        if (tc == null)
            return false;

        tc.RemoveAllChildren<Paragraph>();
        tc.RemoveAllChildren<Table>();
        foreach (var block in blocks)
            tc.AppendChild(block);

        EnsureCellEndsWithParagraph(tc);

        return true;
    }

    internal static bool TrySetSdtInlineRichText(SdtElement sdt, List<OpenXmlElement> blocks)
    {
        // Inline SDT inside a table cell - insert blocks into the cell.
        var parentCell = sdt.Ancestors<TableCell>().FirstOrDefault();
        if (parentCell != null)
            return InsertBlocksIntoTableCell(sdt, blocks, parentCell);

        // Inline SDT inside a body-level paragraph (e.g. SamenvattingAlgemeenPlot) -
        // replace the parent paragraph with the generated blocks.
        if (sdt.Parent is Paragraph bodyPara && bodyPara.Parent is not null)
            return ReplaceBodyParagraph(sdt, blocks, bodyPara);

        return false;
    }

    private static bool InsertBlocksIntoTableCell(SdtElement sdt, List<OpenXmlElement> blocks, TableCell parentCell)
    {
        var anchor = sdt.Parent is Paragraph parentPara ? (OpenXmlElement)parentPara : sdt;
        foreach (var block in blocks)
            parentCell.InsertBefore(block, anchor);

        anchor.Remove();

        EnsureCellEndsWithParagraph(parentCell);

        return true;
    }

    private static bool ReplaceBodyParagraph(SdtElement sdt, List<OpenXmlElement> blocks, Paragraph bodyPara)
    {
        var container = bodyPara.Parent;
        foreach (var block in blocks)
            container.InsertBefore(block, bodyPara);

        AppendTrailingRuns(sdt, blocks, bodyPara);

        bodyPara.Remove();
        return true;
    }

    /// <summary>
    /// A table cell must end with a paragraph; a bare trailing table produces an
    /// invalid document.  Appends an empty paragraph when needed.
    /// </summary>
    private static void EnsureCellEndsWithParagraph(TableCell cell)
    {
        if (cell.LastChild is not Paragraph)
            cell.AppendChild(new Paragraph());
    }

    /// <summary>
    /// Preserve trailing runs from the parent paragraph that appeared
    /// after the SDT (e.g. a page break run) by appending them to the
    /// last generated paragraph.
    /// </summary>
    private static void AppendTrailingRuns(SdtElement sdt, List<OpenXmlElement> blocks, Paragraph bodyPara)
    {
        var lastPara = blocks.OfType<Paragraph>().LastOrDefault();
        if (lastPara is null) return;

        var afterSdt = false;
        foreach (var child in bodyPara.ChildElements.ToList())
        {
            if (child == sdt)
            {
                afterSdt = true;
                continue;
            }
            if (afterSdt && child is Run)
                lastPara.AppendChild(child.CloneNode(true));
        }
    }

    /// <summary>
    /// Converts a rich-text HTML fragment into a list of block-level OpenXML
    /// elements (paragraphs and tables).  Tables are extracted at the top level
    /// and converted separately; everything around them is handled by the
    /// paragraph/list converter.
    /// </summary>
    internal static List<OpenXmlElement> ConvertHtmlToBlocks(string html, RunProperties templateRunProps, ParagraphProperties templateParaProps = null)
    {
        // Normalise: remove newlines (they are meaningless in HTML)
        html = html.Replace("\r", string.Empty).Replace("\n", string.Empty);

        var blocks = new List<OpenXmlElement>();
        var lastIndex = 0;

        foreach (Match tableMatch in TableRegex().Matches(html))
        {
            AddParagraphBlocks(blocks, html[lastIndex..tableMatch.Index], templateRunProps, templateParaProps);
            blocks.Add(ConvertHtmlTable(tableMatch.Value, templateRunProps));
            lastIndex = tableMatch.Index + tableMatch.Length;
        }

        AddParagraphBlocks(blocks, html[lastIndex..], templateRunProps, templateParaProps);

        // A table may not be the final block or sit directly next to another
        // table without an intervening paragraph, or Word treats the document
        // as corrupt.
        NormalizeTableSeparators(blocks, templateParaProps);

        if (blocks.Count == 0)
            blocks.Add(CreateEmptyParagraph(templateParaProps));

        return blocks;
    }

    /// <summary>
    /// Converts a table-free HTML segment into paragraphs and appends them to
    /// <paramref name="blocks"/>.  Whitespace-only segments are skipped so that
    /// text surrounding a table does not produce spurious empty paragraphs.
    /// </summary>
    private static void AddParagraphBlocks(List<OpenXmlElement> blocks, string html, RunProperties templateRunProps, ParagraphProperties templateParaProps)
    {
        if (string.IsNullOrWhiteSpace(html))
            return;

        if (!HtmlBlockDetectRegex().IsMatch(html)
            && string.IsNullOrWhiteSpace(DecodeHtml(StripHtmlTags(html))))
            return;

        blocks.AddRange(ConvertHtmlToParagraphs(html, templateRunProps, templateParaProps));
    }

    private static void NormalizeTableSeparators(List<OpenXmlElement> blocks, ParagraphProperties templateParaProps)
    {
        for (var i = blocks.Count - 1; i > 0; i--)
        {
            if (blocks[i] is Table && blocks[i - 1] is Table)
                blocks.Insert(i, CreateEmptyParagraph(templateParaProps));
        }

        if (blocks.Count > 0 && blocks[^1] is Table)
            blocks.Add(CreateEmptyParagraph(templateParaProps));
    }

    private static List<Paragraph> ConvertHtmlToParagraphs(string html, RunProperties templateRunProps, ParagraphProperties templateParaProps = null)
    {
        var paragraphs = new List<Paragraph>();

        // Split on block-level elements: <p>, <li>, <h1>..<h6>.
        var blockPattern = BlockPatternRegex();

        // Determine list context for <li> numbering
        var liNumbers = PreScanListContext(html);

        // Now process block elements
        var liIndex = 0;
        foreach (var groups in blockPattern.Matches(html).Select(match => match.Groups))
        {
            var tagName = groups[1].Value.ToLowerInvariant();
            var attrs = groups["attrs"].Value;
            var innerHtml = groups["inner"].Value;

            var para = new Paragraph();
            ApplyParagraphProperties(para, templateParaProps, attrs);

            // Add list bullet/number prefix for <li> elements
            if (tagName == "li")
            {
                string prefix;
                if (liNumbers.TryGetValue(liIndex, out var liInfo) && liInfo.Type == "ol")
                {
                    prefix = $"{liInfo.Number}. ";
                }
                else
                {
                    prefix = "\u2022 ";
                }

                var prefixRun = CreateRun(prefix, templateRunProps, bold: false, italic: false, underline: false);
                para.AppendChild(prefixRun);
                liIndex++;
            }

            // Parse inline content
            var runs = ParseInlineHtml(innerHtml, templateRunProps);
            foreach (var run in runs)
            {
                para.AppendChild(run);
            }

            if (IsHeadingTag(tagName))
                ApplyHeadingStyle(para, tagName);

            paragraphs.Add(para);
        }

        // If no block elements found, treat the whole thing as a single paragraph
        if (paragraphs.Count == 0)
        {
            var para = new Paragraph();
            if (templateParaProps != null)
                para.ParagraphProperties = templateParaProps.CloneNode(true) as ParagraphProperties;
            var stripped = StripHtmlTags(html);
            para.AppendChild(CreateRun(stripped, templateRunProps, bold: false, italic: false, underline: false));
            paragraphs.Add(para);
        }

        return paragraphs;
    }

    /// <summary>
    /// Applies the template paragraph properties plus any alignment implied by
    /// a Quill <c>ql-align-*</c> class on the block element.
    /// </summary>
    private static void ApplyParagraphProperties(Paragraph para, ParagraphProperties templateParaProps, string attrs)
    {
        var paraProps = templateParaProps?.CloneNode(true) as ParagraphProperties;

        var alignment = GetAlignmentFromClass(attrs);
        if (alignment is not null)
        {
            paraProps ??= new ParagraphProperties();
            paraProps.Justification = new Justification { Val = alignment };
        }

        if (paraProps is not null)
            para.ParagraphProperties = paraProps;
    }

    // ========= Tables =========

    private static Table ConvertHtmlTable(string tableHtml, RunProperties templateRunProps)
    {
        var rows = ExtractTableRows(tableHtml);
        var columnCount = rows.Count > 0 ? rows.Max(r => r.Count) : 0;

        // Order matters: CT_Tbl requires tblPr, then tblGrid, then rows.
        var table = new Table();
        table.AppendChild(BuildTableProperties());
        table.AppendChild(BuildTableGrid(columnCount));

        foreach (var cells in rows)
        {
            var row = new TableRow();
            foreach (var cell in cells)
                row.AppendChild(BuildTableCell(cell.Inner, cell.Attrs, templateRunProps));

            // Pad short rows so the table stays rectangular.
            for (var i = cells.Count; i < columnCount; i++)
                row.AppendChild(BuildTableCell(string.Empty, string.Empty, templateRunProps));

            table.AppendChild(row);
        }

        return table;
    }

    private static TableProperties BuildTableProperties()
        => new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
            new TableBorders(
                // Order is schema-significant: top, left, bottom, right, insideH, insideV.
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "auto" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "auto" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "auto" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "auto" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "auto" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "auto" }));

    private static TableGrid BuildTableGrid(int columnCount)
    {
        var grid = new TableGrid();
        for (var i = 0; i < columnCount; i++)
            grid.AppendChild(new GridColumn());
        return grid;
    }

    private static TableCell BuildTableCell(string inner, string attrs, RunProperties templateRunProps)
    {
        var para = new Paragraph();

        var alignment = GetAlignmentFromClass(attrs);
        if (alignment is not null)
        {
            var paraProps = new ParagraphProperties();
            paraProps.AppendChild(new Justification { Val = alignment });
            para.ParagraphProperties = paraProps;
        }

        foreach (var run in ParseInlineHtml(inner, templateRunProps))
            para.AppendChild(run);

        var cell = new TableCell();
        cell.AppendChild(para);
        return cell;
    }

    /// <summary>
    /// Extracts the cells of each table row.  Uses <c>&lt;tr&gt;</c> elements when
    /// present, otherwise falls back to grouping <c>&lt;td&gt;</c> cells by their
    /// Quill <c>data-row</c> attribute.
    /// </summary>
    private static List<List<(string Inner, string Attrs)>> ExtractTableRows(string tableHtml)
    {
        var rows = new List<List<(string, string)>>();

        var rowMatches = TableRowRegex().Matches(tableHtml);
        if (rowMatches.Count > 0)
        {
            foreach (Match row in rowMatches)
                rows.Add(ExtractCells(row.Groups["inner"].Value));

            return rows;
        }

        // Fallback: no <tr> wrappers - group consecutive cells by data-row.
        string currentRow = null;
        List<(string, string)> group = null;
        foreach (var cell in ExtractCells(tableHtml))
        {
            var dataRow = GetDataRow(cell.Attrs);
            if (group is null || dataRow != currentRow)
            {
                group = [];
                rows.Add(group);
                currentRow = dataRow;
            }

            group.Add(cell);
        }

        return rows;
    }

    private static List<(string Inner, string Attrs)> ExtractCells(string rowHtml)
    {
        var cells = new List<(string, string)>();
        foreach (var groups in TableCellRegex().Matches(rowHtml).Select(match => match.Groups))
            cells.Add((groups["inner"].Value, groups["attrs"].Value));

        return cells;
    }

    private static string GetDataRow(string attrs)
    {
        var match = DataRowRegex().Match(attrs);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static JustificationValues? GetAlignmentFromClass(string attrs)
    {
        if (string.IsNullOrEmpty(attrs))
            return null;
        if (attrs.Contains("ql-align-center", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Center;
        if (attrs.Contains("ql-align-right", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Right;
        if (attrs.Contains("ql-align-justify", StringComparison.OrdinalIgnoreCase))
            return JustificationValues.Both;
        return null;
    }

    // ========= Headings =========

    private static bool IsHeadingTag(string tagName)
        => tagName.Length == 2 && tagName[0] == 'h' && tagName[1] is >= '1' and <= '6';

    /// <summary>
    /// Renders a heading as bold, larger text using direct run formatting only.
    /// No paragraph style is applied, so these headings are not picked up by the
    /// document's auto Table of Contents.
    /// </summary>
    private static void ApplyHeadingStyle(Paragraph para, string tagName)
    {
        var sizeHalfPoints = tagName switch
        {
            "h1" => "32",
            "h2" => "28",
            "h3" => "26",
            "h4" => "24",
            "h5" => "23",
            _ => "22",
        };

        foreach (var run in para.Elements<Run>())
        {
            var rp = run.RunProperties ??= new RunProperties();
            rp.Bold ??= new Bold();
            rp.FontSize = new FontSize { Val = sizeHalfPoints };
        }
    }

    private static Paragraph CreateEmptyParagraph(ParagraphProperties templateParaProps)
    {
        var para = new Paragraph();
        if (templateParaProps != null)
            para.ParagraphProperties = templateParaProps.CloneNode(true) as ParagraphProperties;
        return para;
    }

    private static Dictionary<int, (string Type, int Number)> PreScanListContext(string html)
    {
        var listStack = new Stack<(string Type, int Counter)>();
        var liNumbers = new Dictionary<int, (string Type, int Number)>();
        var scanPos = 0;
        var liOccurrence = 0;

        while (scanPos < html.Length)
        {
            var (nextPos, action, closeEnd) = FindNextListAction(html, scanPos);
            if (nextPos == int.MaxValue)
                break;

            switch (action)
            {
                case "ul-open":
                    listStack.Push(("ul", 0));
                    scanPos = nextPos + 1;
                    break;
                case "ol-open":
                    listStack.Push(("ol", 0));
                    scanPos = nextPos + 1;
                    break;
                case "list-close":
                    if (listStack.Count > 0) listStack.Pop();
                    scanPos = closeEnd;
                    break;
                case "li-open":
                    if (listStack.Count > 0)
                    {
                        var current = listStack.Pop();
                        current.Counter++;
                        listStack.Push(current);
                        liNumbers[liOccurrence] = (current.Type, current.Counter);
                    }
                    liOccurrence++;
                    scanPos = nextPos + 1;
                    break;
            }
        }

        return liNumbers;
    }

    private static (int Position, string Action, int CloseEnd) FindNextListAction(string html, int scanPos)
    {
        var ulOpen = FindTag(html, scanPos, "ul", opening: true);
        var olOpen = FindTag(html, scanPos, "ol", opening: true);
        var listClose = FindClosingListTag(html, scanPos);
        var liOpen = FindTag(html, scanPos, "li", opening: true);

        var nextPos = int.MaxValue;
        var action = string.Empty;
        var closeEnd = 0;

        if (ulOpen >= 0 && ulOpen < nextPos)
        {
            nextPos = ulOpen;
            action = "ul-open";
        }
        if (olOpen >= 0 && olOpen < nextPos)
        {
            nextPos = olOpen;
            action = "ol-open";
        }
        if (listClose.Pos >= 0 && listClose.Pos < nextPos)
        {
            nextPos = listClose.Pos;
            action = "list-close";
            closeEnd = listClose.End;
        }
        if (liOpen >= 0 && liOpen < nextPos)
        {
            nextPos = liOpen;
            action = "li-open";
        }

        return (nextPos, action, closeEnd);
    }

    private static List<Run> ParseInlineHtml(string html, RunProperties templateRunProps)
    {
        var runs = new List<Run>();

        // Pattern to match inline formatting tags: <strong>, <b>, <em>, <i>, <u>, <s>, <br>, <br/>
        // We process by walking through the string
        var pos = 0;
        ParseInlineContent(html, ref pos, html.Length, templateRunProps, runs, bold: false, italic: false, underline: false, strike: false);

        return runs;
    }

    private static void ParseInlineContent(string html, ref int pos, int end, RunProperties templateRunProps, List<Run> runs, bool bold, bool italic, bool underline, bool strike)
    {
        var textBuffer = new System.Text.StringBuilder();

        while (pos < end)
        {
            if (html[pos] == '<')
            {
                // Flush any accumulated text
                if (textBuffer.Length > 0)
                {
                    runs.Add(CreateRun(textBuffer.ToString(), templateRunProps, bold, italic, underline, strike));
                    textBuffer.Clear();
                }

                if (!TryProcessHtmlTag(html, ref pos, end, templateRunProps, runs, bold, italic, underline, strike))
                {
                    // Bare '<' character
                    textBuffer.Append(html[pos]);
                    pos++;
                }
            }
            else
            {
                textBuffer.Append(html[pos]);
                pos++;
            }
        }

        // Flush remaining text
        if (textBuffer.Length > 0)
        {
            runs.Add(CreateRun(textBuffer.ToString(), templateRunProps, bold, italic, underline, strike));
        }
    }

    private static bool TryProcessHtmlTag(string html, ref int pos, int end, RunProperties templateRunProps, List<Run> runs, bool bold, bool italic, bool underline, bool strike)
    {
        // Check for <br> or <br/>
        var brMatch = BrMatchRegex().Match(html[pos..]);
        if (brMatch.Success)
        {
            var breakRun = new Run();
            breakRun.AppendChild(new Break());
            runs.Add(breakRun);
            pos += brMatch.Length;
            return true;
        }

        // Check for opening inline tags
        var openMatch = OpenMatchRegex().Match(html[pos..]);
        if (openMatch.Success)
        {
            var tagName = openMatch.Groups[1].Value.ToLowerInvariant();
            pos += openMatch.Length;

            // Find the matching close tag
            var closeTag = $"</{tagName}>";
            var closePos = html.IndexOf(closeTag, pos, StringComparison.OrdinalIgnoreCase);
            if (closePos < 0) closePos = end;

            var newBold = bold || tagName is "strong" or "b";
            var newItalic = italic || tagName is "em" or "i";
            var newUnderline = underline || tagName == "u";
            var newStrike = strike || tagName is "s" or "strike" or "del";

            ParseInlineContent(html, ref pos, closePos, templateRunProps, runs, newBold, newItalic, newUnderline, newStrike);
            pos = closePos + closeTag.Length;
            return true;
        }

        // Check for closing tags (skip them, they're handled by the opening tag logic)
        var closeMatch = CloseMatchRegex().Match(html[pos..]);
        if (closeMatch.Success)
        {
            pos += closeMatch.Length;
            return true;
        }

        // Check for self-closing or void tags (e.g. <br/>)
        var voidMatch = SkipMatchRegex().Match(html[pos..]);
        if (voidMatch.Success)
        {
            pos += voidMatch.Length;
            return true;
        }

        // Unknown tag - skip it
        var skipMatch = SkipMatchRegex().Match(html[pos..]);
        if (skipMatch.Success)
        {
            pos += skipMatch.Length;
            return true;
        }

        return false;
    }

    private static Run CreateRun(string text, RunProperties templateRunProps, bool bold, bool italic, bool underline, bool strike = false)
    {
        var run = new Run();

        // Clone template properties if available, otherwise create new
        var rp = templateRunProps?.CloneNode(true) as RunProperties ?? new RunProperties();

        if (bold)
            rp.Bold ??= new Bold();
        if (italic)
            rp.Italic ??= new Italic();
        if (underline)
            rp.Underline ??= new Underline { Val = UnderlineValues.Single };
        if (strike)
            rp.Strike ??= new Strike();

        if (rp.HasChildren)
            run.AppendChild(rp);

        // Decode HTML entities (&nbsp; -> \u00A0, &amp; -> &, etc.)
        run.AppendChild(new Text(DecodeHtml(text)) { Space = SpaceProcessingModeValues.Preserve });
        return run;
    }

    /// <summary>
    /// Decodes HTML entities and normalises non-breaking spaces to regular spaces.
    /// </summary>
    private static string DecodeHtml(string text)
    {
        var decoded = WebUtility.HtmlDecode(text);
        // Replace non-breaking space (U+00A0) with a regular space so the docx
        // text reads naturally and doesn't display the "nbspace" glyph.
        return decoded.Replace('\u00A0', ' ');
    }

    private static string StripHtmlTags(string html)
        => StripHtmlRegex().Replace(html, string.Empty);

    private static int FindTag(string html, int startPos, string tagName, bool opening)
    {
        var pattern = opening ? $"<{tagName}" : $"</{tagName}>";
        var idx = html.IndexOf(pattern, startPos, StringComparison.OrdinalIgnoreCase);
        return idx;
    }

    private static (int Pos, int End) FindClosingListTag(string html, int startPos)
    {
        var ulClose = html.IndexOf("</ul>", startPos, StringComparison.OrdinalIgnoreCase);
        var olClose = html.IndexOf("</ol>", startPos, StringComparison.OrdinalIgnoreCase);

        if (ulClose < 0 && olClose < 0) return (-1, -1);
        if (ulClose < 0) return (olClose, olClose + 5);
        if (olClose < 0) return (ulClose, ulClose + 5);

        return ulClose < olClose
            ? (ulClose, ulClose + 5)
            : (olClose, olClose + 5);
    }

    [GeneratedRegex(@"<(?:p|ul|ol|li|table|h[1-6]|blockquote|pre)(?:\s|>|/)", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex HtmlBlockDetectRegex();

    [GeneratedRegex(@"<(p|li|h[1-6])(?<attrs>(?:\s[^>]*)?)>(?<inner>.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "nl-NL")]
    private static partial Regex BlockPatternRegex();

    [GeneratedRegex(@"<table(?:\s[^>]*)?>.*?</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "nl-NL")]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"<tr(?:\s[^>]*)?>(?<inner>.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "nl-NL")]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(@"<(td|th)(?<attrs>[^>]*)>(?<inner>.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline, "nl-NL")]
    private static partial Regex TableCellRegex();

    [GeneratedRegex(@"data-row\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex DataRowRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex StripHtmlRegex();

    [GeneratedRegex(@"^<br\s*/?>", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex BrMatchRegex();

    [GeneratedRegex(@"^<(strong|b|em|i|u|s|strike|del)(?:\s[^>]*)?>", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex OpenMatchRegex();

    [GeneratedRegex(@"^</[^>]+>", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex CloseMatchRegex();

    [GeneratedRegex(@"^<[^>]*>", RegexOptions.IgnoreCase, "nl-NL")]
    private static partial Regex SkipMatchRegex();
}
