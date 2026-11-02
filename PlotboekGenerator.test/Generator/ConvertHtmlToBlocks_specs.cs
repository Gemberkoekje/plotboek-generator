#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;
using System.IO;

namespace PlotboekGenerator.test.Generator;

public sealed class ConvertHtmlToBlocks_specs
{
    // Mirrors the semantic HTML Quill 2.0 stores for a pasted table:
    // rows wrapped in <tr>, cells carrying a data-row attribute and an
    // optional ql-align-* class.
    private const string QuillTableHtml =
        "<table style=\"border: 1px solid #000;\"><tbody>" +
        "<tr><td data-row=\"1\"></td><td data-row=\"1\">HP</td></tr>" +
        "<tr><td data-row=\"2\">ijsheksen</td><td data-row=\"2\" class=\"ql-align-right\">8</td></tr>" +
        "</tbody></table>";

    [Test]
    public void Converts_table_to_word_table()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null);

        blocks.OfType<Table>().Should().ContainSingle();
    }

    [Test]
    public void Preserves_rows_and_cells()
    {
        var table = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null).OfType<Table>().Single();

        var rows = table.Elements<TableRow>().ToList();
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(r => r.Elements<TableCell>().Count() == 2);
    }

    [Test]
    public void Preserves_cell_text()
    {
        var table = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null).OfType<Table>().Single();

        var secondRow = table.Elements<TableRow>().ElementAt(1);
        var texts = secondRow.Elements<TableCell>().Select(c => c.InnerText).ToList();

        texts.Should().Equal("ijsheksen", "8");
    }

    [Test]
    public void Applies_cell_alignment_from_ql_align_class()
    {
        var table = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null).OfType<Table>().Single();

        var alignedCell = table.Elements<TableRow>().ElementAt(1).Elements<TableCell>().ElementAt(1);
        var justification = alignedCell.Descendants<Justification>().Single();

        justification.Val!.Value.Should().Be(JustificationValues.Right);
    }

    [Test]
    public void Unaligned_cell_has_no_justification()
    {
        var table = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null).OfType<Table>().Single();

        var plainCell = table.Elements<TableRow>().ElementAt(1).Elements<TableCell>().ElementAt(0);

        plainCell.Descendants<Justification>().Should().BeEmpty();
    }

    [Test]
    public void Table_has_borders()
    {
        var table = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null).OfType<Table>().Single();

        table.Descendants<TableBorders>().Should().ContainSingle();
    }

    [Test]
    public void Table_is_followed_by_a_paragraph()
    {
        // A table may not be the final block, or Word treats the file as corrupt.
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null);

        blocks[^1].Should().BeOfType<Paragraph>();
    }

    [Test]
    public void Falls_back_to_data_row_grouping_without_tr_wrappers()
    {
        const string html =
            "<table><tbody>" +
            "<td data-row=\"1\">A</td><td data-row=\"1\">B</td>" +
            "<td data-row=\"2\">C</td>" +
            "</tbody></table>";

        var table = DocxTemplateEngine.ConvertHtmlToBlocks(html, null).OfType<Table>().Single();

        var rows = table.Elements<TableRow>().ToList();
        rows.Should().HaveCount(2);
        rows[0].Elements<TableCell>().Select(c => c.InnerText).Should().Equal("A", "B");
        // Short rows are padded so the table stays rectangular.
        rows[1].Elements<TableCell>().Should().HaveCount(2);
        rows[1].Elements<TableCell>().First().InnerText.Should().Be("C");
    }

    [Test]
    public void Keeps_paragraphs_and_table_in_document_order()
    {
        const string html = "<p>Intro</p>" +
            "<table><tr><td>X</td></tr></table>" +
            "<p>Outro</p>";

        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks(html, null);

        blocks.Should().HaveCount(3);
        blocks[0].Should().BeOfType<Paragraph>().Which.InnerText.Should().Be("Intro");
        blocks[1].Should().BeOfType<Table>();
        blocks[2].Should().BeOfType<Paragraph>().Which.InnerText.Should().Be("Outro");
    }

    [Test]
    public void Applies_paragraph_alignment_from_ql_align_class()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks("<p class=\"ql-align-center\">Centered</p>", null);

        var justification = blocks.Single().Descendants<Justification>().Single();
        justification.Val!.Value.Should().Be(JustificationValues.Center);
    }

    [Test]
    public void Renders_strikethrough()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks("<p><s>gone</s></p>", null);

        var run = blocks.Single().Descendants<Run>().Single();
        run.RunProperties!.Strike.Should().NotBeNull();
    }

    [Test]
    public void Renders_heading_as_bold_sized_text_without_a_style()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks("<h2>Title</h2>", null);

        var para = blocks.Single().Should().BeOfType<Paragraph>().Subject;
        var run = para.Descendants<Run>().Single();

        run.RunProperties!.Bold.Should().NotBeNull();
        run.RunProperties.FontSize!.Val!.Value.Should().Be("28");
        // No heading style, so the auto Table of Contents does not pick it up.
        para.ParagraphProperties?.ParagraphStyleId.Should().BeNull();
    }

    [Test]
    public void Preserves_bold_italic_underline_inside_cells()
    {
        const string html = "<table><tr><td><strong>b</strong><em>i</em><u>u</u></td></tr></table>";

        var table = DocxTemplateEngine.ConvertHtmlToBlocks(html, null).OfType<Table>().Single();
        var runs = table.Descendants<Run>().ToList();

        runs.Should().Contain(r => r.RunProperties != null && r.RunProperties.Bold != null);
        runs.Should().Contain(r => r.RunProperties != null && r.RunProperties.Italic != null);
        runs.Should().Contain(r => r.RunProperties != null && r.RunProperties.Underline != null);
    }

    [Test]
    public void Empty_html_yields_a_single_paragraph()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks("<p></p>", null);

        blocks.Should().ContainSingle().Which.Should().BeOfType<Paragraph>();
    }

    [TestCase("<table><tr><td>x</td></tr></table>")]
    [TestCase("<h1>Heading</h1>")]
    [TestCase("<p>paragraph</p>")]
    [TestCase("<ul><li>item</li></ul>")]
    [TestCase("<ol><li>item</li></ol>")]
    public void IsHtmlContent_detects_block_markup(string value)
    {
        DocxTemplateEngine.IsHtmlContent(value).Should().BeTrue();
    }

    [TestCase("plain text")]
    [TestCase("")]
    [TestCase("<span>inline only</span>")]
    public void IsHtmlContent_rejects_non_block_content(string value)
    {
        DocxTemplateEngine.IsHtmlContent(value).Should().BeFalse();
    }

    [Test]
    public void Handles_text_table_list_text_table_in_order()
    {
        const string html =
            "<p>Some intro.</p>" +
            "<table><tbody><tr><td data-row=\"1\">R1C1</td><td data-row=\"1\">R1C2</td></tr></tbody></table>" +
            "<ul><li>alpha</li><li><a href=\"https://example.com\">beta link</a></li></ul>" +
            "<p>More text between.</p>" +
            "<table><tbody><tr><td data-row=\"1\">X</td></tr></tbody></table>";

        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks(html, null);

        // Intro para, table, two list items, middle para, table, trailing para.
        blocks.Select(b => b.GetType().Name).Should().Equal(
            nameof(Paragraph),
            nameof(Table),
            nameof(Paragraph),
            nameof(Paragraph),
            nameof(Paragraph),
            nameof(Table),
            nameof(Paragraph));

        blocks[0].InnerText.Should().Be("Some intro.");
        blocks[1].Should().BeOfType<Table>().Which.InnerText.Should().Contain("R1C1");
        blocks[2].InnerText.Should().Be("• alpha");
        // A hyperlink keeps its visible text; the href is not emitted.
        blocks[3].InnerText.Should().Be("• beta link");
        blocks[4].InnerText.Should().Be("More text between.");
        blocks[5].Should().BeOfType<Table>().Which.InnerText.Should().Contain("X");

        // The whole sequence is still schema-valid (no corrupt document).
        ValidateAsDocumentBody(blocks).Should().BeEmpty();
    }

    [Test]
    public void Produces_schema_valid_openxml_for_a_table()
    {
        var blocks = DocxTemplateEngine.ConvertHtmlToBlocks(QuillTableHtml, null);

        var errors = ValidateAsDocumentBody(blocks);

        errors.Should().BeEmpty();
    }

    [Test]
    public void Produces_schema_valid_openxml_for_mixed_content()
    {
        const string html = "<h1>Header</h1><p>Text <strong>bold</strong></p>" +
            "<ul><li>one</li><li>two</li></ul>" + QuillTableHtml + "<p>After</p>";

        var errors = ValidateAsDocumentBody(DocxTemplateEngine.ConvertHtmlToBlocks(html, null));

        errors.Should().BeEmpty();
    }

    private static IReadOnlyList<ValidationErrorInfo> ValidateAsDocumentBody(List<OpenXmlElement> blocks)
    {
        using var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document(new Body());

        foreach (var block in blocks)
            main.Document.Body!.AppendChild(block.CloneNode(true));
        main.Document.Body!.AppendChild(new SectionProperties());

        return new OpenXmlValidator().Validate(doc).ToList();
    }
}
