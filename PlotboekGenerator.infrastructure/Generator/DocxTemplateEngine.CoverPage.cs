using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PlotboekGenerator.infrastructure.Generator;

/// <summary>
/// Keeps the cover on one page.  The template reserves a fixed box for the picture, but a tall
/// picture combined with a plotboek name that wraps onto a second line overflows it - which pushed
/// the title onto page two and shifted every page number in the document by one.
/// </summary>
public static partial class DocxTemplateEngine
{
    /// <summary>
    /// Fraction of the usable page height left free when scaling the cover image down, absorbing
    /// the estimation error of the layout approximation.
    /// </summary>
    private const double CoverImageSafetyRatio = 0.04;

    /// <summary>
    /// Smallest fraction of the image's original height it is ever scaled down to. A title long
    /// enough to overflow the page by more than the whole image height still shrinks the image
    /// this far rather than being left completely untouched - the text will still wrap onto many
    /// lines either way, but the picture no longer makes the overflow worse.
    /// </summary>
    private const double MinimumCoverImageHeightRatio = 0.05;

    /// <summary>
    /// Scales the cover image down until the whole cover - the "Plotboek" header, the picture and
    /// the plotboek name - fits on a single page.  Leaves the image alone when it already fits.
    /// Always shrinks as far as it usefully can rather than giving up: an untouched, oversized
    /// image only ever makes an overflowing cover worse.
    /// </summary>
    internal static void FitCoverImageToFirstPage(OpenXmlElement body, MainDocumentPart mainPart)
    {
        var drawing = FindCoverDrawing(body);
        var extent = drawing?.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>().FirstOrDefault();
        if (extent?.Cy?.Value is not > 0 || extent.Cx?.Value is not > 0)
            return;

        var geometry = ReadPageGeometry(body);
        var styles = BuildStyleMetrics(mainPart);
        var coverHeight = MeasureBlocks(CollectCoverBlocks(body), geometry.UsableWidth, styles);

        var excess = coverHeight - geometry.UsableHeight + (geometry.UsableHeight * CoverImageSafetyRatio);
        if (excess <= 0) return;

        var minimumHeight = (long)(extent.Cy.Value * MinimumCoverImageHeightRatio);
        var reduced = Math.Max(minimumHeight, extent.Cy.Value - (long)(excess * EmusPerTwip));
        if (reduced >= extent.Cy.Value) return;

        ScaleDrawing(drawing, extent, reduced);
    }

    /// <summary>Returns the block-level elements that make up the cover: everything up to and
    /// including the first explicit page break.</summary>
    private static List<OpenXmlElement> CollectCoverBlocks(OpenXmlElement body)
    {
        var blocks = new List<OpenXmlElement>();

        foreach (var block in body.ChildElements)
        {
            blocks.Add(block);
            if (ContainsExplicitPageBreak(block))
                break;
        }

        return blocks;
    }

    private static Drawing FindCoverDrawing(OpenXmlElement body)
        => CollectCoverBlocks(body)
            .SelectMany(block => block.Descendants<Drawing>())
            .FirstOrDefault();

    private static bool ContainsExplicitPageBreak(OpenXmlElement element)
        => element.Descendants<Break>().Any(pageBreak => pageBreak.Type?.Value == BreakValues.Page);

    /// <summary>Sets a new height on an inline drawing, scaling the width to keep the aspect ratio.</summary>
    private static void ScaleDrawing(
        Drawing drawing,
        DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent extent,
        long height)
    {
        var ratio = (double)height / extent.Cy.Value;
        var width = (long)(extent.Cx.Value * ratio);

        extent.Cx = width;
        extent.Cy = height;

        foreach (var transform in drawing.Descendants<DocumentFormat.OpenXml.Drawing.Extents>())
        {
            transform.Cx = width;
            transform.Cy = height;
        }
    }
}
