#pragma warning disable S3220 // All OpenXML composite-element constructors have both params and IEnumerable overloads

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// The three-argument <c>ResolvePageRefFieldsToStaticText</c> overload nudges the estimator's raw
/// page numbers by a per-bookmark correction (<c>real - claimed</c> from a previous Google Drive
/// render). These specs cover the offset math directly: applied correctly, clamped at both
/// bounds so a stale or wrong correction can't inject a wild page number, and silently ignored
/// for a bookmark that no longer exists this generation.
/// </summary>
public sealed class ResolvePageRefFieldsToStaticText_Corrections_specs
{
    [Test]
    public void A_correction_shifts_the_resolved_page_by_its_offset()
    {
        var body = SingleBookmarkBody("here");

        var resolved = Resolve(body, new Dictionary<string, int> { ["here"] = 2 });

        resolved.Should().Be("3", "the raw estimate (page 1) plus a +2 correction");
    }

    [Test]
    public void A_correction_larger_than_the_clamp_is_capped()
    {
        var body = SingleBookmarkBody("here");

        var resolved = Resolve(body, new Dictionary<string, int> { ["here"] = 100 });

        resolved.Should().Be("6", "the offset is capped at 5 pages regardless of how large the correction is");
    }

    [Test]
    public void A_correction_never_pushes_the_page_below_one()
    {
        var body = SingleBookmarkBody("here");

        var resolved = Resolve(body, new Dictionary<string, int> { ["here"] = -100 });

        resolved.Should().Be("1", "even a large negative correction cannot resolve to a page before the first");
    }

    [Test]
    public void A_correction_for_a_bookmark_that_no_longer_exists_is_ignored()
    {
        var body = SingleBookmarkBody("here");

        var resolved = Resolve(body, new Dictionary<string, int> { ["gone"] = 3 });

        resolved.Should().Be("1", "a correction key with no matching bookmark this generation has no effect");
    }

    [Test]
    public void No_corrections_leaves_the_raw_estimate_untouched()
    {
        var body = SingleBookmarkBody("here");

        var resolved = Resolve(body, corrections: null);

        resolved.Should().Be("1");
    }

    private static string Resolve(Body body, IReadOnlyDictionary<string, int> corrections)
    {
        DocxTemplateEngine.ResolvePageRefFieldsToStaticText(body, mainPart: null, corrections);

        var last = body.Elements<Paragraph>().Last(p => p.Descendants<Text>().Any());
        return last.Descendants<Text>().Last().Text;
    }

    private static Body SingleBookmarkBody(string bookmarkName)
        => new(
            new Paragraph(
                new BookmarkStart { Id = "1", Name = bookmarkName },
                new Run(new Text("regel")),
                new BookmarkEnd { Id = "1" }),
            new Paragraph(BuildPageRefField(bookmarkName)),
            new SectionProperties(
                new PageSize { Width = 11909U, Height = 16834U },
                new PageMargin { Top = 1440, Bottom = 1440, Left = 1440U, Right = 1440U }));

    private static OpenXmlElement[] BuildPageRefField(string bookmarkName) =>
    [
        new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }),
        new Run(new FieldCode($" PAGEREF {bookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }),
        new Run(new Text("?") { Space = SpaceProcessingModeValues.Preserve }),
        new Run(new FieldChar { FieldCharType = FieldCharValues.End }),
    ];
}
