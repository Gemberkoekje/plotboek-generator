#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class RemoveNativeTocField_specs
{
    [Test]
    public void Does_nothing_when_body_has_no_TOC()
    {
        var body = new Body(
            new Paragraph(new Run(new Text("Hello"))),
            new Paragraph(new Run(new Text("World"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
    }

    [Test]
    public void Removes_SDT_wrapped_TOC_matched_by_gallery_name()
    {
        var sdtBlock = CreateSdtBlockWithGallery("Table of Contents");
        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            sdtBlock,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    [Test]
    public void Removes_SDT_wrapped_TOC_and_trailing_empty_paragraph()
    {
        var sdtBlock = CreateSdtBlockWithGallery("Table of Contents");
        var trailingEmpty = new Paragraph();
        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            sdtBlock,
            trailingEmpty,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    [Test]
    public void Does_not_remove_trailing_paragraph_with_content()
    {
        var sdtBlock = CreateSdtBlockWithGallery("Table of Contents");
        var body = new Body(
            sdtBlock,
            new Paragraph(new Run(new Text("Keep me"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().ContainSingle();
        body.Descendants<Text>().Single().Text.Should().Be("Keep me");
    }

    [Test]
    public void Removes_SDT_wrapped_TOC_matched_by_TOC_field_inside_SDT()
    {
        var sdtBlock = CreateSdtBlockWithTocField();
        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            sdtBlock,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    [Test]
    public void Removes_field_based_TOC_spanning_multiple_paragraphs()
    {
        // Simulate: [FieldChar Begin] [FieldCode "TOC ..."] [FieldChar Separate]
        //           ... content paragraphs ...
        //           [FieldChar End]
        var beginPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" TOC \\o \"1-3\" ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));

        var contentPara = new Paragraph(new Run(new Text("Chapter 1.........1")));

        var endPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            beginPara,
            contentPara,
            endPara,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    [Test]
    public void Removes_field_based_TOC_and_trailing_empty_paragraph()
    {
        var beginPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" TOC \\o \"1-3\" ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));

        var endPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var body = new Body(
            beginPara,
            endPara,
            new Paragraph(), // trailing empty
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().ContainSingle();
        body.Descendants<Text>().Single().Text.Should().Be("After");
    }

    [Test]
    public void Removes_field_based_TOC_with_nested_fields()
    {
        // Outer TOC field contains an inner PAGEREF field
        var beginPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" TOC \\o \"1-3\" ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));

        var nestedFieldPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" PAGEREF _Toc123 ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
            new Run(new Text("5")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var endPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            beginPara,
            nestedFieldPara,
            endPara,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    [Test]
    public void Gallery_name_match_is_case_insensitive()
    {
        var sdtBlock = CreateSdtBlockWithGallery("TABLE OF CONTENTS");
        var body = new Body(sdtBlock);

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().BeEmpty();
    }

    [Test]
    public void Field_code_match_is_case_insensitive()
    {
        var beginPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" toc \\o \"1-3\" ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var body = new Body(beginPara);

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().BeEmpty();
    }

    [Test]
    public void Does_not_remove_SDT_without_TOC_gallery()
    {
        var sdtBlock = CreateSdtBlockWithGallery("Cover Pages");
        var body = new Body(sdtBlock);

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().ContainSingle();
    }

    [Test]
    public void Removes_field_based_TOC_contained_in_single_paragraph()
    {
        var tocPara = new Paragraph(
            new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode("TOC \\h")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
            new Run(new Text("Contents")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

        var body = new Body(
            new Paragraph(new Run(new Text("Before"))),
            tocPara,
            new Paragraph(new Run(new Text("After"))));

        DocxTemplateEngine.RemoveNativeTocField(body);

        body.ChildElements.Should().HaveCount(2);
        body.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("Before", "After");
    }

    /// <summary>
    /// Creates an <see cref="SdtBlock"/> that mimics a modern Word TOC
    /// wrapped in a structured document tag with a gallery name.
    /// </summary>
    private static SdtBlock CreateSdtBlockWithGallery(string galleryName)
    {
        return new SdtBlock(
            new SdtProperties(
                new SdtContentDocPartObject(
                    new DocPartGallery { Val = galleryName })),
            new SdtContentBlock(
                new Paragraph(new Run(new Text("TOC content")))));
    }

    /// <summary>
    /// Creates an <see cref="SdtBlock"/> that contains a TOC field
    /// (identified by a FieldChar Begin followed by a FieldCode starting
    /// with "TOC").
    /// </summary>
    private static SdtBlock CreateSdtBlockWithTocField()
    {
        return new SdtBlock(
            new SdtProperties(),
            new SdtContentBlock(
                new Paragraph(
                    new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
                    new Run(new FieldCode(" TOC \\o \"1-3\" ")),
                    new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
                    new Run(new Text("Contents")),
                    new Run(new FieldChar { FieldCharType = FieldCharValues.End }))));
    }
}


