using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Reflection;

namespace PlotboekGenerator.infrastructure.Generator;

public static partial class DocxTemplateEngine
{
    private static void ApplyPlaceholderStyle(SdtElement sdt)
    {
        foreach (var run in sdt.Descendants<Run>())
        {
            var rp = run.RunProperties ??= new RunProperties();
            rp.Italic ??= new Italic();
            rp.Color = new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "8B0045" };
        }
    }

    /// <summary>
    /// Checks whether the incomplete (italic + berry red) style should be applied
    /// to the NaamEncounter field when the page is not marked as volledig klaar.
    /// </summary>
    private static bool ShouldApplyIncompleteStyle(object ctx, string tag)
    {
        if (!string.Equals(tag, nameof(PlotboekGenerator.core.Generator.Templates.PageData.NaamEncounter), StringComparison.OrdinalIgnoreCase) && !string.Equals(tag, nameof(PlotboekGenerator.core.Generator.Templates.PageData.SamenvattingPlot), StringComparison.OrdinalIgnoreCase))
            return false;

        var prop = ctx.GetType().GetProperty(nameof(PlotboekGenerator.core.Generator.Templates.PageData.IsVolledigKlaar), BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(ctx) is false;
    }

    /// <summary>
    /// Resolves the BookmarkName property from a repeater item, if present.
    /// </summary>
    private static string ResolveBookmarkName(object item)
    {
        if (item is null) return null;
        var prop = item.GetType().GetProperty(
            nameof(PlotboekGenerator.core.Generator.Templates.ExtraInfoData.BookmarkName),
            BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) as string;
    }

    /// <summary>
    /// Resolves the HideTopBorder property from a repeater item.
    /// </summary>
    private static bool ResolveHideTopBorder(object item)
    {
        if (item is null) return false;
        var prop = item.GetType().GetProperty(
            nameof(PlotboekGenerator.core.Generator.Templates.TijdschemaData.HideTopBorder),
            BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) is true;
    }

    /// <summary>
    /// Resolves the IsMeeting property from a repeater item.
    /// </summary>
    private static bool ResolveIsMeeting(object item)
    {
        if (item is null) return false;
        var prop = item.GetType().GetProperty(
            nameof(PlotboekGenerator.core.Generator.Templates.TijdschemaData.IsMeeting),
            BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) is true;
    }

    /// <summary>
    /// Resolves the PageBreakBefore property from a repeater item.
    /// </summary>
    private static bool ResolvePageBreakBefore(object item)
    {
        if (item is null) return false;
        var prop = item.GetType().GetProperty(
            nameof(PlotboekGenerator.core.Generator.Templates.TijdschemaData.PageBreakBefore),
            BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) is true;
    }

    /// <summary>
    /// Checks whether a repeater item is a tijdschema entry (has an IsMeeting property).
    /// </summary>
    private static bool IsTijdschemaItem(object item)
    {
        if (item is null) return false;
        return item.GetType().GetProperty(
            nameof(PlotboekGenerator.core.Generator.Templates.TijdschemaData.IsMeeting),
            BindingFlags.Public | BindingFlags.Instance) is not null;
    }

    /// <summary>
    /// Sets the top and/or bottom border of all table cells inside an element to nil,
    /// removing the horizontal line between adjacent rows in the same group.
    /// </summary>
    private static void SetCellBorders(OpenXmlElement element, bool top = false, bool bottom = false)
    {
        foreach (var cell in element.Descendants<TableCell>())
        {
            var tcPr = cell.TableCellProperties ??= new TableCellProperties();
            var borders = tcPr.TableCellBorders ??= new TableCellBorders();
            if (top)
                borders.TopBorder = new TopBorder { Val = BorderValues.Nil };
            if (bottom)
                borders.BottomBorder = new BottomBorder { Val = BorderValues.Nil };
        }
    }

    /// <summary>
    /// Forces black text color on all runs in an element, overriding any
    /// placeholder or inherited styling.
    /// </summary>
    private static void ForceBlackText(OpenXmlElement element)
    {
        foreach (var run in element.Descendants<Run>())
        {
            var rp = run.RunProperties ??= new RunProperties();
            rp.Color = new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "000000" };
        }
    }

    /// <summary>
    /// Forces a page break before the row by setting <c>pageBreakBefore</c> on the
    /// first paragraph of each cell, pushing the whole row onto a new page.
    /// </summary>
    private static void ApplyPageBreakBefore(OpenXmlElement element)
    {
        foreach (var cell in element.Descendants<TableCell>())
        {
            var paragraph = cell.GetFirstChild<Paragraph>();
            if (paragraph is null) continue;

            var pPr = paragraph.ParagraphProperties ??= new ParagraphProperties();
            pPr.PageBreakBefore ??= new PageBreakBefore();
        }
    }

    /// <summary>
    /// Applies meeting styling to a repeater clone: bold text on all runs
    /// and light gray background on all table cells.
    /// </summary>
    private static void ApplyMeetingStyle(OpenXmlElement element)
    {
        // Make all runs bold with black text
        foreach (var run in element.Descendants<Run>())
        {
            var rp = run.RunProperties ??= new RunProperties();
            rp.Bold ??= new Bold();
            rp.Color = new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "000000" };
        }

        // Set light gray background on all table cells
        foreach (var cell in element.Descendants<TableCell>())
        {
            var tcPr = cell.TableCellProperties ??= new TableCellProperties();
            tcPr.Shading = new Shading
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = "D9D9D9",
            };
        }
    }
}
