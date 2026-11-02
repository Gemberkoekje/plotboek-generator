namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_InitializePaginaEditableFields_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Sets_defaults_when_no_selected_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = null;

        model.InitializePaginaEditableFields();

        model.PaginaVersion.Should().Be(0);
        model.EditableTitle.Should().BeNull();
        model.EditableDomainId.Should().Be(Guid.Empty);
        model.EditableDay.Should().Be(Dag.None);
        model.EditableStarttijd.Should().Be(StarttijdIds.None);
        model.EditableOrder.Should().Be(0);
        model.EditableVerantwoordelijkeSlId.Should().BeNull();
        model.EditableExtraSlIds.Should().BeEmpty();
        model.EditableAantalBlokken.Should().Be(AantalBlokkenIds.None);
        model.EditableEncounterTypesMajor.Should().BeEmpty();
        model.EditableEncounterTypesMinor.Should().BeEmpty();
        model.EditableLocatieEncounter.Should().Be(LocatieEncounter.None);
        model.EditableAlleNpcs.Should().BeFalse();
        model.EditableNpcRollen.Should().BeEmpty();
        model.EditablePlotlijnIdRaw.Should().BeNull();
        model.EditableElevatorPitch.Should().BeNull();
        model.EditableSamenvattingPlot.Should().BeNull();
        model.EditableMeerInformatie.Should().BeNull();
        model.EditableIsVolledigKlaar.Should().BeFalse();
        model.EditableIsGedraaid.Should().BeFalse();
        model.EditableIsSpelerGedreven.Should().BeFalse();
        model.EditableIsFlexibel.Should().BeFalse();
        model.EditableTimingCruciaal.Should().BeFalse();
        model.EditablePlotCruciaal.Should().BeFalse();
        model.EditableMagAlsEersteCancelled.Should().BeFalse();
        model.EditableUitgebreideBriefing.Should().BeFalse();
        model.EditableGrootPakOfKostuum.Should().BeFalse();
        model.EditableMuziekVanuitTechniek.Should().BeFalse();
        model.EditableLichtOfRookVanuitTechniek.Should().BeFalse();
        model.EditableEventBreed.Should().BeFalse();
        model.EditableProps.Should().BeEmpty();
        model.EditableLoot.Should().BeEmpty();
        model.EditableOpmerkingen.Should().BeNull();
        model.EditableDebriefGewenst.Should().BeFalse();
        model.EditableDebriefVragen.Should().BeEmpty();
    }

    [Test]
    public void Copies_basic_fields_from_selected_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            EventVersion = 42,
            Title = "My Encounter",
            Dag = Dag.Zaterdag,
            StarttijdId = StarttijdIds.Tijd2000,
            Order = 5,
            VerantwoordelijkeSlId = "user-1",
            ElevatorPitch = "Pitch text",
            SamenvattingPlot = "Summary text",
            MeerInformatie = "More info",
            Opmerkingen = "Remarks",
            InstructiePentagon = "Techniek text",
            AantalBlokkenId = AantalBlokkenIds.TweeBlokken,
        };

        model.InitializePaginaEditableFields();

        model.PaginaVersion.Should().Be(42);
        model.EditableTitle.Should().Be("My Encounter");
        model.EditableDay.Should().Be(Dag.Zaterdag);
        model.EditableStarttijd.Should().Be(StarttijdIds.Tijd2000);
        model.EditableOrder.Should().Be(5);
        model.EditableVerantwoordelijkeSlId.Should().Be("user-1");
        model.EditableElevatorPitch.Should().Be("Pitch text");
        model.EditableSamenvattingPlot.Should().Be("Summary text");
        model.EditableMeerInformatie.Should().Be("More info");
        model.EditableOpmerkingen.Should().Be("Remarks");
        model.EditableInstructiePentagon.Should().Be("Techniek text");
        model.EditableAantalBlokken.Should().Be(AantalBlokkenIds.TweeBlokken);
    }

    [Test]
    public void Copies_boolean_flags_from_selected_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            IsVolledigKlaar = true,
            IsGedraaid = true,
            IsSpelerGedreven = true,
            IsFlexibel = true,
        };

        model.InitializePaginaEditableFields();

        model.EditableIsVolledigKlaar.Should().BeTrue();
        model.EditableIsGedraaid.Should().BeTrue();
        model.EditableIsSpelerGedreven.Should().BeTrue();
        model.EditableIsFlexibel.Should().BeTrue();
    }

    [Test]
    public void Decomposes_icon_flags()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            Iconen = PlotboekPaginaIcons.TimingCruciaal
                   | PlotboekPaginaIcons.PlotCruciaal
                   | PlotboekPaginaIcons.MuziekVanuitTechniek,
        };

        model.InitializePaginaEditableFields();

        model.EditableTimingCruciaal.Should().BeTrue();
        model.EditablePlotCruciaal.Should().BeTrue();
        model.EditableMuziekVanuitTechniek.Should().BeTrue();
        model.EditableMagAlsEersteCancelled.Should().BeFalse();
        model.EditableUitgebreideBriefing.Should().BeFalse();
        model.EditableGrootPakOfKostuum.Should().BeFalse();
        model.EditableLichtOfRookVanuitTechniek.Should().BeFalse();
    }

    [Test]
    public void Sets_EventBreed_when_Hoofdveld_and_EventBreed_icon()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            Iconen = PlotboekPaginaIcons.EventBreed,
            LocatieEncounter = new LocatieEncounterData { Locatie = LocatieEncounter.Hoofdveld },
        };

        model.InitializePaginaEditableFields();

        model.EditableEventBreed.Should().BeTrue();
    }

    [Test]
    public void Does_not_set_EventBreed_when_not_Hoofdveld()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            Iconen = PlotboekPaginaIcons.EventBreed,
            LocatieEncounter = new LocatieEncounterData { Locatie = LocatieEncounter.Questveld },
        };

        model.InitializePaginaEditableFields();

        model.EditableEventBreed.Should().BeFalse();
    }

    [Test]
    public void Sets_EventBreed_when_Hoofdveld_and_AlleNpcs()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            LocatieEncounter = new LocatieEncounterData { Locatie = LocatieEncounter.Hoofdveld },
            AantalNpcs = AantalNpcsData.Create([], alleNpcs: true),
        };

        model.InitializePaginaEditableFields();

        model.EditableEventBreed.Should().BeTrue();
    }

    [Test]
    public void Initializes_plotlijn_raw_from_plotlijn_id()
    {
        var plotlijnId = PlotlijnId.Next();
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            PlotlijnId = plotlijnId,
        };

        model.InitializePaginaEditableFields();

        model.EditablePlotlijnIdRaw.Should().Be($"plotlijn:{plotlijnId}");
    }

    [Test]
    public void Plotlijn_raw_is_null_when_no_plotlijn()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
        };

        model.InitializePaginaEditableFields();

        model.EditablePlotlijnIdRaw.Should().BeNull();
    }

    [Test]
    public void Initializes_NPC_rollen_from_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            AantalNpcs = AantalNpcsData.Create(
            [
                NpcRolData.Create("Guard", 2, 4, isTerukerend: true, isEerderGespeeld: false, gespeeldDoor: null, isAlGekozen: false, gekozenPersoon: null, kledingEnSchmink: "Armor"),
            ], alleNpcs: false),
        };

        model.InitializePaginaEditableFields();

        model.EditableNpcRollen.Should().ContainSingle();
        model.EditableNpcRollen[0].Rol.Should().Be("Guard");
        model.EditableNpcRollen[0].MinimumAantal.Should().Be(2);
        model.EditableNpcRollen[0].MaximumAantal.Should().Be(4);
        model.EditableNpcRollen[0].IsTerukerend.Should().BeTrue();
        model.EditableNpcRollen[0].KledingEnSchmink.Should().Be("Armor");
    }

    // Init and BuildPaginaData have to stay symmetric: a field that is only read back would silently lose
    // the user's input on a concurrency reload, since that path runs Init again.
    [Test]
    public void Initializes_NPC_casting_fields_from_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            AantalNpcs = AantalNpcsData.Create(
            [
                NpcRolData.Create(
                    "Slachtoffer",
                    1,
                    castingInstructie: "goede roleplayer",
                    vereistAfstemming: true,
                    afstemmingToelichting: "wordt gemarteld in scène 2"),
            ], alleNpcs: false),
        };

        model.InitializePaginaEditableFields();

        model.EditableNpcRollen[0].CastingInstructie.Should().Be("goede roleplayer");
        model.EditableNpcRollen[0].VereistAfstemming.Should().BeTrue();
        model.EditableNpcRollen[0].AfstemmingToelichting.Should().Be("wordt gemarteld in scène 2");
    }

    [Test]
    public void Initializes_debrief_fields()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            Debrief = DebriefData.Create(true, ["Q1", "Q2"]),
        };

        model.InitializePaginaEditableFields();

        model.EditableDebriefGewenst.Should().BeTrue();
        model.EditableDebriefVragen.Should().BeEquivalentTo("Q1", "Q2");
    }

    [Test]
    public void Initializes_extra_SL_ids()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            ExtraSlIds = ["user-2", "user-3"],
        };

        model.InitializePaginaEditableFields();

        model.EditableExtraSlIds.Should().BeEquivalentTo("user-2", "user-3");
    }

    [Test]
    public void Initializes_props_and_loot()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = new PlotboekModel.PlotboekPaginaInfo
        {
            Id = PlotboekPaginaId.Next(),
            Title = "Test",
            Dag = Dag.None,
            StarttijdId = StarttijdIds.None,
            Order = 1,
            PropsEnLoot = PropsEnLootData.Create(
                [PropLootItemData.Create("Sword", "user-1", "Alice")],
                [PropLootItemData.Create("Gold", null, null)]),
        };

        model.InitializePaginaEditableFields();

        model.EditableProps.Should().ContainSingle();
        model.EditableProps[0].Omschrijving.Should().Be("Sword");
        model.EditableProps[0].VerantwoordelijkeSlId.Should().Be("user-1");
        model.EditableLoot.Should().ContainSingle();
        model.EditableLoot[0].Omschrijving.Should().Be("Gold");
    }
}
