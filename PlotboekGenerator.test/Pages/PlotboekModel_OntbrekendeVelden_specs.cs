namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_OntbrekendeVelden_specs
{
    [Test]
    public void A_fully_filled_in_pagina_misses_nothing()
    {
        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(CompletePagina(), isEvergreen: false);

        ontbrekend.IsCompleet.Should().BeTrue(because: "every required field is filled in");
        ontbrekend.Alle.Should().BeEmpty();
    }

    [TestCase("Title", "Naam Encounter")]
    [TestCase("DomainId", "Domein")]
    public void Lists_a_missing_title_field(string veld, string verwacht)
    {
        var pagina = veld switch
        {
            "Title" => CompletePagina() with { Title = string.Empty },
            _ => CompletePagina() with { DomainId = Guid.Empty },
        };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Titel.Should().Equal([verwacht]);
        ontbrekend.IsCompleet.Should().BeFalse();
    }

    [TestCase("VerantwoordelijkeSlName", "Verantwoordelijke SL")]
    [TestCase("StarttijdId", "Dagdeel")]
    [TestCase("AantalBlokkenId", "Geschatte Tijdsduur")]
    public void Lists_a_missing_planning_field(string veld, string verwacht)
    {
        var pagina = veld switch
        {
            "VerantwoordelijkeSlName" => CompletePagina() with { VerantwoordelijkeSlName = string.Empty },
            "StarttijdId" => CompletePagina() with { StarttijdId = StarttijdIds.None },
            _ => CompletePagina() with { AantalBlokkenId = AantalBlokkenIds.None },
        };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Planning.Should().Equal([verwacht]);
    }

    [Test]
    public void Dagdeel_is_not_required_in_evergreen()
    {
        var pagina = CompletePagina() with { StarttijdId = StarttijdIds.None };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: true);

        ontbrekend.IsCompleet.Should().BeTrue(because: "Evergreen plots have no dagdeel");
    }

    [Test]
    public void Dagdeel_is_not_required_for_a_speler_gedreven_plot()
    {
        var pagina = CompletePagina() with { StarttijdId = StarttijdIds.None, IsSpelerGedreven = true };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.IsCompleet.Should().BeTrue(because: "a player-driven plot has no fixed slot");
    }

    [TestCase("EncounterTypeIds", "Soort Encounter")]
    [TestCase("LocatieEncounter", "Locatie Encounter")]
    public void Lists_a_missing_encounter_field(string veld, string verwacht)
    {
        var pagina = veld switch
        {
            "EncounterTypeIds" => CompletePagina() with { EncounterTypeIds = EncounterTypeIds.Empty },
            _ => CompletePagina() with { LocatieEncounter = LocatieEncounterData.Empty },
        };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Encounter.Should().Equal([verwacht]);
    }

    [Test]
    public void Lists_missing_npcs_and_their_clothing_when_no_npc_info_is_given()
    {
        var pagina = CompletePagina() with { AantalNpcs = AantalNpcsData.Empty };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Npc.Should().Equal(["Aantal NPCs", "Kleding & Schmink"]);
    }

    [Test]
    public void Lists_missing_clothing_when_no_role_describes_it()
    {
        var pagina = CompletePagina() with { AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("stadswacht", 2)]) };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Npc.Should().Equal(["Kleding & Schmink"]);
    }

    [Test]
    public void Clothing_is_not_required_when_no_npcs_are_needed()
    {
        var pagina = CompletePagina() with { AantalNpcs = AantalNpcsData.Create([], geenNpcsNodig: true) };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.IsCompleet.Should().BeTrue(because: "'Geen NPC's nodig' counts as complete NPC info");
    }

    [TestCase("ElevatorPitch", "Elevator Pitch")]
    [TestCase("SamenvattingPlot", "Samenvatting Plot")]
    public void Lists_a_missing_plot_field(string veld, string verwacht)
    {
        var pagina = veld switch
        {
            "ElevatorPitch" => CompletePagina() with { ElevatorPitch = string.Empty },
            _ => CompletePagina() with { SamenvattingPlot = string.Empty },
        };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Plot.Should().Equal([verwacht]);
    }

    [Test]
    public void Lists_props_and_loot_an_sl_brings_without_the_sl_picked()
    {
        var zonderSl = PropLootItemData.Create("kist met goud");
        var pagina = CompletePagina() with { PropsEnLoot = PropsEnLootData.Create([zonderSl], [zonderSl]) };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.PropsEnLoot.Should().Equal(["Verantwoordelijke SL bij prop(s)", "Verantwoordelijke SL bij loot"]);
    }

    [Test]
    public void Props_brought_by_someone_other_than_an_sl_need_no_sl()
    {
        var doorSpeler = PropLootItemData.Create("kist met goud", verantwoordelijkeSource: "SpelerOfNpc");
        var pagina = CompletePagina() with { PropsEnLoot = PropsEnLootData.Create([doorSpeler], []) };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.IsCompleet.Should().BeTrue();
    }

    [Test]
    public void Alle_lists_every_section_in_detail_panel_order()
    {
        var pagina = CompletePagina() with { Title = string.Empty, SamenvattingPlot = string.Empty, AantalBlokkenId = AantalBlokkenIds.None };

        var ontbrekend = PlotboekModel.BepaalOntbrekendeVelden(pagina, isEvergreen: false);

        ontbrekend.Alle.Should().Equal(["Naam Encounter", "Geschatte Tijdsduur", "Samenvatting Plot"]);
    }

    [Test]
    public void Only_the_tick_is_left_on_a_complete_pagina_that_is_not_klaar()
    {
        PlotboekModel.IsAlleenVinkjeNog(CompletePagina(), isEvergreen: false)
            .Should().BeTrue(because: "every field is filled in and the switch is still off");
    }

    [Test]
    public void Not_only_the_tick_left_once_the_pagina_is_klaar()
    {
        var pagina = CompletePagina() with { IsVolledigKlaar = true };

        PlotboekModel.IsAlleenVinkjeNog(pagina, isEvergreen: false).Should().BeFalse();
    }

    [Test]
    public void Not_only_the_tick_left_once_the_pagina_is_gedraaid()
    {
        var pagina = CompletePagina() with { IsGedraaid = true };

        PlotboekModel.IsAlleenVinkjeNog(pagina, isEvergreen: true).Should().BeFalse();
    }

    [Test]
    public void Not_only_the_tick_left_while_a_required_field_is_missing()
    {
        var pagina = CompletePagina() with { ElevatorPitch = string.Empty };

        PlotboekModel.IsAlleenVinkjeNog(pagina, isEvergreen: false).Should().BeFalse();
    }

    [Test]
    public void Not_only_the_tick_left_while_feedback_is_open()
    {
        var pagina = CompletePagina() with { Feedback = [Feedback(FeedbackStatus.Open)] };

        PlotboekModel.IsAlleenVinkjeNog(pagina, isEvergreen: false)
            .Should().BeFalse(because: "open feedback blocks the tick, so a reminder to tick it is noise");
    }

    [Test]
    public void Answered_feedback_does_not_block_the_reminder()
    {
        var pagina = CompletePagina() with { Feedback = [Feedback(FeedbackStatus.Beantwoord)] };

        PlotboekModel.IsAlleenVinkjeNog(pagina, isEvergreen: false)
            .Should().BeTrue(because: "answered feedback no longer blocks 'volledig klaar'");
    }

    private static FeedbackData Feedback(FeedbackStatus status) => new ()
    {
        Id = FeedbackId.Next(),
        Tekst = "De eindbaas is te sterk.",
        Status = status,
    };

    private static PlotboekModel.PlotboekPaginaInfo CompletePagina() => new ()
    {
        Id = PlotboekPaginaId.Next(),
        Title = "De val van de toren",
        DomainId = Guid.NewGuid(),
        Dag = Dag.Zaterdag,
        StarttijdId = StarttijdIds.Tijd2000,
        Order = 1,
        VerantwoordelijkeSlId = "user-1",
        VerantwoordelijkeSlName = "Anna",
        AantalBlokkenId = AantalBlokkenIds.EenBlok,
        EncounterTypeIds = EncounterTypeIds.Create(["Vechten"]),
        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld),
        AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("stadswacht", 2, kledingEnSchmink: "tabbaard, helm")]),
        ElevatorPitch = "De toren valt.",
        SamenvattingPlot = "<p>De spelers moeten de toren verdedigen.</p>",
        PropsEnLoot = PropsEnLootData.Empty,
    };
}
