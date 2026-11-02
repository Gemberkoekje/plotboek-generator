using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildTijdschemasData_specs
{
    [Test]
    public void Returns_data_with_day_label()
    {
        var pages = new List<PlotboekPagina.ExportData>();
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>();

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Day.Should().Be("Vrijdag");
        result.Tijdschema.Should().BeEmpty();
    }

    [Test]
    public void Includes_meetings_in_output()
    {
        var pages = new List<PlotboekPagina.ExportData>();
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.EersteDag, "09:00", "Briefing", 1),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().ContainSingle();
        result.Tijdschema[0].NaamEncounter.Should().Be("Briefing");
        result.Tijdschema[0].Tijd.Should().Be("09:00");
        result.Tijdschema[0].IsMeeting.Should().BeTrue();
    }

    [Test]
    public void Meeting_rows_have_space_placeholders()
    {
        var pages = new List<PlotboekPagina.ExportData>();
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.EersteDag, "09:00", "Stand-up", 1),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema[0].Icons.Should().Be(" ");
        result.Tijdschema[0].SL.Should().Be(" ");
        result.Tijdschema[0].NPCs.Should().Be(" ");
        result.Tijdschema[0].Pagina.Should().Be(" ");
    }

    [Test]
    public void Includes_page_entries_sorted_by_time()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1430, order: 1, titel: "Afternoon"),
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Morning"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>();

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().HaveCount(2);
        result.Tijdschema[0].NaamEncounter.Should().Contain("Morning");
        result.Tijdschema[1].NaamEncounter.Should().Contain("Afternoon");
    }

    [Test]
    public void Merges_meetings_and_pages_in_sort_order()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Encounter"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.EersteDag, "09:00", "Briefing", 1),
            Meeting.Create(MeetingDagType.EersteDag, "12:00", "Lunch", 2),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().HaveCount(3);
        result.Tijdschema[0].NaamEncounter.Should().Be("Briefing");
        result.Tijdschema[0].IsMeeting.Should().BeTrue();
        result.Tijdschema[1].NaamEncounter.Should().Contain("Encounter");
        result.Tijdschema[1].IsMeeting.Should().BeFalse();
        result.Tijdschema[2].NaamEncounter.Should().Be("Lunch");
        result.Tijdschema[2].IsMeeting.Should().BeTrue();
    }

    [Test]
    public void Hides_time_for_subsequent_pages_in_same_time_block()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "First"),
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 2, titel: "Second"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>();

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().HaveCount(2);
        result.Tijdschema[0].Tijd.Should().NotBe(" ");
        result.Tijdschema[0].HideTopBorder.Should().BeFalse();
        result.Tijdschema[1].Tijd.Should().Be(" ");
        result.Tijdschema[1].HideTopBorder.Should().BeTrue();
    }

    [Test]
    public void Meeting_resets_time_grouping_so_next_page_is_first_in_block()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Before meeting"),
            CreatePage(starttijdId: StarttijdIds.Tijd1430, order: 1, titel: "After meeting"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.EersteDag, "12:00", "Lunch", 1),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        var pageEntries = result.Tijdschema.Where(t => !t.IsMeeting).ToList();
        pageEntries[0].HideTopBorder.Should().BeFalse();
        pageEntries[1].HideTopBorder.Should().BeFalse();
    }

    [Test]
    public void Page_with_Starttijd_None_uses_space_for_time()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.None, order: 1, titel: "Unscheduled"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>();

        var result = PlotboekExportBuilder.BuildTijdschemasData("Nog niet ingepland", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().ContainSingle();
        result.Tijdschema[0].Tijd.Should().Be(" ");
    }

    [Test]
    public void Sets_PaginaBookmark_for_page_entries()
    {
        var paginaId = PlotboekPaginaId.Next();
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1030, paginaId: paginaId),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>();

        var result = PlotboekExportBuilder.BuildTijdschemasData("Vrijdag", pages, nummers, userLookup, meetings);

        result.Tijdschema[0].PaginaBookmark.Should().Be(PlotboekExportBuilder.SanitizePageBookmarkName(paginaId));
    }

    [TestCase("Avondeten")]
    [TestCase("Dinner")]
    public void Sets_PageBreakBefore_on_meeting_after_Avondeten(string avondeten)
    {
        var pages = new List<PlotboekPagina.ExportData>();
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.Tussendag, "17:30", avondeten, 1),
            Meeting.Create(MeetingDagType.Tussendag, "19:00", "Blok 20:00 Rolverdeling", 2),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Zaterdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().HaveCount(2);
        result.Tijdschema[0].NaamEncounter.Should().Be(avondeten);
        result.Tijdschema[0].PageBreakBefore.Should().BeFalse();
        result.Tijdschema[1].NaamEncounter.Should().Be("Blok 20:00 Rolverdeling");
        result.Tijdschema[1].PageBreakBefore.Should().BeTrue();
    }

    [Test]
    public void Sets_PageBreakBefore_on_first_plot_after_Avondeten()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1630, order: 1, titel: "Daytime plot"),
            CreatePage(starttijdId: StarttijdIds.Tijd2000, order: 1, titel: "Evening plot"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.Tussendag, "17:30", "Avondeten", 1),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Zaterdag", pages, nummers, userLookup, meetings);

        // 16:30 plot, Avondeten (17:30), then 20:00 plot -> break before the evening plot only.
        var daytime = result.Tijdschema.Single(t => t.NaamEncounter.Contains("Daytime"));
        var evening = result.Tijdschema.Single(t => t.NaamEncounter.Contains("Evening"));
        daytime.PageBreakBefore.Should().BeFalse();
        evening.PageBreakBefore.Should().BeTrue();
    }

    [Test]
    public void Does_not_set_PageBreakBefore_when_no_Avondeten()
    {
        var pages = new List<PlotboekPagina.ExportData>
        {
            CreatePage(starttijdId: StarttijdIds.Tijd1030, order: 1, titel: "Morning"),
        };
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        var userLookup = new Dictionary<string, string>();
        var meetings = new List<Meeting>
        {
            Meeting.Create(MeetingDagType.Tussendag, "12:30", "Lunch", 1),
        };

        var result = PlotboekExportBuilder.BuildTijdschemasData("Zaterdag", pages, nummers, userLookup, meetings);

        result.Tijdschema.Should().OnlyContain(t => !t.PageBreakBefore);
    }

    private static PlotboekPagina.ExportData CreatePage(
        string starttijdId = StarttijdIds.None,
        int order = 0,
        string titel = "Test",
        PlotboekPaginaId paginaId = default)
    {
        return new PlotboekPagina.ExportData
        {
            PaginaId = paginaId == default ? PlotboekPaginaId.Next() : paginaId,
            Titel = titel,
            Dag = Dag.Vrijdag,
            StarttijdId = starttijdId,
            Order = order,
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
