namespace PlotboekGenerator.test.Services;

public sealed class BuildIconenPrefix_specs
{
    [Test]
    public void Returns_empty_when_no_icons_and_no_combat()
    {
        var page = CreatePage();
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().BeEmpty();
    }

    [Test]
    public void Includes_swords_when_encounter_has_Vechten()
    {
        var page = CreatePage(encounterTypeIds: EncounterTypeIds.Create([SoortEncounterIds.Vechten], []));
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\u2694\uFE0F");
    }

    [Test]
    public void Includes_FlexibelNachtPlot_icon()
    {
        var page = CreatePage(isFlexibelNachtPlot: true);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F319");
    }

    [Test]
    public void Includes_EventBreed_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.EventBreed);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F46F");
    }

    [Test]
    public void Includes_TimingCruciaal_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.TimingCruciaal);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\u2B50");
    }

    [Test]
    public void Includes_PlotCruciaal_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.PlotCruciaal);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\u2757");
    }

    [Test]
    public void Includes_MagAlsEersteCancelled_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.MagAlsEersteCancelled);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\u2753");
    }

    [Test]
    public void Includes_UitgebreideBriefing_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.UitgebreideBriefing);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F4D6");
    }

    [Test]
    public void Includes_Questveld_icon()
    {
        var page = CreatePage(locatie: LocatieEncounter.Questveld);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001FAA6");
    }

    [Test]
    public void Includes_GrootPakOfKostuum_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.GrootPakOfKostuum);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\u2764\uFE0F");
    }

    [Test]
    public void Includes_MuziekVanuitTechniek_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.MuziekVanuitTechniek);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F3B6");
    }

    [Test]
    public void Includes_LichtOfRookVanuitTechniek_icon()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F4A1");
    }

    [Test]
    public void Multiple_icons_are_concatenated()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal);
        var result = PlotboekExportBuilder.BuildIconenPrefix(page);
        result.Should().Contain("\u2B50");
        result.Should().Contain("\u2757");
    }

    // Derived from the NPC roles instead of from an icon flag.
    [Test]
    public void Includes_Afstemming_icon_when_a_role_needs_it()
    {
        var page = CreatePage(aantalNpcs: AantalNpcsData.Create([
            NpcRolData.Create("slachtoffer", 1, vereistAfstemming: true, afstemmingToelichting: "wordt gemarteld"),
        ]));
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().Contain("\U0001F91D");
    }

    [Test]
    public void Omits_Afstemming_icon_when_no_role_needs_it()
    {
        var page = CreatePage(aantalNpcs: AantalNpcsData.Create([NpcRolData.Create("guard", 2)]));
        PlotboekExportBuilder.BuildIconenPrefix(page).Should().NotContain("\U0001F91D");
    }

    private static PlotboekPagina.ExportData CreatePage(
        PlotboekPaginaIcons iconen = PlotboekPaginaIcons.None,
        EncounterTypeIds encounterTypeIds = default,
        LocatieEncounter locatie = LocatieEncounter.None,
        bool isFlexibelNachtPlot = false,
        AantalNpcsData aantalNpcs = default)
    {
        return new PlotboekPagina.ExportData
        {
            Titel = "Test",
            IsFlexibelNachtPlot = isFlexibelNachtPlot,
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = string.Empty,
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = string.Empty,
            InstructiePentagon = string.Empty,
            Opmerkingen = string.Empty,
            Iconen = iconen,
            EncounterTypeIds = encounterTypeIds == default ? EncounterTypeIds.Empty : encounterTypeIds,

            LocatieEncounter = new LocatieEncounterData { Locatie = locatie },
            AantalNpcs = aantalNpcs == default ? AantalNpcsData.Empty : aantalNpcs,
        };
    }
}
