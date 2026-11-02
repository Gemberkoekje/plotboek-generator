using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildExtraInfoList_specs
{
    [Test]
    public void Returns_empty_when_no_pages_have_MeerInformatie()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, meerInformatie: string.Empty),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().BeEmpty();
    }

    [Test]
    public void Returns_empty_when_only_empty_quill_markup_is_present()
    {
        const string emptyQuillMarkup = "<div class=\"ql-editor ql-blank\" contenteditable=\"true\"><p><br></p></div><div class=\"ql-tooltip ql-hidden\"><a class=\"ql-preview\" rel=\"noopener noreferrer\" target=\"_blank\" href=\"about:blank\"></a><input type=\"text\" data-formula=\"e=mc^2\" data-link=\"https://quilljs.com\" data-video=\"Embed URL\"><a class=\"ql-action\"></a><a class=\"ql-remove\"></a></div>";
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, meerInformatie: emptyQuillMarkup),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().BeEmpty();
    }

    [Test]
    public void Includes_pages_with_MeerInformatie_for_scheduled_days()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, meerInformatie: "Details A"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1430, meerInformatie: string.Empty),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().ContainSingle();
        result[0].MeerInformatie.Should().Be("Details A");
    }

    [Test]
    public void Includes_pages_for_Dag_None()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.None, starttijdId: StarttijdIds.Tijd1030, meerInformatie: "Unplanned details"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().ContainSingle();
        result[0].MeerInformatie.Should().Be("Unplanned details");
    }

    [Test]
    public void Includes_SpelerGedreven_pages_with_MeerInformatie()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, isSpelerGedreven: true, meerInformatie: "Player info"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().ContainSingle();
        result[0].MeerInformatie.Should().Be("Player info");
    }

    [Test]
    public void Excludes_SpelerGedreven_from_regular_day_groups()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, meerInformatie: "Regular info"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1430, isSpelerGedreven: true, meerInformatie: "Player info"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().HaveCount(2);
    }

    [Test]
    public void Orders_within_day_by_starttijd_then_order()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1430, order: 1, meerInformatie: "Later"),
            CreatePage(dag: Dag.Vrijdag, starttijdId: StarttijdIds.Tijd1030, order: 1, meerInformatie: "Earlier"),
        };
        var daysInRange = new List<Dag> { Dag.None, Dag.Vrijdag };
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoList(pages, daysInRange, nummers);

        result.Should().HaveCount(2);
        result[0].MeerInformatie.Should().Be("Earlier");
        result[1].MeerInformatie.Should().Be("Later");
    }

    private static PlotboekPagina.ExportData CreatePage(
        Dag dag = Dag.None,
        string starttijdId = StarttijdIds.None,
        int order = 0,
        bool isSpelerGedreven = false,
        string meerInformatie = "")
    {
        return new PlotboekPagina.ExportData
        {
            PaginaId = PlotboekPaginaId.Next(),
            Titel = "Test",
            Dag = dag,
            StarttijdId = starttijdId,
            Order = order,
            IsSpelerGedreven = isSpelerGedreven,
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = string.Empty,
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = meerInformatie,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            Iconen = PlotboekPaginaIcons.None,

            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
        };
    }
}

