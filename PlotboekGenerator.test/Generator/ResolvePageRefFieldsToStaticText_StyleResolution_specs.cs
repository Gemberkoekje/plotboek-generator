#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;
using System.IO;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// Production always resolves page numbers with the real <c>MainDocumentPart</c> overload of
/// <c>ResolvePageRefFieldsToStaticText</c>, which walks the document's style definitions -
/// <c>docDefaults</c>, a paragraph style's own properties, and its <c>w:basedOn</c> chain. Every
/// other spec in this project calls the one-argument overload (no <c>MainDocumentPart</c>), which
/// falls back to flat Word defaults and never exercises that resolution at all. These specs cover
/// that gap directly: the same input text, resolved with and without a real style chain, must
/// produce different page numbers - proving the style chain actually drives the estimate.
/// </summary>
public sealed class ResolvePageRefFieldsToStaticText_StyleResolution_specs
{
    /// <summary>docDefaults: 11pt, matching Word's own default and the rest of this test project.</summary>
    private const string NormalFontHalfPoints = "22";

    /// <summary>A custom style based on Normal, overriding only the font size - 44pt.</summary>
    private const string BigFontHalfPoints = "88";

    [Test]
    public void A_paragraph_styled_far_larger_than_the_flat_default_wraps_onto_more_pages()
    {
        var text = string.Join(' ', Enumerable.Repeat("woordje", 50));
        var body = BuildBody(text);

        using var stream = new MemoryStream();
        using var doc = BuildDocumentWithStyles(body, stream);

        var withoutStyles = ResolveOn(CloneBody(body), null);
        var withStyles = ResolveOn(CloneBody(body), doc.MainDocumentPart);

        // Same characters, same bookmark, same PAGEREF - only whether the "Big" style (44pt,
        // reached through Normal via w:basedOn) actually gets applied differs.
        withoutStyles.Should().Be("1", "without a MainDocumentPart, style resolution is skipped and the flat 11pt default applies");
        int.Parse(withStyles).Should().BeGreaterThan(int.Parse(withoutStyles), "the real 44pt style must wrap this text onto enough extra lines to push the bookmark forward");
    }

    private static Body BuildBody(string text)
        => new Body(
            new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Big" }),
                new Run(new Text(text))),
            new Paragraph(
                new BookmarkStart { Id = "1", Name = "after" },
                new Run(new Text("na")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(BuildPageRefField("after")),
            new SectionProperties(
                new PageSize { Width = 11909U, Height = 16834U },
                new PageMargin { Top = 1440, Bottom = 1440, Left = 1440U, Right = 1440U }));

    /// <summary>Deep-clones a body so each resolution runs against an independent tree.</summary>
    private static Body CloneBody(Body body) => (Body)body.CloneNode(true);

    private static string ResolveOn(Body body, MainDocumentPart? mainPart)
    {
        if (mainPart is null)
            DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);
        else
            DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body, mainPart);

        var last = body.Elements<Paragraph>().Last(p => p.Descendants<Text>().Any());
        return last.Descendants<Text>().Last().Text;
    }

    /// <summary>
    /// Builds a real in-memory WordprocessingDocument with a StyleDefinitionsPart: docDefaults at
    /// 11pt, a "Normal" style with no override (inherits the 11pt default), and "Big" based on
    /// Normal, overriding only the font size to 44pt - so resolving it correctly requires reading
    /// docDefaults, the style's own basedOn link, and the override, not just one of the three.
    /// </summary>
    private static WordprocessingDocument BuildDocumentWithStyles(Body body, MemoryStream stream)
    {
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(body);

        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(new FontSize { Val = NormalFontHalfPoints }))),
            new Style(
                new StyleName { Val = "Normal" },
                new PrimaryStyle())
            {
                StyleId = "Normal",
                Type = StyleValues.Paragraph,
                Default = true,
            },
            new Style(
                new StyleName { Val = "Big" },
                new BasedOn { Val = "Normal" },
                new StyleRunProperties(new FontSize { Val = BigFontHalfPoints }))
            {
                StyleId = "Big",
                Type = StyleValues.Paragraph,
            });
        stylesPart.Styles.Save();

        return doc;
    }

    private static OpenXmlElement[] BuildPageRefField(string bookmarkName) =>
    [
        new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
        new Run(new FieldCode($" PAGEREF {bookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
        new Run(new Text("?") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
    ];
}
