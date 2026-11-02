using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildDayDataList_specs
{
    [Test]
    public void Returns_empty_list_when_no_pages()
    {
        var pages = new List<PlotboekPagina.ExportData>();
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().BeEmpty();
    }

    [Test]
    public void Groups_pages_by_dag_and_starttijd()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Scene A"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, order: 2, titel: "Scene B"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1430, order: 1, titel: "Scene C"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().HaveCount(2);
        result[0].Day.Should().Contain("Vrijdag");
        result[0].Pages.Should().HaveCount(2);
        result[1].Pages.Should().ContainSingle();
    }

    [Test]
    public void Adds_Nog_niet_ingepland_group_for_Dag_None()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.None, starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Unscheduled"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().ContainSingle();
        result[0].Day.Should().Be("Nog niet ingepland");
        result[0].Pages.Should().ContainSingle();
    }

    [Test]
    public void Adds_Speler_Gedreven_group_for_player_driven_pages()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, isSpelerGedreven: true, titel: "Player Plot"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().ContainSingle();
        result[0].Day.Should().Be("Speler Gedreven");
        result[0].Pages.Should().ContainSingle();
    }

    [Test]
    public void Excludes_SpelerGedreven_pages_from_regular_day_groups()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Regular"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, order: 2, isSpelerGedreven: true, titel: "Player"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().HaveCount(2);
        result[0].Day.Should().Contain("Vrijdag");
        result[0].Pages.Should().ContainSingle();
        result[1].Day.Should().Be("Speler Gedreven");
        result[1].Pages.Should().ContainSingle();
    }

    [Test]
    public void Orders_Nog_niet_ingepland_pages_by_starttijd_then_order()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.None, starttijdId: StarttijdIds.Tijd1430, order: 1, titel: "Later"),
            CreatePage(dag: Dag.None, starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Earlier"),
        };
        var daysInRange = new List<Dag> { Dag.None };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().ContainSingle();
        result[0].Pages[0].NaamEncounter.Should().Contain("Earlier");
        result[0].Pages[1].NaamEncounter.Should().Contain("Later");
    }

    [Test]
    public void Orders_SpelerGedreven_pages_by_dag_then_starttijd_then_order()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Zaterdag, starttijdId: StarttijdIds.Tijd1030, order: 1, isSpelerGedreven: true, titel: "Zat"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd2000, order: 1, isSpelerGedreven: true, titel: "Vrij"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag, Dag.Zaterdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();

        var result = PlotboekExportBuilder.BuildDayDataList(pages, daysInRange, nummers, userLookup);

        result.Should().ContainSingle();
        result[0].Day.Should().Be("Speler Gedreven");
        result[0].Pages[0].NaamEncounter.Should().Contain("Vrij");
        result[0].Pages[1].NaamEncounter.Should().Contain("Zat");
    }

    private static PlotboekPagina.ExportData CreatePage(
        Dag dag = Dag.None,
        string starttijdId = StarttijdIds.None,
        int order = 0,
        bool isSpelerGedreven = false,
        string titel = "Test")
    {
        return new PlotboekPagina.ExportData
        {
            PaginaId = PlotboekPaginaId.Next(),
            Titel = titel,
            Dag = dag,
            StarttijdId = starttijdId,
            Order = order,
            IsSpelerGedreven = isSpelerGedreven,
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
        };
    }
}

