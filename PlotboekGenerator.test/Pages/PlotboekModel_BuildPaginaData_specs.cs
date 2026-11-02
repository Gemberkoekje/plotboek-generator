namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_BuildPaginaData_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Maps_basic_fields()
    {
        var model = CreateModel();
        model.EditableTitle = "Test Encounter";
        model.EditableDay = Dag.Vrijdag;
        model.EditableStarttijd = "Tijd1030";
        model.EditableOrder = 3;
        model.EditableElevatorPitch = "Pitch";
        model.EditableSamenvattingPlot = "Summary";
        model.EditableMeerInformatie = "Extra info";
        model.EditableOpmerkingen = "Notes";
        model.EditableInstructiePentagon = "Instructions";

        var result = model.BuildPaginaData();

        result.Titel.Should().Be("Test Encounter");
        result.Dag.Should().Be(Dag.Vrijdag);
        result.StarttijdId.Should().Be("Tijd1030");
        result.Order.Should().Be(3);
        result.ElevatorPitch.Should().Be("Pitch");
        result.SamenvattingPlot.Should().Be("Summary");
        result.MeerInformatie.Should().Be("Extra info");
        result.Opmerkingen.Should().Be("Notes");
        result.InstructiePentagon.Should().Be("Instructions");
    }

    [Test]
    public void Null_text_fields_become_empty_string()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableElevatorPitch = null;
        model.EditableSamenvattingPlot = null;
        model.EditableMeerInformatie = null;
        model.EditableOpmerkingen = null;
        model.EditableInstructiePentagon = null;

        var result = model.BuildPaginaData();

        result.ElevatorPitch.Should().BeEmpty();
        result.SamenvattingPlot.Should().BeEmpty();
        result.MeerInformatie.Should().BeEmpty();
        result.Opmerkingen.Should().BeEmpty();
        result.InstructiePentagon.Should().BeEmpty();
    }

    [Test]
    public void Maps_boolean_flags()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableIsVolledigKlaar = true;
        model.EditableIsGedraaid = true;
        model.EditableIsSpelerGedreven = true;
        model.EditableIsFlexibel = true;

        var result = model.BuildPaginaData();

        result.IsVolledigKlaar.Should().BeTrue();
        result.IsGedraaid.Should().BeTrue();
        result.IsSpelerGedreven.Should().BeTrue();
        result.IsFlexibel.Should().BeTrue();
    }

    [Test]
    public void Maps_AantalBlokken()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableAantalBlokken = "TweeBlokken";

        var result = model.BuildPaginaData();

        result.AantalBlokkenId.Should().Be("TweeBlokken");
    }

    [Test]
    public void Resolves_SL_display_name_from_AvailableSls()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableVerantwoordelijkeSlId = "user-1";
        model.AvailableSls =
        [
            new PlotboekModel.SlUserInfo { Id = "user-1", DisplayName = "Alice" },
        ];

        var result = model.BuildPaginaData();

        result.VerantwoordelijkeSlId.Should().Be("user-1");
        result.VerantwoordelijkeSlName.Should().Be("Alice");
    }

    [Test]
    public void SL_name_is_null_when_not_found()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableVerantwoordelijkeSlId = "user-99";

        var result = model.BuildPaginaData();

        result.VerantwoordelijkeSlName.Should().BeNull();
    }

    [Test]
    public void Filters_empty_extra_SL_ids()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableExtraSlIds = ["user-1", string.Empty, "user-2"];
        model.AvailableSls =
        [
            new PlotboekModel.SlUserInfo { Id = "user-1", DisplayName = "Alice" },
            new PlotboekModel.SlUserInfo { Id = "user-2", DisplayName = "Bob" },
        ];

        var result = model.BuildPaginaData();

        result.ExtraSlIds.Should().HaveCount(2);
        result.ExtraSlNames.Should().BeEquivalentTo("Alice", "Bob");
    }

    [Test]
    public void Filters_blank_NPC_rollen()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableNpcRollen =
        [
            new PlotboekModel.NpcRolInput { Rol = "Guard", MinimumAantal = 2, MaximumAantal = null, IsTerukerend = false, IsEerderGespeeld = false, GespeeldDoor = null, IsAlGekozen = false, GekozenPersoon = null, KledingEnSchmink = null },
            new PlotboekModel.NpcRolInput { Rol = "  ", MinimumAantal = 1, MaximumAantal = null, IsTerukerend = false, IsEerderGespeeld = false, GespeeldDoor = null, IsAlGekozen = false, GekozenPersoon = null, KledingEnSchmink = null },
        ];

        var result = model.BuildPaginaData();

        result.AantalNpcs.Rollen.Should().ContainSingle();
    }

    [Test]
    public void Maps_NPC_casting_fields()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableNpcRollen =
        [
            new PlotboekModel.NpcRolInput
            {
                Rol = "Slachtoffer",
                MinimumAantal = 1,
                MaximumAantal = null,
                IsTerukerend = false,
                IsEerderGespeeld = false,
                GespeeldDoor = null,
                IsAlGekozen = false,
                GekozenPersoon = null,
                KledingEnSchmink = null,
                CastingInstructie = "goede roleplayer",
                VereistAfstemming = true,
                AfstemmingToelichting = "wordt gemarteld in scène 2",
            },
        ];

        var result = model.BuildPaginaData();

        var rol = result.AantalNpcs.Rollen.Single();
        rol.CastingInstructie.Should().Be("goede roleplayer");
        rol.VereistAfstemming.Should().BeTrue();
        rol.AfstemmingToelichting.Should().Be("wordt gemarteld in scène 2");
        result.AantalNpcs.HeeftAfstemmingNodig.Should().BeTrue();
    }

    [Test]
    public void Drops_afstemming_toelichting_when_the_checkbox_is_off()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableNpcRollen =
        [
            new PlotboekModel.NpcRolInput
            {
                Rol = "Slachtoffer",
                MinimumAantal = 1,
                MaximumAantal = null,
                IsTerukerend = false,
                IsEerderGespeeld = false,
                GespeeldDoor = null,
                IsAlGekozen = false,
                GekozenPersoon = null,
                KledingEnSchmink = null,
                VereistAfstemming = false,
                AfstemmingToelichting = "blijft hangen uit een eerdere invoer",
            },
        ];

        var result = model.BuildPaginaData();

        result.AantalNpcs.Rollen.Single().AfstemmingToelichting.Should().BeNull();
    }

    [Test]
    public void Maps_geen_npcs_nodig()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableGeenNpcsNodig = true;

        var result = model.BuildPaginaData();

        result.AantalNpcs.GeenNpcsNodig.Should().BeTrue();
        result.AantalNpcs.HasValue.Should().BeTrue();
    }

    [Test]
    public void Geen_npcs_nodig_clears_alle_npcs_and_rollen()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableGeenNpcsNodig = true;
        model.EditableAlleNpcs = true;
        model.EditableNpcRollen =
        [
            new PlotboekModel.NpcRolInput { Rol = "Guard", MinimumAantal = 2, MaximumAantal = null, IsTerukerend = false, IsEerderGespeeld = false, GespeeldDoor = null, IsAlGekozen = false, GekozenPersoon = null, KledingEnSchmink = null },
        ];

        var result = model.BuildPaginaData();

        result.AantalNpcs.GeenNpcsNodig.Should().BeTrue();
        result.AantalNpcs.AlleNpcs.Should().BeFalse();
        result.AantalNpcs.Rollen.Should().BeEmpty();
    }

    [Test]
    public void Filters_blank_props_and_loot()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableProps =
        [
            new PlotboekModel.PropLootItemInput { Omschrijving = "Sword" },
            new PlotboekModel.PropLootItemInput { Omschrijving = "  " },
        ];
        model.EditableLoot =
        [
            new PlotboekModel.PropLootItemInput { Omschrijving = null },
            new PlotboekModel.PropLootItemInput { Omschrijving = "Gold coins" },
        ];

        var result = model.BuildPaginaData();

        result.PropsEnLoot.Props.Should().ContainSingle();
        result.PropsEnLoot.Loot.Should().ContainSingle();
    }

    [Test]
    public void EventBreed_auto_assigns_Hoofdveld_when_no_location_set()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableEventBreed = true;
        model.EditableLocatieEncounter = LocatieEncounter.None;

        var result = model.BuildPaginaData();

        result.LocatieEncounter.Locatie.Should().Be(LocatieEncounter.Hoofdveld);
    }

    [Test]
    public void EventBreed_does_not_override_existing_location()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableEventBreed = true;
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;

        var result = model.BuildPaginaData();

        result.LocatieEncounter.Locatie.Should().Be(LocatieEncounter.Questveld);
    }

    [Test]
    public void Resolves_domain_info_from_AvailableDomains()
    {
        var domainId = Guid.NewGuid();
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableDomainId = domainId;
        model.AvailableDomains =
        [
            Domain.Create("C", "Chaos", 1) with { Id = domainId },
        ];

        var result = model.BuildPaginaData();

        result.DomainId.Should().Be(domainId);
        result.DomainShorthand.Should().Be("C");
        result.DomainDescription.Should().Be("Chaos");
    }

    [Test]
    public void Domain_is_null_when_not_found()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableDomainId = Guid.NewGuid();

        var result = model.BuildPaginaData();

        result.DomainShorthand.Should().BeNull();
        result.DomainDescription.Should().BeNull();
    }

    [Test]
    public void Maps_encounter_types()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableEncounterTypesMajor = ["Vechten"];
        model.EditableEncounterTypesMinor = ["Puzzel"];

        var result = model.BuildPaginaData();

        result.EncounterTypeIds.Major.Should().Contain("Vechten");
        result.EncounterTypeIds.Minor.Should().Contain("Puzzel");
    }

    [Test]
    public void Maps_debrief_data()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableDebriefGewenst = true;
        model.EditableDebriefVragen = ["Vraag 1", "Vraag 2"];

        var result = model.BuildPaginaData();

        result.Debrief.IsDebriefGewenst.Should().BeTrue();
        result.Debrief.Vragen.Should().HaveCount(2);
    }

    [Test]
    public void Evergreen_forces_plotlijn_to_empty()
    {
        var plotlijnId = PlotlijnId.Next();
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EventId = "Evergreen";
        model.EditablePlotlijnIdRaw = $"plotlijn:{plotlijnId}";

        var result = model.BuildPaginaData();

        result.PlotlijnId.Should().Be(PlotlijnId.Empty);
    }

    [Test]
    public void Non_evergreen_maps_plotlijn()
    {
        var plotlijnId = PlotlijnId.Next();
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EventId = "Voorjaar";
        model.EditablePlotlijnIdRaw = $"plotlijn:{plotlijnId}";

        var result = model.BuildPaginaData();

        result.PlotlijnId.Should().Be(plotlijnId);
    }

    [Test]
    public void Maps_referenced_bijlagen()
    {
        var id1 = BijlageId.Next();
        var id2 = BijlageId.Next();
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableReferencedBijlagen = [id1.ToString(), id2.ToString()];

        var result = model.BuildPaginaData();

        result.BijlageIds.Should().BeEquivalentTo(new[] { id1, id2 });
    }

    [Test]
    public void Filters_empty_and_invalid_referenced_bijlagen()
    {
        var id = BijlageId.Next();
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableReferencedBijlagen = [id.ToString(), string.Empty, "not-a-valid-id"];

        var result = model.BuildPaginaData();

        result.BijlageIds.Should().ContainSingle().Which.Should().Be(id);
    }
}
