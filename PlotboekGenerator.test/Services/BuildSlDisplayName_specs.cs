using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildSlDisplayName_specs
{
    [Test]
    public void Returns_placeholder_when_no_SL_assigned()
    {
        var page = CreatePage(verantwoordelijkeSl: string.Empty);
        var lookup = new Dictionary<string, string>();
        PlotboekExportBuilder.BuildSlDisplayName(page, lookup).Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Returns_username_for_primary_SL()
    {
        var page = CreatePage(verantwoordelijkeSl: "user1");
        var lookup = new Dictionary<string, string> { ["user1"] = "Alice" };
        PlotboekExportBuilder.BuildSlDisplayName(page, lookup).Should().Be("Alice");
    }

    [Test]
    public void Returns_comma_separated_names_with_extra_SLs()
    {
        var page = CreatePage(verantwoordelijkeSl: "user1", extraSlIds: ["user2", "user3"]);
        var lookup = new Dictionary<string, string>
        {
            ["user1"] = "Alice",
            ["user2"] = "Bob",
            ["user3"] = "Charlie",
        };
        PlotboekExportBuilder.BuildSlDisplayName(page, lookup).Should().Be("Alice, Bob, Charlie");
    }

    [Test]
    public void Skips_extra_SL_not_in_lookup()
    {
        var page = CreatePage(verantwoordelijkeSl: "user1", extraSlIds: ["unknown"]);
        var lookup = new Dictionary<string, string> { ["user1"] = "Alice" };
        PlotboekExportBuilder.BuildSlDisplayName(page, lookup).Should().Be("Alice");
    }

    [Test]
    public void Returns_placeholder_when_primary_SL_not_in_lookup()
    {
        var page = CreatePage(verantwoordelijkeSl: "unknown");
        var lookup = new Dictionary<string, string>();
        PlotboekExportBuilder.BuildSlDisplayName(page, lookup).Should().Be(PageData.NogNietIngevuld);
    }

    private static PlotboekPagina.ExportData CreatePage(
        string verantwoordelijkeSl = "",
        List<string>? extraSlIds = null)
    {
        return new PlotboekPagina.ExportData
        {
            Titel = "Test",
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = verantwoordelijkeSl,
            ExtraSlIds = extraSlIds ?? [],
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = string.Empty,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            Iconen = PlotboekPaginaIcons.None,

            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
        };
    }
}

