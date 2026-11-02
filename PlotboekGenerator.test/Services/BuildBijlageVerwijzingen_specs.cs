using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildBijlageVerwijzingen_specs
{
    [Test]
    public void Returns_empty_when_page_references_none()
    {
        var page = CreatePage();

        var result = PlotboekExportBuilder.BuildBijlageVerwijzingen(page, new Dictionary<BijlageId, BijlageData>());

        result.Should().BeEmpty();
    }

    [Test]
    public void Builds_a_reference_per_bijlage()
    {
        var id = BijlageId.Next();
        var page = CreatePage(id);
        var lookup = Lookup(BijlageData.Create(id, "Prijslijst", "<p>x</p>", 0));

        var result = PlotboekExportBuilder.BuildBijlageVerwijzingen(page, lookup);

        result.Should().ContainSingle();
        result[0].Naam.Should().Be("Prijslijst");
        result[0].Nummer.Should().Be("1");
        result[0].PaginaBookmark.Should().Be(PlotboekExportBuilder.SanitizeBijlageBookmarkName(id));
    }

    [Test]
    public void Drops_refs_not_present_in_the_lookup()
    {
        // Orphan filter: a reference to a deleted bijlage silently disappears from the export.
        var present = BijlageId.Next();
        var missing = BijlageId.Next();
        var page = CreatePage(present, missing);
        var lookup = Lookup(BijlageData.Create(present, "Prijslijst", "<p>x</p>", 0));

        var result = PlotboekExportBuilder.BuildBijlageVerwijzingen(page, lookup);

        result.Should().ContainSingle();
        result[0].Naam.Should().Be("Prijslijst");
    }

    [Test]
    public void Dedupes_repeated_refs()
    {
        var id = BijlageId.Next();
        var page = CreatePage(id, id);
        var lookup = Lookup(BijlageData.Create(id, "Prijslijst", "<p>x</p>", 0));

        var result = PlotboekExportBuilder.BuildBijlageVerwijzingen(page, lookup);

        result.Should().ContainSingle();
    }

    [Test]
    public void Orders_references_by_bijlage_order()
    {
        var first = BijlageId.Next();
        var second = BijlageId.Next();
        var page = CreatePage(second, first); // listed in reverse; output follows bijlage Order
        var lookup = Lookup(
            BijlageData.Create(first, "Eerste", "<p>a</p>", 0),
            BijlageData.Create(second, "Tweede", "<p>b</p>", 1));

        var result = PlotboekExportBuilder.BuildBijlageVerwijzingen(page, lookup);

        result[0].Naam.Should().Be("Eerste");
        result[1].Naam.Should().Be("Tweede");
    }

    private static IReadOnlyDictionary<BijlageId, BijlageData> Lookup(params BijlageData[] bijlagen)
        => bijlagen.ToDictionary(b => b.Id);

    private static PlotboekPagina.ExportData CreatePage(params BijlageId[] refs)
        => new ()
        {
            PaginaId = PlotboekPaginaId.Next(),
            Titel = "Handelaar",
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = string.Empty,
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = string.Empty,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            Iconen = PlotboekPaginaIcons.None,
            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
            BijlageIds = [.. refs],
        };
}
