#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.core.Generator.Templates;

using Color = DocumentFormat.OpenXml.Wordprocessing.Color;

namespace PlotboekGenerator.test.Generator;

public sealed class ExpandRepeater_Binding_specs
{
    [Test]
    public void Removes_parent_table_row_when_value_is_empty()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithFieldInTableRow("Items", "Value");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Value = string.Empty } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // The table row containing the empty-value field should have been removed
        body.Descendants<TableRow>().Should().BeEmpty();
    }

    [Test]
    public void Binds_HTML_content_to_rich_text()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p>Hello <strong>World</strong></p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain(t => t.Contains("Hello"));
        texts.Should().Contain(t => t.Contains("World"));
    }

    [Test]
    public void HTML_strong_tag_produces_bold_run()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p><strong>Bold</strong></p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var boldRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text?.Contains("Bold") == true);
        boldRun.Should().NotBeNull();
        boldRun!.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
    }

    [Test]
    public void HTML_em_tag_produces_italic_run()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p><em>Italic</em></p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var italicRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text?.Contains("Italic") == true);
        italicRun.Should().NotBeNull();
        italicRun!.RunProperties!.GetFirstChild<Italic>().Should().NotBeNull();
    }

    [Test]
    public void HTML_u_tag_produces_underline_run()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p><u>Underlined</u></p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var underlineRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text?.Contains("Underlined") == true);
        underlineRun.Should().NotBeNull();
        underlineRun!.RunProperties!.GetFirstChild<Underline>().Should().NotBeNull();
    }

    [Test]
    public void HTML_unordered_list_produces_bullet_prefix()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<ul><li>First</li><li>Second</li></ul>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain(t => t.Contains('•'));
        texts.Should().Contain(t => t.Contains("First"));
        texts.Should().Contain(t => t.Contains("Second"));
    }

    [Test]
    public void HTML_ordered_list_produces_numbered_prefix()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<ol><li>Alpha</li><li>Beta</li></ol>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain(t => t.Contains("1."));
        texts.Should().Contain(t => t.Contains("2."));
        texts.Should().Contain(t => t.Contains("Alpha"));
    }

    [Test]
    public void HTML_br_tag_produces_break_element()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p>Line1<br/>Line2</p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<Break>().Should().NotBeEmpty();
    }

    [Test]
    public void Applies_placeholder_style_for_NogNietIngevuld()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { NaamEncounter = PageData.NogNietIngevuld } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var styledRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == PageData.NogNietIngevuld);
        styledRun.Should().NotBeNull();
        styledRun!.RunProperties!.GetFirstChild<Italic>().Should().NotBeNull();
        styledRun.RunProperties!.GetFirstChild<Color>()!.Val!.Value.Should().Be("8B0045");
    }

    [Test]
    public void Applies_incomplete_style_when_IsVolledigKlaar_is_false()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { NaamEncounter = "Some encounter", IsVolledigKlaar = false } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var styledRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "Some encounter");
        styledRun.Should().NotBeNull();
        styledRun!.RunProperties!.GetFirstChild<Italic>().Should().NotBeNull();
        styledRun.RunProperties!.GetFirstChild<Color>()!.Val!.Value.Should().Be("8B0045");
    }

    [Test]
    public void Does_not_apply_incomplete_style_when_IsVolledigKlaar_is_true()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { NaamEncounter = "Complete encounter", IsVolledigKlaar = true } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var styledRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "Complete encounter");
        styledRun.Should().NotBeNull();
        (styledRun!.RunProperties?.GetFirstChild<Color>()?.Val?.Value).Should().NotBe("8B0045");
    }

    [Test]
    public void Applies_meeting_style_when_IsMeeting_is_true()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithFieldInTableRow("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { NaamEncounter = "Meeting", IsMeeting = true } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // Meeting rows should have bold runs
        var meetingRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "Meeting");
        meetingRun.Should().NotBeNull();
        meetingRun!.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();

        // Meeting rows should have shaded cells
        body.Descendants<Shading>().Should().NotBeEmpty();
    }

    [Test]
    public void Forces_black_text_on_tijdschema_items()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithFieldInTableRow("Items", "NaamEncounter");
        body.AppendChild(repeater);

        // IsMeeting property presence marks it as a tijdschema item
        var model = new { Items = new[] { new { NaamEncounter = "Encounter", IsMeeting = false } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var coloredRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "Encounter");
        coloredRun.Should().NotBeNull();
        coloredRun!.RunProperties!.GetFirstChild<Color>()!.Val!.Value.Should().Be("000000");
    }

    [Test]
    public void Hides_top_border_between_grouped_rows()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithFieldInTableRow("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new
        {
            Items = new[]
            {
                new { NaamEncounter = "First", HideTopBorder = false, IsMeeting = false },
                new { NaamEncounter = "Second", HideTopBorder = true, IsMeeting = false },
            },
        };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // The second clone's cells should have nil top borders
        var sdtBlocks = body.ChildElements.OfType<SdtBlock>().ToList();
        sdtBlocks.Should().HaveCount(2);

        var secondCloneCells = sdtBlocks[1].Descendants<TableCell>().ToList();
        secondCloneCells.Should().NotBeEmpty();
        secondCloneCells[0].TableCellProperties!.TableCellBorders!.TopBorder!.Val!.Value.Should().Be(BorderValues.Nil);
    }

    [Test]
    public void Applies_page_break_before_when_PageBreakBefore_is_true()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithFieldInTableRow("Items", "NaamEncounter");
        body.AppendChild(repeater);

        var model = new
        {
            Items = new[]
            {
                new { NaamEncounter = "Daytime", IsMeeting = false, PageBreakBefore = false },
                new { NaamEncounter = "Evening", IsMeeting = false, PageBreakBefore = true },
            },
        };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var sdtBlocks = body.ChildElements.OfType<SdtBlock>().ToList();
        sdtBlocks.Should().HaveCount(2);

        // First (daytime) row keeps flowing; second (evening) row starts a new page.
        sdtBlocks[0].Descendants<PageBreakBefore>().Should().BeEmpty();
        sdtBlocks[1].Descendants<PageBreakBefore>().Should().NotBeEmpty();
    }

    [Test]
    public void Resolves_dot_path_properties()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "Name");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Name = "Resolved" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<Text>().Select(t => t.Text)
            .Should().Contain("Resolved");
    }

    [Test]
    public void Binds_text_with_newlines_to_multiple_lines()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "Name");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Name = "Line1\nLine2" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // Newlines are rendered as Break elements in the run
        body.Descendants<Break>().Should().NotBeEmpty();
        body.Descendants<Text>().Select(t => t.Text).Should().Contain("Line1");
        body.Descendants<Text>().Select(t => t.Text).Should().Contain("Line2");
    }

    [Test]
    public void Skips_binding_for_null_property_value()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "NonExistent");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Other = "value" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        // Original placeholder text should remain since NonExistent is not resolved
        body.Descendants<Text>().Select(t => t.Text)
            .Should().Contain("placeholder");
    }

    [Test]
    public void HTML_with_nested_formatting_tags()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p><strong><em>BoldItalic</em></strong></p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var formattedRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text?.Contains("BoldItalic") == true);
        formattedRun.Should().NotBeNull();
        formattedRun!.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
        formattedRun.RunProperties!.GetFirstChild<Italic>().Should().NotBeNull();
    }

    [Test]
    public void HTML_with_multiple_paragraphs()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p>Para1</p><p>Para2</p><p>Para3</p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain("Para1");
        texts.Should().Contain("Para2");
        texts.Should().Contain("Para3");
    }

    [Test]
    public void HTML_entities_are_decoded()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithBlockField("Items", "Description");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { Description = "<p>A &amp; B &lt; C</p>" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var texts = body.Descendants<Text>().Select(t => t.Text).ToList();
        texts.Should().Contain(t => t.Contains("A & B < C"));
    }

    [Test]
    public void Binds_SamenvattingPlot_with_incomplete_style()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "SamenvattingPlot");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { SamenvattingPlot = "Some plot", IsVolledigKlaar = false } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        var styledRun = body.Descendants<Run>()
            .FirstOrDefault(r => r.GetFirstChild<Text>()?.Text == "Some plot");
        styledRun.Should().NotBeNull();
        styledRun!.RunProperties!.GetFirstChild<Italic>().Should().NotBeNull();
    }

    [Test]
    public void Inserts_PAGEREF_field_when_Bookmark_property_exists()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "MeerInformatie");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { MeerInformatie = "See more", MeerInformatieBookmark = "_bm_info" } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<FieldCode>().Should().NotBeEmpty();
        body.Descendants<FieldCode>().First().Text.Should().Contain("PAGEREF");
        body.Descendants<FieldCode>().First().Text.Should().Contain("_bm_info");
    }

    [Test]
    public void Does_not_insert_PAGEREF_when_bookmark_is_empty()
    {
        var body = new Body();
        var repeater = CreateRepeaterWithTextField("Items", "MeerInformatie");
        body.AppendChild(repeater);

        var model = new { Items = new[] { new { MeerInformatie = "See more", MeerInformatieBookmark = string.Empty } } };

        DocxTemplateEngine.ExpandRepeater(repeater, model, "Items");

        body.Descendants<FieldCode>().Should().BeEmpty();
        body.Descendants<Text>().Select(t => t.Text).Should().Contain("See more");
    }

    /// <summary>
    /// Creates a repeater containing a text field SDT within a paragraph.
    /// </summary>
    private static SdtBlock CreateRepeaterWithTextField(string repeaterTag, string fieldTag) =>
        new (
            new SdtProperties(
                new Tag { Val = repeaterTag },
                new SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(
                    new SdtProperties(new SdtRepeatedSectionItem()),
                    new SdtContentBlock(
                        new SdtBlock(
                            new SdtProperties(new Tag { Val = fieldTag }),
                            new SdtContentBlock(
                                new Paragraph(new Run(new Text("placeholder")))))))));

    /// <summary>
    /// Creates a repeater with a text field SDT placed inside a table row,
    /// enabling tests for table-row removal and cell border styling.
    /// </summary>
    private static SdtBlock CreateRepeaterWithFieldInTableRow(string repeaterTag, string fieldTag)
    {
        var innerSdt = new SdtRun(
            new SdtProperties(new Tag { Val = fieldTag }),
            new SdtContentRun(new Run(new Text("placeholder"))));

        var cell = new TableCell(
            new TableCellProperties(),
            new Paragraph(innerSdt));

        var row = new TableRow(cell);
        var table = new Table(new TableProperties(), row);

        return new SdtBlock(
            new SdtProperties(
                new Tag { Val = repeaterTag },
                new SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(
                    new SdtProperties(new SdtRepeatedSectionItem()),
                    new SdtContentBlock(table))));
    }

    /// <summary>
    /// Creates a repeater with an SdtBlock field, suitable for rich text / HTML binding.
    /// </summary>
    private static SdtBlock CreateRepeaterWithBlockField(string repeaterTag, string fieldTag) =>
        new (
            new SdtProperties(
                new Tag { Val = repeaterTag },
                new SdtRepeatedSection()),
            new SdtContentBlock(
                new SdtBlock(
                    new SdtProperties(new SdtRepeatedSectionItem()),
                    new SdtContentBlock(
                        new SdtBlock(
                            new SdtProperties(new Tag { Val = fieldTag }),
                            new SdtContentBlock(
                                new Paragraph(
                                    new Run(
                                        new RunProperties(),
                                        new Text("placeholder")))))))));
}


