using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PlotboekGenerator.core.Generator.Templates;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.test.Generator;

/// <summary>
/// End-to-end coverage of the bijlagen feature through the whole template merge: the "Bijlagen"
/// appendix must render each authored bijlage (title + HTML content) with a bookmark, and each
/// referencing encounter must render a "Voor {titel} zie bijlage op pagina {N}" cross-reference
/// whose PAGEREF resolves to a real page number.
/// </summary>
public sealed class DocumentGeneration_Bijlagen_specs
{
    private const string Bookmark = "_bl_test1";

    [Test]
    public async Task Renders_bijlagen_appendix_and_encounter_cross_reference()
    {
        var result = await new DocumentGenerationService(TestTemplate.Provider).GeneratePlotboekDocumentAsync(BuildModel());

        result.IsValid.Should().BeTrue();

        using var ms = new MemoryStream(result.Value);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var text = body.InnerText;

        // The appendix rendered the numbered heading and its HTML content.
        text.Should().Contain("Bijlage 1: Prijslijst");
        text.Should().Contain("Brazen Bull prijzen");

        // The appendix entry is anchored by the bijlage bookmark.
        body.Descendants<BookmarkStart>().Select(b => b.Name?.Value).Should().Contain(Bookmark);

        // The encounter renders the numbered cross-reference with a resolved page number (not the "?" placeholder).
        Regex.IsMatch(text, @"Voor\s+Prijslijst\s+zie bijlage\s+1\s+op pagina\s+\d+")
            .Should().BeTrue("the PAGEREF to the bijlage must resolve to a real page number");

        // The static placeholder row must be gone.
        text.Should().NotContain("Bijlage 1: Informatie en Geruchten");
    }

    [Test]
    public async Task Renders_appendix_even_when_no_encounter_references_it()
    {
        var model = BuildModel() with
        {
            Days = ImmutableList.Create(new DayData
            {
                Day = "Zaterdag 14:30",
                Pages = ImmutableList.Create(BuildPage(ImmutableList<BijlageVerwijzingData>.Empty)),
            }),
        };

        var result = await new DocumentGenerationService(TestTemplate.Provider).GeneratePlotboekDocumentAsync(model);

        result.IsValid.Should().BeTrue();
        using var ms = new MemoryStream(result.Value);
        using var doc = WordprocessingDocument.Open(ms, false);
        var text = doc.MainDocumentPart!.Document.Body!.InnerText;

        // A general-explanation bijlage renders even with zero references.
        text.Should().Contain("Bijlage 1: Prijslijst");
        text.Should().Contain("Brazen Bull prijzen");
    }

    [Test]
    public async Task Title_only_bijlage_renders_without_leaking_the_content_placeholder()
    {
        var model = BuildModel() with
        {
            Bijlagen = ImmutableList.Create(new BijlageAppendixData
            {
                Nummer = "1",
                Titel = "Prijslijst",
                Inhoud = "<p></p>", // what BuildBijlagenList emits for an empty (title-only) bijlage
                BookmarkName = Bookmark,
            }),
        };

        var result = await new DocumentGenerationService(TestTemplate.Provider).GeneratePlotboekDocumentAsync(model);

        result.IsValid.Should().BeTrue();
        using var ms = new MemoryStream(result.Value);
        using var doc = WordprocessingDocument.Open(ms, false);
        var text = doc.MainDocumentPart!.Document.Body!.InnerText;

        text.Should().Contain("Bijlage 1: Prijslijst");
        // The Inhoud content-control placeholder (the literal tag name) must not leak into the output.
        // The \b word boundary avoids matching the unrelated "Inhoudsopgave" table-of-contents heading.
        Regex.IsMatch(text, @"\bInhoud\b").Should().BeFalse("the empty-content SDT placeholder must not leak");
    }

    private static PlotboekTemplateData BuildModel()
        => new()
        {
            Naam = "Test Plotboek",
            SamenvattingAlgemeenPlot = string.Empty,
            Tijdschemas = ImmutableList<TijdschemasData>.Empty,
            Days = ImmutableList.Create(new DayData
            {
                Day = "Zaterdag 14:30",
                Pages = ImmutableList.Create(BuildPage(ImmutableList.Create(new BijlageVerwijzingData
                {
                    Naam = "Prijslijst",
                    Nummer = "1",
                    PaginaBookmark = Bookmark,
                }))),
            }),
            ExtraInfo = ImmutableList<ExtraInfoData>.Empty,
            Bijlagen = ImmutableList.Create(new BijlageAppendixData
            {
                Nummer = "1",
                Titel = "Prijslijst",
                Inhoud = "<p>Brazen Bull prijzen</p>",
                BookmarkName = Bookmark,
            }),
            AslDomeinen = ImmutableList<DomeinData>.Empty,
            GslDomeinen = ImmutableList<DomeinData>.Empty,
            EvergreenPages = ImmutableList<PageData>.Empty,
            Namen = ImmutableList<NaamData>.Empty,
            Werelden = ImmutableList<NaamData>.Empty,
            EvergreenBijlageNummer = "2",
            NamenBijlageNummer = "3",
        };

    private static PageData BuildPage(ImmutableList<BijlageVerwijzingData> verwijzingen)
        => new()
        {
            NaamEncounter = "Handelaar encounter",
            VerantwoordelijkeSl = "SL",
            Dagdeel = "Zaterdag 14:30",
            GeschatteTijdsduur = "1 blok",
            SoortEncounter = "Handel",
            LocatieEncounter = "Hoofdveld",
            AantalNpcs = "3",
            KledingEnSchmink = string.Empty,
            ElevatorPitch = "Koop en verkoop",
            SamenvattingPlot = string.Empty,
            BenodigdeProps = string.Empty,
            GewensteLoot = string.Empty,
            InstructiePentagon = string.Empty,
            MeerInformatie = string.Empty,
            Opmerkingen = string.Empty,
            Debrief = string.Empty,
            IsVolledigKlaar = true,
            BijlageVerwijzingen = verwijzingen,
        };
}
