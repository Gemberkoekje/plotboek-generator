using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Security.Cryptography;
using System.Text;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    /// <summary>
    /// Prefix for the deterministic, text-derived bookmark given to a table-of-contents entry
    /// that has no underlying stable, domain-ID-derived bookmark of its own (a section or
    /// day/time heading, as opposed to an encounter/extra-info/bijlage entry).
    /// </summary>
    private const string HeadingBookmarkPrefix = "_hd_";

    /// <summary>Hex characters kept from the heading-text hash, well under Word's 40-char bookmark-name limit.</summary>
    private const int HeadingBookmarkHashLength = 32;

    /// <summary>
    /// Holds the cloned TOC repeater template and the insertion point so the
    /// auto-generated entries can be placed where the repeater originally was.
    /// </summary>
    private sealed record TocRepeaterInfo(
        SdtElement Template,
        OpenXmlElement Parent,
        OpenXmlElement PreviousSibling);

    /// <summary>
    /// Finds and extracts the <c>TableOfContents</c> repeater SDT from the
    /// document tree, returning a clone of the template and the insertion
    /// point.  The original repeater is removed from the tree so that the
    /// normal <see cref="BindElement"/> pass does not try to expand it.
    /// The repeater is identified by the <see cref="SdtRepeatedSection"/>
    /// property combined with a tag of <c>TableOfContents</c> (with or
    /// without a <c>Repeat:</c> prefix).
    /// </summary>
    private static TocRepeaterInfo ExtractTocRepeater(OpenXmlElement body)
    {
        var repeater = body.Descendants<SdtElement>()
            .FirstOrDefault(sdt =>
            {
                // Must be an actual repeating section control
                if (sdt.SdtProperties?.GetFirstChild<SdtRepeatedSection>() is null)
                    return false;

                var tag = GetTagValue(sdt);
                if (string.IsNullOrEmpty(tag)) return false;

                // Match "TableOfContents" or "Repeat:TableOfContents"
                if (string.Equals(tag, "TableOfContents", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (tag.StartsWith("Repeat:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = tag["Repeat:".Length..];
                    return string.Equals(path, "TableOfContents", StringComparison.OrdinalIgnoreCase);
                }

                return false;
            });

        if (repeater is null) return null;

        var template = (SdtElement)repeater.CloneNode(true);
        var parent = repeater.Parent;
        var prevSibling = repeater.PreviousSibling();

        repeater.Remove();

        return new TocRepeaterInfo(template, parent, prevSibling);
    }

    /// <summary>
    /// Scans the merged document for heading paragraphs, inserts a bookmark at
    /// each heading, then clones the TOC repeater template for every heading -
    /// binding it to a synthetic <see cref="PlotboekGenerator.core.Generator.Templates.TableOfContentsData"/>
    /// so the ToCPage field renders as a PAGEREF.
    /// </summary>
    /// <param name="headingText">
    /// Populated with, for every heading, the bookmark its TOC row's PAGEREF actually resolves
    /// against, mapped to the heading's exact display text. Encounter/extra-info/bijlage headings
    /// reuse their entity's own stable, domain-ID-derived bookmark (so a correction to that
    /// bookmark fixes the TOC row too, not just the entity's cross-references); section and
    /// day/time headings, which have no such identity, get a bookmark name deterministically
    /// derived from their text instead of the previous sequential counter - stable across
    /// regenerations as long as the heading text itself doesn't change, which is what lets a
    /// persisted correction be found and reapplied on the next cycle.
    /// </param>
    private static void PopulateAutoTableOfContents(
        OpenXmlElement body,
        TocRepeaterInfo tocInfo,
        Dictionary<string, int> headingStyles,
        Dictionary<string, string> headingText)
    {
        var headings = FindHeadingParagraphs(body, headingStyles);
        if (headings.Count == 0) return;

        // Normalize heading levels to sequential indent positions so that
        // e.g. H1, H2, H5 become indent levels 1, 2, 3 instead of 1, 2, 5.
        var uniqueLevels = headings.Select(h => h.Level).Distinct().OrderBy(l => l).ToList();
        var normalizedLevelMap = new Dictionary<int, int>();
        for (var i = 0; i < uniqueLevels.Count; i++)
            normalizedLevelMap[uniqueLevels[i]] = i + 1;

        var stableBookmarkByHeading = MapHeadingsToEnclosingStableBookmark(body);
        var nextBmId = GetNextBookmarkId(body);
        var usedHeadingBookmarks = new HashSet<string>(StringComparer.Ordinal);
        OpenXmlElement insertAfter = tocInfo.PreviousSibling;

        foreach (var (para, text, level) in headings)
        {
            var bmName = ResolveTocEntryBookmark(
                para, text, stableBookmarkByHeading, usedHeadingBookmarks, ref nextBmId);

            headingText[bmName] = text;

            // Clone the repeater template and bind to a synthetic TOC item
            var clone = (SdtElement)tocInfo.Template.CloneNode(true);
            SetTagValue(clone, "Scope");

            // Strip repeating-section markers so clones behave as plain scope
            // SDTs during unwrapping (avoids visual artifacts in Word).
            foreach (var rs in clone.Descendants<SdtRepeatedSection>().ToList()) rs.Remove();
            foreach (var rsi in clone.Descendants<SdtRepeatedSectionItem>().ToList()) rsi.Remove();

            var tocData = new PlotboekGenerator.core.Generator.Templates.TableOfContentsData
            {
                ToCItem = text,
                ToCPage = string.Empty,
                ToCPageBookmark = bmName,
            };

            var container = GetContentContainer(clone);
            if (container is not null)
            {
                foreach (var child in container.ChildElements.ToList())
                    BindElement(child, tocData);
            }

            // Apply indentation and bold based on normalized heading level
            ApplyTocEntryFormatting(clone, normalizedLevelMap[level]);

            if (insertAfter is not null)
            {
                tocInfo.Parent.InsertAfter(clone, insertAfter);
            }
            else
            {
                tocInfo.Parent.PrependChild(clone);
            }

            insertAfter = clone;
        }

        // Remove visible borders from the TOC table
        var tocTable = tocInfo.Parent as Table ?? tocInfo.Parent.Ancestors<Table>().FirstOrDefault();
        if (tocTable is not null)
        {
            var tPr = tocTable.GetFirstChild<TableProperties>() ?? tocTable.PrependChild(new TableProperties());
            tPr.TableBorders = new TableBorders
            {
                TopBorder = new TopBorder { Val = BorderValues.None, Size = 0 },
                BottomBorder = new BottomBorder { Val = BorderValues.None, Size = 0 },
                LeftBorder = new LeftBorder { Val = BorderValues.None, Size = 0 },
                RightBorder = new RightBorder { Val = BorderValues.None, Size = 0 },
                InsideHorizontalBorder = new InsideHorizontalBorder { Val = BorderValues.None, Size = 0 },
                InsideVerticalBorder = new InsideVerticalBorder { Val = BorderValues.None, Size = 0 },
            };

            // Zero out default cell margins at the table level
            tPr.TableCellMarginDefault = new TableCellMarginDefault
            {
                TopMargin = new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
                BottomMargin = new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
            };
        }
    }

    /// <summary>
    /// Resolves the bookmark a heading's TOC row (and thus its PAGEREF) should target: the
    /// entity's own stable bookmark when one already encloses this heading, otherwise a fresh,
    /// deterministically-named bookmark inserted around the heading text itself.
    /// </summary>
    private static string ResolveTocEntryBookmark(
        Paragraph para,
        string text,
        Dictionary<Paragraph, string> stableBookmarkByHeading,
        HashSet<string> usedHeadingBookmarks,
        ref int nextBmId)
    {
        if (stableBookmarkByHeading.TryGetValue(para, out var stableBookmark))
            return stableBookmark;

        // The same heading text can legitimately appear more than once in the document (e.g. the
        // "Nog niet ingepland" day-group heading is rendered both in the Tijdschema overview and
        // in the Encounters detail section). Each occurrence still needs its own bookmark so its
        // TOC row resolves to its own physical page instead of collapsing onto whichever
        // occurrence happened to insert its bookmark first.
        var baseBmName = DeriveHeadingBookmarkName(text);
        var bmName = baseBmName;
        var suffix = 1;
        while (!usedHeadingBookmarks.Add(bmName))
        {
            suffix++;
            bmName = $"{baseBmName}_{suffix}";
        }

        var bmId = nextBmId.ToString();
        nextBmId++;

        para.InsertBefore(new BookmarkStart { Id = bmId, Name = bmName }, para.FirstChild);
        para.AppendChild(new BookmarkEnd { Id = bmId });

        return bmName;
    }

    /// <summary>
    /// Derives a deterministic, valid OOXML bookmark name from heading text: stable across
    /// regenerations as long as the text itself is unchanged, unlike a sequential counter, which
    /// is what lets a section/day heading's persisted page-number correction be found again.
    /// </summary>
    private static string DeriveHeadingBookmarkName(string text)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return HeadingBookmarkPrefix + hash[..HeadingBookmarkHashLength];
    }

    /// <summary>
    /// Walks the document in order, tracking which stable, domain-ID-derived bookmark (if any) -
    /// an encounter's, extra-info entry's, or bijlage's - currently encloses each heading
    /// paragraph. Bookmarks in this document are never interleaved (each wraps one self-contained
    /// repeater clone), so a simple stack correctly tracks nesting for every bookmark, stable or
    /// not, while only stable-prefixed names are considered when answering "what's open now".
    /// </summary>
    private static Dictionary<Paragraph, string> MapHeadingsToEnclosingStableBookmark(OpenXmlElement body)
    {
        var map = new Dictionary<Paragraph, string>();
        var openBookmarks = new Stack<string>();

        foreach (var element in body.Descendants())
        {
            switch (element)
            {
                case BookmarkStart start:
                    openBookmarks.Push(start.Name?.Value ?? string.Empty);
                    break;
                case BookmarkEnd when openBookmarks.Count > 0:
                    openBookmarks.Pop();
                    break;
                case Paragraph para:
                    var stable = openBookmarks.FirstOrDefault(IsStableEntityBookmarkName);
                    if (stable is not null)
                        map[para] = stable;
                    break;
                default:
                    break;
            }
        }

        return map;
    }

    /// <summary>
    /// Whether a bookmark name is one of the stable, domain-ID-derived families produced by
    /// <c>PlotboekExportBuilder</c> (<c>SanitizePageBookmarkName</c>, <c>SanitizeBookmarkName</c>,
    /// <c>SanitizeBijlageBookmarkName</c>) - as opposed to this file's own ephemeral TOC bookmarks.
    /// </summary>
    private static bool IsStableEntityBookmarkName(string name)
        => !string.IsNullOrEmpty(name) &&
           (name.StartsWith("_pg_", StringComparison.Ordinal) ||
            name.StartsWith("_ei_", StringComparison.Ordinal) ||
            name.StartsWith("_bl_", StringComparison.Ordinal));

    /// <summary>
    /// Applies level-dependent formatting to a TOC entry:
    /// <list type="bullet">
    ///   <item>Heading 1 entries are rendered <b>bold</b>.</item>
    ///   <item>Heading 2+ entries are indented by 360 twips (0.25 in) per
    ///         level beyond the first.</item>
    ///   <item>All rows use tight/exact line spacing with zero margins.</item>
    ///   <item>Page numbers (last cell) are right-aligned and forced black.</item>
    ///   <item>Cell borders are removed.</item>
    /// </list>
    /// </summary>
    internal static void ApplyTocEntryFormatting(OpenXmlElement tocEntry, int headingLevel)
    {
        var cells = tocEntry.Descendants<TableCell>().ToList();
        if (cells.Count == 0) return;

        // Set tight row height on every table row in the entry
        foreach (var row in tocEntry.Descendants<TableRow>())
        {
            var trPr = row.TableRowProperties ??= new TableRowProperties();
            var existingHeight = trPr.GetFirstChild<TableRowHeight>();
            existingHeight?.Remove();
            trPr.AppendChild(new TableRowHeight
            {
                Val = 280,
                HeightType = HeightRuleValues.Exact,
            });
        }

        // Condensed spacing + remove cell borders + zero cell margins on every cell
        foreach (var cell in cells)
        {
            ApplyTocCellFormatting(cell);
        }

        // First cell: ToCItem - indentation and bold
        ApplyTocFirstCellFormatting(cells[0], headingLevel);

        // Last cell: ToCPage - right-align page numbers and force black text
        ApplyTocLastCellFormatting(cells[^1]);
    }

    /// <summary>
    /// Applies condensed spacing, removes borders, and sets zero margins on a TOC cell.
    /// </summary>
    private static void ApplyTocCellFormatting(TableCell cell)
    {
        var tcp = cell.TableCellProperties ??= new TableCellProperties();
        tcp.TableCellBorders = new TableCellBorders
        {
            TopBorder = new TopBorder { Val = BorderValues.None, Size = 0 },
            BottomBorder = new BottomBorder { Val = BorderValues.None, Size = 0 },
            LeftBorder = new LeftBorder { Val = BorderValues.None, Size = 0 },
            RightBorder = new RightBorder { Val = BorderValues.None, Size = 0 },
        };

        // Small top/bottom cell margins for a touch of vertical padding
        tcp.TableCellMargin = new TableCellMargin
        {
            TopMargin = new TopMargin { Width = "15", Type = TableWidthUnitValues.Dxa },
            BottomMargin = new BottomMargin { Width = "15", Type = TableWidthUnitValues.Dxa },
        };

        foreach (var para in cell.Elements<Paragraph>())
        {
            var pPr = para.ParagraphProperties ??= new ParagraphProperties();
            pPr.SpacingBetweenLines = new SpacingBetweenLines
            {
                Before = "0",
                After = "0",
                Line = "240",
                LineRule = LineSpacingRuleValues.Exact,
            };
        }
    }

    /// <summary>
    /// Applies indentation and bold styling to the first TOC cell based on heading level.
    /// </summary>
    private static void ApplyTocFirstCellFormatting(TableCell firstCell, int headingLevel)
    {
        foreach (var para in firstCell.Elements<Paragraph>())
        {
            if (headingLevel > 1)
            {
                var pPr = para.ParagraphProperties ??= new ParagraphProperties();
                pPr.Indentation = new Indentation
                {
                    Left = ((headingLevel - 1) * 360).ToString(),
                };
            }

            if (headingLevel == 1)
            {
                foreach (var run in para.Descendants<Run>())
                {
                    var rPr = run.RunProperties ??= new RunProperties();
                    rPr.Bold = new Bold();
                }
            }
        }
    }

    /// <summary>
    /// Applies right-alignment and black text to the last TOC cell (page numbers).
    /// </summary>
    private static void ApplyTocLastCellFormatting(TableCell lastCell)
    {
        foreach (var para in lastCell.Elements<Paragraph>())
        {
            var pPr = para.ParagraphProperties ??= new ParagraphProperties();
            pPr.Justification = new Justification { Val = JustificationValues.Right };

            foreach (var run in para.Descendants<Run>())
            {
                var rPr = run.RunProperties ??= new RunProperties();
                rPr.Color = new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "000000" };
            }
        }
    }

    /// <summary>
    /// Returns all paragraphs in the document that have a heading style,
    /// together with their plain-text content and heading level.
    /// </summary>
    private static List<(Paragraph Para, string Text, int Level)> FindHeadingParagraphs(
        OpenXmlElement root,
        Dictionary<string, int> headingStyles)
    {
        var headings = new List<(Paragraph, string, int)>();

        foreach (var para in root.Descendants<Paragraph>())
        {
            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            if (string.IsNullOrEmpty(styleId)) continue;

            if (!headingStyles.TryGetValue(styleId, out var level)) continue;

            var text = GetParagraphPlainText(para);
            if (!string.IsNullOrWhiteSpace(text))
            {
                headings.Add((para, text.Trim(), level));
            }
        }

        return headings;
    }

    /// <summary>
    /// Builds a case-insensitive map from paragraph style ID -> heading level
    /// by reading the document's style definitions (outline levels).  Falls
    /// back to well-known name patterns ("Heading1", "Kop1", etc.) so the
    /// TOC works even when the styles part is missing or incomplete.
    /// </summary>
    private static Dictionary<string, int> BuildHeadingStyleMap(MainDocumentPart mainPart)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Seed with well-known style-ID patterns as fallback
        for (var i = 1; i <= 9; i++)
        {
            map[$"Heading{i}"] = i;
            map[$"Kop{i}"] = i;
        }

        // Override / extend with actual outline levels from the document's styles
        var styles = mainPart.StyleDefinitionsPart?.Styles;
        if (styles is not null)
        {
            foreach (var style in styles.Elements<Style>())
            {
                if (style.Type?.Value != StyleValues.Paragraph) continue;

                var outlineLevel = style.StyleParagraphProperties
                    ?.OutlineLevel?.Val?.Value;

                if (outlineLevel is >= 0 and <= 8)
                {
                    var styleId = style.StyleId?.Value;
                    if (!string.IsNullOrEmpty(styleId))
                    {
                        map[styleId] = outlineLevel.Value + 1; // OutlineLevel 0 -> Heading 1
                    }
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Collects all visible text from a paragraph, including text inside SDT
    /// content controls that have not yet been unwrapped.
    /// </summary>
    private static string GetParagraphPlainText(Paragraph para)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var text in para.Descendants<Text>())
        {
            sb.Append(text.Text);
        }

        return sb.ToString();
    }

    private static int GetNextBookmarkId(OpenXmlElement root) =>
        root.Descendants<BookmarkStart>()
            .Select(b => int.TryParse(b.Id?.Value, out var id) ? id : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

    /// <summary>
    /// Removes the native Word TOC field from the document body.  The native
    /// TOC renders as a grey bar and is no longer needed because we build our
    /// own custom table of contents.  This also handles TOC fields wrapped
    /// in an <see cref="SdtBlock"/> (common in modern Word documents).
    /// </summary>
    internal static void RemoveNativeTocField(OpenXmlElement body)
    {
        if (TryRemoveSdtWrappedToc(body)) return;

        RemoveFieldBasedToc(body);
    }

    /// <summary>
    /// Checks for SDT-wrapped native TOC (modern Word documents) and removes it.
    /// Returns <c>true</c> if a TOC SDT was found and removed.
    /// </summary>
    private static bool TryRemoveSdtWrappedToc(OpenXmlElement body)
    {
        foreach (var sdt in body.Descendants<SdtBlock>().ToList())
        {
            var docPartObj = sdt.SdtProperties?.GetFirstChild<SdtContentDocPartObject>();
            var gallery = docPartObj?.DocPartGallery;
            if (gallery?.Val?.Value?.Contains("Table of Contents", StringComparison.OrdinalIgnoreCase) == true)
            {
                RemoveElementAndTrailingEmptyParagraph(sdt);
                return true;
            }

            if (ContainsTocFieldBegin(sdt))
            {
                RemoveElementAndTrailingEmptyParagraph(sdt);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Falls back to field-based TOC removal: finds the body-level element
    /// containing the TOC field begin, then removes all elements until the
    /// matching end.
    /// </summary>
    private static void RemoveFieldBasedToc(OpenXmlElement body)
    {
        var tocStartElement = FindTocStartElement(body);
        if (tocStartElement is null) return;

        var elementsToRemove = CollectTocFieldElements(tocStartElement);

        // Also include a trailing empty paragraph (common TOC artifact)
        if (elementsToRemove.Count > 0)
        {
            var lastRemoved = elementsToRemove[^1];
            var nextSibling = lastRemoved.NextSibling();
            if (nextSibling is Paragraph p && !p.Descendants<Run>().Any())
                elementsToRemove.Add(p);
        }

        foreach (var el in elementsToRemove)
            el.Remove();
    }

    /// <summary>
    /// Finds the first body-level child element that contains a TOC field begin.
    /// </summary>
    private static OpenXmlElement FindTocStartElement(OpenXmlElement body)
        => body.ChildElements.FirstOrDefault(ContainsTocFieldBegin);

    /// <summary>
    /// Returns <c>true</c> if the given element contains a TOC field begin.
    /// </summary>
    private static bool ContainsTocFieldBegin(OpenXmlElement element)
        => element.Descendants<FieldChar>().Any(fc =>
        {
            if (fc.FieldCharType?.Value != FieldCharValues.Begin) return false;
            var instrRun = fc.Parent?.NextSibling<Run>();
            var instrCode = instrRun?.GetFirstChild<FieldCode>()?.Text;
            return instrCode != null &&
                   instrCode.TrimStart().StartsWith("TOC", StringComparison.OrdinalIgnoreCase);
        });

    /// <summary>
    /// Walks forward from the given start element, tracking nested field depth,
    /// until the outermost TOC field end is reached.
    /// </summary>
    private static List<OpenXmlElement> CollectTocFieldElements(OpenXmlElement tocStartElement)
    {
        var elementsToRemove = new List<OpenXmlElement>();
        var depth = 0;
        var foundEnd = false;

        for (var el = tocStartElement; el is not null && !foundEnd; el = el.NextSibling())
        {
            elementsToRemove.Add(el);

            foreach (var charType in el.Descendants<FieldChar>().Select(fc => fc.FieldCharType?.Value))
            {
                if (charType == FieldCharValues.Begin)
                {
                    depth++;
                }
                else if (charType == FieldCharValues.End)
                {
                    depth--;
                    if (depth == 0)
                    {
                        foundEnd = true;
                        break;
                    }
                }
            }
        }

        return elementsToRemove;
    }

    /// <summary>
    /// Removes the given element and any trailing empty paragraph.
    /// </summary>
    internal static void RemoveElementAndTrailingEmptyParagraph(OpenXmlElement element)
    {
        var nextSibling = element.NextSibling();
        element.Remove();
        if (nextSibling is Paragraph p && !p.Descendants<Run>().Any())
            p.Remove();
    }
}
