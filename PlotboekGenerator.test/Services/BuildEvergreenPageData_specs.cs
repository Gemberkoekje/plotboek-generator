using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildEvergreenPageData_specs
{
    [Test]
    public void Sets_Dagdeel_to_Evergreen()
    {
        var page = CreatePage();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.Dagdeel.Should().Be("Evergreen");
    }

    [Test]
    public void Sets_BookmarkName_to_empty()
    {
        var page = CreatePage();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.BookmarkName.Should().BeEmpty();
    }

    [Test]
    public void Includes_icon_prefix_in_name()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.TimingCruciaal, titel: "Evergreen Scene");
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.NaamEncounter.Should().Contain("\u2B50");
        result.NaamEncounter.Should().Contain("Evergreen Scene");
        result.IconenPrefix.Should().Contain("\u2B50");
    }

    [Test]
    public void Uses_placeholder_when_title_is_empty()
    {
        var page = CreatePage(titel: string.Empty);
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.NaamEncounter.Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Sets_VerantwoordelijkeSl_from_lookup()
    {
        var page = CreatePage(verantwoordelijkeSl: "user1");
        var userLookup = new Dictionary<string, string> { ["user1"] = "Alice" };

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.VerantwoordelijkeSl.Should().Be("Alice");
    }

    [Test]
    public void Sets_GeschatteTijdsduur_from_AantalBlokken()
    {
        var page = CreatePage(aantalBlokkenId: AantalBlokkenIds.TweeBlokken);
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.GeschatteTijdsduur.Should().NotBe(PageData.NogNietIngevuld);
    }

    [Test]
    public void Sets_placeholder_when_AantalBlokken_is_None()
    {
        var page = CreatePage(aantalBlokkenId: AantalBlokkenIds.None);
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.GeschatteTijdsduur.Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Populates_MeerInformatie_directly()
    {
        var page = CreatePage(meerInformatie: "Evergreen extra info");
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.MeerInformatie.Should().Be("Evergreen extra info");
    }

    [Test]
    public void Populates_all_encounter_fields()
    {
        var page = CreatePage(
            elevatorPitch: "Quick pitch",
            samenvattingPlot: "Summary",
            opmerkingen: "Notes");
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.ElevatorPitch.Should().Be("Quick pitch");
        result.SamenvattingPlot.Should().Be("Summary");
        result.Opmerkingen.Should().Be("Notes");
    }

    [Test]
    public void Sets_IsVolledigKlaar_from_page()
    {
        var page = CreatePage(isVolledigKlaar: true);
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildEvergreenPageData(page, userLookup);

        result.IsVolledigKlaar.Should().BeTrue();
    }

    private static PlotboekPagina.ExportData CreatePage(
        string titel = "Test",
        string verantwoordelijkeSl = "",
        string elevatorPitch = "Pitch",
        string samenvattingPlot = "Summary",
        string meerInformatie = "",
        string opmerkingen = "",
        string aantalBlokkenId = AantalBlokkenIds.None,
        PlotboekPaginaIcons iconen = PlotboekPaginaIcons.None,
        bool isVolledigKlaar = false)
    {
        return new PlotboekPagina.ExportData
        {
            PaginaId = PlotboekPaginaId.Next(),
            Titel = titel,
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = verantwoordelijkeSl,
            ElevatorPitch = elevatorPitch,
            SamenvattingPlot = samenvattingPlot,
            MeerInformatie = meerInformatie,
            InstructiePentagon = string.Empty,
            Opmerkingen = opmerkingen,
            Iconen = iconen,

            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
            AantalBlokkenId = aantalBlokkenId,
            IsVolledigKlaar = isVolledigKlaar,
        };
    }
}

