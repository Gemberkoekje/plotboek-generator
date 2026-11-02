using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.Immutable;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// <see cref="PageNumberCorrectionService.ExtractRealPages"/> is the pure PDF-reading core of the
/// Drive page-number correction feature - given the bytes of a real rendered PDF and a map of
/// bookmark to expected heading text, it must find which page each heading actually landed on,
/// preferring the largest-font match so a real heading is not confused with the same words
/// appearing as ordinary body text elsewhere. These specs build small, real PDFs with PdfPig's
/// own writer (the same library doing the reading) so the test never depends on an external
/// renderer.
/// </summary>
public sealed class PageNumberCorrectionService_specs
{
    [Test]
    public void Finds_the_page_a_heading_landed_on()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page1 = builder.AddPage(PageSize.A4);
        page1.AddText("Onbelangrijke tekst", 11, new PdfPoint(50, 700), font);

        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("Encounter Foo", 20, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_pg_1"] = "Encounter Foo" });

        result.Should().ContainKey("_pg_1").WhoseValue.Should().Be(2);
    }

    [Test]
    public void Ligature_glyphs_normalize_back_to_their_plain_letters()
    {
        // A PDF renderer can choose to draw common letter pairs ("fi", "ffi", ...) using a single
        // ligature glyph instead of two/three separate letter glyphs - PdfPig then extracts that
        // glyph's own Unicode codepoint (U+FB01 "ﬁ"), not the plain "fi" sequence the source
        // document actually contains. Real risk for Dutch encounter titles ("informatie",
        // "geïnteresseerd", "officieel", ...). NFKC normalization must collapse it back.
        PageNumberCorrectionService.NormalizeForMatching("inﬁormatie").Should().Be("infiormatie", "the ligature glyph decomposes to plain f + i");
    }

    [Test]
    public void Matching_is_case_insensitive()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A4).AddText("encounter FOO", 20, new PdfPoint(50, 700), font);
        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_pg_1"] = "Encounter foo" });

        result.Should().ContainKey("_pg_1").WhoseValue.Should().Be(1, "case differences alone must not block a match");
    }

    [Test]
    public void Prefers_the_largest_font_match_over_a_smaller_body_text_duplicate()
    {
        // The same phrase appears twice: once as a small body-text mention (page 1), once as a
        // large real heading (page 2). The heading must win, regardless of document order.
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page1 = builder.AddPage(PageSize.A4);
        page1.AddText("Zaterdag 14:30", 11, new PdfPoint(50, 700), font);

        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("Zaterdag 14:30", 28, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["heading:Zaterdag1430"] = "Zaterdag 14:30" });

        result.Should().ContainKey("heading:Zaterdag1430").WhoseValue.Should().Be(2, "the larger-font occurrence is the real heading");
    }

    [Test]
    public void Finds_the_largest_match_even_when_both_occurrences_share_a_page()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page = builder.AddPage(PageSize.A4);
        page.AddText("Bijlagen", 11, new PdfPoint(50, 700), font);
        page.AddText("Bijlagen", 20, new PdfPoint(50, 600), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["heading:Bijlagen"] = "Bijlagen" });

        result.Should().ContainKey("heading:Bijlagen").WhoseValue.Should().Be(1);
    }

    [Test]
    public void Same_page_disambiguation_actually_picks_the_larger_font_size()
    {
        // The page-level assertion above can't detect a regression in *which* same-page
        // occurrence wins (both candidates are on page 1 either way), so this asserts the
        // internal per-page decision directly: given both a small and a large occurrence on one
        // page, the winning font size must be the larger one, not merely "a" match.
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page = builder.AddPage(PageSize.A4);
        page.AddText("Bijlagen", 11, new PdfPoint(50, 700), font);
        page.AddText("Bijlagen", 20, new PdfPoint(50, 600), font);

        var pdfBytes = builder.Build();

        using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        var words = doc.GetPage(1).GetWords().ToList();
        var normalizedTexts = words.Select(w => w.Text).ToArray();

        var fontSize = PageNumberCorrectionService.LargestMatchFontSize(words, normalizedTexts, ["Bijlagen"]);

        fontSize.Should().Be(20d, "the larger of the two same-page occurrences must win, not whichever is found first");
    }

    [Test]
    public void A_short_heading_does_not_false_positive_match_as_a_prefix_of_a_longer_unrelated_heading()
    {
        // "Extra informatie" (entity A's real heading) is also the literal opening words of an
        // unrelated, longer heading "Extra informatie over het programma" (entity B) elsewhere in
        // the same document, at the identical Heading2 font size. Entity A's own heading must
        // still win, even though it renders on a later page than the unrelated longer heading.
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page1 = builder.AddPage(PageSize.A4);
        page1.AddText("Extra informatie over het programma", 20, new PdfPoint(50, 700), font);

        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("Extra informatie", 20, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_ei_A"] = "Extra informatie" });

        result.Should().ContainKey("_ei_A").WhoseValue.Should().Be(2, "entity A's own heading, not the unrelated longer heading that happens to start with the same words");
    }

    [Test]
    public void A_decorative_prefix_that_pdfpig_tokenizes_separately_from_the_title_still_matches()
    {
        // Reproduces the real production bug: the app builds a heading as
        // "{iconPrefix} {title}" with several emoji concatenated with NO space between them
        // (e.g. "⚔️👯⭐❗🎶💡 CA 1.1 Titel"). PdfPig's own word extractor breaks a "word" on a font
        // change or a big glyph-advance gap (DefaultWordExtractor.nextFontDiffers / nextBigSpace) -
        // icon glyphs almost always render in a different font with irregular widths, so that one
        // un-spaced source run comes back from the real PDF as several separate PdfPig words, not
        // the single token a plain split on the source text would expect - and previously the
        // heading matched nothing at all as a result. Represented here with two plain-ASCII
        // "icon" tokens positioned with a deliberate gap, since the test font can't render real
        // emoji, but exercising the exact same PdfPig tokenization behavior.
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page = builder.AddPage(PageSize.A4);
        page.AddText("**", 11, new PdfPoint(50, 700), font);
        page.AddText("##", 11, new PdfPoint(90, 700), font);
        page.AddText("CA 1.1 Titel", 11, new PdfPoint(130, 700), font);

        var pdfBytes = builder.Build();

        // "**##" mirrors how the app actually concatenates icon glyphs with no internal space,
        // followed by one space before the real title - the exact shape that used to never match.
        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_pg_1"] = "**## CA 1.1 Titel" });

        result.Should().ContainKey("_pg_1").WhoseValue.Should().Be(1, "the decorative prefix must be skipped, not required to match token-for-token");
    }

    [Test]
    public void A_real_word_immediately_before_the_match_on_the_same_line_still_disqualifies_it()
    {
        // The relaxation that lets a decorative icon prefix precede a match (see the test above)
        // must not also let an unrelated REAL word immediately before the match slip through -
        // only content with no letters/digits (decoration) is tolerated there.
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page1 = builder.AddPage(PageSize.A4);
        page1.AddText("Some Extra Info", 20, new PdfPoint(50, 700), font);

        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("Extra Info", 20, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_ei_A"] = "Extra Info" });

        result.Should().ContainKey("_ei_A").WhoseValue.Should().Be(2, "\"Some\" is a real word, not decoration, so the page-1 occurrence must not count as a match");
    }

    [Test]
    public void Nbsp_in_the_expected_text_still_matches_pdfpig_words_split_on_a_normal_space()
    {
        // Simulates a heading whose source <w:t> run contained a literal U+00A0 (non-breaking
        // space) between two words instead of a normal U+0020 space - e.g. pasted from Word/a
        // web page into a plain (non-rich-text) title field, which GetParagraphPlainText
        // concatenates verbatim with no normalization (unlike the Quill/HTML DecodeHtml path).
        // The vendored PdfPig writer's Standard14 fonts have no glyph for U+00A0, so the PDF here
        // is built with a normal space (matching what PdfPig's own word extractor would yield
        // from a real, glyph-bearing NBSP too - a separate, ordinary word break); what's under
        // test is that the *expected* text's embedded NBSP still splits into the matching words.
        const string nbspExpectedText = "Sfeer avond";

        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        var page1 = builder.AddPage(PageSize.A4);
        page1.AddText("Onbelangrijke tekst", 11, new PdfPoint(50, 700), font);

        var page2 = builder.AddPage(PageSize.A4);
        page2.AddText("Sfeer avond", 20, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["heading:SfeerAvond"] = nbspExpectedText });

        result.Should().ContainKey("heading:SfeerAvond").WhoseValue.Should().Be(2);
    }

    [Test]
    public void Missing_text_is_simply_absent_from_the_result()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A4).AddText("Iets anders", 11, new PdfPoint(50, 700), font);

        var pdfBytes = builder.Build();

        var result = PageNumberCorrectionService.ExtractRealPages(
            pdfBytes,
            new Dictionary<string, string> { ["_pg_1"] = "Encounter dat niet bestaat" });

        result.Should().BeEmpty();
    }

    [Test]
    public async Task ComputeCorrectionsAsync_returns_the_diff_between_real_and_claimed_pages()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A4).AddText("Filler", 11, new PdfPoint(50, 700), font);
        var realPage = builder.AddPage(PageSize.A4);
        realPage.AddText("Encounter Foo", 20, new PdfPoint(50, 700), font);
        var pdfBytes = builder.Build();

        var drive = Substitute.For<IGoogleDriveService>();
        drive.TryExportCurrentFileAsPdfAsync("folder", "file.docx", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var sut = new PageNumberCorrectionService(drive, Substitute.For<ILogger<PageNumberCorrectionService>>());

        var priorState = new GoogleDriveSyncState
        {
            Id = "plotboek-1",
            EstimatedBookmarkPages = ImmutableDictionary<string, int>.Empty.Add("_pg_1", 1),
            BookmarkHeadingText = ImmutableDictionary<string, string>.Empty.Add("_pg_1", "Encounter Foo"),
        };

        var corrections = await sut.ComputeCorrectionsAsync(priorState, "folder", "file.docx");

        corrections.Should().ContainKey("_pg_1").WhoseValue.Should().Be(1, "claimed page 1, really landed on page 2");
    }

    [Test]
    public async Task ComputeCorrectionsAsync_logs_which_headings_were_found_unchanged_changed_and_missing()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(PageSize.A4).AddText("Filler", 11, new PdfPoint(50, 700), font);
        builder.AddPage(PageSize.A4).AddText("Filler", 11, new PdfPoint(50, 700), font);
        builder.AddPage(PageSize.A4).AddText("Encounter Verschoven", 20, new PdfPoint(50, 700), font);
        builder.AddPage(PageSize.A4).AddText("Filler", 11, new PdfPoint(50, 700), font);
        builder.AddPage(PageSize.A4).AddText("Encounter Onveranderd", 20, new PdfPoint(50, 700), font);
        var pdfBytes = builder.Build();

        var drive = Substitute.For<IGoogleDriveService>();
        drive.TryExportCurrentFileAsPdfAsync("folder", "file.docx", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(pdfBytes));

        var capturingLogger = new CapturingLogger<PageNumberCorrectionService>();
        var sut = new PageNumberCorrectionService(drive, capturingLogger);

        var priorState = new GoogleDriveSyncState
        {
            Id = "plotboek-1",
            EstimatedBookmarkPages = ImmutableDictionary<string, int>.Empty
                .Add("_pg_verschoven", 1)
                .Add("_pg_onveranderd", 5)
                .Add("_pg_ontbreekt", 2),
            BookmarkHeadingText = ImmutableDictionary<string, string>.Empty
                .Add("_pg_verschoven", "Encounter Verschoven")
                .Add("_pg_onveranderd", "Encounter Onveranderd")
                .Add("_pg_ontbreekt", "Encounter Die Niet Bestaat"),
        };

        await sut.ComputeCorrectionsAsync(priorState, "folder", "file.docx");

        var summary = capturingLogger.Messages.Should().ContainSingle().Subject;
        summary.Should().Contain("2/3 headings found", "two of the three expected headings actually appear in the PDF");
        summary.Should().Contain("1 pages changed", "only the shifted heading's page differs from what was claimed");
        summary.Should().Contain("Encounter Verschoven (p3)", "found headings are listed with the page they were found on");
        summary.Should().Contain("Encounter Onveranderd (p5)");
        summary.Should().Contain("Encounter Die Niet Bestaat", "the heading absent from the PDF is called out as not found");
        summary.Should().Contain("Encounter Verschoven: p1 -> p3 (+2)", "a changed heading shows its claimed page, real page, and signed offset");
        summary.Should().NotContain("Encounter Onveranderd:", "an unchanged heading is not listed as changed");
    }

    [Test]
    public async Task ComputeCorrectionsAsync_returns_empty_when_no_prior_file_exists_yet()
    {
        var drive = Substitute.For<IGoogleDriveService>();
        drive.TryExportCurrentFileAsPdfAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]>(null));

        var capturingLogger = new CapturingLogger<PageNumberCorrectionService>();
        var sut = new PageNumberCorrectionService(drive, capturingLogger);

        var priorState = new GoogleDriveSyncState
        {
            Id = "plotboek-1",
            EstimatedBookmarkPages = ImmutableDictionary<string, int>.Empty.Add("_pg_1", 1),
            BookmarkHeadingText = ImmutableDictionary<string, string>.Empty.Add("_pg_1", "Encounter Foo"),
        };

        var corrections = await sut.ComputeCorrectionsAsync(priorState, "folder", "file.docx");

        corrections.Should().BeEmpty();
        capturingLogger.Messages.Should().ContainSingle(m => m.Contains("no existing file found"), "a skip must still be visible in the logs, not silent");
    }

    [Test]
    public async Task ComputeCorrectionsAsync_never_throws_when_the_drive_export_fails()
    {
        var drive = Substitute.For<IGoogleDriveService>();
        drive.TryExportCurrentFileAsPdfAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<byte[]>>(_ => throw new InvalidOperationException("Drive is down"));

        var sut = new PageNumberCorrectionService(drive, Substitute.For<ILogger<PageNumberCorrectionService>>());

        var priorState = new GoogleDriveSyncState
        {
            Id = "plotboek-1",
            EstimatedBookmarkPages = ImmutableDictionary<string, int>.Empty.Add("_pg_1", 1),
            BookmarkHeadingText = ImmutableDictionary<string, string>.Empty.Add("_pg_1", "Encounter Foo"),
        };

        var corrections = await sut.ComputeCorrectionsAsync(priorState, "folder", "file.docx");

        corrections.Should().BeEmpty();
    }

    [Test]
    public async Task ComputeCorrectionsAsync_returns_empty_when_no_prior_headings_are_known()
    {
        var drive = Substitute.For<IGoogleDriveService>();
        var capturingLogger = new CapturingLogger<PageNumberCorrectionService>();
        var sut = new PageNumberCorrectionService(drive, capturingLogger);

        var priorState = new GoogleDriveSyncState { Id = "plotboek-1" };

        var corrections = await sut.ComputeCorrectionsAsync(priorState, "folder", "file.docx");

        corrections.Should().BeEmpty();
        await drive.DidNotReceive().TryExportCurrentFileAsPdfAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        capturingLogger.Messages.Should().ContainSingle(m => m.Contains("no heading baseline"), "a skip must still be visible in the logs, not silent");
    }
}
