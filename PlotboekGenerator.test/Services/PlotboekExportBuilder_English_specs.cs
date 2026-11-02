using PlotboekGenerator.core.Generator.Templates;

namespace PlotboekGenerator.test.Services;

/// <summary>
/// The export builder's own words follow the export's language (the web layer runs it in the
/// deployment's default language). NUnit puts the thread's UI culture back after each spec.
/// </summary>
[SetUICulture("en")]
public sealed class PlotboekExportBuilder_English_specs
{
    [Test]
    public void A_player_driven_plot_reads_in_english()
        => PlotboekExportBuilder.BuildDagdeel(CreatePage(isSpelerGedreven: true)).Should().Be("Player-driven");

    [Test]
    public void A_flexible_plot_reads_in_english()
        => PlotboekExportBuilder.BuildDagdeel(CreatePage(starttijdId: StarttijdIds.Tijd2000, isFlexibel: true)).Should().Be("20:00 (flexible)");

    [Test]
    public void An_empty_field_gets_the_english_placeholder()
        => PlotboekExportBuilder.OrPlaceholder(" ").Should().Be("Not filled in yet").And.Be(PageData.NogNietIngevuld);

    [Test]
    public void All_npcs_reads_in_english_in_the_time_table()
        => PlotboekExportBuilder.BuildNpcsSummary(AantalNpcsData.Create([], alleNpcs: true)).Should().Be("ALL");

    private static PlotboekPagina.ExportData CreatePage(
        string starttijdId = StarttijdIds.None,
        bool isSpelerGedreven = false,
        bool isFlexibel = false)
        => new ()
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
