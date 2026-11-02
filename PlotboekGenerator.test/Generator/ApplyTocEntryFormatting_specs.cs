#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

public sealed class ApplyTocEntryFormatting_specs
{
    [Test]
    public void Does_nothing_when_entry_has_no_cells()
    {
        var row = new TableRow(new Paragraph(new Run(new Text("No cells"))));

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        // Should not throw or crash
        row.Descendants<Text>().Select(t => t.Text)
            .Should().BeEquivalentTo("No cells");
    }

    [Test]
    public void Sets_exact_row_height()
    {
        var row = CreateTocRow("Item", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        var rowHeight = row.Descendants<TableRowHeight>().Single();
        rowHeight.Val!.Value.Should().Be(280U);
        rowHeight.HeightType!.Value.Should().Be(HeightRuleValues.Exact);
    }

    [Test]
    public void Removes_cell_borders()
    {
        var row = CreateTocRow("Item", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        foreach (var cell in row.Descendants<TableCell>())
        {
            var borders = cell.TableCellProperties!.TableCellBorders!;
            borders.TopBorder!.Val!.Value.Should().Be(BorderValues.None);
            borders.BottomBorder!.Val!.Value.Should().Be(BorderValues.None);
            borders.LeftBorder!.Val!.Value.Should().Be(BorderValues.None);
            borders.RightBorder!.Val!.Value.Should().Be(BorderValues.None);
        }
    }

    [Test]
    public void Sets_condensed_spacing_on_paragraphs()
    {
        var row = CreateTocRow("Item", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        foreach (var para in row.Descendants<Paragraph>())
        {
            var spacing = para.ParagraphProperties!.SpacingBetweenLines!;
            spacing.Before!.Value.Should().Be("0");
            spacing.After!.Value.Should().Be("0");
            spacing.Line!.Value.Should().Be("240");
            spacing.LineRule!.Value.Should().Be(LineSpacingRuleValues.Exact);
        }
    }

    [Test]
    public void Heading_level_1_makes_first_cell_text_bold()
    {
        var row = CreateTocRow("Chapter", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        var firstCellRuns = row.Descendants<TableCell>().First()
            .Descendants<Run>().ToList();
        firstCellRuns.Should().AllSatisfy(r =>
            r.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull());
    }

    [Test]
    public void Heading_level_2_does_not_apply_bold()
    {
        var row = CreateTocRow("SubSection", "2");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 2);

        var firstCellRuns = row.Descendants<TableCell>().First()
            .Descendants<Run>().ToList();
        firstCellRuns.Should().AllSatisfy(r =>
        {
            var bold = r.RunProperties?.GetFirstChild<Bold>();
            bold.Should().BeNull();
        });
    }

    [Test]
    public void Heading_level_2_adds_indentation()
    {
        var row = CreateTocRow("SubSection", "2");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 2);

        var firstCellPara = row.Descendants<TableCell>().First()
            .Elements<Paragraph>().First();
        firstCellPara.ParagraphProperties!.Indentation!.Left!.Value
            .Should().Be("360");
    }

    [Test]
    public void Heading_level_3_adds_double_indentation()
    {
        var row = CreateTocRow("SubSubSection", "3");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 3);

        var firstCellPara = row.Descendants<TableCell>().First()
            .Elements<Paragraph>().First();
        firstCellPara.ParagraphProperties!.Indentation!.Left!.Value
            .Should().Be("720");
    }

    [Test]
    public void Heading_level_1_does_not_add_indentation()
    {
        var row = CreateTocRow("Top", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        var firstCellPara = row.Descendants<TableCell>().First()
            .Elements<Paragraph>().First();
        firstCellPara.ParagraphProperties!.Indentation.Should().BeNull();
    }

    [Test]
    public void Last_cell_is_right_aligned()
    {
        var row = CreateTocRow("Item", "5");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        var lastCellPara = row.Descendants<TableCell>().Last()
            .Elements<Paragraph>().First();
        lastCellPara.ParagraphProperties!.Justification!.Val!.Value
            .Should().Be(JustificationValues.Right);
    }

    [Test]
    public void Last_cell_text_is_forced_black()
    {
        var row = CreateTocRow("Item", "5");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        var lastCellRuns = row.Descendants<TableCell>().Last()
            .Descendants<Run>().ToList();
        lastCellRuns.Should().AllSatisfy(r =>
            r.RunProperties!.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Color>()!
                .Val!.Value.Should().Be("000000"));
    }

    [Test]
    public void Sets_small_cell_margins()
    {
        var row = CreateTocRow("Item", "1");

        DocxTemplateEngine.ApplyTocEntryFormatting(row, 1);

        foreach (var cell in row.Descendants<TableCell>())
        {
            var margin = cell.TableCellProperties!.TableCellMargin!;
            margin.TopMargin!.Width!.Value.Should().Be("15");
            margin.BottomMargin!.Width!.Value.Should().Be("15");
        }
    }

    /// <summary>
    /// Creates a minimal table wrapping a two-cell row for TOC testing (item cell + page cell).
    /// The method is an SdtBlock containing a table row, mirroring production structure.
    /// </summary>
    private static Table CreateTocRow(string itemText, string pageText)
    {
        return new Table(
            new TableRow(
                new TableCell(
                    new Paragraph(new Run(new Text(itemText)))),
                new TableCell(
                    new Paragraph(new Run(new Text(pageText))))));
    }
}


