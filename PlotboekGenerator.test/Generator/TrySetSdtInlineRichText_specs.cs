#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class TrySetSdtInlineRichText_specs
{
    [Test]
    public void Returns_false_when_sdt_has_no_table_cell_ancestor_and_no_paragraph_parent()
    {
        var body = new Body();
        var sdt = new SdtRun();
        body.AppendChild(sdt);

        var result = DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("Hello"));

        result.Should().BeFalse();
    }

    [Test]
    public void Returns_false_when_sdt_parent_paragraph_has_no_parent()
    {
        var para = new Paragraph();
        var sdt = new SdtRun();
        para.AppendChild(sdt);

        var result = DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("Hello"));

        result.Should().BeFalse();
    }

    [Test]
    public void Inserts_paragraphs_into_ancestor_table_cell()
    {
        var (sdt, cell) = CreateSdtInTableCell();
        var paragraphs = CreateParagraphs("First", "Second");

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, paragraphs);

        var texts = cell.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().BeEquivalentTo("First", "Second");
    }

    [Test]
    public void Returns_true_for_table_cell_path()
    {
        var (sdt, _) = CreateSdtInTableCell();

        var result = DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("X"));

        result.Should().BeTrue();
    }

    [Test]
    public void Removes_parent_paragraph_anchor_from_table_cell()
    {
        var (sdt, cell) = CreateSdtInTableCell();

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("Replaced"));

        // The original placeholder paragraph is removed; only the new one remains.
        cell.Elements<Paragraph>().Should().ContainSingle();
        cell.Descendants<Text>().Single().Text.Should().Be("Replaced");
    }

    [Test]
    public void Ensures_table_cell_has_paragraph_when_paragraphs_list_is_empty()
    {
        var (sdt, cell) = CreateSdtInTableCell();

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, []);

        cell.Elements<Paragraph>().Should().ContainSingle();
    }

    [Test]
    public void Inserts_paragraphs_into_table_cell_when_sdt_is_direct_child()
    {
        var cell = new TableCell();
        var sdt = new SdtRun();
        cell.AppendChild(sdt);

        // Add a dummy paragraph so the cell is valid before the call.
        cell.AppendChild(new Paragraph());

        var row = new TableRow(cell);
        _ = new Table(row);

        var paragraphs = CreateParagraphs("Direct");

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, paragraphs);

        cell.Descendants<Text>().Select(t => t.Text).Should().Contain("Direct");
    }

    [Test]
    public void Replaces_body_level_parent_paragraph_with_generated_paragraphs()
    {
        var body = new Body();
        var parentPara = new Paragraph();
        var sdt = new SdtRun();
        parentPara.AppendChild(sdt);
        body.AppendChild(parentPara);

        var paragraphs = CreateParagraphs("A", "B");

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, paragraphs);

        body.Elements<Paragraph>().Should().HaveCount(2);
        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().BeEquivalentTo("A", "B");
    }

    [Test]
    public void Returns_true_for_body_paragraph_path()
    {
        var body = new Body();
        var parentPara = new Paragraph();
        parentPara.AppendChild(new SdtRun());
        body.AppendChild(parentPara);

        var sdt = parentPara.GetFirstChild<SdtRun>();
        var result = DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("X"));

        result.Should().BeTrue();
    }

    [Test]
    public void Preserves_trailing_runs_after_sdt_in_body_paragraph()
    {
        var body = new Body();
        var parentPara = new Paragraph();
        parentPara.AppendChild(new Run(new Text("before")));
        var sdt = new SdtRun();
        parentPara.AppendChild(sdt);
        parentPara.AppendChild(new Run(new Break { Type = BreakValues.Page }));
        body.AppendChild(parentPara);

        var paragraphs = CreateParagraphs("content");

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, paragraphs);

        var lastPara = body.Elements<Paragraph>().Last();
        lastPara.Descendants<Break>().Should().ContainSingle();
    }

    [Test]
    public void Does_not_append_trailing_runs_when_paragraphs_list_is_empty()
    {
        var body = new Body();
        var parentPara = new Paragraph();
        var sdt = new SdtRun();
        parentPara.AppendChild(sdt);
        parentPara.AppendChild(new Run(new Text("trailing")));
        body.AppendChild(parentPara);

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, []);

        body.Elements<Paragraph>().Should().BeEmpty();
    }

    [Test]
    public void Removes_original_body_paragraph_after_replacement()
    {
        var body = new Body();
        var parentPara = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
        var sdt = new SdtRun();
        parentPara.AppendChild(sdt);
        body.AppendChild(parentPara);

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, CreateParagraphs("new"));

        // Only the generated paragraph should remain, not the original.
        body.Elements<Paragraph>().Should().ContainSingle();
        body.Descendants<Text>().Single().Text.Should().Be("new");
    }

    [Test]
    public void Injects_table_into_cell_and_keeps_it_ending_with_a_paragraph()
    {
        var (sdt, cell) = CreateSdtInTableCell();
        var nestedTable = new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text("nested"))))));
        List<OpenXmlElement> blocks = [nestedTable, new Paragraph()];

        DocxTemplateEngine.TrySetSdtInlineRichText(sdt, blocks);

        // The nested table is present and the cell still ends with a paragraph,
        // which OpenXML requires for a valid table cell.
        cell.Elements<Table>().Should().ContainSingle();
        cell.LastChild.Should().BeOfType<Paragraph>();
    }

    private static List<OpenXmlElement> CreateParagraphs(params string[] texts)
        => [.. texts.Select(OpenXmlElement (t) => new Paragraph(new Run(new Text(t))))];

    private static (SdtRun Sdt, TableCell Cell) CreateSdtInTableCell()
    {
        var sdt = new SdtRun();
        var para = new Paragraph(sdt);
        var cell = new TableCell(para);
        var row = new TableRow(cell);
        _ = new Table(row);
        return (sdt, cell);
    }
}


