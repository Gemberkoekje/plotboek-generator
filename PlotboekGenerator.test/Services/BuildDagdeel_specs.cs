using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

public sealed class BuildDagdeel_specs
{
    [Test]
    public void Returns_Speler_gedreven_when_IsSpelerGedreven()
    {
        var page = CreatePage(isSpelerGedreven: true);
        PlotboekExportBuilder.BuildDagdeel(page).Should().Be("Speler gedreven");
    }

    [Test]
    public void Returns_placeholder_when_Starttijd_is_None()
    {
        var page = CreatePage(starttijdId: StarttijdIds.None);
        PlotboekExportBuilder.BuildDagdeel(page).Should().Be(PageData.NogNietIngevuld);
    }

    [Test]
    public void Returns_display_name_when_not_flexibel()
    {
        var page = CreatePage(starttijdId: StarttijdIds.Tijd1430);
        PlotboekExportBuilder.BuildDagdeel(page).Should().Be("14:30");
    }

    [Test]
    public void Appends_Flexibel_when_IsFlexibel()
    {
        var page = CreatePage(starttijdId: StarttijdIds.Tijd2000, isFlexibel: true);
        PlotboekExportBuilder.BuildDagdeel(page).Should().Be("20:00 (Flexibel)");
    }

    [Test]
    public void SpelerGedreven_takes_priority_over_Starttijd()
    {
        var page = CreatePage(isSpelerGedreven: true, starttijdId: StarttijdIds.Tijd1030);
        PlotboekExportBuilder.BuildDagdeel(page).Should().Be("Speler gedreven");
    }

    private static PlotboekPagina.ExportData CreatePage(
        string starttijdId = StarttijdIds.None,
        bool isSpelerGedreven = false,
        bool isFlexibel = false)
    {
        return new PlotboekPagina.ExportData
        {
            Titel = "Test",
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
            StarttijdId = starttijdId,
            IsSpelerGedreven = isSpelerGedreven,
            IsFlexibel = isFlexibel,
        };
    }
}

