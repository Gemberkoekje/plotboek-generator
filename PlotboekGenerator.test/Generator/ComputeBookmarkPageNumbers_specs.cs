#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// The page numbers printed in the table of contents come from a layout estimate that walks the
/// merged document simulating Word's page flow.  These specs cover what the old approach - counting
/// explicit page breaks - could not see: content that runs past the bottom of a page.
/// </summary>
public sealed class ComputeBookmarkPageNumbers_specs
{
    /// <summary>A4 minus one-inch margins leaves 13954 twips: 51 lines of 11pt Calibri.</summary>
    private const int LinesPerPage = 51;

    /// <summary>Comfortably more than one page worth of lines, comfortably less than two.</summary>
    private const int LinesPastFirstPage = 60;

    [Test]
    public void Content_overflowing_a_page_pushes_later_bookmarks_forward()
    {
        var body = new Body();
        for (var i = 0; i < LinesPastFirstPage; i++)
            body.AppendChild(Line($"regel {i}"));

        body.AppendChild(Anchored("later"));
        body.AppendChild(new Paragraph(BuildPageRefField("later")));
        body.AppendChild(A4Section());

        Resolve(body).Should().Be("2", $"{LinesPastFirstPage} lines do not fit on one page of {LinesPerPage}");
    }

    [Test]
    public void A_long_table_pushes_later_bookmarks_forward()
    {
        // Mirrors the table of contents: one single-line row per entry, spilling past the page.
        var table = new Table(new TableGrid(new GridColumn { Width = "9000" }));
        for (var i = 0; i < LinesPastFirstPage; i++)
            table.AppendChild(new TableRow(new TableCell(Line($"ingang {i}"))));

        var body = new Body(
            table,
            Anchored("after"),
            new Paragraph(BuildPageRefField("after")),
            A4Section());

        Resolve(body).Should().Be("2", "the table itself runs past the bottom of page one");
    }

    [Test]
    public void A_table_with_no_declared_column_widths_measures_cells_by_their_divided_column_share()
    {
        // Mirrors what the HTML->OOXML table converter actually produces: a GridColumn per column,
        // but with no Width set (Word auto-divides an unconstrained table's width across its
        // columns; the estimator must assume the same, not the full page width per cell).
        var wide = string.Join(' ', Enumerable.Repeat("woordje", 9));

        var table = new Table(new TableGrid(new GridColumn(), new GridColumn(), new GridColumn(), new GridColumn()));
        for (var i = 0; i < 20; i++)
        {
            table.AppendChild(new TableRow(
                new TableCell(Line(wide)),
                new TableCell(Line($"cel {i}")),
                new TableCell(Line($"cel {i}")),
                new TableCell(Line($"cel {i}"))));
        }

        var body = new Body(
            table,
            Anchored("after"),
            new Paragraph(BuildPageRefField("after")),
            A4Section());

        // At the true, quarter-page column width this content wraps onto extra lines and the table
        // spills onto a second page. Measuring every cell against the full page width (the bug)
        // would keep it all on page 1.
        Resolve(body).Should().Be("2", "cells must be measured against their real column share, not the full page width");
    }

    [Test]
    public void A_bookmark_placed_as_a_table_level_sibling_of_a_row_still_resolves()
    {
        // Mirrors what InsertBookmarkIfNeeded produces for a table-row-level repeater whose item
        // carries a BookmarkName: the bookmark ends up a direct Table child, not nested in the row.
        var table = new Table(
            new TableGrid(new GridColumn { Width = "9000" }),
            new BookmarkStart { Id = "1", Name = "rowBookmark" },
            new TableRow(new TableCell(Line("rij"))),
            new BookmarkEnd { Id = "1" });

        var body = new Body(
            table,
            new Paragraph(BuildPageRefField("rowBookmark")),
            A4Section());

        Resolve(body).Should().Be("1", "a bookmark sitting beside a row, not inside it, must still be found");
    }

    [Test]
    public void Short_content_stays_on_the_first_page()
    {
        var body = new Body(
            Line("een enkele regel"),
            Anchored("here"),
            new Paragraph(BuildPageRefField("here")),
            A4Section());

        Resolve(body).Should().Be("1");
    }

    [Test]
    public void A_paragraph_wrapping_onto_extra_lines_is_counted_per_line()
    {
        // Each paragraph is a little wider than one line, so it wraps onto two.
        var wide = string.Join(' ', Enumerable.Repeat("woordje", 20));

        var body = new Body();
        for (var i = 0; i < LinesPastFirstPage / 2; i++)
            body.AppendChild(Line(wide));

        body.AppendChild(Anchored("wrapped"));
        body.AppendChild(new Paragraph(BuildPageRefField("wrapped")));
        body.AppendChild(A4Section());

        Resolve(body).Should().Be("2", "30 paragraphs of two lines each are 60 lines, more than a page holds");
    }

    [Test]
    public void Page_break_before_starts_a_new_page()
    {
        var body = new Body(
            Line("eerste"),
            new Paragraph(
                new ParagraphProperties(new PageBreakBefore()),
                new BookmarkStart { Id = "1", Name = "forced" },
                new Run(new Text("tweede")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(BuildPageRefField("forced")),
            A4Section());

        Resolve(body).Should().Be("2");
    }

    [Test]
    public void KeepNext_moves_the_heading_along_with_the_paragraph_it_cannot_be_orphaned_from()
    {
        // 50 filler lines leave 512 twips: enough for the heading's own single line (268.84), but
        // not enough for the heading plus even the first line of what follows (537.68) - so a
        // keepNext heading must move to page 2 with it, not stay behind alone.
        var body = new Body();
        for (var i = 0; i < 50; i++)
            body.AppendChild(Line($"regel {i}"));

        body.AppendChild(new Paragraph(
            new ParagraphProperties(new KeepLines(), new KeepNext()),
            new BookmarkStart { Id = "1", Name = "heading" },
            new Run(new Text("Kop")),
            new BookmarkEnd { Id = "1" }));

        // Three lines: cannot fit in the sliver left after the heading, so widow control would
        // correctly push it to page 2 regardless - the heading must go with it.
        var wide = string.Join(' ', Enumerable.Repeat("woordje", 60));
        body.AppendChild(Line(wide));

        body.AppendChild(new Paragraph(BuildPageRefField("heading")));
        body.AppendChild(A4Section());

        Resolve(body).Should().Be("2", "keepNext must move the heading with the paragraph that can't fit alongside it");
    }

    [Test]
    public void Explicit_page_breaks_still_count()
    {
        var body = new Body(
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            new Paragraph(new Run(new Break { Type = BreakValues.Page })),
            Anchored("third"),
            new Paragraph(BuildPageRefField("third")),
            A4Section());

        Resolve(body).Should().Be("3");
    }

    [Test]
    public void Bare_page_break_landing_near_a_page_boundary_advances_only_once()
    {
        // Exactly fills the page (51 lines), leaving less than one more line of room, then a
        // page-break paragraph with no text of its own - the shape the template's own standalone
        // page breaks use. A phantom "line before the break" must not also consume a page.
        var body = new Body();
        for (var i = 0; i < LinesPerPage; i++)
            body.AppendChild(Line($"regel {i}"));

        body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        body.AppendChild(Anchored("afterBreak"));
        body.AppendChild(new Paragraph(BuildPageRefField("afterBreak")));
        body.AppendChild(A4Section());

        Resolve(body).Should().Be("2", "one page break after a full page is one page turn, not two");
    }

    [Test]
    public void Redundant_table_row_page_break_is_stripped_when_the_row_already_starts_a_fresh_page()
    {
        // Nothing precedes this row - same "nothing to push away from" shape as
        // PageBreakBefore_on_the_first_paragraph_does_not_skip_a_page below, just for a table row
        // instead of a plain paragraph - so it already starts a fresh page on its own. Its forced
        // PageBreakBefore (mirroring what ApplyPageBreakBefore places on every cell's first
        // paragraph, e.g. the "evening programme after Avondeten" row) is then a no-op per the
        // OOXML spec - but not every renderer honours that nuance faithfully (Google Docs has been
        // observed inserting a genuinely blank page for exactly this case). Generation must strip
        // the now-provably-unnecessary break rather than depend on the renderer getting it right.
        var breakRow = new TableRow(new TableCell(
            new Paragraph(new ParagraphProperties(new PageBreakBefore()), new Run(new Text("Avond")))));

        var table = new Table(new TableGrid(new GridColumn { Width = "9000" }), breakRow);
        var body = new Body(table, A4Section());

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        breakRow.Descendants<PageBreakBefore>().Should()
            .BeEmpty("the row already starts a fresh page on its own; the forced break is redundant");
    }

    [Test]
    public void A_table_row_break_is_left_in_place_when_only_the_previous_rows_estimated_height_suggests_it_is_redundant()
    {
        // Deliberately NOT stripped, even though a "Blok 22:00 ... Avondeten" row's own estimated
        // height suggests it just overflowed onto a fresh page on its own (making the very next
        // row's forced break look redundant): that judgement rests on the same approximate
        // glyph-width row-height measurements this estimator's own docs admit can be off by a few
        // percent. Being wrong would silently and permanently strip a break Google's real
        // rendering still needs - a structural error the Drive-render correction cycle can never
        // undo (it only nudges printed numbers). Only the exact, provable "nothing at all is on
        // this page yet" case (the test above) is safe enough to act on.
        var fillerRow = new TableRow(
            new TableRowProperties(new TableRowHeight { Val = 13454, HeightType = HeightRuleValues.Exact }),
            new TableCell(Line("vult bijna de hele pagina")));

        var avondetenRow = new TableRow(
            new TableRowProperties(new TableRowHeight { Val = 1000, HeightType = HeightRuleValues.Exact }),
            new TableCell(Line("Avondeten")));

        var breakRow = new TableRow(new TableCell(
            new Paragraph(new ParagraphProperties(new PageBreakBefore()), new Run(new Text("Avond programma")))));

        var table = new Table(
            new TableGrid(new GridColumn { Width = "9000" }), fillerRow, avondetenRow, breakRow);
        var body = new Body(table, A4Section());

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        breakRow.Descendants<PageBreakBefore>().Should()
            .NotBeEmpty("an estimate-based redundancy judgement is not safe enough to permanently remove a break");
    }

    [Test]
    public void A_genuinely_needed_table_row_page_break_is_left_in_place()
    {
        // Only a handful of lines precede it - nowhere near a page boundary - so the forced break
        // is actually doing work here and must survive resolution.
        var table = new Table(new TableGrid(new GridColumn { Width = "9000" }));
        for (var i = 0; i < 10; i++)
            table.AppendChild(new TableRow(new TableCell(Line($"ingang {i}"))));

        var breakRow = new TableRow(new TableCell(
            new Paragraph(new ParagraphProperties(new PageBreakBefore()), new Run(new Text("Avond")))));
        table.AppendChild(breakRow);

        var body = new Body(table, A4Section());

        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        breakRow.Descendants<PageBreakBefore>().Should()
            .NotBeEmpty("the break is genuinely needed here and must not be removed");
    }

    [Test]
    public void PageBreakBefore_on_the_first_paragraph_does_not_skip_a_page()
    {
        // Nothing precedes this paragraph, so there is no page to "push away from" - it must stay
        // on page 1, not be bumped to page 2 for no reason.
        var body = new Body(
            new Paragraph(
                new ParagraphProperties(new PageBreakBefore()),
                new BookmarkStart { Id = "1", Name = "first" },
                new Run(new Text("eerste")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(BuildPageRefField("first")),
            A4Section());

        Resolve(body).Should().Be("1");
    }

    [Test]
    public void Capitals_take_more_room_than_lowercase()
    {
        // The same character count in capitals wraps onto more lines, so it fills the page sooner.
        var lower = string.Join(' ', Enumerable.Repeat("llllllllll", 30));

        var lowerBody = FillPage(lower);
        var upperBody = FillPage(lower.ToUpperInvariant());

        var lowerPage = int.Parse(Resolve(lowerBody));
        var upperPage = int.Parse(Resolve(upperBody));

        upperPage.Should().BeGreaterThan(lowerPage, "capitals are wider, so the same text needs more lines");
    }

    private static Body FillPage(string text)
    {
        var body = new Body();
        for (var i = 0; i < 20; i++)
            body.AppendChild(Line(text));

        body.AppendChild(Anchored("end"));
        body.AppendChild(new Paragraph(BuildPageRefField("end")));
        body.AppendChild(A4Section());
        return body;
    }

    /// <summary>Resolves the single PAGEREF field in the body and returns the rendered page number.</summary>
    private static string Resolve(Body body)
    {
        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body);

        var last = body.Elements<Paragraph>().Last(p => p.Descendants<Text>().Any());
        return last.Descendants<Text>().Last().Text;
    }

    private static Paragraph Line(string text) => new Paragraph(new Run(new Text(text)));

    private static Paragraph Anchored(string bookmark) => new Paragraph(
        new BookmarkStart { Id = "9", Name = bookmark },
        new Run(new Text(bookmark)),
        new BookmarkEnd { Id = "9" });

    private static SectionProperties A4Section() => new SectionProperties(
        new PageSize { Width = 11909U, Height = 16834U },
        new PageMargin { Top = 1440, Bottom = 1440, Left = 1440U, Right = 1440U });

    private static OpenXmlElement[] BuildPageRefField(string bookmarkName) =>
    [
        new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
        new Run(new FieldCode($" PAGEREF {bookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
        new Run(new Text("?") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
    ];
}
