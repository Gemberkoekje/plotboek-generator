using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.core.Generator.Templates;
using System.Collections.Immutable;
using System.IO;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// The embedded production template legitimately renders the same heading text
/// ("Nog niet ingepland") twice for a plotboek with an unscheduled page - once in the
/// Tijdschema overview section, once in the Encounters detail section. Each occurrence must get
/// its own bookmark so its TOC row resolves to its own physical page, instead of the two
/// occurrences silently collapsing onto a single, shared bookmark.
/// </summary>
public sealed class PopulateAutoTableOfContents_DuplicateHeadings_specs
{
    [Test]
    public void Two_headings_with_identical_text_each_get_their_own_bookmark()
    {
        var model = new PlotboekTemplateData
        {
            Naam = "Repro",
            SamenvattingAlgemeenPlot = string.Empty,
            Tijdschemas =
            [
                new TijdschemasData
                {
                    Day = "Nog niet ingepland",
                    Tijdschema =
                    [
                        new TijdschemaData
                        {
                            Tijd = "10:00",
                            Icons = "n.v.t.",
                            NaamEncounter = "Test Encounter",
                            SL = "SL1",
                            NPCs = "n.v.t.",
                            Pagina = "1",
                        },
                    ],
                },
            ],
            Days =
            [
                new DayData
                {
                    Day = "Nog niet ingepland",
                    Pages =
                    [
                        new PageData
                        {
                            NaamEncounter = "Test Encounter",
                            VerantwoordelijkeSl = "SL1",
                            Dagdeel = "n.v.t.",
                            GeschatteTijdsduur = "n.v.t.",
                            SoortEncounter = "n.v.t.",
                            LocatieEncounter = "n.v.t.",
                            AantalNpcs = "n.v.t.",
                            KledingEnSchmink = "n.v.t.",
                            ElevatorPitch = "n.v.t.",
                            SamenvattingPlot = "n.v.t.",
                            BenodigdeProps = "n.v.t.",
                            GewensteLoot = "n.v.t.",
                            InstructiePentagon = "n.v.t.",
                            MeerInformatie = "n.v.t.",
                            Opmerkingen = "n.v.t.",
                            Debrief = "n.v.t.",
                            IsVolledigKlaar = true,
                        },
                    ],
                },
            ],
            ExtraInfo = ImmutableList<ExtraInfoData>.Empty,
            Bijlagen = [],
            AslDomeinen = ImmutableList<DomeinData>.Empty,
            GslDomeinen = ImmutableList<DomeinData>.Empty,
            EvergreenPages = ImmutableList<PageData>.Empty,
            Namen = ImmutableList<NaamData>.Empty,
            Werelden = ImmutableList<NaamData>.Empty,
            EvergreenBijlageNummer = "1",
            NamenBijlageNummer = "2",
        };

        using var template = TestTemplate.Provider.OpenTemplate();
        var merged = PlotboekGenerator.infrastructure.Generator.DocxTemplateEngine.MergeModelWithCorrections(
            template,
            model,
            new Dictionary<string, int>());

        using var ms = new MemoryStream(merged.Bytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var headingParagraphs = body.Descendants<Paragraph>()
            .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value == "Heading2")
            .Where(p => string.Concat(p.Descendants<Text>().Select(t => t.Text)) == "Nog niet ingepland")
            .ToList();

        headingParagraphs.Should().HaveCount(2, "the day-group heading is rendered once in the Tijdschema overview and once in the Encounters detail section");

        var ownBookmarks = headingParagraphs
            .Select(p => p.Descendants<BookmarkStart>().Select(b => b.Name?.Value).FirstOrDefault(n => n != null && n.StartsWith("_hd_", StringComparison.Ordinal)))
            .ToList();

        ownBookmarks.Should().OnlyHaveUniqueItems("each occurrence of the duplicate heading text must get its own bookmark, not collapse onto a shared one");
        ownBookmarks.Should().AllSatisfy(bm => bm.Should().NotBeNull("every heading paragraph must have its own BookmarkStart inserted"));

        var bookmarksForThisHeadingText = merged.BookmarkHeadingText
            .Where(kv => kv.Value == "Nog niet ingepland")
            .Select(kv => kv.Key)
            .ToList();

        bookmarksForThisHeadingText.Should().HaveCount(2, "each occurrence of the duplicate heading must be persisted under its own correction-baseline key");

        var headingBookmarkPages = bookmarksForThisHeadingText
            .Select(bm => merged.BookmarkPages[bm])
            .ToList();

        headingBookmarkPages.Should().OnlyHaveUniqueItems("the two headings sit in different sections and must resolve to different physical pages");
    }
}
