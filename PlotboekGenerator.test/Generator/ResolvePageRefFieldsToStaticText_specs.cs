#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class ResolvePageRefFieldsToStaticText_specs
{
    [Test]
    public void Does_nothing_when_body_has_no_fields()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("Hello"))));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Hello");
    }

    [Test]
    public void Replaces_PAGEREF_field_with_static_page_number()
    {
        var body = new Body(
            new Paragraph( // Bookmark on page 1
                new BookmarkStart { Id = "1", Name = "myBookmark" },
                new Run(new Text("Chapter 1")),
                new BookmarkEnd { Id = "1" }),
            // PAGEREF field referencing myBookmark
            new Paragraph(
                BuildPageRefField("myBookmark")));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        // The PAGEREF field should be replaced with "1"
        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain("1");
        texts.Should().NotContain("?");
    }

    [Test]
    public void Resolves_page_number_after_page_break()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("Page 1 content"))),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            // Bookmark on page 2
            new Paragraph(
                new BookmarkStart { Id = "1", Name = "section2" },
                new Run(new Text("Page 2 content")),
                new BookmarkEnd { Id = "1" }),
            // PAGEREF field
            new Paragraph(
                BuildPageRefField("section2")));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain("2");
    }

    [Test]
    public void Leaves_non_PAGEREF_fields_untouched()
    {
        var body = new Body(
            new Paragraph(
                new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(new FieldCode(" DATE ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
                new Run(new Text("01/01/2025")),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End })));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        body.Descendants<Text>().Select(t => t.Text)
            .Should().Contain("01/01/2025");
    }

    [Test]
    public void Ignores_PAGEREF_with_unknown_bookmark()
    {
        var body = new Body(
            new Paragraph(
                BuildPageRefField("nonexistent")));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        // Field remains (not resolved)
        body.Descendants<FieldChar>().Should().NotBeEmpty();
    }

    [Test]
    public void Resolves_multiple_PAGEREF_fields()
    {
        var body = new Body(
            new Paragraph(
                new BookmarkStart { Id = "1", Name = "bm1" },
                new Run(new Text("Section A")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            new Paragraph(
                new BookmarkStart { Id = "2", Name = "bm2" },
                new Run(new Text("Section B")),
                new BookmarkEnd { Id = "2" }),
            // Two PAGEREF fields
            new Paragraph(BuildPageRefField("bm1")),
            new Paragraph(BuildPageRefField("bm2")));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain("1");
        texts.Should().Contain("2");
    }

    [Test]
    public void Preserves_formatting_from_result_run()
    {
        var resultRunProps = new RunProperties(new Bold());
        var body = new Body(
            new Paragraph(
                new BookmarkStart { Id = "1", Name = "bm" },
                new Run(new Text("Content")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(
                new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(new FieldCode(" PAGEREF bm \\h ") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
                new Run(resultRunProps, new Text("?") { Space = SpaceProcessingModeValues.Preserve }),
                new Run(new FieldChar { FieldCharType = FieldCharValues.End })));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        var replacementRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "1");
        replacementRun.Should().NotBeNull();
        replacementRun!.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
    }

    [Test]
    public void Page_number_increments_per_page_break()
    {
        var body = new Body(
            new Paragraph(
                new BookmarkStart { Id = "1", Name = "p1" },
                new Run(new Text("A")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            new Paragraph(
                new BookmarkStart { Id = "2", Name = "p3" },
                new Run(new Text("C")),
                new BookmarkEnd { Id = "2" }),
            new Paragraph(BuildPageRefField("p1")),
            new Paragraph(BuildPageRefField("p3")));

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain("1");
        texts.Should().Contain("3");
    }

    /// <summary>
    /// Builds a sequence of runs representing a PAGEREF complex field.
    /// </summary>
    private static OpenXmlElement[] BuildPageRefField(string bookmarkName) =>
    [
        new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
        new Run(new FieldCode($" PAGEREF {bookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
        new Run(new Text("?") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
    ];
}


