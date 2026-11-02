namespace PlotboekGenerator.test.Services;

public sealed class GetDisplayTitleWithPlotlijn_specs
{
    [Test]
    public void Returns_title_only_when_no_domain_and_no_plotlijn()
    {
        var page = CreatePage(titel: "De Handelaar");
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        PlotboekExportBuilder.GetDisplayTitleWithPlotlijn(page, nummers).Should().Be("De Handelaar");
    }

    [Test]
    public void Includes_domain_shorthand_when_set()
    {
        var page = CreatePage(titel: "De Handelaar", domainShorthand: "Ti");
        var nummers = new Dictionary<PlotboekPaginaId, string>();
        PlotboekExportBuilder.GetDisplayTitleWithPlotlijn(page, nummers).Should().Be("Ti De Handelaar");
    }

    [Test]
    public void Includes_domain_and_plotlijn_number_when_both_set()
    {
        var id = PlotboekPaginaId.Next();
        var page = CreatePage(titel: "De Handelaar", domainShorthand: "Ti", paginaId: id);
        var nummers = new Dictionary<PlotboekPaginaId, string> { [id] = "3.1" };
        PlotboekExportBuilder.GetDisplayTitleWithPlotlijn(page, nummers).Should().Be("Ti 3.1 De Handelaar");
    }

    [Test]
    public void Returns_title_when_no_domain_even_with_plotlijn_number()
    {
        var id = PlotboekPaginaId.Next();
        var page = CreatePage(titel: "De Handelaar", paginaId: id);
        var nummers = new Dictionary<PlotboekPaginaId, string> { [id] = "2" };
        PlotboekExportBuilder.GetDisplayTitleWithPlotlijn(page, nummers).Should().Be("De Handelaar");
    }

    private static PlotboekPagina.ExportData CreatePage(
        string titel = "Test",
        string domainShorthand = "",
        PlotboekPaginaId paginaId = default)
    {
        return new PlotboekPagina.ExportData
        {
            PaginaId = paginaId,
            Titel = titel,
            DomainShorthand = domainShorthand,
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

