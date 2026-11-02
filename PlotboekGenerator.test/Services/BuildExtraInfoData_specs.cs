using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildExtraInfoData_specs
{
    [Test]
    public void Builds_data_with_icon_prefix()
    {
        var page = CreatePage(
            titel: "Gevecht in het bos",
            domainShorthand: "A",
            meerInformatie: "Extra details here",
            encounterTypeIds: EncounterTypeIds.Create([SoortEncounterIds.Vechten], []));
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.NaamEncounter.Should().Contain("\u2694\uFE0F");
        result.NaamEncounter.Should().Contain("A Gevecht in het bos");
        result.MeerInformatie.Should().Be("Extra details here");
    }

    [Test]
    public void Builds_data_without_icon_prefix()
    {
        var page = CreatePage(titel: "Simpele scene", meerInformatie: "Some info");
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.NaamEncounter.Should().Be("Simpele scene");
        result.MeerInformatie.Should().Be("Some info");
    }

    [Test]
    public void Sets_bookmark_name_from_paginaId()
    {
        var paginaId = PlotboekPaginaId.Next();
        var page = CreatePage(paginaId: paginaId, meerInformatie: "Info");
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.BookmarkName.Should().Be(PlotboekExportBuilder.SanitizeBookmarkName(paginaId));
    }

    [Test]
    public void Uses_placeholder_when_title_is_empty()
    {
        var page = CreatePage(titel: string.Empty, meerInformatie: "Info");
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.NaamEncounter.Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Includes_plotlijn_number_in_name()
    {
        var paginaId = PlotboekPaginaId.Next();
        var page = CreatePage(paginaId: paginaId, titel: "Scene", domainShorthand: "B", meerInformatie: "Details");
        var nummers = new Dictionary<PlotboekPaginaId, string> { [paginaId] = "2.1" };

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.NaamEncounter.Should().Be("B 2.1 Scene");
    }

    [Test]
    public void Returns_empty_MeerInformatie_when_empty()
    {
        var page = CreatePage(meerInformatie: string.Empty);
        var nummers = new Dictionary<PlotboekPaginaId, string>();

        var result = PlotboekExportBuilder.BuildExtraInfoData(page, nummers);

        result.MeerInformatie.Should().BeEmpty();
    }

    private static PlotboekPagina.ExportData CreatePage(
        string titel = "Test",
        string domainShorthand = "",
        string meerInformatie = "",
        PlotboekPaginaId paginaId = default,
        EncounterTypeIds encounterTypeIds = default)
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
            MeerInformatie = meerInformatie,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            Iconen = PlotboekPaginaIcons.None,
            EncounterTypeIds = encounterTypeIds == default ? EncounterTypeIds.Empty : encounterTypeIds,

            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
        };
    }
}

