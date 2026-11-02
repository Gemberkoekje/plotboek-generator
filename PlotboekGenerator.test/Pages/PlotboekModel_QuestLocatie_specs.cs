namespace PlotboekGenerator.test.Pages;

/// <summary>
/// The questlocatie picker must be written by <c>BuildPaginaData</c> AND read back by
/// <c>InitializePaginaEditableFields</c>. If the init half is missing, a concurrency conflict reloads the
/// form and silently drops the user's choice — the same trap the bijlagen picker fell into.
/// </summary>
public sealed class PlotboekModel_QuestLocatie_specs
{
    private static PlotboekModel CreateModel()
    {
        var model = new PlotboekModel(null!, null!, null!, null!, null!, null!, null!, null!, null!)
        {
            AvailableQuestLocatieCategorieConfigs =
            [
                new QuestLocatieCategorieConfig { Id = "Plekken", DisplayName = "Plekken", Notitie = "Controleer op kampeerders.", Ernst = QuestLocatieErnst.LetOp, Order = 1 },
                new QuestLocatieCategorieConfig { Id = "Afgeraden", DisplayName = "Liever niet", Notitie = "Liever niet gebruiken.", Ernst = QuestLocatieErnst.Afgeraden, Order = 2 },
            ],
            AvailableQuestLocatieConfigs =
            [
                new QuestLocatieConfig { Id = "Ruine", DisplayName = "Ruïne", CategorieId = "Plekken", Notitie = "Alleen als het ruïneveld leeg is.", IsActief = true, Order = 1 },
                new QuestLocatieConfig { Id = "Tommesbos", DisplayName = "Tommesbos", CategorieId = "Afgeraden", IsActief = true, Order = 1 },
                new QuestLocatieConfig { Id = "OudeRuine", DisplayName = "Oude ruïne", CategorieId = "Plekken", IsActief = false, Order = 2 },
            ],
        };

        return model;
    }

    private static PlotboekModel.PlotboekPaginaInfo PaginaWith(LocatieEncounterData locatie) => new ()
    {
        Id = PlotboekPaginaId.Next(),
        Title = "Test",
        Dag = Dag.None,
        StarttijdId = StarttijdIds.None,
        Order = 1,
        LocatieEncounter = locatie,
    };

    [Test]
    public void BuildPaginaData_stores_the_picked_questlocatie()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "Ruine";
        model.EditableLocatieEncounterSpecificatie = "oude vrije tekst";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().Be("Ruine");
        result.LocatieEncounter.Specificatie.Should().BeNull("the picked location replaces the free text");
    }

    [Test]
    public void BuildPaginaData_treats_the_Anders_sentinel_as_free_text()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = PlotboekModel.QuestLocatieAndersSentinel;
        model.EditableLocatieEncounterSpecificatie = "Achter de schuur";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().BeNull();
        result.LocatieEncounter.Specificatie.Should().Be("Achter de schuur");
    }

    [Test]
    public void BuildPaginaData_drops_a_locatie_id_that_no_longer_resolves()
    {
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "VerwijderdeLocatie";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().BeNull("a dangling id would render as a raw key everywhere");
    }

    [Test]
    public void BuildPaginaData_drops_a_questlocatie_left_over_from_another_encounter_type()
    {
        // The picker keeps its value when the user switches encounter type, so the posted id can
        // outlive the Questveld it was chosen for. Everything that reads the choice back — the save
        // path and the elevator-pitch context — leans on this normalization.
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.EditableLocatieEncounter = LocatieEncounter.Groeperingsveld;
        model.EditableQuestLocatieId = "Ruine";
        model.EditableLocatieEncounterSpecificatie = "Achter de schuur";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().BeNull("a questlocatie only means something for a Questveld");
        result.LocatieEncounter.Specificatie.Should().Be("Achter de schuur");
    }

    [Test]
    public void BuildPaginaData_drops_a_deactivated_locatie_that_was_not_already_chosen()
    {
        // The picker hides retired locations, but that is client-side only: a hand-crafted POST must
        // not be able to pick one up again.
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.SelectedPlotboekPagina = PaginaWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Ruine"));
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "OudeRuine";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().BeNull("the admin retired this location from new use");
    }

    [Test]
    public void BuildPaginaData_keeps_a_deactivated_locatie_that_is_already_the_paginas_choice()
    {
        // Mirror of GroupByCategorie's includeInactiveId: retiring a location must not silently move
        // an existing plot somewhere else.
        var model = CreateModel();
        model.EditableTitle = "Test";
        model.SelectedPlotboekPagina = PaginaWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "OudeRuine"));
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "OudeRuine";

        var result = model.BuildPaginaData();

        result.LocatieEncounter.LocatieId.Should().Be("OudeRuine");
    }

    [Test]
    public void Init_reads_back_what_Build_wrote()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = PaginaWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Ruine"));

        model.InitializePaginaEditableFields();

        model.EditableQuestLocatieId.Should().Be(
            "Ruine",
            "otherwise a concurrency reload silently drops the chosen questlocatie");
    }

    [Test]
    public void Init_falls_back_to_the_Anders_sentinel_for_a_free_text_questveld()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = PaginaWith(LocatieEncounterData.Create(LocatieEncounter.Questveld, "Achter de schuur"));

        model.InitializePaginaEditableFields();

        model.EditableQuestLocatieId.Should().Be(PlotboekModel.QuestLocatieAndersSentinel);
        model.EditableLocatieEncounterSpecificatie.Should().Be("Achter de schuur");
    }

    [Test]
    public void Init_falls_back_to_the_Anders_sentinel_when_there_is_no_pagina()
    {
        var model = CreateModel();
        model.SelectedPlotboekPagina = null;

        model.InitializePaginaEditableFields();

        model.EditableQuestLocatieId.Should().Be(PlotboekModel.QuestLocatieAndersSentinel);
    }

    [Test]
    public void Display_resolves_the_locatie_id_to_its_current_name()
    {
        var model = CreateModel();

        var display = model.GetLocatieEncounterDisplay(LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Ruine"));

        display.Should().Be("Questveld (Ruïne)");
    }

    [Test]
    public void Display_keeps_the_free_text_rendering_for_the_Anders_branch()
    {
        var model = CreateModel();

        var display = model.GetLocatieEncounterDisplay(LocatieEncounterData.Create(LocatieEncounter.Questveld, "Achter de schuur"));

        display.Should().Be("Questveld (Achter de schuur)");
    }

    [Test]
    public void Notitie_combines_the_category_and_location_notes()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "Ruine";

        var notitie = model.CurrentQuestLocatieNotitie;

        notitie.Text.Should().Be("Controleer op kampeerders. Alleen als het ruïneveld leeg is.");
        notitie.CssClass.Should().Be("alert-info");
    }

    [Test]
    public void Notitie_uses_a_warning_for_a_discouraged_category()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableQuestLocatieId = "Tommesbos";

        model.CurrentQuestLocatieNotitie.CssClass.Should().Be("alert-warning");
    }

    [Test]
    public void Notitie_is_empty_when_the_locatie_is_not_a_questveld()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Hoofdveld;
        model.EditableQuestLocatieId = "Ruine";

        model.CurrentQuestLocatieNotitie.HasValue.Should().BeFalse();
    }
}
